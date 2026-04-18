using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Refline.Api.Data;
using Refline.Api.Services.Admin;
using Refline.Api.Services.Auth;
using Refline.Api.Services.ClassificationRules;
using Refline.Api.Services.Internal;
using Refline.Api.Services.InternalCompanies;
using Refline.Api.Services.Licenses;
using Refline.Api.Swagger;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("ReflineInternalApiKey", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        Name = InternalApiHeaders.ApiKey,
        In = ParameterLocation.Header,
        Description = "Internal API key for Refline platform endpoints."
    });

    options.OperationFilter<InternalApiKeyOperationFilter>();
});
builder.Services.Configure<InternalApiOptions>(
    builder.Configuration.GetSection(InternalApiOptions.SectionName));
builder.Services.AddScoped<IRequestUserContextService, RequestUserContextService>();
builder.Services.AddScoped<IAdminAccessService, AdminAccessService>();
builder.Services.AddScoped<InternalApiAuthorizationService>();
builder.Services.AddScoped<CompanyProvisioningService>();
builder.Services.AddScoped<AdminAnalyticsService>();
builder.Services.AddScoped<AdminCompanyLicenseService>();
builder.Services.AddScoped<AdminUserManagementService>();
builder.Services.AddScoped<AdminClassificationRuleManagementService>();
builder.Services.AddScoped<ClassificationRuleReadService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<LicenseActivationService>();

builder.Services.AddDbContext<ReflineDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

    options.UseNpgsql(connectionString);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ReflineDbContext>();
    dbContext.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
app.MapControllers();

app.Run();
