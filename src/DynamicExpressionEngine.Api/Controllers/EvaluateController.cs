using DynamicExpressionEngine.Core.Abstractions;
using DynamicExpressionEngine.Core.Models;
using DynamicExpressionEngine.Core.Models.Requests;
using DynamicExpressionEngine.Core.Models.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DynamicExpressionEngine.Api.Controllers;

[ApiController]
[Route("api/evaluate")]
public sealed class EvaluateController(ExpressionEngineBase engine) : ControllerBase
{
    [HttpPost]
    [Authorize]
    public IActionResult Evaluate([FromBody] EvaluateRequest request)
    {
        var result = engine.Evaluate(new EvaluationContext
        {
            Expression = request.Expression,
            Data = request.Data
        });

        if (!result.IsSuccess)
            return BadRequest(new EvaluateResponse { Error = result.Error });

        return Ok(new EvaluateResponse { Result = result.Result });
    }
}