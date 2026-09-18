using System.Security.Claims;
using System.Text;
using FacilitiesMgmt.Application;
using FacilitiesMgmt.Application.Abstractions;
using FacilitiesMgmt.Infrastructure;
using FacilitiesMgmt.Infrastructure.Persistence;
using FacilitiesMgmt.WebApi.Auth;
using FacilitiesMgmt.WebApi.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Configuration and secrets
//
// The connection string and the JWT signing key are read from configuration and
// must be supplied out of band: `dotnet user-secrets` locally, environment
// variables anywhere else. appsettings.json carries structure and non-secrets
// only. There is deliberately no fallback default for the signing key — a
// development default is exactly the kind of convenience that reaches production
// and signs tokens anybody can forge.
// ---------------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:Default is not configured. See README.md for setup.");

builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    // Validate at startup, not on the first login attempt. A misconfigured
    // deployment should refuse to start, loudly, rather than accept traffic and
    // fail at the worst moment.
    .ValidateOnStart();

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddApplication();

builder.Services.AddHttpContextAccessor();

// Scoped. Never AsyncLocal — see CurrentTenant for why, and TenantConcurrencyTests
// for the test that holds the line.
builder.Services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("Jwt configuration section is missing.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ValidateLifetime = true,

            // Default is five minutes of slack, which quietly extends every token's
            // life past its stated expiry. On a system holding financial data,
            // "expired" should mean expired.
            ClockSkew = TimeSpan.Zero,

            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.Name
        };
    });

builder.Services.AddAuthorizationBuilder().AddApplicationPolicies();

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        // Enums cross the wire as names, not ordinals. "Status": 2 is unreadable in a
        // log, breaks the moment someone reorders the enum, and forces the SPA to keep
        // a parallel copy of the numbering. "Status": "Approved" does not.
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

builder.Services.AddOpenApi();

// The Angular dev server is a different origin. Locked to the configured origins
// with credentials allowed; a wildcard would be both wrong and, with credentials,
// rejected by browsers anyway.
const string SpaCorsPolicy = "spa";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:4200"];

builder.Services.AddCors(options => options.AddPolicy(SpaCorsPolicy, policy =>
    policy.WithOrigins(allowedOrigins)
          .AllowAnyHeader()
          .AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Migrate and seed on startup so a clean machine reaches a working system in one
    // command. Development only: in production, migrations are a deliberate,
    // reviewable deployment step, never a side effect of an app instance booting —
    // several instances starting at once would race each other.
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.UseHttpsRedirection();
app.UseCors(SpaCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.RunAsync();

/// <summary>Exposed so the integration tests can drive the real pipeline via WebApplicationFactory.</summary>
public partial class Program;
