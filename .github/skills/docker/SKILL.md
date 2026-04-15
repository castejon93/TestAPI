---
name: docker
description: "Use when containerizing, deploying, or troubleshooting Docker for .NET Clean Architecture projects. Covers Dockerfile, docker-compose, environment configuration, SQL Server containers, and production hardening."
---

# Docker Containerization Skill

## When to Use

- Creating or updating Dockerfile for .NET projects
- Setting up docker-compose with SQL Server
- Configuring environment-specific appsettings for Docker
- Troubleshooting container startup, networking, or migration issues
- Reviewing Docker readiness or production hardening

## Project Docker Structure

```
├── Dockerfile                    # Multi-stage build
├── docker-compose.yml            # Service orchestration
├── .dockerignore                 # Build context exclusions
└── WebAPI/
    ├── appsettings.json          # Base configuration
    └── appsettings.Docker.json   # Docker-specific overrides
```

## Dockerfile Pattern (Multi-Stage Build)

Use a 3-stage build for optimized image size:

### Stage 1: Build
- Base image: `mcr.microsoft.com/dotnet/sdk:10.0`
- Copy `.slnx` and all `.csproj` files first for layer caching
- Run `dotnet restore` on the WebAPI project
- Copy full source and build in Release mode

### Stage 2: Publish
- Extend from build stage
- Run `dotnet publish` with `/p:UseAppHost=false`

### Stage 3: Runtime
- Base image: `mcr.microsoft.com/dotnet/aspnet:10.0` (smaller, no SDK)
- Expose ports 8080 and 8081
- Set `ASPNETCORE_URLS=http://+:8080`
- Set `ASPNETCORE_ENVIRONMENT=Docker`
- Copy published output and set `ENTRYPOINT ["dotnet", "WebAPI.dll"]`

### Layer Caching Strategy
```
COPY ["Test.slnx", "."]
COPY ["WebAPI/WebAPI.csproj", "WebAPI/"]
COPY ["Application/Application.csproj", "Application/"]
COPY ["Domain/Domain.csproj", "Domain/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "WebAPI/WebAPI.csproj"
COPY . .
```
Copy project files before source code so `dotnet restore` is cached unless dependencies change.

## Docker Compose Pattern

### Services

#### SQL Server
- Image: `mcr.microsoft.com/mssql/server:2022-latest`
- Required env vars: `ACCEPT_EULA=Y`, `MSSQL_SA_PASSWORD`, `MSSQL_PID=Developer`
- Port: `1433:1433`
- Named volume for data persistence: `sqlserver-data:/var/opt/mssql`
- Health check using `sqlcmd`:
  ```yaml
  healthcheck:
    test: /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "password" -C -Q "SELECT 1" || exit 1
    interval: 10s
    timeout: 5s
    retries: 10
    start_period: 30s
  ```

#### WebAPI
- Build from Dockerfile in project root
- Port mapping: `5000:8080` (host:container)
- Environment variable overrides for connection string and JWT settings
- `depends_on` with `condition: service_healthy` to wait for SQL Server
- Use Docker service DNS name (`sqlserver`) in connection strings

### Networking
- Create a dedicated bridge network for inter-service communication
- Services communicate using service names as DNS hostnames

### Volumes
- Use named volumes for database persistence across container restarts

## Environment Configuration

### appsettings.Docker.json
- Connection string must use Docker service name as server: `Server=sqlserver;Database=TestDb;...`
- Include `TrustServerCertificate=True` for SQL Server container
- JWT settings with issuer/audience matching the Docker-exposed URL

### Environment Variable Overrides in docker-compose
Connection strings use double-underscore notation:
```yaml
- ConnectionStrings__DefaultConnection=Server=sqlserver;Database=TestDb;...
- JwtSettings__Issuer=http://localhost:5000
- JwtSettings__Audience=http://localhost:5000
```

## Application Readiness Checklist

### Swagger Access
Swagger UI is gated behind `IsDevelopment()` by default. For Docker, extend the condition:
```csharp
if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Docker")
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

### Auto-Migration on Startup
Enable automatic EF Core migration for Docker (creates DB and applies migrations):
```csharp
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
}
```
Wrap in try/catch and log errors — do not let migration failures crash the app silently.

### .dockerignore Must Include
```
**/.git
**/.vs
**/bin
**/obj
**/*.user
**/[Dd]ebug/
**/[Rr]elease/
**/packages/
**/TestResults/
```

## Running Commands

```bash
# Build and start all services
docker-compose up --build

# Run in background
docker-compose up --build -d

# View logs
docker-compose logs -f webapi

# Stop and remove containers
docker-compose down

# Stop and remove containers + volumes (destroys data)
docker-compose down -v
```

## Access Points (Default)

| Service | URL |
|---------|-----|
| API | `http://localhost:5000` |
| Swagger UI | `http://localhost:5000/swagger` |
| SQL Server | `localhost,1433` (from host) or `sqlserver,1433` (from container) |

## Production Hardening Checklist

1. **Secrets**: Never hardcode passwords in docker-compose. Use Docker secrets or environment files (`.env`) excluded from source control
2. **JWT Keys**: Inject `JwtSettings__SecretKey` via environment variable or secret, not appsettings
3. **CORS**: Restrict `AllowAnyOrigin()` to specific domains
4. **SQL Password**: Use a strong, unique SA password — not the default template value
5. **HTTPS**: Configure TLS termination via reverse proxy (nginx, Traefik) or ASP.NET Core HTTPS
6. **Health Checks**: Add application-level health checks (`/health` endpoint) for orchestrators
7. **Resource Limits**: Set CPU/memory limits in docker-compose for production
8. **Logging**: Configure structured logging (Serilog) with log aggregation

## Troubleshooting

| Issue | Cause | Fix |
|-------|-------|-----|
| WebAPI can't connect to SQL Server | Started before DB is ready | Ensure `depends_on` with `condition: service_healthy` |
| Connection string fails | Wrong server name | Use Docker service name (`sqlserver`), not `localhost` |
| Swagger not showing | Environment mismatch | Check `ASPNETCORE_ENVIRONMENT=Docker` and Swagger condition |
| Migration fails on startup | DB not ready despite health check | Increase `start_period` or add retry logic in migration code |
| Port conflict on host | Port already in use | Change host port in `ports` mapping |
| Build fails in Docker | Missing project in COPY | Ensure all `.csproj` files are copied before restore |
