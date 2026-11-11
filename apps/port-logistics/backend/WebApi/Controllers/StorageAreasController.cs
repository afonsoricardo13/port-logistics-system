using Application.DTO;
using Application.Services;
using Microsoft.AspNetCore.Mvc;
using WebApi.Authorization;

namespace WebApi.Controllers
{
    [Route("api/storage-areas")]
    [ApiController]
    [RequirePermission("storage-areas")]
    public class StorageAreasController : ControllerBase
    {
        private readonly StorageAreaService _service;
        private readonly ILogger<StorageAreasController> _logger;
        private readonly List<string> _errors = new();

        public StorageAreasController(StorageAreaService service, ILogger<StorageAreasController> logger)
        {
            _service = service; _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<StorageAreaDTO>>> GetAll()
        {
            var list = await _service.GetAll();
            return Ok(list);
        }

        [HttpGet("{identifier}")]
        public async Task<ActionResult<StorageAreaDTO>> GetByIdentifier(string identifier)
        {
            var s = await _service.GetByIdentifier(identifier);
            if (s == null) return NotFound();
            return Ok(s);
        }

        [HttpPost]
        public async Task<ActionResult<StorageAreaDTO>> Create(StorageAreaDTO dto)
        {
            _logger.LogInformation("Registering storage area {Identifier}", dto.Identifier);
            var created = await _service.Add(dto, _errors);
            if (created == null) return BadRequest(_errors);
            return CreatedAtAction(nameof(GetByIdentifier), new { identifier = created.Identifier }, created);
        }

        [HttpPut("{identifier}")]
        public async Task<ActionResult<StorageAreaDTO>> Update(string identifier, StorageAreaDTO dto)
        {
            _logger.LogInformation("Updating storage area {Identifier}", identifier);
            var updated = await _service.Update(identifier, dto, _errors);
            if (updated == null) return BadRequest(_errors);
            return Ok(updated);
        }
    }
}
