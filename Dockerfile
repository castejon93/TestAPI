# =============================================================================
# Dockerfile for .NET 10 Clean Architecture WebAPI
# Multi-stage build for optimized image size
# =============================================================================

# -----------------------------------------------------------------------------
# Stage 1: Build Stage
# Uses the full .NET SDK to restore packages and build the application
# -----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution file and all project files first (for better layer caching)
# This allows Docker to cache the restore step if only code changes
COPY ["Test.slnx", "."]
COPY ["WebAPI/WebAPI.csproj", "WebAPI/"]
COPY ["Application/Application.csproj", "Application/"]
COPY ["Domain/Domain.csproj", "Domain/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]
COPY ["Shared/Shared.csproj", "Shared/"]

# Restore NuGet packages for all projects
# This step is cached unless .csproj files change
RUN dotnet restore "WebAPI/WebAPI.csproj"

# Copy the entire source code
COPY . .

# Build the application in Release mode
WORKDIR "/src/WebAPI"
RUN dotnet build "WebAPI.csproj" -c Release -o /app/build

# -----------------------------------------------------------------------------
# Stage 2: Publish Stage
# Creates the final published output
# -----------------------------------------------------------------------------
FROM build AS publish
RUN dotnet publish "WebAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# -----------------------------------------------------------------------------
# Stage 3: Runtime Stage
# Uses the smaller ASP.NET runtime image (no SDK)
# -----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Expose the port the application will run on
EXPOSE 8080
EXPOSE 8081

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker

# Copy the published application from the publish stage
COPY --from=publish /app/publish .

# Set the entry point to run the WebAPI
ENTRYPOINT ["dotnet", "WebAPI.dll"]