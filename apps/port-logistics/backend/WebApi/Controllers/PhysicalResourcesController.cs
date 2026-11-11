namespace Controllers;

using Application.DTO;
using Application.Services;
using Microsoft.AspNetCore.Mvc;
using WebApi.Authorization;

[ApiController]
[Route("api/[controller]")]
[RequirePermission("resources")]
public class PhysicalResourcesController : ControllerBase
{
    private readonly PhysicalResourceService _service;

    public PhysicalResourcesController(PhysicalResourceService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PhysicalResourceDTO>>> GetAll([FromQuery] string? code, [FromQuery] string? description, [FromQuery] string? type, [FromQuery] string? status)
    {
        var list = await _service.Search(code, description, type, status);
        return Ok(list);
    }

    [HttpGet("{code}")]
    public async Task<ActionResult<PhysicalResourceDTO>> GetByCode(string code)
    {
        var r = await _service.GetByCode(code);
        if (r == null) return NotFound();
        return Ok(r);
    }

    [HttpPost]
    public async Task<ActionResult<PhysicalResourceDTO>> Create(PhysicalResourceDTO dto)
    {
        var errors = new List<string>();
        var created = await _service.Add(dto, errors);
        if (created == null) return BadRequest(new { errors });
        return CreatedAtAction(nameof(GetByCode), new { code = created.Code }, created);
    }

    [HttpPut("{code}")]
    public async Task<IActionResult> Update(string code, PhysicalResourceDTO dto)
    {
        var errors = new List<string>();
        var ok = await _service.Update(code, dto, errors);
        if (!ok) return BadRequest(new { errors });
        return NoContent();
    }

    [HttpPatch("{code}")]
    public async Task<IActionResult> Patch(string code, [FromQuery] string? status, [FromQuery] string? changedBy)
    {
        var errors = new List<string>();
        
        if (string.IsNullOrWhiteSpace(status))
        {
            return BadRequest(new { errors = new[] { "Status parameter is required" } });
        }

        bool ok;
        if (status.Equals("inactive", StringComparison.OrdinalIgnoreCase))
        {
            ok = await _service.Deactivate(code, changedBy, errors);
        }
        else if (status.Equals("active", StringComparison.OrdinalIgnoreCase))
        {
            ok = await _service.Reactivate(code, changedBy, errors);
        }
        else
        {
            return BadRequest(new { errors = new[] { "Invalid status. Valid values: active, inactive" } });
        }

        if (!ok) return BadRequest(new { errors });
        return NoContent();
    }
}
