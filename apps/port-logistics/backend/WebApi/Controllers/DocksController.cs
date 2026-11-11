using Application.DTO;
using Application.Services;
using Microsoft.AspNetCore.Mvc;
using WebApi.Authorization;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [RequirePermission("docks")]
    public class DocksController : ControllerBase
    {
        private readonly DockService _service;
        private readonly ILogger<DocksController> _logger;
        private readonly List<string> _errors = new();

        public DocksController(DockService service, ILogger<DocksController> logger)
        {
            _service = service; _logger = logger;
        }

        // GET: api/docks
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DockDTO>>> GetAll([FromQuery] string? name, [FromQuery] string? vesselType, [FromQuery] string? location)
        {
            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(vesselType) && string.IsNullOrWhiteSpace(location))
            {
                var all = await _service.GetAll();
                return Ok(all);
            }
            var res = await _service.Search(name, vesselType, location);
            return Ok(res);
        }

        // GET: api/docks/{code}
        [HttpGet("{code}")]
        public async Task<ActionResult<DockDTO>> GetByCode(string code)
        {
            var d = await _service.GetByCode(code);
            if (d == null) return NotFound();
            return Ok(d);
        }

        // POST: api/docks
        [HttpPost]
        public async Task<ActionResult<DockDTO>> Create(DockDTO dto)
        {
            _logger.LogInformation("Registering dock {Code}", dto.Code);
            var created = await _service.Add(dto, _errors);
            if (created == null) return BadRequest(_errors);
            return CreatedAtAction(nameof(GetByCode), new { code = created.Code }, created);
        }

        // PUT: api/docks/{code}
        [HttpPut("{code}")]
        public async Task<IActionResult> Update(string code, DockDTO dto)
        {
            if (!string.Equals(code, dto.Code, StringComparison.OrdinalIgnoreCase))
                return BadRequest("Code in URL and payload must match");
            _logger.LogInformation("Updating dock {Code}", code);
            var ok = await _service.Update(code, dto, _errors);
            if (!ok) return BadRequest(_errors);
            return Ok();
        }
    }
}
