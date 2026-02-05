using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Pms.Api.Authorization;
using Pms.Application.Validators;
using Pms.Infrastructure;
using Pms.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateReservationRequestValidator>();
builder.Services.AddEndpointsApiExplorer();
var companyName = builder.Configuration["Branding:CompanyName"] ?? "PMS";
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = $"{companyName} PMS API", Version = "v1" });
});

// Authentication (Keycloak OIDC)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Keycloak:Authority"];
        options.Audience = builder.Configuration["Keycloak:ClientId"];
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();

        // Use separate metadata address if provided (for Docker networking)
        var metadataAddress = builder.Configuration["Keycloak:MetadataAddress"];
        if (!string.IsNullOrEmpty(metadataAddress))
        {
            options.MetadataAddress = metadataAddress;
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidIssuer = builder.Configuration["Keycloak:Authority"],
            ValidAudience = builder.Configuration["Keycloak:ClientId"],
            RoleClaimType = ClaimTypes.Role
        };

        // Map Keycloak realm_access.roles to standard role claims
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                if (context.Principal?.Identity is ClaimsIdentity identity)
                {
                    var realmAccessClaim = identity.FindFirst("realm_access");
                    if (realmAccessClaim != null)
                    {
                        try
                        {
                            var realmAccess = JsonSerializer.Deserialize<JsonElement>(realmAccessClaim.Value);
                            if (realmAccess.TryGetProperty("roles", out var roles))
                            {
                                foreach (var role in roles.EnumerateArray())
                                {
                                    var roleValue = role.GetString();
                                    if (!string.IsNullOrEmpty(roleValue))
                                    {
                                        identity.AddClaim(new Claim(ClaimTypes.Role, roleValue));
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // Ignore JSON parsing errors
                        }
                    }
                }
                return Task.CompletedTask;
            }
        };
    });

// Authorization policies
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireManager", policy => policy.RequireRole("manager"))
    .AddPolicy("RequireFrontdesk", policy => policy.RequireRole("frontdesk", "manager"))
    .AddPolicy("RequireMaintenance", policy => policy.RequireRole("maintenance", "manager"))
    .AddPolicy("RequireCleaner", policy => policy.RequireRole("cleaner", "manager"))
    .AddPolicy("RequireTherapist", policy => policy.RequireRole("therapist", "manager"))
    .AddPolicy("RequireAccounting", policy => policy.RequireRole("accounting", "manager"));

// Resource-based authorization handlers
builder.Services.AddScoped<IAuthorizationHandler, TherapistOwnAppointmentHandler>();

// Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Global rate limit: 100 requests per minute per IP
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10
            }));

    // Named policy for write operations (more restrictive)
    options.AddFixedWindowLimiter("WriteOperations", limiterOptions =>
    {
        limiterOptions.PermitLimit = 30;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 5;
    });
});

var app = builder.Build();

// Auto-migrate database in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<PmsDbContext>();
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health check endpoint (unauthenticated)
app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();

app.Run();

// Make Program accessible for integration tests
public partial class Program { }
