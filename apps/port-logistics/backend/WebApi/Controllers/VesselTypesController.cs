using Application.DTO;
using Application.Services;
using Microsoft.AspNetCore.Mvc;
using WebApi.Authorization;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [RequirePermission("vessel-types")]
    public class VesselTypesController : ControllerBase
    {
        private readonly VesselTypeService _service;
        private readonly ILogger<VesselTypesController> _logger;
        private readonly List<string> _errors = new();

        public VesselTypesController(VesselTypeService service, ILogger<VesselTypesController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // GET: api/vesseltypes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VesselTypeDTO>>> GetAll([FromQuery] string? name, [FromQuery] string? description)
        {
            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(description))
            {
                var all = await _service.GetAll();
                return Ok(all);
            }
            var res = await _service.Search(name, description);
            return Ok(res);
        }

        // GET: api/vesseltypes/{name}
        [HttpGet("{name}")]
        public async Task<ActionResult<VesselTypeDTO>> GetByName(string name)
        {
            var vt = await _service.GetByName(name);
            if (vt == null) return NotFound();
            return Ok(vt);
        }

        // POST: api/vesseltypes
        [HttpPost]
        public async Task<ActionResult<VesselTypeDTO>> Create(VesselTypeDTO dto)
        {
            _logger.LogInformation("Creating vessel type {Name}", dto.Name);
            var created = await _service.Add(dto, _errors);
            if (created == null) return BadRequest(_errors);
            return CreatedAtAction(nameof(GetByName), new { name = created.Name }, created);
        }

        // PUT: api/vesseltypes/{name}
        [HttpPut("{name}")]
        public async Task<IActionResult> Update(string name, VesselTypeDTO dto)
        {
            if (!string.Equals(name, dto.Name, StringComparison.OrdinalIgnoreCase))
                return BadRequest("Name in URL and payload must match");

            _logger.LogInformation("Updating vessel type {Name}", name);
            var ok = await _service.Update(name, dto, _errors);
            if (!ok) return BadRequest(_errors);
            return Ok();
        }
    }
}
