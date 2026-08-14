using System.Net;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using RunningApp.Api.Auth;
using RunningApp.Api.ErrorHandling;
using RunningApp.Api.Logging;
using RunningApp.Api.Startup;
using RunningApp.Api.Swagger;
using RunningApp.Application.Adaptation;
using RunningApp.Application.Identity;
using RunningApp.Application.PlanGeneration;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.Services;
using RunningApp.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Phase 4L.6A: resolve the catalog against the stable application content
// root, validate the complete packaged authoring tree before the API begins
// accepting traffic, and never fall back through the process working directory.
var catalogRoot = PlanCatalogRootResolver.Resolve(
    builder.Configuration[$"{PlanCatalogOptions.SectionName}:CatalogRootPath"],
    builder.Environment.ContentRootPath,
    builder.Environment.IsDevelopment());
var catalogInventory = PlanCatalogPackageValidator.Validate(catalogRoot.CatalogRootPath);

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
// Phase 4L.6B: appsettings.json (the Production-reachable base layer) no
// longer carries a usable connection string. Development's local Postgres
// default lives only in appsettings.Development.json; Production must supply
// ConnectionStrings:DefaultConnection through external configuration (an
// environment variable, secret manager, or deployment-platform secret).
// ProductionConfigurationValidator below fails startup if it is missing or
// still resolves to a known local/development authority.
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
builder.Services.Configure<PlanCatalogOptions>(options => options.CatalogRootPath = catalogRoot.CatalogRootPath);
builder.Services.Configure<RunningApp.Application.RuntimeCatalog.PreviewRouting.CatalogLivePilotOptions>(
    builder.Configuration.GetSection(RunningApp.Application.RuntimeCatalog.PreviewRouting.CatalogLivePilotOptions.SectionName));
// Backend Integration Phase 4F.9.3: Development-only local-acceptance seam.
// Disabled by default; see LocalCatalogAcceptanceOptions's own doc comment.
builder.Services.Configure<RunningApp.Application.RuntimeCatalog.PreviewRouting.LocalCatalogAcceptanceOptions>(
    builder.Configuration.GetSection(RunningApp.Application.RuntimeCatalog.PreviewRouting.LocalCatalogAcceptanceOptions.SectionName));
// Backend Integration Phase 4G.6B.1: candidate-scoped activation gate for the
// 15-20 week TEN_K Preparation Runway public preview path (Phase 4G.6B).
// Defaults enabled; see PreparationRunwayPilotActivationOptions's own doc
// comment for exact scope and disabled-rollback behavior.
builder.Services.Configure<RunningApp.Application.RuntimeCatalog.PreviewRouting.PreparationRunwayPilotActivationOptions>(
    builder.Configuration.GetSection(RunningApp.Application.RuntimeCatalog.PreviewRouting.PreparationRunwayPilotActivationOptions.SectionName));
// Phase 4L.6C: server-authoritative, generation-only kill switch for the
// dedicated Long-Horizon public preview endpoint. Defaults enabled (current
// behavior unchanged); no client request can read or set this value. See
// LongHorizonGenerationOptions's own doc comment for exact scope.
builder.Services.Configure<RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.PublicPreview.LongHorizonGenerationOptions>(
    builder.Configuration.GetSection(RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.PublicPreview.LongHorizonGenerationOptions.SectionName));
builder.Services.AddScoped<IPlanCatalogBundleLoader, PlanCatalogBundleLoader>();
builder.Services.AddScoped<ICanonicalDistanceFamilyResolver, CanonicalDistanceFamilyResolver>();

// Backend Integration Phase 4F.6A: same PlanCatalog:CatalogRootPath configuration as
// IPlanCatalogBundleLoader above, but reads a WORKOUT_PROGRESSION document's fine-grained
// stage content, which PlanCatalogCandidateSummary deliberately does not carry. Consumed by
// CatalogPreviewGenerator's public constructor (widened by one genuine parameter — see that
// class's own doc comment for why this dependency could not be composed internally like its
// other dark-pipeline collaborators).
builder.Services.AddScoped<RunningApp.Application.RuntimeCatalog.Schedule.Progression.ICatalogWorkoutProgressionLoader,
    RunningApp.Application.RuntimeCatalog.Schedule.Progression.CatalogWorkoutProgressionLoader>();

// Backend Integration Phase 4F.6B: same PlanCatalog:CatalogRootPath configuration again, but
// reads a WORKOUT_DEFINITION document's family/eligible-phases/prescription-modes/status —
// needed to validate an exact workout-definition binding. Consumed by
// CatalogPreviewGenerator's public constructor (widened by a second genuine parameter, for
// the same reason as ICatalogWorkoutProgressionLoader above).
builder.Services.AddScoped<RunningApp.Application.RuntimeCatalog.Schedule.Binding.ICatalogWorkoutDefinitionLoader,
    RunningApp.Application.RuntimeCatalog.Schedule.Binding.CatalogWorkoutDefinitionLoader>();

builder.Services.AddScoped<RunningApp.Application.RuntimeCatalog.Prescription.Volume.ICatalogPeakVolumeBandLoader,
    RunningApp.Application.RuntimeCatalog.Prescription.Volume.CatalogPeakVolumeBandLoader>();

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

// Backend Integration Phase 4E.1 / Phase 4F.8.2: catalog preview routing + immutable
// resolution snapshot foundation. Route decision happens once per request
// (IGenerationRouteDecider). Phase 4F.8.2 adds an explicit default-disabled
// CatalogLivePilot activation control and a PUBLISHED-only lifecycle boundary
// before a real user can be routed to catalog generation. Candidate/dependency
// eligibility remains enforced again by ICatalogCandidateEligibilityGate.
builder.Services.AddScoped<RunningApp.Application.RuntimeCatalog.PreviewRouting.IGenerationRouteDecider,
    RunningApp.Application.RuntimeCatalog.PreviewRouting.LivePlanPreviewRoutingService>();
builder.Services.AddScoped<RunningApp.Application.RuntimeCatalog.PreviewRouting.ICatalogCandidateEligibilityGate,
    RunningApp.Application.RuntimeCatalog.PreviewRouting.CatalogCandidateEligibilityGate>();

// Backend Integration Phase 4F.4: CatalogPreviewGenerator now also dark-invokes
// the Phase 4F.3 internal skeleton orchestrator (ICatalogPlanSkeletonOrchestrator)
// after eligibility/resolver success (see CatalogPreviewGenerator.BuildDarkInternalSkeleton).
// That type and its resolver/factory/materializer/validator collaborators remain
// deliberately `internal` to RunningApp.Application (Phase 4F.3's own boundary) --
// no explicit DI registration is added here, since doing so would require making
// them public and RunningApp.Api has no InternalsVisibleTo grant. Instead,
// CatalogPreviewGenerator's public constructor composes a default orchestrator
// internally (a pure, stateless, dependency-free composition, safe to build once
// per Scoped CatalogPreviewGenerator instance) — see that class's own doc comment.
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
builder.Services.AddScoped<ILongHorizonPublicPlanService,
    RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.PublicPreview.LongHorizonPublicPlanService>();
builder.Services.AddScoped<IPlanManagementService, PlanServices>();
builder.Services.AddScoped<IHomeQueryService, QueryAndMutationServices>();
builder.Services.AddScoped<ICalendarQueryService, QueryAndMutationServices>();
builder.Services.AddScoped<ITrainingDayService, QueryAndMutationServices>();
builder.Services.AddScoped<IWorkoutCompletionService, QueryAndMutationServices>();
builder.Services.AddScoped<INotTodayService, QueryAndMutationServices>();
builder.Services.AddScoped<ILongHorizonActiveReadModelProvider, LongHorizonActiveReadModelProvider>();
builder.Services.AddScoped<ILongHorizonRollingSessionMutationService, LongHorizonRollingSessionMutationService>();
builder.Services.AddScoped<ILongHorizonRollingWindowActivationService, LongHorizonRollingWindowActivationService>();
builder.Services.AddScoped<ILongHorizonRollingRetryContinuationService, LongHorizonRollingRetryContinuationService>();
builder.Services.AddScoped<IPendingConfirmationService, QueryAndMutationServices>();
builder.Services.AddScoped<IProfileService, QueryAndMutationServices>();
builder.Services.AddScoped<IUserSynchronizationService, UserSynchronizationService>();

// ─── CORS ───────────────────────────────────────────────────────────────────
// The mobile app is a native Flutter client; browser CORS enforcement never
// applies to it, so no Production browser origin needs to be invented here.
// Development keeps the permissive dev-only policy (Swagger, local tooling).
// Production has no configured origin by default, which is a fully
// restrictive policy (WithOrigins is never called, so the CORS middleware
// rejects every cross-origin browser request) — see Cors:AllowedOrigins if a
// future web client needs one added explicitly.
builder.Services.AddCors(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.AddDefaultPolicy(policy =>
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
    }
    else
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? Array.Empty<string>();
        options.AddDefaultPolicy(policy =>
        {
            if (allowedOrigins.Length > 0)
                policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader();
            // else: policy allows nothing (no WithOrigins call) — restrictive
            // default until an explicit browser client is approved.
        });
    }
});

var app = builder.Build();

// ─── Production configuration fail-fast ────────────────────────────────────
// Runs after DI/options are built but before the host accepts any traffic.
// ProductionConfigurationValidation:Enabled defaults to true; it exists only
// so tests that boot the real host under the "Production" environment name
// to exercise catalog-tier Production-only behavior (not full production
// security posture — see ProductionConfigurationValidatorTests for that) can
// opt out explicitly instead of silently bypassing a real gate.
var productionValidationEnabled = builder.Configuration
    .GetSection(ProductionConfigurationValidationOptions.SectionName)
    .Get<ProductionConfigurationValidationOptions>()?.Enabled ?? true;
if (app.Environment.IsProduction() && productionValidationEnabled)
{
    ProductionConfigurationValidator.Validate(builder.Configuration);
}

app.Logger.LogInformation(
    "Runtime plan catalog validated. Source={CatalogRootSource} Root={CatalogRoot} JsonFiles={CatalogFileCount}",
    catalogRoot.Source,
    catalogRoot.CatalogRootPath,
    catalogInventory.JsonFileCount);

// ─── Reverse proxy / HTTPS authority ────────────────────────────────────────
// Two supported deployment models, chosen by configuration — never assumed:
//   A. This process terminates/enforces HTTPS directly: set
//      Deployment:EnforceHttpsRedirection=true. No forwarded-header trust is
//      needed or enabled.
//   B. A trusted reverse proxy/load balancer terminates TLS and forwards
//      X-Forwarded-For/X-Forwarded-Proto: set Deployment:TrustedProxies
//      and/or Deployment:TrustedNetworks to the exact proxy IPs/CIDRs. Only
//      forwarded headers from those addresses are trusted — arbitrary public
//      clients can never spoof scheme/IP this way. EnforceHttpsRedirection
//      stays false; the proxy is the HTTPS authority.
// Outside Development, if neither is configured, the app trusts no forwarded
// headers and enforces no redirection — HTTP exposure is not silently
// accepted as "probably behind a proxy somewhere."
if (!app.Environment.IsDevelopment())
{
    var trustedProxies = builder.Configuration.GetSection("Deployment:TrustedProxies").Get<string[]>()
        ?? Array.Empty<string>();
    var trustedNetworks = builder.Configuration.GetSection("Deployment:TrustedNetworks").Get<string[]>()
        ?? Array.Empty<string>();

    if (trustedProxies.Length > 0 || trustedNetworks.Length > 0)
    {
        var forwardedHeadersOptions = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
        };
        forwardedHeadersOptions.KnownProxies.Clear();
        forwardedHeadersOptions.KnownNetworks.Clear();
        foreach (var proxy in trustedProxies)
            forwardedHeadersOptions.KnownProxies.Add(IPAddress.Parse(proxy));
        foreach (var network in trustedNetworks)
        {
            var parts = network.Split('/');
            forwardedHeadersOptions.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse(parts[0]), int.Parse(parts[1])));
        }
        app.UseForwardedHeaders(forwardedHeadersOptions);
    }

    if (builder.Configuration.GetValue<bool>("Deployment:EnforceHttpsRedirection"))
    {
        app.UseHsts();
        app.UseHttpsRedirection();
    }
}

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
