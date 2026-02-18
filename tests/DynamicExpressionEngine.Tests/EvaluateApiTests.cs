using System.Net;
using System.Net.Http.Json;
using DynamicExpressionEngine.Api;
using DynamicExpressionEngine.Core.Models.Requests;
using DynamicExpressionEngine.Core.Models.Responses;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace DynamicExpressionEngine.Tests;

public sealed class EvaluateApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string ApiKey = "dev-api-key";
    private readonly WebApplicationFactory<Program> _factory;

    public EvaluateApiTests(WebApplicationFactory<Program> factory)
    {
        // WebApplicationFactory may not load appsettings.json the same way as when running the API.
        // Force the API key into configuration for deterministic auth behavior in tests.
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Authentication:ApiKey"] = ApiKey
                });
            });
        });
    }

    [Fact]
    public async Task Valid_date_and_time_expression_returns_expected_result()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", ApiKey);

        var request = new EvaluateRequest
        {
            Data = new Dictionary<string, object?>
            {
                ["date"] = "22-02-2026",
                ["time"] = "17:00"
            },
            Expression =
                "ToString(FormatDate(DateParseExact([date], 'dd-MM-yyyy'),'yyyy-MM-dd') + ' ' + [time] + ':00')"
        };

        var response = await client.PostAsJsonAsync("/api/evaluate", request);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            var debug = await response.Content.ReadFromJsonAsync<EvaluateResponse>();
            throw new Xunit.Sdk.XunitException($"Unexpected status {(int)response.StatusCode} {response.StatusCode}. Error: {debug?.Error}");
        }

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<EvaluateResponse>();
        payload!.Result.Should().Be("2026-02-22 17:00:00");
        payload.Error.Should().BeNull();
    }

    [Fact]
    public async Task Missing_time_parameter_returns_400_with_error()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", ApiKey);

        var request = new EvaluateRequest
        {
            Data = new Dictionary<string, object?>
            {
                ["date"] = "22-02-2026"
            },
            Expression = "ToString([time])"
        };

        var response = await client.PostAsJsonAsync("/api/evaluate", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var payload = await response.Content.ReadFromJsonAsync<EvaluateResponse>();
        payload!.Error.Should().Contain("time");
    }

    [Fact]
    public async Task Wrong_format_string_returns_dateparseexact_error()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", ApiKey);

        var request = new EvaluateRequest
        {
            Data = new Dictionary<string, object?>
            {
                ["date"] = "22-02-2026",
                ["time"] = "17:00"
            },
            Expression = "ToString(DateParseExact([date], 'yyyy-MM-dd'))"
        };

        var response = await client.PostAsJsonAsync("/api/evaluate", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var payload = await response.Content.ReadFromJsonAsync<EvaluateResponse>();
        payload!.Error.Should().Contain("DateParseExact");
        payload.Error.Should().Contain("could not parse");
    }

    [Fact]
    public async Task Invalid_expression_syntax_returns_error()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", ApiKey);

        var request = new EvaluateRequest
        {
            Data = new Dictionary<string, object?> { ["x"] = 1 },
            Expression = "ToString("
        };

        var response = await client.PostAsJsonAsync("/api/evaluate", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var payload = await response.Content.ReadFromJsonAsync<EvaluateResponse>();
        payload!.Error.Should().NotBeNullOrWhiteSpace();
        payload.Error!.Should().Contain("Invalid expression");
    }

    [Fact]
    public async Task Null_input_value_does_not_crash_and_returns_friendly_error_or_empty_string()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", ApiKey);

        // This uses concatenation. If [time] is null, NCalc may error; we just want a friendly failure.
        var request = new EvaluateRequest
        {
            Data = new Dictionary<string, object?>
            {
                ["date"] = "22-02-2026",
                ["time"] = null
            },
            Expression = "ToString(FormatDate(DateParseExact([date], 'dd-MM-yyyy'),'yyyy-MM-dd') + ' ' + [time])"
        };

        var response = await client.PostAsJsonAsync("/api/evaluate", request);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var ok = await response.Content.ReadFromJsonAsync<EvaluateResponse>();
            ok!.Result.Should().NotBeNull();
        }
        else
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var bad = await response.Content.ReadFromJsonAsync<EvaluateResponse>();
            bad!.Error.Should().Contain("Invalid expression");
        }
    }

    [Fact]
    public async Task Missing_api_key_returns_401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/evaluate", new EvaluateRequest
        {
            Data = new Dictionary<string, object?> { ["x"] = 1 },
            Expression = "ToString([x])"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Nested_parameter_path_expression_returns_expected_result()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", ApiKey);

        var request = new EvaluateRequest
        {
            Data = new Dictionary<string, object?>
            {
                ["person"] = new Dictionary<string, object?>
                {
                    ["name"] = "Ali",
                    ["birth"] = "22-02-2026"
                },
                ["meta"] = new Dictionary<string, object?>
                {
                    ["score"] = 10
                },
                ["time"] = "17:00"
            },
            Expression = "ToString([person.name] + ' ' + [time])"
        };

        var response = await client.PostAsJsonAsync("/api/evaluate", request);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            var debug = await response.Content.ReadFromJsonAsync<EvaluateResponse>();
            throw new Xunit.Sdk.XunitException(
                $"Unexpected status {(int)response.StatusCode} {response.StatusCode}. Error: {debug?.Error}");
        }

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<EvaluateResponse>();
        payload!.Result.Should().Be("Ali 17:00");
        payload.Error.Should().BeNull();
    }
    
    [Fact]
    public async Task Nested_array_parameter_path_expression_returns_expected_result()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", ApiKey);

        var request = new EvaluateRequest
        {
            Data = new Dictionary<string, object?>
            {
                ["person"] = new List<object?>
                {
                    new Dictionary<string, object?> { ["name"] = "Ali" },
                    new Dictionary<string, object?> { ["age"] = 40 },
                    new Dictionary<string, object?> { ["name"] = "Ayse", ["birth"] = "22-02-2026" }
                }
            },
            Expression = "ToString([person.2.name])"
        };

        var response = await client.PostAsJsonAsync("/api/evaluate", request);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            var debug = await response.Content.ReadFromJsonAsync<EvaluateResponse>();
            throw new Xunit.Sdk.XunitException(
                $"Unexpected status {(int)response.StatusCode} {response.StatusCode}. Error: {debug?.Error}");
        }

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<EvaluateResponse>();
        payload!.Result.Should().Be("Ayse");
        payload.Error.Should().BeNull();
    }
}