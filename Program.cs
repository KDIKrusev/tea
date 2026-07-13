using KSailCalc.Api.Models;
using KSailCalc.Api.Repositories.Interfaces;
using KSailCalc.Api.Repositories;
using KSailCalc.Api.Services;
using KSailCalc.Api.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Bind configurable business constants from appsettings.json
builder.Services.Configure<CalculatorSettings>(builder.Configuration.GetSection("CalculatorSettings"));

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// REPOSITORIES:
// Singleton: caches config data in-memory, thread-safe via SemaphoreSlim, connections created per-call
builder.Services.AddSingleton<IKSailCalcConfigRepository, HybridConfigRepository>();
// Singleton: caches sail contribution lookup table, thread-safe via SemaphoreSlim
builder.Services.AddSingleton<ISailContributionRepository, SailContributionRepository>();

// Application-level cache for AppDataAggregationService (24h TTL)
builder.Services.AddMemoryCache();

// SERVICES: Scoped — one instance per HTTP request
builder.Services.AddScoped<ISfocService, SfocService>();
builder.Services.AddScoped<ISailContributionService, SailContributionService>();
builder.Services.AddScoped<ILevel1OptimizationService, Level1OptimizationService>();
builder.Services.AddScoped<ILevel2OptimizationService, Level2OptimizationService>();
builder.Services.AddScoped<ILevel3DrcService, Level3DrcService>();
builder.Services.AddScoped<ICalculatorService, CalculatorService>();
builder.Services.AddScoped<IAppDataAggregationService, AppDataAggregationService>();
builder.Services.AddScoped<IVesselResolutionService, VesselResolutionService>();
builder.Services.AddScoped<IValidationService, ValidationService>();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "KSailCalc API", 
        Version = "v1",
        Description = "API for iEMS Savings Calculator"
    });
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins(
            "http://localhost:4200",
            "http://localhost:56709",
            "http://localhost:80",
            "http://localhost:8080",
            "http://localhost",
            "https://energysavingscalculator-fe-fwe8cybrhpfmcjdr.westeurope-01.azurewebsites.net"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAngularApp");

// In Azure, HTTPS termination is handled by the gateway, not the app
// So HTTPS redirection is only needed in Development
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();
app.MapControllers();

app.Run();
