using Microsoft.EntityFrameworkCore;
using TodoBackend.Data;

namespace TodoBackend.Services
{
    public class DatabaseMigrationService
    {
        public static async Task MigrateDatabaseAsync(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<DatabaseMigrationService>>();
            var dbContext = services.GetRequiredService<ApplicationDbContext>();

            try
            {
                logger.LogInformation("Testing database connection...");

                // Test connection with retry logic
                await WaitForDatabaseAsync(dbContext, logger);

                logger.LogInformation("Applying database migrations...");
                await dbContext.Database.MigrateAsync();

                logger.LogInformation("Database migration completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Databse migration failed");
                throw;
            }
        }

        private static async Task WaitForDatabaseAsync(ApplicationDbContext dbContext, ILogger<DatabaseMigrationService> logger)
        {
            const int maxRetryies = 5;
            const int delaySeconds = 2;

            for (int i = 0; i <= maxRetryies; i++)
            {
                try
                {
                    if (await dbContext.Database.CanConnectAsync())
                    {
                        logger.LogInformation("Database connection successful");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogInformation("Databasse connection attempt {Attmpt} failed: {Message}", i, ex.Message);

                    if (i == maxRetryies)
                    {
                        logger.LogError("All database connection attempts failed");
                        throw;
                    }

                    logger.LogInformation("Retrying in {Delay} seconds...", delaySeconds);
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                }
            }
        }
    }
}
