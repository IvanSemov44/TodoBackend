using Microsoft.EntityFrameworkCore;
using TodoBackend.Data;
using TodoBackend.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register custom services
builder.Services.AddSingleton<DatabaseConfigService>();

// Configure Datase
builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    var configService = serviceProvider.GetRequiredService<DatabaseConfigService>();
    var connectionString = configService.GetConnectionString();
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
    });
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

// In production, Render handles SSL termination
if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

// Map endpoints
app.MapControllers();
app.MapHealthChecks("/health");

// Log database configuration
var configService = app.Services.GetService<DatabaseConfigService>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();
configService.LogConfiguraion(logger);

// Run Database migrations
await DatabaseMigrationService.MigrateDatabaseAsync(app);

logger.LogInformation("Application starting...");
app.Run();