using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OrderManagement.API.Middlewares;
using OrderManagement.Application.Services;
using OrderManagement.Application.Services.OrderService;
using OrderManagement.Infrastructure.Data;
using OrderManagement.Infrastructure.Services;
using Scalar.AspNetCore;
using System.Text;
using System.Threading.RateLimiting;

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
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

// ── Controllers + validation ──────────────────────────────────────────────
builder.Services.AddControllers();

// ── Swagger / OpenAPI ─────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

//-------- Logger-----------------------
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

//------Rate Limiter------
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("auth", httpContext =>
         RateLimitPartition.GetFixedWindowLimiter(
             partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
             factory: _ => new FixedWindowRateLimiterOptions
             {
                 PermitLimit = 3,
                 Window = TimeSpan.FromHours(12),
                 QueueLimit = 0
             }
         ));

    options.AddPolicy("add", httpContext =>
         RateLimitPartition.GetFixedWindowLimiter(
             partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
             factory: _ => new FixedWindowRateLimiterOptions
             {
                 PermitLimit = 5,
                 Window = TimeSpan.FromMinutes(15),
                 QueueLimit = 0
             }
         ));

    
});


// ── Services ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IOrderService, ServiceOrder>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

var app = builder.Build();

// ── Auto-Migration ────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

// ── Pipeline HTTP ─────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Order Management API");
    });
}
app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<RateLimiterMiddelware>();
app.UseRateLimiter();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();