using DynamicExpressionEngine.Api.Authentication;
using DynamicExpressionEngine.Api.Infrastructure;
using DynamicExpressionEngine.Core.Abstractions;
using DynamicExpressionEngine.Core.Catalog;
using DynamicExpressionEngine.Core.Models.Responses;
using DynamicExpressionEngine.Core.Registry;
using DynamicExpressionEngine.Core.Validation;
using DynamicExpressionEngine.NCalc;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Ensure configuration sources include appsettings.json even when hosted by WebApplicationFactory
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables();

builder.Services.AddControllers();

// Add FluentValidation automatic model validation + register validators
builder.Services.AddValidatorsFromAssemblyContaining<EvaluateRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();

//I added rateLimiter to protect our API
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", rateLimiterOptions =>
    {
        rateLimiterOptions.AutoReplenishment = true;
        rateLimiterOptions.PermitLimit = 10;
        rateLimiterOptions.Window = TimeSpan.FromSeconds(1);
    });
});


// Global exception handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "DynamicExpressionEngine", Version = "v1" });

    // API key support in Swagger UI
    var apiKeyScheme = new OpenApiSecurityScheme
    {
        Name = "X-API-Key",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "API Key needed to call secured endpoints. Add header: X-API-Key: <your-key>",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "ApiKey"
        }
    };

    c.AddSecurityDefinition("ApiKey", apiKeyScheme);

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            apiKeyScheme,
            Array.Empty<string>()
        }
    });
});

//In order to return a custom error message when a model is invalid,
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var firstError = context.ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .SelectMany(x => x.Value!.Errors)
            .Select(e => e.ErrorMessage)
            .FirstOrDefault() ?? "Invalid request";

        return new BadRequestObjectResult(new EvaluateResponse
        {
            Error = firstError
        });
    };
});

// Core services
builder.Services.AddSingleton<IFunctionCatalog, FunctionCatalog>();
builder.Services.AddSingleton<IFunctionRegistry, FunctionRegistry>();
builder.Services.AddSingleton<ExpressionEngineBase, NCalcExpressionEngine>();

// API key authentication
builder.Services
    .AddAuthentication("ApiKey")
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>("ApiKey", _ => { });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();

app.Run();

// Required for WebApplicationFactory in integration tests
namespace DynamicExpressionEngine.Api
{
    public partial class Program
    {
    }
}