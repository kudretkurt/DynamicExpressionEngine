using DynamicExpressionEngine.Core.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DynamicExpressionEngine.Api.Controllers;

[ApiController]
[Route("api/functions")]
public sealed class FunctionsController(IFunctionCatalog functionCatalog) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public IActionResult Get()
    {
        return Ok(new
        {
            functions = functionCatalog.GetAll().Select(f => new
            {
                name = f.Name,
                description = f.Description,
                parameters = f.Parameters,
                example = f.Example
            })
        });
    }
}