using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using RunningApp.Api.Auth;
using RunningApp.Api.ErrorHandling;
using RunningApp.Api.Logging;
using RunningApp.Api.Swagger;
using RunningApp.Application.Adaptation;
using RunningApp.Application.Identity;
using RunningApp.Application.PlanGeneration;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.Services;
using RunningApp.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ─── Controllers ────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.SnakeCaseLower));
    });

// ─── Swagger / OpenAPI ──────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Antigravity Running API", Version = "v1" });
    c.SchemaFilter<DtoExamplesSchemaFilter>();
});

// ─── Database ───────────────────────────────────────────────────────────────
// TODO (Step 2): Ensure PostgreSQL connection string is in appsettings.Development.json
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.MigrationsAssembly("RunningApp.Persistence")
    ));

// ─── Error handling ─────────────────────────────────────────────────────────
// AddProblemDetails() is required by UseExceptionHandler() as a startup
// validation fallback. GlobalExceptionHandler always handles the exception
// itself (writes ApiErrorResponse), so the ProblemDetails service never
// actually produces a response in practice.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// ─── Auth abstraction ───────────────────────────────────────────────────────
// Auth:Provider controls which identity provider is active.
//   "Firebase"  (default / production) — FirebaseAuthMiddleware validates Bearer
//               tokens; FirebaseIdentityProvider reads the verified identity.
//   "Mock"      (development only)     — MockIdentityProvider resolves every
//               request to mock-user-001; no token header required.
//
// appsettings.json              → "Firebase"  (production default)
// appsettings.Development.json  → "Mock"      (local dev without credentials)
//
// To test Firebase auth locally: override Auth:Provider to "Firebase" via user
// secrets and set GOOGLE_APPLICATION_CREDENTIALS to the service-account JSON path.
var authProvider = builder.Configuration["Auth:Provider"] ?? "Firebase";
var useMockAuth  = string.Equals(authProvider, "Mock", StringComparison.OrdinalIgnoreCase);

if (useMockAuth)
{
    // Mock path: MockAuthMiddleware calls SynchronizeAsync to upsert the mock
    // user in the DB, then stores an AuthenticatedIdentity (with the real
    // InternalUserId from the Users table) in HttpContext.Items under the same
    // key used by FirebaseAuthMiddleware. FirebaseIdentityProvider reads from
    // that key, so it works identically in both auth modes.
    //
    // Do NOT use MockIdentityProvider here — it returns a static identity with
    // InternalUserId = Guid.Empty which causes FK violations on dependent tables.
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<IIdentityProvider, FirebaseIdentityProvider>();
    builder.Services.AddScoped<ICurrentUserAccessor, MockCurrentUserAccessor>();
}
else
{
    // Firebase Admin SDK reads credentials from the GOOGLE_APPLICATION_CREDENTIALS
    // environment variable. Never put service-account keys in appsettings files.
    var firebaseProjectId = builder.Configuration["Auth:Firebase:ProjectId"]
        ?? throw new InvalidOperationException(
            "Auth:Firebase:ProjectId must be configured when Auth:Provider is Firebase.");

    FirebaseApp.Create(new AppOptions
    {
        Credential = GoogleCredential.GetApplicationDefault(),
        ProjectId  = firebaseProjectId,
    });

    // FirebaseIdentityProvider reads the identity stored by FirebaseAuthMiddleware.
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<IIdentityProvider, FirebaseIdentityProvider>();

    // MockCurrentUserAccessor delegates to IIdentityProvider.GetCurrentIdentity().UserId —
    // the delegation logic is reused unchanged; only the underlying provider differs.
    builder.Services.AddScoped<ICurrentUserAccessor, MockCurrentUserAccessor>();
}

// ─── Adaptation Engine (Placeholder) ────────────────────────────────────────
builder.Services.AddSingleton<IAdaptationEngine, PlaceholderAdaptationEngine>();

// ─── Plan Generation Engine (Placeholder) ───────────────────────────────────
builder.Services.AddScoped<IPlanGenerationEngine, PlaceholderPlanGenerationEngine>();

// ─── Process A plan-catalog read-only reader + distance-family resolver ────
// Backend Integration Phase 1: read-only, NOT wired into plan generation yet.
// See PHASE0_RELEASE_IMPACT_CLARIFICATION.md / this phase's final report for
// the architectural boundary (Process A = catalog authoring, Process B =
// runtime plan assignment/generation, which this phase begins to integrate
// with read-only).
builder.Services.Configure<PlanCatalogOptions>(builder.Configuration.GetSection(PlanCatalogOptions.SectionName));
builder.Services.AddScoped<IPlanCatalogBundleLoader, PlanCatalogBundleLoader>();
builder.Services.AddScoped<ICanonicalDistanceFamilyResolver, CanonicalDistanceFamilyResolver>();

// Backend Integration Phase 2: analysis-only vocabulary mapper. Consumes Phase 1's
// PlanCatalogCandidateSummary and reports backend representation support per concept
// (see PlanCatalogDomainMapper). Never generates TrainingWeeks/TrainingDays, and not
// wired into IPlanGenerationEngine.
builder.Services.AddScoped<IPlanCatalogDomainMapper, PlanCatalogDomainMapper>();

// Backend Integration Phase 4C: read-only registry reader, used to validate a
// future resolver's output value against the real runtime-condition-values
// registry file. Registered because it is a pure, read-only evidence utility
// (same risk profile as IPlanCatalogBundleLoader above) -- NOT because
// anything consumes it yet. No IRuntimeConditionResolver/*Resolver
// implementation is registered here: none exists in production code, and
// none may be invoked from PlanServices/PlaceholderPlanGenerationEngine until
// a future phase (4D+) implements real resolver logic. See
// PHASE4C_RUNTIME_RESOLVER_CONTRACT_AND_DECISION_TRACE_SKELETON.md.
builder.Services.AddScoped<RunningApp.Application.RuntimeCatalog.Resolvers.IRuntimeConditionRegistryReader,
    RunningApp.Application.RuntimeCatalog.Resolvers.RuntimeConditionRegistryReader>();

// Backend Integration Phase 4D.1-4D.4: the four concrete runtime-condition
// resolvers (TIME_ADEQUACY_IN, PACE_SOURCE_IN, CORE_ENTRY_READINESS_IN,
// GOAL_FEASIBILITY_IN). Backend Integration Phase 4D.5 adds the orchestration
// service that runs them in order. ALL of these are registered here as
// INTERNAL services only -- nothing in RunningApp.Api's controllers,
// PlanServices, or PlaceholderPlanGenerationEngine takes any of these types
// as a constructor dependency (verified by the *NotWiredToGenerationTests
// reflection-based regression guards in RunningApp.IntegrationTests). No
// resolver output reaches any live generate-preview/confirm request path.
// See PHASE4D_5_RUNTIME_CONDITION_RESOLVER_ORCHESTRATION_SERVICE.md.
builder.Services.AddScoped<RunningApp.Application.RuntimeCatalog.Resolvers.ITimeAdequacyResolver,
    RunningApp.Application.RuntimeCatalog.Resolvers.TimeAdequacyResolver>();
builder.Services.AddScoped<RunningApp.Application.RuntimeCatalog.Resolvers.IPaceSourceResolver,
    RunningApp.Application.RuntimeCatalog.Resolvers.PaceSourceResolver>();
builder.Services.AddScoped<RunningApp.Application.RuntimeCatalog.Resolvers.ICoreEntryReadinessResolver,
    RunningApp.Application.RuntimeCatalog.Resolvers.CoreEntryReadinessResolver>();
builder.Services.AddScoped<RunningApp.Application.RuntimeCatalog.Resolvers.IGoalFeasibilityResolver,
    RunningApp.Application.RuntimeCatalog.Resolvers.GoalFeasibilityResolver>();
builder.Services.AddScoped<RunningApp.Application.RuntimeCatalog.Resolvers.IRuntimeConditionResolutionService,
    RunningApp.Application.RuntimeCatalog.Resolvers.RuntimeConditionResolutionService>();
// Also registered under its concrete type: CatalogPreviewGenerator (Phase
// 4E.1, below) depends on the concrete class directly because
// ResolveAllResults(...) is defined only on RuntimeConditionResolutionService,
// not on IRuntimeConditionResolutionService.
builder.Services.AddScoped<RunningApp.Application.RuntimeCatalog.Resolvers.RuntimeConditionResolutionService>();

// Backend Integration Phase 4E.1: catalog preview routing + immutable
// resolution snapshot foundation. Route decision happens once per request
// (IGenerationRouteDecider); catalog execution is gated by candidate +
// dependency lifecycle eligibility (ICatalogCandidateEligibilityGate); the
// resolvers registered above are now invoked, but ONLY for the narrowly
// scoped TEN_K/INTERMEDIATE/4D pilot combination, and ONLY during preview
// (never confirm). See
// PHASE4E_1_CATALOG_PREVIEW_ROUTING_AND_IMMUTABLE_RESOLUTION_SNAPSHOT.md.
builder.Services.AddScoped<RunningApp.Application.RuntimeCatalog.PreviewRouting.IGenerationRouteDecider,
    RunningApp.Application.RuntimeCatalog.PreviewRouting.PilotGenerationRouteDecider>();
builder.Services.AddScoped<RunningApp.Application.RuntimeCatalog.PreviewRouting.ICatalogCandidateEligibilityGate,
    RunningApp.Application.RuntimeCatalog.PreviewRouting.CatalogCandidateEligibilityGate>();
builder.Services.AddScoped<RunningApp.Application.RuntimeCatalog.PreviewRouting.ICatalogPreviewGenerator,
    RunningApp.Application.RuntimeCatalog.PreviewRouting.CatalogPreviewGenerator>();

// Backend Integration Phase 4F.1: strongly typed, immutable generated-schedule
// contract + its pure structural validator. No database/clock/catalog-loader/
// resolver/route-decider dependency of its own. Not yet consumed by anything
// that produces real (non-null) schedules -- see
// PHASE4F_1_PERSISTABLE_CATALOG_SCHEDULE_CONTRACT.md.
builder.Services.AddScoped<RunningApp.Application.RuntimeCatalog.Schedule.IGeneratedCatalogPlanPayloadValidator,
    RunningApp.Application.RuntimeCatalog.Schedule.GeneratedCatalogPlanPayloadValidator>();

// Backend Integration Phase 4E.2: catalog preview confirmation service.
// This service depends ONLY on AppDbContext, ILogger, and (Phase 4F.1)
// IGeneratedCatalogPlanPayloadValidator — it does NOT depend on
// IGenerationRouteDecider, ICatalogCandidateEligibilityGate,
// ICatalogPreviewGenerator, any resolver, or StageEligibilityEvaluator.
// It reads only the already-stored CatalogPreviewSnapshot; no catalog files
// are re-loaded at confirm time.
builder.Services.AddScoped<RunningApp.Application.RuntimeCatalog.PreviewRouting.ICatalogPlanConfirmationService,
    RunningApp.Application.RuntimeCatalog.PreviewRouting.CatalogPlanConfirmationService>();


// ─── Application Services ───────────────────────────────────────────────────
builder.Services.AddScoped<IBootstrapService, BootstrapService>();
builder.Services.AddScoped<IPlanPreviewService, PlanServices>();
builder.Services.AddScoped<IPlanConfirmationService, PlanServices>();
builder.Services.AddScoped<IPlanManagementService, PlanServices>();
builder.Services.AddScoped<IHomeQueryService, QueryAndMutationServices>();
builder.Services.AddScoped<ICalendarQueryService, QueryAndMutationServices>();
builder.Services.AddScoped<ITrainingDayService, QueryAndMutationServices>();
builder.Services.AddScoped<IWorkoutCompletionService, QueryAndMutationServices>();
builder.Services.AddScoped<INotTodayService, QueryAndMutationServices>();
builder.Services.AddScoped<IPendingConfirmationService, QueryAndMutationServices>();
builder.Services.AddScoped<IProfileService, QueryAndMutationServices>();
builder.Services.AddScoped<IUserSynchronizationService, UserSynchronizationService>();

// ─── CORS (dev-friendly) ────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// ─── Middleware ──────────────────────────────────────────────────────────────
// Order matters:
//   1. RequestLogging wraps everything so the final status code is always logged.
//   2. ExceptionHandler converts unhandled exceptions → standard JSON error shape.
//   3. FirebaseAuthMiddleware (Firebase mode only) validates the Bearer token and
//      stores the resolved identity before any controller runs.
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

if (useMockAuth)
    app.UseMiddleware<MockAuthMiddleware>();
else
    app.UseMiddleware<FirebaseAuthMiddleware>();

app.UseAuthorization();
app.MapControllers();

app.Run();

// Reopens the implicit top-level-statement Program class as public so
// RunningApp.IntegrationTests can use it as WebApplicationFactory<Program>.
public partial class Program { }
