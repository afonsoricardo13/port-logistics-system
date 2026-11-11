using Application.DTO;
using Application.Services;
using Domain.IRepository;
using Domain.Model;
using Microsoft.AspNetCore.Mvc;
using WebApi.Authorization;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[RequirePermission("vessel-visit-executions")]
public class VesselVisitExecutionsController : ControllerBase
{
    private readonly VesselVisitExecutionService _service;
    private readonly IVesselVisitExecutionRepository _repository;
    private readonly ILogger<VesselVisitExecutionsController> _logger;

    public VesselVisitExecutionsController(
        VesselVisitExecutionService service,
        IVesselVisitExecutionRepository repository,
        ILogger<VesselVisitExecutionsController> logger)
    {
        _service = service;
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Get vessel visit executions with optional filters
    /// </summary>
    /// <param name="vvnId">Filter by vessel visit notification ID</param>
    /// <param name="status">Filter by execution status (InProgress, Completed)</param>
    /// <param name="startDate">Filter by arrival date (from)</param>
    /// <param name="endDate">Filter by arrival date (to)</param>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<VesselVisitExecutionDTO>>> GetAll(
        [FromQuery] string? vvnId = null,
        [FromQuery] VesselVisitExecutionStatus? status = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        IEnumerable<VesselVisitExecutionDTO> executions;

        // Apply primary filter
        if (!string.IsNullOrEmpty(vvnId))
        {
            executions = await _service.GetByVvnIdAsync(vvnId);
        }
        else if (status.HasValue)
        {
            executions = await _service.GetByStatusAsync(status.Value);
        }
        else if (startDate.HasValue && endDate.HasValue)
        {
            executions = await _service.GetByDateRangeAsync(startDate.Value, endDate.Value);
        }
        else
        {
            executions = await _service.GetAllAsync();
        }

        return Ok(executions);
    }

    /// <summary>
    /// US 4.1.10: Search and list VVEs over a given period or for a specific vessel with execution metrics
    /// </summary>
    /// <param name="startDate">Start date for filtering (optional)</param>
    /// <param name="endDate">End date for filtering (optional)</param>
    /// <param name="vesselImo">Vessel IMO number to filter by (optional, partial match)</param>
    /// <param name="status">Execution status to filter by (optional)</param>
    /// <returns>List of VVEs with calculated execution metrics</returns>
    [HttpGet("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<VesselVisitExecutionSearchResultDTO>>> SearchWithMetrics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? vesselImo = null,
        [FromQuery] VesselVisitExecutionStatus? status = null)
    {
        try
        {
            _logger.LogInformation("US 4.1.10: Searching VVEs - Period: {StartDate} to {EndDate}, Vessel: {Vessel}, Status: {Status}",
                startDate, endDate, vesselImo, status);

            // Validate date range if provided
            if (startDate.HasValue && endDate.HasValue && startDate > endDate)
            {
                return BadRequest(new { message = "Start date cannot be after end date" });
            }

            var results = await _service.SearchWithMetricsAsync(startDate, endDate, vesselImo, status);

            _logger.LogInformation("✅ US 4.1.10: Returning {Count} VVE search results", results.Count());
            
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ US 4.1.10: Error searching VVEs");
            return StatusCode(500, new { message = "An error occurred while searching vessel visit executions", error = ex.Message });
        }
    }

    /// <summary>
    /// Get vessel visit execution by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VesselVisitExecutionDTO>> GetById(string id)
    {
        var execution = await _service.GetByIdAsync(id);
        if (execution == null)
        {
            _logger.LogWarning("VVE with ID {Id} not found", id);
            return NotFound(new { message = $"Vessel visit execution with ID {id} not found" });
        }

        return Ok(execution);
    }

    /// <summary>
    /// US 4.1.7: Create a new vessel visit execution
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VesselVisitExecutionDTO>> Create([FromBody] CreateVveDTO dto)
    {
        try
        {
            // Get authenticated user email from header
            var userEmail = Request.Headers["X-User-Email"].ToString();
            if (string.IsNullOrEmpty(userEmail))
            {
                userEmail = "system";
            }

            // Call service to create VVE
            var created = await _service.CreateAsync(dto, userEmail);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Validation error creating VVE: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error creating VVE");
            return BadRequest(new { message = "Error creating vessel visit execution", error = ex.Message });
        }
    }

    /// <summary>
    /// US 4.1.8: Update VVE with actual berth time and dock
    /// </summary>
    [HttpPatch("{id}/berthing")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VesselVisitExecutionDTO>> UpdateBerthing(string id, [FromBody] UpdateVveBerthingDTO dto)
    {
        try
        {
            // Get authenticated user email from header
            var userEmail = Request.Headers["X-User-Email"].ToString();
            if (string.IsNullOrEmpty(userEmail))
            {
                userEmail = "system";
            }

            var updated = await _service.UpdateBerthingAsync(id, dto, userEmail);
            if (updated == null)
            {
                return NotFound(new { message = $"Vessel visit execution with ID {id} not found" });
            }
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating berthing for VVE {Id}", id);
            return StatusCode(500, new { message = "An error occurred while updating berthing information" });
        }
    }

    /// <summary>
    /// US 4.1.11: Complete a Vessel Visit Execution
    /// </summary>
    [HttpPost("{id}/complete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VesselVisitExecutionDTO>> Complete(string id, [FromBody] CompleteVveDTO dto)
    {
        try
        {
            // Get authenticated user email from header
            var userEmail = Request.Headers["X-User-Email"].ToString();
            if (string.IsNullOrEmpty(userEmail))
            {
                userEmail = "system";
            }

            var completed = await _service.CompleteAsync(id, dto, userEmail);
            if (completed == null)
            {
                return NotFound(new { message = $"Vessel visit execution with ID {id} not found" });
            }

            return Ok(completed);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Cannot complete VVE {Id}: {Message}", id, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Validation error completing VVE {Id}: {Message}", id, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error completing VVE {Id}", id);
            return StatusCode(500, new { message = "An error occurred while completing the vessel visit execution" });
        }
    }

    
    /// Update an existing vessel visit execution
    
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VesselVisitExecution>> Update(string id, [FromBody] VesselVisitExecution vve)
    {
        try
        {
            if (id != vve.Id)
            {
                return BadRequest(new { message = "ID mismatch" });
            }

            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound(new { message = $"Vessel visit execution with ID {id} not found" });
            }

            var updated = await _repository.UpdateAsync(vve);
            _logger.LogInformation("VVE with ID {Id} updated", id);

            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating VVE with ID {Id}", id);
            return BadRequest(new { message = "Error updating vessel visit execution", error = ex.Message });
        }
    }

   
    /// Partially update a vessel visit execution (e.g., change status to completed)
    
    [HttpPatch("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VesselVisitExecution>> Patch(string id, [FromBody] VesselVisitExecutionPatchDto patchDto)
    {
        try
        {
            var vve = await _repository.GetByIdAsync(id);
            if (vve == null)
            {
                return NotFound(new { message = $"Vessel visit execution with ID {id} not found" });
            }

            // Update status if provided
            if (patchDto.Status.HasValue)
            {
                if (patchDto.Status.Value == VesselVisitExecutionStatus.Completed)
                {
                    if (vve.Status == VesselVisitExecutionStatus.Completed)
                    {
                        return BadRequest(new { message = "Vessel visit execution is already completed" });
                    }

                    // Validate that all required fields are filled
                    if (!vve.ActualArrivalTime.HasValue || !vve.ActualBerthTime.HasValue ||
                        !vve.ActualUnberthTime.HasValue || !vve.ActualDepartureTime.HasValue)
                    {
                        return BadRequest(new { message = "Cannot complete VVE: missing required timestamps" });
                    }
                }
                
                vve.Status = patchDto.Status.Value;
            }

            // Update other fields if provided
            if (patchDto.ActualBerthTime.HasValue)
                vve.ActualBerthTime = patchDto.ActualBerthTime;
            
            if (patchDto.ActualUnberthTime.HasValue)
                vve.ActualUnberthTime = patchDto.ActualUnberthTime;
            
            if (patchDto.ActualDepartureTime.HasValue)
                vve.ActualDepartureTime = patchDto.ActualDepartureTime;
            
            if (!string.IsNullOrEmpty(patchDto.DockUsed))
                vve.DockUsed = patchDto.DockUsed;
            
            if (!string.IsNullOrEmpty(patchDto.Notes))
                vve.Notes = patchDto.Notes;

            var updated = await _repository.UpdateAsync(vve);
            _logger.LogInformation("VVE with ID {Id} partially updated", id);

            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating VVE with ID {Id}", id);
            return BadRequest(new { message = "Error updating vessel visit execution", error = ex.Message });
        }
    }
}
