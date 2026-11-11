using Application.DTO;
using Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

/// <summary>
/// US 3.3.2: Port Layout API for 3D visualization
/// </summary>
[Route("api/port-layout")]
[ApiController]
public class PortLayoutController : ControllerBase
{
    private readonly PortLayoutService _service;
    private readonly ILogger<PortLayoutController> _logger;

    public PortLayoutController(PortLayoutService service, ILogger<PortLayoutController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// US 3.3.2: Get available port layout IDs
    /// </summary>
    /// <returns>List of layout IDs (A, B)</returns>
    [HttpGet("available")]
    public async Task<ActionResult<List<string>>> GetAvailableLayouts()
    {
        _logger.LogInformation("US 3.3.2: Listing available port layouts");
        var layouts = await _service.GetAvailableLayoutsAsync();
        return Ok(layouts);
    }

    /// <summary>
    /// US 3.3.2: Get port layout by ID for 3D visualization
    /// </summary>
    /// <param name="layoutId">Layout ID (A or B). Defaults to A if not specified.</param>
    /// <returns>Port layout with docks, warehouses, container yards and other elements</returns>
    /// <response code="200">Returns the port layout JSON</response>
    /// <response code="404">Layout ID not found</response>
    [HttpGet]
    [ProducesResponseType(typeof(PortLayoutDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PortLayoutDTO>> GetPortLayout([FromQuery] string layoutId = "A")
    {
        _logger.LogInformation("US 3.3.2: Retrieving port layout {LayoutId}", layoutId);
        
        var layout = await _service.GetLayoutAsync(layoutId);
        
        if (layout == null)
        {
            _logger.LogWarning("US 3.3.2: Layout {LayoutId} not found", layoutId);
            return NotFound(new { error = $"Layout '{layoutId}' not found", availableLayouts = new[] { "A", "B" } });
        }

        _logger.LogInformation("US 3.3.2: Returning layout {LayoutName} with {Count} elements", 
            layout.LayoutName, layout.Elements.Count);
        
        return Ok(layout);
    }

    /// <summary>
    /// US 3.3.2: Get specific layout by ID in path
    /// </summary>
    /// <param name="layoutId">Layout ID (A or B)</param>
    /// <returns>Port layout JSON</returns>
    [HttpGet("{layoutId}")]
    [ProducesResponseType(typeof(PortLayoutDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PortLayoutDTO>> GetPortLayoutById(string layoutId)
    {
        return await GetPortLayout(layoutId);
    }
}
