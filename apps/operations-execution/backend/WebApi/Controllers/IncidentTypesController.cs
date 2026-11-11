using Application.DTO;
using Application.Services;
using Microsoft.AspNetCore.Mvc;
using WebApi.Authorization;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[RequirePermission("incident-types")]
public class IncidentTypesController : ControllerBase
{
    private readonly IncidentTypeService _service;
    private readonly ILogger<IncidentTypesController> _logger;

    public IncidentTypesController(
        IncidentTypeService service,
        ILogger<IncidentTypesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Get incident types with optional filters
    /// </summary>
    /// <param name="rootOnly">Return only root types (no parent)</param>
    /// <param name="parentCode">Filter by parent type code (get children)</param>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<IncidentTypeDTO>>> GetAll(
        [FromQuery] bool? rootOnly = null,
        [FromQuery] string? parentCode = null)
    {
        var types = await _service.GetAllAsync(rootOnly, parentCode);
        return Ok(types);
    }

    /// <summary>
    /// Get incident type by code
    /// </summary>
    [HttpGet("{code}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentTypeDTO>> GetByCode(string code)
    {
        var type = await _service.GetByCodeAsync(code);
        if (type == null)
            return NotFound(new { message = $"Incident type with code {code} not found" });

        return Ok(type);
    }

    /// <summary>
    /// Create a new incident type
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IncidentTypeDTO>> Create([FromBody] CreateIncidentTypeDTO dto)
    {
        try
        {
            var created = await _service.CreateAsync(dto);
            _logger.LogInformation("Incident type created with code {Code}", created.Code);
            return CreatedAtAction(nameof(GetByCode), new { code = created.Code }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating incident type");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update an existing incident type
    /// </summary>
    [HttpPut("{code}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentTypeDTO>> Update(string code, [FromBody] UpdateIncidentTypeDTO dto)
    {
        try
        {
            var updated = await _service.UpdateAsync(code, dto);
            _logger.LogInformation("Incident type with code {Code} updated", code);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating incident type");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete an incident type
    /// </summary>
    [HttpDelete("{code}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string code)
    {
        try
        {
            var deleted = await _service.DeleteAsync(code);
            if (!deleted)
                return NotFound(new { message = $"Incident type with code {code} not found" });

            _logger.LogInformation("Incident type with code {Code} deleted", code);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting incident type");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
