using Application.DTO;
using Application.Services;
using Microsoft.AspNetCore.Mvc;
using WebApi.Authorization;

namespace WebApi.Controllers;

/// <summary>
/// US 4.1.9: API Controller for managing executed operations within vessel visit executions
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[RequirePermission("executed-operations")]
public class ExecutedOperationsController : ControllerBase
{
    private readonly ExecutedOperationService _service;
    private readonly ILogger<ExecutedOperationsController> _logger;

    public ExecutedOperationsController(
        ExecutedOperationService service,
        ILogger<ExecutedOperationsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// US 4.1.9: Get all executed operations for a specific VVE
    /// </summary>
    [HttpGet("vve/{vveId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ExecutedOperationDTO>>> GetByVveId(string vveId)
    {
        var operations = await _service.GetOperationsByVveIdAsync(vveId);
        return Ok(operations);
    }

    /// <summary>
    /// US 4.1.9: Get executed operation by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExecutedOperationDTO>> GetById(Guid id)
    {
        var operation = await _service.GetByIdAsync(id);
        if (operation == null)
            return NotFound($"Executed operation {id} not found");

        return Ok(operation);
    }

    /// <summary>
    /// US 4.1.9: Initialize executed operations from an operation plan
    /// This creates ExecutedOperation records from the planned operations for easy tracking
    /// </summary>
    [HttpPost("initialize-from-plan")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InitializeOperationsFromPlanResponseDTO>> InitializeFromPlan(
        [FromBody] InitializeFromPlanRequest request)
    {
        var userEmail = Request.Headers["X-User-Email"].ToString();
        if (string.IsNullOrEmpty(userEmail))
            userEmail = "system";

        var response = await _service.InitializeOperationsFromPlanAsync(
            request.VveId, 
            request.OperationPlanId, 
            userEmail);

        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }

    /// <summary>
    /// US 4.1.9: Create a new executed operation manually
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ExecutedOperationDTO>> Create(
        [FromBody] CreateExecutedOperationDTO dto)
    {
        var userEmail = Request.Headers["X-User-Email"].ToString();
        if (string.IsNullOrEmpty(userEmail))
            userEmail = "system";

        try
        {
            var created = await _service.CreateAsync(dto, userEmail);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// US 4.1.9: Update an executed operation with actual start/end times and resource usage
    /// The operator can confirm or modify times and resources
    /// Status is automatically determined based on timing
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ExecutedOperationDTO>> Update(
        Guid id,
        [FromBody] UpdateExecutedOperationDTO dto)
    {
        var userEmail = Request.Headers["X-User-Email"].ToString();
        if (string.IsNullOrEmpty(userEmail))
            userEmail = "system";

        try
        {
            var updated = await _service.UpdateAsync(id, dto, userEmail);
            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Delete an executed operation
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _service.DeleteAsync(id);
        if (!success)
            return NotFound($"Executed operation {id} not found");

        return NoContent();
    }
}

/// <summary>
/// Request DTO for initializing operations from a plan
/// </summary>
public class InitializeFromPlanRequest
{
    public required string VveId { get; set; }
    public Guid OperationPlanId { get; set; }
}
