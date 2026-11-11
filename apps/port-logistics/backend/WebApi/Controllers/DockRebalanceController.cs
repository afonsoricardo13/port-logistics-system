using Microsoft.AspNetCore.Mvc;
using Application.DTO;
using Application.Services;

namespace WebApi.Controllers;

/// <summary>
/// US 4.3.3 - Controller for dock rebalancing operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DockRebalanceController : ControllerBase
{
    private readonly DockRebalanceService _rebalanceService;
    private readonly ILogger<DockRebalanceController> _logger;

    public DockRebalanceController(
        DockRebalanceService rebalanceService,
        ILogger<DockRebalanceController> logger)
    {
        _rebalanceService = rebalanceService;
        _logger = logger;
    }

    /// <summary>
    /// Generates a dock rebalancing proposal comparing current vs proposed allocations
    /// </summary>
    /// <param name="date">Target date (format: yyyy-MM-dd)</param>
    /// <param name="officerId">ID of the requesting officer</param>
    [HttpGet("proposal")]
    public async Task<ActionResult<DockRebalanceProposalDTO>> GetProposal(
        [FromQuery] string date,
        [FromQuery] string officerId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(date))
            {
                return BadRequest("Date is required");
            }

            if (!DateTime.TryParse(date, out _))
            {
                return BadRequest("Invalid date format. Use yyyy-MM-dd");
            }

            _logger.LogInformation("Generating dock rebalance proposal for date {Date}, officer {OfficerId}", date, officerId);

            var request = new DockRebalanceRequestDTO
            {
                Date = date,
                OfficerId = officerId ?? "system"
            };

            var proposal = await _rebalanceService.GenerateRebalanceProposal(request);
            return Ok(proposal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating dock rebalance proposal");
            return StatusCode(500, new { error = "Failed to generate rebalance proposal", details = ex.Message });
        }
    }

    /// <summary>
    /// Applies the confirmed dock reassignments
    /// </summary>
    [HttpPost("apply")]
    public async Task<ActionResult<DockRebalanceResultDTO>> ApplyRebalancing(
        [FromBody] DockRebalanceConfirmDTO confirmation)
    {
        try
        {
            if (confirmation == null || confirmation.Reassignments == null || !confirmation.Reassignments.Any())
            {
                return BadRequest("No reassignments provided");
            }

            if (string.IsNullOrWhiteSpace(confirmation.OfficerId))
            {
                return BadRequest("Officer ID is required");
            }

            _logger.LogInformation(
                "Applying dock rebalancing for date {Date}, officer {OfficerId}, {Count} reassignments",
                confirmation.Date,
                confirmation.OfficerId,
                confirmation.Reassignments.Count);

            var result = await _rebalanceService.ApplyRebalancing(confirmation);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying dock rebalancing");
            return StatusCode(500, new { error = "Failed to apply rebalancing", details = ex.Message });
        }
    }
}
