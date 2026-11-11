using Microsoft.AspNetCore.Mvc;
using Application.Services;
using Application.DTO;
using WebApi.Authorization;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [RequirePermission("staff")]
    public class OperatingStaffController : ControllerBase
    {
        private readonly OperatingStaffService _staffService;

        public OperatingStaffController(OperatingStaffService staffService)
        {
            _staffService = staffService;
        }

        // GET: api/OperatingStaff
        // Supports optional query parameters: ?status=Available&name=John&email=john@&qualification=STS Crane Operator
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OperatingStaffDTO>>> GetAllStaff(
            [FromQuery] string? status = null,
            [FromQuery] string? name = null,
            [FromQuery] string? email = null,
            [FromQuery] string? qualification = null)
        {
            IEnumerable<OperatingStaffDTO> staffDTOs;

            // If any filters are provided, use search
            if (!string.IsNullOrWhiteSpace(status) || !string.IsNullOrWhiteSpace(name) || 
                !string.IsNullOrWhiteSpace(email) || !string.IsNullOrWhiteSpace(qualification))
            {
                staffDTOs = await _staffService.SearchAsync(name, email, status, qualification);
            }
            else
            {
                staffDTOs = await _staffService.GetAllAsync();
            }

            return Ok(staffDTOs);
        }

        // GET: api/OperatingStaff/mecanographic/M12345
        [HttpGet("mecanographic/{mecanographicNumber}")]
        public async Task<ActionResult<OperatingStaffDTO>> GetStaffByMecanographicNumber(string mecanographicNumber)
        {
            var staffDTO = await _staffService.GetByMecanographicNumberAsync(mecanographicNumber);

            if (staffDTO == null)
            {
                return NotFound(new { message = "Staff member not found" });
            }

            return Ok(staffDTO);
        }

        // GET: api/OperatingStaff/status/Available
        [HttpGet("status/{status}")]
        public async Task<ActionResult<IEnumerable<OperatingStaffDTO>>> GetStaffByStatus(string status)
        {
            var staffDTOs = await _staffService.GetByStatusAsync(status);
            return Ok(staffDTOs);
        }

        // POST: api/OperatingStaff
        [HttpPost]
        public async Task<ActionResult<OperatingStaffDTO>> PostStaff(CreateOperatingStaffDTO staffDTO)
        {
            var errorMessages = new List<string>();
            
            OperatingStaffDTO? resultDTO = await _staffService.AddAsync(staffDTO, errorMessages);

            if (resultDTO != null)
            {
                return CreatedAtAction(
                    nameof(GetStaffByMecanographicNumber), 
                    new { mecanographicNumber = resultDTO.MecanographicNumber }, 
                    resultDTO);
            }
            else
            {
                return BadRequest(new { errors = errorMessages });
            }
        }

        // PUT: api/OperatingStaff/M12345
        [HttpPut("{mecanographicNumber}")]
        public async Task<IActionResult> PutStaff(string mecanographicNumber, UpdateOperatingStaffDTO staffDTO)
        {
            var errorMessages = new List<string>();
            
            OperatingStaffDTO? updatedDTO = await _staffService.UpdateAsync(mecanographicNumber, staffDTO, errorMessages);

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

        // PATCH: api/OperatingStaff/M12345/status
        [HttpPatch("{mecanographicNumber}/status")]
        public async Task<ActionResult<OperatingStaffDTO>> UpdateStaffStatus(
            string mecanographicNumber,
            [FromBody] StatusUpdateRequest request)
        {
            var errorMessages = new List<string>();
            OperatingStaffDTO? resultDTO = null;

            if (request.IsActive.HasValue)
            {
                if (request.IsActive.Value)
                {
                    // Reactivate
                    resultDTO = await _staffService.ReactivateAsync(
                        mecanographicNumber,
                        request.OperatorName,
                        request.Reason,
                        errorMessages);
                }
                else
                {
                    // Deactivate
                    resultDTO = await _staffService.DeactivateAsync(
                        mecanographicNumber,
                        request.OperatorName,
                        request.Reason,
                        errorMessages);
                }
            }
            else
            {
                errorMessages.Add("IsActive field is required");
            }

            if (resultDTO != null)
            {
                return Ok(resultDTO);
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
    }

    // Request DTO for status update operations
    public class StatusUpdateRequest
    {
        public bool? IsActive { get; set; }
        public string OperatorName { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }
}
