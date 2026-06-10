using Lucy.Identity.Api.Authentication;
using Lucy.Identity.Domain.Identity;
using Lucy.Identity.Infrastructure;
using Lucy.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LUCY Identity API",
        Version = "v1"
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            []
        }
    });
});
builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys")));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddSingleton<PasswordHasher>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddSingleton<IAccessTokenService>(provider => provider.GetRequiredService<JwtTokenService>());
builder.Services.AddScoped<IdentityService>();
builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.Services
    .AddAuthentication(JwtAuthenticationDefaults.AuthenticationScheme)
    .AddScheme<JwtAuthenticationOptions, JwtAuthenticationHandler>(
        JwtAuthenticationDefaults.AuthenticationScheme,
        options => { });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("MentorAccess", policy => policy.RequireRole("Pro", "Super"));
    options.AddPolicy("SuperOnly", policy => policy.RequireRole("Super"));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    await dbContext.Database.MigrateAsync();
    var seeder = scope.ServiceProvider.GetRequiredService<IdentityDataSeeder>();
    await seeder.SeedAsync(CancellationToken.None);
}

if (!app.Environment.IsDevelopment())
{
if (builder.Configuration.GetValue("LUCY_ENABLE_HTTPS_REDIRECTION", false))
{
    app.UseHttpsRedirection();
}
}
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", () => Results.Ok(new
{
    service = "lucy-identity",
    status = "ok",
    utc = DateTimeOffset.UtcNow
}));

app.MapControllers();

app.Run();
