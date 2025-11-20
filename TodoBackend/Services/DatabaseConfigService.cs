namespace TodoBackend.Services
{
    public class DatabaseConfigService
    {
        private readonly IConfiguration _configuration;

        public DatabaseConfigService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GetConnectionString()
        {
            // Check for Render envirment first
            var renderDbHost = Environment.GetEnvironmentVariable("DB_HOST");
            var renderExternalUrl = Environment.GetEnvironmentVariable("EXTERNAL_DB_URL");

            if (!string.IsNullOrEmpty(renderExternalUrl))
            {
                return renderExternalUrl;
            }

            if (!string.IsNullOrEmpty(renderDbHost))
            {
                return BuilderConnectionStringFromEnvironment();
            }

            return _configuration.GetConnectionString("DefaultConnection")!;
        }

        private string BuilderConnectionStringFromEnvironment()
        {
            var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
            var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "todoapp";
            var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "todoapp_user";
            var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD")
                ?? throw new InvalidOperationException("DB_PASSWORD is required");

            var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";

            return $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword};SSL Mode=Require;Trust Server Certificate=true;";
        }

        public void LogConfiguraion(ILogger logger)
        {
            var renderDbHost = Environment.GetEnvironmentVariable("DB_HOST");

            if (!string.IsNullOrEmpty(renderDbHost))
            {
                logger.LogInformation("using render PostgreSQL: {DbHost}", renderDbHost);
            }
            else
            {
                logger.LogInformation("Using local development database");
            }
        }
    }
}
