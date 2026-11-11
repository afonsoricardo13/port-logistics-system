using Application.DTO;
using Application.Services;
using Microsoft.AspNetCore.Mvc;
using WebApi.Authorization;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[RequirePermission("complementary-tasks")]
public class ComplementaryTasksController : ControllerBase
{
    private readonly ComplementaryTaskService _service;
    private readonly ILogger<ComplementaryTasksController> _logger;

    public ComplementaryTasksController(
        ComplementaryTaskService service,
        ILogger<ComplementaryTasksController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Get complementary tasks with optional filters
    /// </summary>
    /// <param name="vveId">Filter by vessel visit execution ID</param>
    /// <param name="status">Filter by task status (Ongoing, Completed)</param>
    /// <param name="startDate">Filter by start date (from)</param>
    /// <param name="endDate">Filter by end date (to)</param>
    /// <param name="ongoingOnly">Filter to show only ongoing tasks</param>
    /// <param name="impactType">Filter by impact type (Parallel, Blocking)</param>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ComplementaryTaskDTO>>> GetAll(
        [FromQuery] string? vveId = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] bool? ongoingOnly = null,
        [FromQuery] string? impactType = null)
    {
        try
        {
            var filter = new ComplementaryTaskFilterDTO
            {
                VveId = vveId,
                Status = status,
                StartDate = startDate,
                EndDate = endDate,
                OngoingOnly = ongoingOnly,
                ImpactType = impactType
            };

            var tasks = await _service.GetAllAsync(filter);
            return Ok(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving complementary tasks");
            return BadRequest(new { message = "Error retrieving tasks", error = ex.Message });
        }
    }

    /// <summary>
    /// Get ongoing tasks that are currently impacting operations
    /// </summary>
    [HttpGet("ongoing")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ComplementaryTaskDTO>>> GetOngoingTasks()
    {
        try
        {
            var tasks = await _service.GetOngoingTasksAsync();
            return Ok(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ongoing tasks");
            return BadRequest(new { message = "Error retrieving ongoing tasks", error = ex.Message });
        }
    }

    /// <summary>
    /// Get complementary task by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ComplementaryTaskDTO>> GetById(Guid id)
    {
        try
        {
            var task = await _service.GetByIdAsync(id);
            if (task == null)
                return NotFound(new { message = $"Task with ID {id} not found" });

            return Ok(task);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving task {Id}", id);
            return BadRequest(new { message = "Error retrieving task", error = ex.Message });
        }
    }

    /// <summary>
    /// Get tasks by VVE ID
    /// </summary>
    [HttpGet("vve/{vveId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ComplementaryTaskDTO>>> GetByVveId(string vveId)
    {
        try
        {
            var tasks = await _service.GetByVveIdAsync(vveId);
            return Ok(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tasks for VVE {VveId}", vveId);
            return BadRequest(new { message = "Error retrieving tasks", error = ex.Message });
        }
    }

    /// <summary>
    /// Create a new complementary task
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ComplementaryTaskDTO>> Create([FromBody] CreateComplementaryTaskDTO dto)
    {
        try
        {
            var created = await _service.CreateAsync(dto);
            _logger.LogInformation("Complementary task created with ID {Id}", created.Id);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error creating task");
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument creating task");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating task");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing complementary task
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ComplementaryTaskDTO>> Update(Guid id, [FromBody] UpdateComplementaryTaskDTO dto)
    {
        try
        {
            var updated = await _service.UpdateAsync(id, dto);
            _logger.LogInformation("Task with ID {Id} updated", id);
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error updating task {Id}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument updating task {Id}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating task {Id}", id);
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a complementary task
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id)
    {
        try
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { message = $"Task with ID {id} not found" });

            _logger.LogInformation("Task with ID {Id} deleted", id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Task {Id} not found for deletion", id);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting task {Id}", id);
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }
}
