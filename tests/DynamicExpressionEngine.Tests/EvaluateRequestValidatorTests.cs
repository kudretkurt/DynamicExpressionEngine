using DynamicExpressionEngine.Core.Abstractions;
using DynamicExpressionEngine.Core.Models;
using DynamicExpressionEngine.Core.Models.Requests;
using DynamicExpressionEngine.Core.Validation;
using FluentAssertions;

namespace DynamicExpressionEngine.Tests;

public sealed class EvaluateRequestValidatorTests
{
    private sealed class TestFunctionCatalog : IFunctionCatalog
    {
        private readonly HashSet<string> _allowed;

        public TestFunctionCatalog(IEnumerable<string>? allowed = null)
        {
            _allowed = new HashSet<string>(allowed ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlyList<FunctionInfo> GetAll() => Array.Empty<FunctionInfo>();

        public bool IsAllowed(string name) => _allowed.Contains(name);

        public FunctionInfo? Get(string name) => null;
    }

    private static EvaluateRequest CreateValidRequest() => new()
    {
        Expression = "ToString([x])",
        Data = new Dictionary<string, object?> { ["x"] = 1 }
    };

    [Fact]
    public void Empty_request_returns_required_messages()
    {
        var validator = new EvaluateRequestValidator(new TestFunctionCatalog(new[] { "ToString" }));

        // Model binder genelde null model yerine default instance üretir.
        var result = validator.Validate(new EvaluateRequest());

        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.ErrorMessage).Should().Contain("Expression is required");
        // Data property default olarak new() olduğu için "Data is required" beklemiyoruz.
    }

    [Fact]
    public void Empty_expression_is_invalid()
    {
        var validator = new EvaluateRequestValidator(new TestFunctionCatalog(new[] { "ToString" }));
        var req = CreateValidRequest();
        req.Expression = "";

        var result = validator.Validate(req);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.ErrorMessage).Should().Contain("Expression is required");
    }

    [Fact]
    public void Expression_longer_than_2000_is_invalid()
    {
        var validator = new EvaluateRequestValidator(new TestFunctionCatalog(new[] { "ToString" }));
        var req = CreateValidRequest();
        req.Expression = new string('a', 2001);

        var result = validator.Validate(req);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.ErrorMessage)
            .Should().Contain("Expression is too long (max 2000)");
    }

    [Theory]
    [InlineData("{1}")]
    [InlineData("}")]
    [InlineData(";")]
    [InlineData("\\")]
    public void Expression_with_forbidden_characters_is_invalid(string expr)
    {
        var validator = new EvaluateRequestValidator(new TestFunctionCatalog(new[] { "ToString" }));
        var req = CreateValidRequest();
        req.Expression = expr;

        var result = validator.Validate(req);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.ErrorMessage)
            .Should().Contain("Expression contains forbidden characters");
    }

    [Fact]
    public void Null_data_is_invalid()
    {
        var validator = new EvaluateRequestValidator(new TestFunctionCatalog(new[] { "ToString" }));
        var req = CreateValidRequest();
        req.Data = null!;

        var result = validator.Validate(req);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.ErrorMessage).Should().Contain("Data is required");
    }

    [Fact]
    public void Data_with_more_than_200_root_keys_is_invalid()
    {
        var validator = new EvaluateRequestValidator(new TestFunctionCatalog(new[] { "ToString" }));
        var req = CreateValidRequest();

        req.Data = Enumerable.Range(0, 201).ToDictionary(i => $"k{i}", _ => (object?)1);
        req.Expression = "ToString([k0])";

        var result = validator.Validate(req);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.ErrorMessage).Should().Contain("Too many data fields (max 200)");
    }

    [Fact]
    public void Data_key_longer_than_120_is_invalid()
    {
        var validator = new EvaluateRequestValidator(new TestFunctionCatalog(new[] { "ToString" }));
        var req = CreateValidRequest();

        var longKey = new string('k', 121);
        req.Data = new Dictionary<string, object?> { [longKey] = 1 };
        req.Expression = $"ToString([{longKey}])";

        var result = validator.Validate(req);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.ErrorMessage).Should().Contain("Data key is too long (max 120)");
    }

    [Fact]
    public void More_than_50_referenced_parameters_is_invalid()
    {
        var validator = new EvaluateRequestValidator(new TestFunctionCatalog(new[] { "ToString" }));
        var req = CreateValidRequest();

        // Build an expression referencing 51 distinct parameters: [p0] + [p1] + ... + [p50]
        var parameters = Enumerable.Range(0, 51).Select(i => $"[p{i}]").ToArray();
        req.Expression = string.Join(" + ", parameters);
        req.Data = Enumerable.Range(0, 51).ToDictionary(i => $"p{i}", _ => (object?)1);

        var result = validator.Validate(req);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.ErrorMessage)
            .Should().Contain("Too many parameters referenced (max 50). ".Trim());
        // NOTE: message in validator ends with '.'; keep exact match below
        result.Errors.Select(e => e.ErrorMessage)
            .Should().Contain("Too many parameters referenced (max 50).");
    }

    [Fact]
    public void Missing_parameter_is_invalid_and_reports_first_missing_only()
    {
        var validator = new EvaluateRequestValidator(new TestFunctionCatalog(new[] { "ToString" }));
        var req = CreateValidRequest();

        req.Expression = "ToString([missing1] + [missing2])";
        req.Data = new Dictionary<string, object?> { ["x"] = 1 };

        var result = validator.Validate(req);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.ErrorMessage).Should().Contain("Missing parameter 'missing1'");
        result.Errors.Select(e => e.ErrorMessage).Should().NotContain("Missing parameter 'missing2'");
    }

    [Fact]
    public void Nested_path_parameter_resolves_successfully()
    {
        var validator = new EvaluateRequestValidator(new TestFunctionCatalog(new[] { "ToString" }));
        var req = CreateValidRequest();

        req.Expression = "ToString([person.name])";
        req.Data = new Dictionary<string, object?>
        {
            ["person"] = new Dictionary<string, object?> { ["name"] = "Ali" }
        };

        var result = validator.Validate(req);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void More_than_50_distinct_function_calls_is_invalid()
    {
        // Allow everything so we only hit the count limit.
        var allowed = Enumerable.Range(0, 60).Select(i => $"F{i}").Append("ToString");
        var validator = new EvaluateRequestValidator(new TestFunctionCatalog(allowed));
        var req = CreateValidRequest();

        // 51 distinct function names, each called once.
        var functions = Enumerable.Range(0, 51).Select(i => $"F{i}(1)").ToArray();
        req.Expression = string.Join(" + ", functions);
        req.Data = new Dictionary<string, object?> { ["x"] = 1 };

        var result = validator.Validate(req);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.ErrorMessage)
            .Should().Contain("Too many function calls (max 50).");
    }

    [Fact]
    public void Unsupported_function_is_invalid()
    {
        var validator = new EvaluateRequestValidator(new TestFunctionCatalog(new[] { "ToString" }));
        var req = CreateValidRequest();

        req.Expression = "Hack(1)";

        var result = validator.Validate(req);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.ErrorMessage).Should().Contain("Unsupported function 'Hack'");
    }

    [Fact]
    public void Valid_request_is_valid()
    {
        var validator = new EvaluateRequestValidator(new TestFunctionCatalog(new[] { "ToString" }));
        var req = CreateValidRequest();

        var result = validator.Validate(req);

        result.IsValid.Should().BeTrue();
    }
}
