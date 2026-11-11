using Application.DTO;
using Domain.IRepository;
using Domain.Model;
using Microsoft.AspNetCore.Mvc;
using WebApi.Authorization;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[RequirePermission("complementary-task-categories")]
public class ComplementaryTaskCategoriesController : ControllerBase
{
    private readonly IComplementaryTaskCategoryRepository _repository;
    private readonly ILogger<ComplementaryTaskCategoriesController> _logger;

    public ComplementaryTaskCategoriesController(
        IComplementaryTaskCategoryRepository repository,
        ILogger<ComplementaryTaskCategoriesController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Get all complementary task categories
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ComplementaryTaskCategory>>> GetAll()
    {
        var categories = await _repository.GetAllAsync();
        return Ok(categories);
    }

    /// <summary>
    /// Get complementary task category by code
    /// </summary>
    [HttpGet("{code}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ComplementaryTaskCategory>> GetByCode(string code)
    {
        var category = await _repository.GetByCodeAsync(code);
        if (category == null)
            return NotFound(new { message = $"Category with code {code} not found" });

        return Ok(category);
    }

    [HttpPost]
    public async Task<ActionResult<ComplementaryTaskCategory>> Create([FromBody] ComplementaryTaskCategory category)
    {
        try
        {
            var created = await _repository.AddAsync(category);
            _logger.LogInformation("Complementary task category created with code {Code}", created.Code);
            return CreatedAtAction(nameof(GetByCode), new { code = created.Code }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category");
            return BadRequest(new { message = "Error creating category", error = ex.Message });
        }
    }

    [HttpPut("{code}")]
    public async Task<ActionResult<ComplementaryTaskCategory>> Update(string code, [FromBody] UpdateComplementaryTaskCategoryDTO dto)
    {
        try
        {
            var existing = await _repository.GetByCodeAsync(code);
            if (existing == null)
                return NotFound(new { message = $"Category with code {code} not found" });

            existing.Name = dto.Name;
            existing.Description = dto.Description;
            existing.DefaultDuration = dto.DefaultDuration;
            existing.ExpectedImpact = dto.ExpectedImpact;

            var updated = await _repository.UpdateAsync(existing);
            _logger.LogInformation("Category with code {Code} updated", code);
            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category");
            return BadRequest(new { message = "Error updating category", error = ex.Message });
        }
    }

    /// <summary>
    /// US 4.1.14: Delete complementary task category
    /// </summary>
    [HttpDelete("{code}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(string code)
    {
        var category = await _repository.GetByCodeAsync(code);
        if (category == null)
            return NotFound(new { message = $"Category with code {code} not found" });

        await _repository.DeleteAsync(code);
        _logger.LogInformation("Category with code {Code} deleted", code);
        return NoContent();
    }
}
