using Microsoft.EntityFrameworkCore;
using TodoBackend.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

// **FIX: Debug - Log all environment variables**
Console.WriteLine("=== ENVIRONMENT VARIABLES ===");
Console.WriteLine($"DB_HOST: '{Environment.GetEnvironmentVariable("DB_HOST")}'");
Console.WriteLine($"DB_NAME: '{Environment.GetEnvironmentVariable("DB_NAME")}'");
Console.WriteLine($"DB_USER: '{Environment.GetEnvironmentVariable("DB_USER")}'");
Console.WriteLine($"DB_PASSWORD: '{(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DB_PASSWORD")) ? "NOT SET" : "SET")}'");
Console.WriteLine($"DB_PORT: '{Environment.GetEnvironmentVariable("DB_PORT")}'");
Console.WriteLine($"RENDER: '{Environment.GetEnvironmentVariable("RENDER")}'");
Console.WriteLine("==============================");

// **FIX: Configure PostgreSQL for Render**
string connectionString;

// Check if we're in Render environment by looking for Render-specific env vars
var renderDbHost = Environment.GetEnvironmentVariable("DB_HOST");
var renderExternalUrl = Environment.GetEnvironmentVariable("EXTERNAL_DB_URL");
var renderInternalUrl = Environment.GetEnvironmentVariable("INTERNAL_DB_URL");

if (!string.IsNullOrEmpty(renderDbHost) || !string.IsNullOrEmpty(renderExternalUrl))
{
    // We're in Render environment
    Console.WriteLine("Detected Render environment");

    if (!string.IsNullOrEmpty(renderExternalUrl))
    {
        // Use the full connection string if available
        connectionString = renderExternalUrl;
        Console.WriteLine("Using EXTERNAL_DB_URL connection string");
    }
    else if (!string.IsNullOrEmpty(renderDbHost))
    {
        // Build connection string from individual components
        var dbHost = renderDbHost;
        var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "todoapp";
        var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "todoapp_user";
        var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? throw new InvalidOperationException("DB_PASSWORD is required");
        var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";

        connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword};SSL Mode=Require;Trust Server Certificate=true;";
        Console.WriteLine($"Using individual environment variables for database: {dbHost}");
    }
    else
    {
        throw new InvalidOperationException("No Render database configuration found");
    }
}
else
{
    // Local development fallback
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Host=localhost;Database=todoapp;Username=postgres;Password=password;Port=5432";
    Console.WriteLine("Using local development database");
}

// Log the safe connection string (without password)
var safeConnectionString = connectionString.Contains("Password=")
    ? connectionString.Replace(connectionString.Split("Password=")[1].Split(";")[0], "***")
    : connectionString;
Console.WriteLine($"Final Connection String: {safeConnectionString}");

// Configure DbContext with retry logic
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null
        );
    }));

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

// **FIX: Safe database migration with better error handling**
try
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Console.WriteLine("Attempting database connection...");

        // Wait for database to be ready with retries
        var maxRetries = 3;
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                if (await dbContext.Database.CanConnectAsync())
                {
                    Console.WriteLine("Database connection successful!");
                    break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database connection attempt {i + 1} failed: {ex.Message}");
                if (i == maxRetries - 1) throw;
                await Task.Delay(2000); // Wait 2 seconds before retry
            }
        }

        // Apply migrations
        Console.WriteLine("Applying database migrations...");
        await dbContext.Database.MigrateAsync();
        Console.WriteLine("Database migration completed successfully.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Database initialization failed: {ex.Message}");
    Console.WriteLine("Application will continue without database migrations.");
}

Console.WriteLine("Starting application...");
app.Run();