using Application.DTOs.Auth;
using Application.Interfaces;
using Application.Services;
using Domain.Interfaces;
using Domain.Settings;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// SECTION 1: ADD CONTROLLERS AND API SERVICES
// ============================================
builder.Services.AddControllers();

// ============================================
// SECTION 2: JWT CONFIGURATION
// ============================================
// Bind JWT settings from appsettings.json to strongly-typed object
// This allows us to inject IOptions<JwtSettings> anywhere in the app
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection(JwtSettings.SectionName));

// Get JWT settings for authentication configuration
var jwtSettings = builder.Configuration
    .GetSection(JwtSettings.SectionName)
    .Get<JwtSettings>()!;

// ============================================
// SECTION 3: CONFIGURE JWT AUTHENTICATION
// ============================================
builder.Services.AddAuthentication(options =>
{
    // Set JWT Bearer as the default authentication scheme
    // This means [Authorize] attributes will use JWT by default
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // ============================================
    // TOKEN VALIDATION PARAMETERS
    // ============================================
    // These settings control how incoming tokens are validated
    options.TokenValidationParameters = new TokenValidationParameters
    {
        // Validate the issuer (who created the token)
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,

        // Validate the audience (who the token is intended for)
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,

        // Validate the token hasn't expired
        ValidateLifetime = true,

        // Validate the signing key
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),

        // Clock skew compensates for server time drift
        // Set to zero for stricter validation
        ClockSkew = TimeSpan.Zero
    };

    // ============================================
    // JWT BEARER EVENTS (Optional but useful)
    // ============================================
    options.Events = new JwtBearerEvents
    {
        // Called when authentication fails
        OnAuthenticationFailed = context =>
        {
            // Add header to indicate token has expired
            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            {
                context.Response.Headers.Append("Token-Expired", "true");
            }
            return Task.CompletedTask;
        },

        // Called when a request is received
        OnMessageReceived = context =>
        {
            // Optionally read token from query string for SignalR
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

// Add authorization services
builder.Services.AddAuthorization();

// ============================================
// SECTION 4: SWAGGER/OPENAPI SETUP WITH JWT
// ============================================
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Product API",
        Version = "v1",
        Description = "Clean Architecture API with JWT Authentication",
        Contact = new OpenApiContact
        {
            Name = "Your Name",
            Email = "your.email@example.com"
        }
    });

    // ============================================
    // ADD JWT AUTHENTICATION TO SWAGGER
    // ============================================
    // This adds the "Authorize" button to Swagger UI
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token in the format: Bearer {your_token}\n\n" +
                      "Example: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    });

    // Apply security requirement globally to all endpoints
    options.AddSecurityRequirement(document =>
    {
        var requirement = new OpenApiSecurityRequirement();
        requirement.Add(new OpenApiSecuritySchemeReference("Bearer", document), []);
        return requirement;
    });
});

// ============================================
// SECTION 5: ADD CORS
// ============================================
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ============================================
// SECTION 6: ADD DATABASE
// ============================================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    );
});

// ============================================
// SECTION 7: REGISTER DEPENDENCIES
// ============================================
// Existing services
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();

// Authentication services
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();

// ============================================
// BUILD THE APPLICATION
// ============================================
var app = builder.Build();

// ============================================
// SECTION 8: CONFIGURE THE HTTP PIPELINE
// ============================================
if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Docker")
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Product API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "Product API - Swagger Documentation";
    });
}

// Exception handling middleware
var logger = app.Services.GetRequiredService<ILogger<Program>>();
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        if (exception != null)
        {
            logger.LogError(exception.Error, "Unhandled exception occurred");
            await context.Response.WriteAsJsonAsync(new
            {
                message = exception.Error.Message,
                stackTrace = app.Environment.IsDevelopment() ? exception.Error.StackTrace : null
            });
        }
    });
});

// ============================================
// SECTION 9: AUTO-APPLY MIGRATIONS & SEED DATA
// ============================================
// =============================================================================
// Apply pending EF Core migrations automatically (useful for Docker)
// =============================================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        // Apply any pending migrations
        // This creates the database if it doesn't exist and applies all migrations
        context.Database.Migrate();

        Console.WriteLine("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

app.UseHttpsRedirection();
app.UseCors();

// ============================================
// CRITICAL: Authentication MUST come before Authorization
// ============================================
app.UseAuthentication(); // Validates JWT tokens
app.UseAuthorization();  // Checks [Authorize] attributes

app.MapControllers();

app.Run();