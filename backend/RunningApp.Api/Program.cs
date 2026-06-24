using Microsoft.EntityFrameworkCore;
using RunningApp.Application.Adaptation;
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
});

// ─── Database ───────────────────────────────────────────────────────────────
// TODO (Step 2): Ensure PostgreSQL connection string is in appsettings.Development.json
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.MigrationsAssembly("RunningApp.Persistence")
    ));

// ─── Adaptation Engine (Placeholder) ────────────────────────────────────────
// Replace PlaceholderAdaptationEngine with real engine in a future phase
builder.Services.AddSingleton<IAdaptationEngine, PlaceholderAdaptationEngine>();

// [ignoring loop detection]
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

// ─── CORS (dev-friendly) ────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// ─── Middleware ──────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();
