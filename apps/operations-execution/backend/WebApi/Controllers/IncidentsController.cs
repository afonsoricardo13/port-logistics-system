using Application.DTO;
using Application.Services;
using Microsoft.AspNetCore.Mvc;
using WebApi.Authorization;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[RequirePermission("incidents")]
public class IncidentsController : ControllerBase
{
    private readonly IncidentService _service;
    private readonly ILogger<IncidentsController> _logger;

    public IncidentsController(
        IncidentService service,
        ILogger<IncidentsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Get incidents with optional filters
    /// </summary>
    /// <param name="status">Filter by status (Active, Resolved)</param>
    /// <param name="severity">Filter by severity (Minor, Major, Critical)</param>
    /// <param name="startDate">Filter by start date (from)</param>
    /// <param name="endDate">Filter by end date (to)</param>
    /// <param name="activeOnly">Show only active incidents</param>
    /// <param name="vesselVisitId">Filter by vessel visit execution ID</param>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<IncidentDTO>>> GetAll(
        [FromQuery] string? status = null,
        [FromQuery] string? severity = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] bool? activeOnly = null,
        [FromQuery] string? vesselVisitId = null)
    {
        try
        {
            var filter = new IncidentFilterDTO
            {
                Status = status,
                Severity = severity,
                StartDate = startDate,
                EndDate = endDate,
                ActiveOnly = activeOnly,
                VesselVisitId = vesselVisitId
            };

            var incidents = await _service.GetAllAsync(filter);
            return Ok(incidents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving incidents");
            return BadRequest(new { message = "Error retrieving incidents", error = ex.Message });
        }
    }

    /// <summary>
    /// Get incident by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentDTO>> GetById(Guid id)
    {
        try
        {
            var incident = await _service.GetByIdAsync(id);
            if (incident == null)
                return NotFound(new { message = $"Incident with ID {id} not found" });

            return Ok(incident);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving incident {Id}", id);
            return BadRequest(new { message = "Error retrieving incident", error = ex.Message });
        }
    }

    /// <summary>
    /// Create a new incident
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IncidentDTO>> Create([FromBody] CreateIncidentDTO dto)
    {
        try
        {
            // Get user from header (dev auth)
            var userEmail = Request.Headers["X-User-Email"].FirstOrDefault();
            if (!string.IsNullOrEmpty(userEmail))
                dto.CreatedBy = userEmail;

            if (string.IsNullOrEmpty(dto.CreatedBy))
                dto.CreatedBy = "system";

            var created = await _service.CreateAsync(dto);
            _logger.LogInformation("Incident created with ID {Id}", created.Id);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation creating incident");
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument creating incident");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating incident");
            return BadRequest(new { message = "Error creating incident", error = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing incident
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IncidentDTO>> Update(Guid id, [FromBody] UpdateIncidentDTO dto)
    {
        try
        {
            var updated = await _service.UpdateAsync(id, dto);
            _logger.LogInformation("Incident with ID {Id} updated", id);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Incident not found: {Id}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation updating incident");
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument updating incident");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating incident {Id}", id);
            return BadRequest(new { message = "Error updating incident", error = ex.Message });
        }
    }

    /// <summary>
    /// Mark an incident as resolved
    /// </summary>
    [HttpPatch("{id:guid}/resolve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IncidentDTO>> Resolve(Guid id, [FromBody] ResolveIncidentDTO? dto = null)
    {
        try
        {
            var updated = await _service.ResolveAsync(id, dto?.EndTime);
            _logger.LogInformation("Incident with ID {Id} resolved", id);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Incident not found: {Id}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation resolving incident");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving incident {Id}", id);
            return BadRequest(new { message = "Error resolving incident", error = ex.Message });
        }
    }

    /// <summary>
    /// Update affected VVEs for an incident (associate/detach)
    /// </summary>
    [HttpPut("{id:guid}/affected-vves")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IncidentDTO>> UpdateAffectedVVEs(Guid id, [FromBody] UpdateAffectedVVEsDTO dto)
    {
        try
        {
            var updated = await _service.UpdateAffectedVVEsAsync(id, dto.VveIds);
            _logger.LogInformation("Incident with ID {Id} affected VVEs updated", id);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Incident not found: {Id}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation updating affected VVEs");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating affected VVEs for incident {Id}", id);
            return BadRequest(new { message = "Error updating affected VVEs", error = ex.Message });
        }
    }

    /// <summary>
    /// Delete an incident
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { message = $"Incident with ID {id} not found" });

            _logger.LogInformation("Incident with ID {Id} deleted", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting incident {Id}", id);
            return BadRequest(new { message = "Error deleting incident", error = ex.Message });
        }
    }

    /// <summary>
    /// Get active incidents affecting a specific VVE
    /// </summary>
    [HttpGet("vessel-visit/{vveId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<IncidentDTO>>> GetActiveIncidentsForVVE(string vveId)
    {
        try
        {
            var incidents = await _service.GetActiveIncidentsForVVEAsync(vveId);
            return Ok(incidents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving incidents for VVE {VveId}", vveId);
            return BadRequest(new { message = "Error retrieving incidents", error = ex.Message });
        }
    }
}

/// <summary>
/// DTO for resolving an incident
/// </summary>
public class ResolveIncidentDTO
{
    public DateTime? EndTime { get; set; }
}

/// <summary>
/// DTO for updating affected VVEs
/// </summary>
public class UpdateAffectedVVEsDTO
{
    public List<string> VveIds { get; set; } = new();
}
