using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OrderManagement.API.Middlewares;
using OrderManagement.Application.Services;
using OrderManagement.Application.Services.OrderService;
using OrderManagement.Infrastructure.Data;
using OrderManagement.Infrastructure.Services;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── Base de données (EF Core + SQL Server) ────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Authentification JWT ──────────────────────────────────────────────────
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("JWT Key manquante dans la configuration.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidateLifetime         = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer              = jwtSection["Issuer"],
        ValidAudience            = jwtSection["Audience"],
        IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

// ── Services applicatifs ──────────────────────────────────────────────────
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IOrderService, ServiceOrder>();

// ── Controllers + validation ──────────────────────────────────────────────
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value!.Errors.Select(x => x.ErrorMessage).ToArray()
                );

            var errorResponse = new
            {
                status = StatusCodes.Status400BadRequest,
                message = "Validation failed",
                errors = errors
            };

            return new BadRequestObjectResult(errorResponse);
        };
    });

// ── Swagger / OpenAPI ─────────────────────────────────────────────────────
// ── Swagger / OpenAPI  ← REMPLACER PAR CECI ──────────────────────────────
builder.Services.AddEndpointsApiExplorer(); // toujours nécessaire pour les controllers
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new Microsoft.OpenApi.OpenApiInfo
        {
            Title = "Order Management API",
            Version = "v1",
            Description = "API REST de gestion de commandes — ASP.NET Core 10"
        };

        // Schéma de sécurité Bearer
        document.Components ??= new Microsoft.OpenApi.OpenApiComponents();
        document.Components.SecuritySchemes ??=
            new Dictionary<string, Microsoft.OpenApi.IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes["Bearer"] =
            new Microsoft.OpenApi.OpenApiSecurityScheme
            {
                Type = Microsoft.OpenApi.SecuritySchemeType.Http,
                Scheme = "bearer",   // lowercase obligatoire
                BearerFormat = "JWT",
                Description = "Entrez votre token JWT (sans le préfixe 'Bearer ')"
            };

        // Appliquer le cadenas sur toutes les opérations
        if (document.Paths is not null)
            foreach (var path in document.Paths.Values)
                if (path.Operations is not null)
                    foreach (var operation in path.Operations.Values)
                    {
                        operation.Security ??= new List<Microsoft.OpenApi.OpenApiSecurityRequirement>();
                        operation.Security.Add(
                            new Microsoft.OpenApi.OpenApiSecurityRequirement
                            {
                                [new Microsoft.OpenApi.OpenApiSecuritySchemeReference(
                                    "Bearer", document)] = []
                            });
                    }

        return Task.CompletedTask;
    });
});

// ─────────────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

// ── Pipeline HTTP ─────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options =>
    {
        options.RouteTemplate = "openapi/{documentName}.json";
    });
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Order Management API");
    });
}

app.UseHttpsRedirection();

app.UseAuthentication(); // DOIT être avant UseAuthorization
app.UseAuthorization();

app.MapControllers();

app.Run();
