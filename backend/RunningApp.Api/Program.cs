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
