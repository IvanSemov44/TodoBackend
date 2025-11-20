# syntax=docker/dockerfile:1

ARG DOTNET_VERSION=9.0

# --- Build Stage ---
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS builder
WORKDIR /src

# Copy the solution file and restore dependencies
COPY  TodoBackend.sln ./
COPY TodoBackend/TodoBackend.csproj ./TodoBackend/
RUN  dotnet restore

# Copy the rest of the source code
COPY TodoBackend ./TodoBackend/

# Publish the application
WORKDIR /src/TodoBackend
RUN dotnet publish -c Release -o /app/publish --no-restore

# --- Runtime Stage ---
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS final
WORKDIR /app

# Create a non-root user
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser

# Copy published output
COPY --from=builder /app/publish .

# Expose default ASP.NET port
EXPOSE 80

# Set environment to use Docker-specific settings if present
ENV ASPNETCORE_ENVIRONMENT=Docker

ENTRYPOINT ["dotnet", "TodoBackend.dll"]
