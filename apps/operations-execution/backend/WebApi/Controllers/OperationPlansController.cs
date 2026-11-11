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
[RequirePermission("operation-plans")]
public class OperationPlansController : ControllerBase
{
    private readonly IOperationPlanRepository _repository;
    private readonly OperationPlanService _operationPlanService;
    private readonly ILogger<OperationPlansController> _logger;

    public OperationPlansController(
        IOperationPlanRepository repository,
        OperationPlanService operationPlanService,
        ILogger<OperationPlansController> logger)
    {
        _repository = repository;
        _operationPlanService = operationPlanService;
        _logger = logger;
    }

    /// <summary>
    /// US 4.1.3: Search and list Operation Plans with filters and sorting
    /// </summary>
    /// <param name="vvnId">Filter by vessel visit notification ID</param>
    /// <param name="date">Filter by specific date</param>
    /// <param name="startDate">Filter by date range (from)</param>
    /// <param name="endDate">Filter by date range (to)</param>
    /// <param name="sortBy">Sort field: startTime, vesselId, delay (default: startTime)</param>
    /// <param name="sortOrder">Sort order: asc or desc (default: asc)</param>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<OperationPlanDetailDTO>>> GetAll(
        [FromQuery] string? vvnId = null,
        [FromQuery] DateTime? date = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string sortBy = "startTime",
        [FromQuery] string sortOrder = "asc")
    {
        IEnumerable<OperationPlan> plans;

        // Apply primary filter
        if (!string.IsNullOrEmpty(vvnId))
        {
            plans = await _repository.GetByVvnIdAsync(vvnId);
        }
        else if (date.HasValue)
        {
            plans = await _repository.GetByDateAsync(date.Value);
        }
        else if (startDate.HasValue && endDate.HasValue)
        {
            plans = await _repository.GetByDateRangeAsync(startDate.Value, endDate.Value);
        }
        else
        {
            plans = await _repository.GetAllAsync();
        }

        // Additional filtering
        if (!string.IsNullOrEmpty(vvnId))
            plans = plans.Where(p => p.VvnId == vvnId);
        
        if (date.HasValue)
            plans = plans.Where(p => p.Date.Date == date.Value.Date);
        
        if (startDate.HasValue)
            plans = plans.Where(p => p.Date >= startDate.Value);
        
        if (endDate.HasValue)
            plans = plans.Where(p => p.Date <= endDate.Value);

        // Convert to detailed DTOs
        var detailedPlans = await _operationPlanService.ConvertToDetailedDTOs(plans);

        // Apply sorting
        detailedPlans = sortBy.ToLower() switch
        {
            "vesselid" => sortOrder == "desc" 
                ? detailedPlans.OrderByDescending(p => p.VvnId) 
                : detailedPlans.OrderBy(p => p.VvnId),
            "endtime" => sortOrder == "desc" 
                ? detailedPlans.OrderByDescending(p => p.EndTime) 
                : detailedPlans.OrderBy(p => p.EndTime),
            "duration" => sortOrder == "desc" 
                ? detailedPlans.OrderByDescending(p => p.Duration) 
                : detailedPlans.OrderBy(p => p.Duration),
            "delay" => sortOrder == "desc" 
                ? detailedPlans.OrderByDescending(p => p.Delay) 
                : detailedPlans.OrderBy(p => p.Delay),
            _ => sortOrder == "desc" 
                ? detailedPlans.OrderByDescending(p => p.StartTime) 
                : detailedPlans.OrderBy(p => p.StartTime)
        };

        return Ok(detailedPlans);
    }

    /// <summary>
    /// US 4.1.9: Get operation plans by VVN (backward compatible route)
    /// </summary>
    [HttpGet("by-vvn/{vvnId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<OperationPlanDetailDTO>>> GetByVvnId(string vvnId)
    {
        var plans = await _repository.GetByVvnIdAsync(vvnId);
        var detailedPlans = await _operationPlanService.ConvertToDetailedDTOs(plans);
        return Ok(detailedPlans);
    }

    /// <summary>
    /// Get operation plan by ID with detailed information (US 4.1.4)
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OperationPlanDetailDTO>> GetById(Guid id)
    {
        var plan = await _repository.GetByIdAsync(id);
        if (plan == null)
        {
            _logger.LogWarning("Operation plan with ID {Id} not found", id);
            return NotFound(new { message = $"Operation plan with ID {id} not found" });
        }

        // Convert to detailed DTO
        var detailedPlans = await _operationPlanService.ConvertToDetailedDTOs(new[] { plan });
        var detailedPlan = detailedPlans.FirstOrDefault();

        if (detailedPlan == null)
        {
            _logger.LogWarning("Failed to convert operation plan with ID {Id} to detailed DTO", id);
            return NotFound(new { message = $"Operation plan with ID {id} not found" });
        }

        return Ok(detailedPlan);
    }

    /// <summary>
    /// Create a new operation plan
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OperationPlan>> Create([FromBody] OperationPlan operationPlan)
    {
        try
        {
            operationPlan.Id = Guid.NewGuid();
            operationPlan.CreatedAt = DateTime.UtcNow;
            
            // TODO: Get CreatedBy from authenticated user
            if (string.IsNullOrEmpty(operationPlan.CreatedBy))
            {
                operationPlan.CreatedBy = "system";
            }

            var created = await _repository.AddAsync(operationPlan);
            _logger.LogInformation("Operation plan created with ID {Id}", created.Id);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating operation plan");
            return BadRequest(new { message = "Error creating operation plan", error = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing operation plan
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OperationPlan>> Update(Guid id, [FromBody] OperationPlan operationPlan)
    {
        try
        {
            if (id != operationPlan.Id)
            {
                return BadRequest(new { message = "ID mismatch" });
            }

            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound(new { message = $"Operation plan with ID {id} not found" });
            }

            var updated = await _repository.UpdateAsync(operationPlan);
            _logger.LogInformation("Operation plan with ID {Id} updated", id);

            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating operation plan with ID {Id}", id);
            return BadRequest(new { message = "Error updating operation plan", error = ex.Message });
        }
    }

    /// <summary>
    /// US 4.1.4: Manually update an Operation Plan
    /// Allows logistics operators to make last-minute adjustments to resource assignments and timing
    /// Validates changes and alerts on inconsistencies with related VVNs and resources
    /// </summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UpdateOperationPlanResponseDTO>> UpdateOperationPlan(
        Guid id,
        [FromBody] UpdateOperationPlanRequestDTO request)
    {
        try
        {
            // Get user from header (DevAuthenticationMiddleware)
            var userEmail = Request.Headers["X-User-Email"].FirstOrDefault();
            
            if (string.IsNullOrEmpty(userEmail))
            {
                return BadRequest(new UpdateOperationPlanResponseDTO
                {
                    Success = false,
                    Message = "User authentication required. Missing X-User-Email header."
                });
            }

            // Set the user performing the update
            request.UpdatedBy = userEmail;

            _logger.LogInformation(
                "Updating operation plan {PlanId} by {User}. Reason: {Reason}",
                id,
                userEmail,
                request.ReasonForChange);

            var result = await _operationPlanService.UpdateOperationPlanAsync(id, request);

            if (!result.Success)
            {
                if (result.Message.Contains("not found"))
                {
                    return NotFound(result);
                }
                return BadRequest(result);
            }

            // Log warnings if any inconsistencies were found
            if (result.ResourceConflicts.Any() || result.VvnInconsistencies.Any())
            {
                _logger.LogWarning(
                    "Operation plan {PlanId} updated with warnings: {ConflictCount} resource conflicts, {InconsistencyCount} VVN inconsistencies",
                    id,
                    result.ResourceConflicts.Count,
                    result.VvnInconsistencies.Count);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating operation plan {PlanId}", id);
            return BadRequest(new UpdateOperationPlanResponseDTO
            {
                Success = false,
                Message = $"An error occurred while updating the operation plan: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// US 4.1.2: Generate Operation Plans for all VVNs scheduled for a given day
    /// Authorization: Controlled by RequirePermission("operation-plans") attribute
    /// </summary>
    /// <param name="request">Request containing the target date and algorithm</param>
    [HttpPost("generate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GenerateOperationPlansResponseDTO>> GenerateForDay(
        [FromBody] GenerateOperationPlansRequestDTO request)
    {
        try
        {
            // Get user from header (DevAuthenticationMiddleware)
            var userEmail = Request.Headers["X-User-Email"].FirstOrDefault() ?? "system";
            var userRole = Request.Headers["X-User-Role"].FirstOrDefault() ?? "Unknown";

            request.RequestedBy = userEmail;

            _logger.LogInformation(
                "Generating operation plans for date {Date} using algorithm {Algorithm} by {User} (Role: {Role})",
                request.Date.ToString("yyyy-MM-dd"),
                request.Algorithm,
                userEmail,
                userRole);

            var result = await _operationPlanService.GenerateOperationPlansForDayAsync(request);

            if (!result.Success)
            {
                _logger.LogWarning(
                    "Operation plan generation failed for date {Date}. Message: {Message}",
                    request.Date.ToString("yyyy-MM-dd"),
                    result.Message);
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating operation plans for date {Date}", request.Date);
            return BadRequest(new
            {
                success = false,
                message = "An error occurred while generating operation plans",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// US 4.1.5: Get VVNs without Operation Plans for a given date
    /// </summary>
    [HttpGet("missing-plans")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VvnsWithoutPlansResponseDTO>> GetVvnsWithoutPlans([FromQuery] DateTime date)
    {
        try
        {
            _logger.LogInformation(
                "Getting VVNs without Operation Plans for date {Date}",
                date.ToString("yyyy-MM-dd"));

            var vvnsWithoutPlans = await _operationPlanService.GetVvnsWithoutOperationPlansAsync(date);
            
            // Also get existing plans to provide complete statistics
            var existingPlans = await _repository.GetByDateAsync(date);
            
            var response = new VvnsWithoutPlansResponseDTO
            {
                Success = true,
                Message = vvnsWithoutPlans.Count > 0 
                    ? $"Found {vvnsWithoutPlans.Count} VVN(s) without Operation Plans" 
                    : "All Approved VVNs have Operation Plans",
                Date = date,
                TotalApprovedVvns = vvnsWithoutPlans.Count + existingPlans.Count(),
                VvnsWithPlans = existingPlans.Count(),
                VvnsWithoutPlans = vvnsWithoutPlans.Count,
                MissingPlans = vvnsWithoutPlans.Select(vvn => new VvnWithoutPlanDTO
                {
                    Id = vvn.Id,
                    VesselImoNumber = vvn.VesselImoNumber,
                    EstimatedTimeOfArrival = vvn.EstimatedTimeOfArrival,
                    EstimatedTimeOfDeparture = vvn.EstimatedTimeOfDeparture,
                    AssignedDockCode = vvn.AssignedDockCode
                }).ToList()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting VVNs without Operation Plans for date {Date}", date);
            return BadRequest(new VvnsWithoutPlansResponseDTO
            {
                Success = false,
                Message = "Error retrieving VVNs without Operation Plans",
                Date = date
            });
        }
    }

    /// <summary>
    /// US 4.1.5: Regenerate all Operation Plans for a given day
    /// WARNING: This will DELETE existing plans and create new ones
    /// </summary>
    [HttpPost("regenerate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GenerateOperationPlansResponseDTO>> RegenerateForDay(
        [FromBody] GenerateOperationPlansRequestDTO request)
    {
        try
        {
            // Get user from header
            var userEmail = Request.Headers["X-User-Email"].FirstOrDefault() ?? "system";
            var userRole = Request.Headers["X-User-Role"].FirstOrDefault() ?? "Unknown";

            request.RequestedBy = userEmail;

            _logger.LogWarning(
                "⚠️ REGENERATING operation plans for date {Date} using algorithm {Algorithm} by {User} (Role: {Role}). EXISTING PLANS WILL BE DELETED!",
                request.Date.ToString("yyyy-MM-dd"),
                request.Algorithm,
                userEmail,
                userRole);

            var result = await _operationPlanService.RegenerateOperationPlansForDayAsync(request);

            if (!result.Success)
            {
                _logger.LogWarning(
                    "Operation plan regeneration failed for date {Date}. Message: {Message}",
                    request.Date.ToString("yyyy-MM-dd"),
                    result.Message);
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating operation plans for date {Date}", request.Date);
            return BadRequest(new GenerateOperationPlansResponseDTO
            {
                Success = false,
                Message = "An error occurred while regenerating operation plans",
                Date = request.Date,
                PlansGenerated = 0,
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// US 4.1.4: Clear change history logs for an Operation Plan
    /// </summary>
    [HttpDelete("{id:guid}/change-logs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> ClearChangeLogs(Guid id)
    {
        try
        {
            var plan = await _repository.GetByIdAsync(id);
            if (plan == null)
            {
                return NotFound(new { success = false, message = "Operation plan not found" });
            }

            plan.ChangeLogs = null;
            await _repository.UpdateAsync(plan);

            _logger.LogInformation("Change logs cleared for operation plan {PlanId}", id);

            return Ok(new { success = true, message = "Change history cleared successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing change logs for operation plan {PlanId}", id);
            return BadRequest(new { success = false, message = "Error clearing change logs", error = ex.Message });
        }
    }

    /// <summary>
    /// US 4.1.6: Get aggregated resource usage (total allocated time and number of operations) for a resource in a period
    /// </summary>
    [HttpGet("resource-usage")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResourceUsageResultDTO>> GetResourceUsage(
        [FromQuery] string resourceType,
        [FromQuery] string resourceId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(resourceType) || string.IsNullOrWhiteSpace(resourceId))
            {
                return BadRequest(new { success = false, message = "resourceType and resourceId are required" });
            }

            var request = new ResourceUsageRequestDTO
            {
                ResourceType = resourceType,
                ResourceId = resourceId,
                StartDate = startDate,
                EndDate = endDate
            };

            var result = await _operationPlanService.GetResourceUsageAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting resource usage for {ResourceType} {ResourceId}", resourceType, resourceId);
            return BadRequest(new { success = false, message = "Error retrieving resource usage", error = ex.Message });
        }
    }
}
