using Application.DTO;
using Application.Services;
using Microsoft.AspNetCore.Mvc;
using WebApi.Authorization;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [RequirePermission("vessels")]
    public class VesselsController : ControllerBase
    {
        private readonly VesselService _service;
        private readonly ILogger<VesselsController> _logger;
        private readonly List<string> _errors = new();

        public VesselsController(VesselService service, ILogger<VesselsController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // GET: api/vessels
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VesselDTO>>> GetAll(
            [FromQuery] string? imoNumber, 
            [FromQuery] string? name, 
            [FromQuery] string? operatorName)
        {
            if (string.IsNullOrWhiteSpace(imoNumber) && string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(operatorName))
            {
                var all = await _service.GetAll();
                return Ok(all);
            }
            var res = await _service.Search(imoNumber, name, operatorName);
            return Ok(res);
        }

        // GET: api/vessels/{imoNumber}
        [HttpGet("{imoNumber}")]
        public async Task<ActionResult<VesselDTO>> GetByImoNumber(string imoNumber)
        {
            var v = await _service.GetByImoNumber(imoNumber);
            if (v == null) return NotFound();
            return Ok(v);
        }

        // POST: api/vessels
        [HttpPost]
        public async Task<ActionResult<VesselDTO>> Create(VesselDTO dto)
        {
            _logger.LogInformation("Registering vessel with IMO {ImoNumber}", dto.ImoNumber);
            var created = await _service.Add(dto, _errors);
            if (created == null) return BadRequest(_errors);
            return CreatedAtAction(nameof(GetByImoNumber), new { imoNumber = created.ImoNumber }, created);
        }

        // PUT: api/vessels/{imoNumber}
        [HttpPut("{imoNumber}")]
        public async Task<IActionResult> Update(string imoNumber, VesselDTO dto)
        {
            if (!string.Equals(imoNumber, dto.ImoNumber, StringComparison.OrdinalIgnoreCase))
                return BadRequest("IMO number in URL and payload must match");
            
            _logger.LogInformation("Updating vessel with IMO {ImoNumber}", imoNumber);
            var ok = await _service.Update(imoNumber, dto, _errors);
            if (!ok) return BadRequest(_errors);
            return Ok();
        }
    }
}
