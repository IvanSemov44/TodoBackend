## Running the Project with Docker

This project is containerized using Docker and can be run easily with Docker Compose. Below are the specific instructions and requirements for this project:

### Requirements
- **.NET Version:** The Dockerfile uses the version specified in `TodoBackend.csproj`, defaulting to **.NET 9.0** if not found.
- **No external dependencies:** The service does not require external databases or caches.

### Environment Variables
- The application runs with `ASPNETCORE_ENVIRONMENT=Docker` by default (set in the Dockerfile).
- No additional environment variables are required unless you wish to override defaults. If you have a `.env` file, you can uncomment the `env_file` line in `docker-compose.yml`.

### Build and Run Instructions
1. **Build and start the service:**
   ```sh
   docker compose up --build
   ```
   This will build the image using the provided Dockerfile and start the ASP.NET Core backend.

2. **Accessing the service:**
   - The service is exposed on **port 80** of your host machine.
   - You can access the API at `http://localhost:80/`.

### Special Configuration
- The Dockerfile creates a non-root user (`appuser`) for running the application, improving container security.
- The application uses Docker-specific settings from `appsettings.Docker.json` when running in the container.

### Ports
- **csharp-todobackend:**
  - **Exposed port:** `80` (mapped to host port 80)

---

*If you add databases or other services in the future, update the `docker-compose.yml` accordingly and document any new environment variables or configuration steps here.*
