using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace DynamicExpressionEngine.Api.Authentication;

public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IConfiguration _configuration;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _configuration = configuration;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        const string headerName = "X-API-Key";

        if (!Request.Headers.TryGetValue(headerName, out var apiKeyValues))
            return Task.FromResult(AuthenticateResult.Fail("Missing API key"));

        var providedKey = apiKeyValues.FirstOrDefault();
        var configuredKey = _configuration["Authentication:ApiKey"];

        if (string.IsNullOrWhiteSpace(configuredKey))
            return Task.FromResult(AuthenticateResult.Fail("Server authentication not configured"));

        if (!string.Equals(providedKey, configuredKey, StringComparison.Ordinal))
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));

        var claims = new[] { new Claim(ClaimTypes.Name, "ApiKeyClient") };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}