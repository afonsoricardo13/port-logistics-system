using Microsoft.AspNetCore.Mvc;
using Application.Services;
using Application.DTO;
using WebApi.Authorization;

namespace WebApi.Controllers
{
    /// <summary>
    /// Controller for managing Qualifications
    /// US 2.2.13: Register and manage qualifications (create, update)
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [RequirePermission("qualifications")]
    public class QualificationController : ControllerBase
    {
        private readonly QualificationService _qualificationService;

        public QualificationController(QualificationService qualificationService)
        {
            _qualificationService = qualificationService;
        }

        /// <summary>
        /// GET: api/Qualification
        /// Gets all qualifications or filters by name pattern
        /// US 2.2.13: Qualifications must be searchable and filterable by name
        /// </summary>
        /// <param name="namePattern">Optional: Filter by name (partial match, case-insensitive)</param>
        /// <returns>List of qualifications</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<QualificationDTO>>> GetAllQualifications(
            [FromQuery] string? namePattern = null)
        {
            IEnumerable<QualificationDTO> qualificationDTOs;

            // If filter is provided, use search
            if (!string.IsNullOrWhiteSpace(namePattern))
            {
                qualificationDTOs = await _qualificationService.SearchAsync(namePattern);
            }
            else
            {
                qualificationDTOs = await _qualificationService.GetAllAsync();
            }

            return Ok(qualificationDTOs);
        }

        /// <summary>
        /// GET: api/Qualification/{qualificationName}
        /// Gets a specific qualification by name (identifier)
        /// </summary>
        /// <param name="qualificationName">Qualification name (unique identifier)</param>
        /// <returns>Qualification details</returns>
        [HttpGet("{qualificationName}")]
        public async Task<ActionResult<QualificationDTO>> GetQualificationByName(string qualificationName)
        {
            var qualificationDTO = await _qualificationService.GetByQualificationNameAsync(qualificationName);

            if (qualificationDTO == null)
            {
                return NotFound(new { message = $"Qualification with name '{qualificationName}' not found" });
            }

            return Ok(qualificationDTO);
        }

        /// <summary>
        /// POST: api/Qualification
        /// Creates a new qualification
        /// US 2.2.13: Register qualifications - requires qualificationName (ID) and requiredNumber
        /// </summary>
        /// <param name="qualificationDTO">Qualification data</param>
        /// <returns>Created qualification</returns>
        [HttpPost]
        public async Task<ActionResult<QualificationDTO>> PostQualification(CreateQualificationDTO qualificationDTO)
        {
            var errorMessages = new List<string>();
            
            QualificationDTO? resultDTO = await _qualificationService.AddAsync(qualificationDTO, errorMessages);

            if (resultDTO != null)
            {
                return CreatedAtAction(
                    nameof(GetQualificationByName), 
                    new { qualificationName = resultDTO.QualificationName }, 
                    resultDTO);
            }
            else
            {
                return BadRequest(new { errors = errorMessages });
            }
        }

        /// <summary>
        /// PUT: api/Qualification/{qualificationName}
        /// Updates an existing qualification
        /// US 2.2.13: Manage qualifications - only requiredNumber can be updated (qualificationName is the ID and is immutable)
        /// </summary>
        /// <param name="qualificationName">Qualification name (immutable ID)</param>
        /// <param name="qualificationDTO">Updated qualification data</param>
        /// <returns>Updated qualification</returns>
        [HttpPut("{qualificationName}")]
        public async Task<IActionResult> PutQualification(string qualificationName, UpdateQualificationDTO qualificationDTO)
        {
            var errorMessages = new List<string>();
            
            QualificationDTO? updatedDTO = await _qualificationService.UpdateAsync(qualificationName, qualificationDTO, errorMessages);

            if (updatedDTO != null)
            {
                return Ok(updatedDTO);
            }
            else
            {
                if (errorMessages.Any(m => m.Contains("not found")))
                {
                    return NotFound(new { errors = errorMessages });
                }
                return BadRequest(new { errors = errorMessages });
            }
        }

        /// <summary>
        /// GET: api/Qualification/{qualificationName}/exists
        /// Checks if a qualification exists
        /// US 2.2.13: A qualification must exist before it can be assigned to staff members or resources
        /// </summary>
        /// <param name="qualificationName">Qualification name</param>
        /// <returns>Boolean indicating existence</returns>
        [HttpGet("{qualificationName}/exists")]
        public async Task<ActionResult<bool>> CheckQualificationExists(string qualificationName)
        {
            bool exists = await _qualificationService.ExistsByQualificationNameAsync(qualificationName);
            return Ok(new { qualificationName = qualificationName, exists = exists });
        }
    }
}
