using RideMatchingSystem.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RideMatchingSystem.Services;
using RideMatchingSystem.BackgroundServices;
using RideMatchingSystem.Hubs;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.IO;
using System.Linq;
using Swashbuckle.AspNetCore.SwaggerUI;
using Swashbuckle.AspNetCore.Annotations;

var builder = WebApplication.CreateBuilder(args);

// Ensure console logging is enabled so startup addresses and errors are visible when running
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add services to the container.
builder.Services.AddControllers();
// OpenAPI/Swagger - include XML comments so controller/action summaries show in the UI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Enable [SwaggerOperation] if used elsewhere
    c.EnableAnnotations();

    // Include XML comments generated from this project's documentation file so
    // the /// <summary> comments on controllers and actions appear in Swagger UI.
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Configure JWT authentication (development/demo defaults if not provided)
var jwtKey = builder.Configuration["Jwt:Key"] ?? "dev-key-please-change";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "RideMatchingSystem";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true
    };
});

// Register EF Core DbContext (SQL Server for local dev)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
// Add memory cache and services
builder.Services.AddMemoryCache();
builder.Services.AddScoped<DriverService>();
builder.Services.AddScoped<RideService>();
builder.Services.AddScoped<RiderService>();
builder.Services.AddScoped<MatchingService>();
builder.Services.AddHostedService<RideMatchingWorker>();
builder.Services.AddSignalR();

var app = builder.Build();

// Log startup info so it's clear the app reached this point when running
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
startupLogger.LogInformation("RideMatchingSystem: application built and about to start.");

// Hook lifetime and global exception events to capture unexpected shutdowns
var lifetime = app.Lifetime;
lifetime.ApplicationStarted.Register(() => startupLogger.LogInformation("RideMatchingSystem: ApplicationStarted."));
lifetime.ApplicationStopping.Register(() => startupLogger.LogInformation("RideMatchingSystem: ApplicationStopping."));
lifetime.ApplicationStopped.Register(() => startupLogger.LogInformation("RideMatchingSystem: ApplicationStopped."));

AppDomain.CurrentDomain.UnhandledException += (s, e) =>
{
    try
    {
        startupLogger.LogCritical(e.ExceptionObject as Exception, "Unhandled exception in AppDomain");
    }
    catch { }
};

TaskScheduler.UnobservedTaskException += (s, e) =>
{
    try
    {
        startupLogger.LogCritical(e.Exception, "Unobserved task exception");
        e.SetObserved();
    }
    catch { }
};

// Configure the HTTP request pipeline.
// Enable Swagger UI in all environments for easier testing
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "RideMatchingSystem v1");
    c.RoutePrefix = "swagger"; // UI served at /swagger
});

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<RideHub>("/rideHub");
app.MapHub<RiderHub>("/riderHub");
app.MapHub<DriverHub>("/driverHub");

// Log the URL where Swagger UI should be reachable
try
{
    var firstUrl = app.Urls.FirstOrDefault() ?? "http://localhost:5299";
    startupLogger.LogInformation("Swagger UI should be available at: {Url}/swagger", firstUrl.TrimEnd('/'));
}
catch { }

// Ensure database is created for demo purposes (log failures instead of crashing)
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
        logger.LogInformation("Ensured database is created.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to ensure database is created. The app will continue, but DB operations may fail.");
    }
}

app.Run();
