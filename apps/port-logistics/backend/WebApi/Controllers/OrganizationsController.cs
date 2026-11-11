using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTO;
using Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrganizationsController : ControllerBase
    {
        private readonly OrganizationService _organizationService;

        public OrganizationsController(OrganizationService organizationService)
        {
            _organizationService = organizationService 
                ?? throw new ArgumentNullException(nameof(organizationService));
        }

        /// <summary>
        /// US 2.2.5: Register a new shipping agent organization.
        /// Organizations must include at least one representative at creation.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<OrganizationDTO>> Create([FromBody] CreateOrganizationDTO dto)
        {
            try
            {
                var organization = await _organizationService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = organization.Id }, organization);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get an organization by ID.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<OrganizationDTO>> GetById(long id)
        {
            var organization = await _organizationService.GetByIdAsync(id);
            if (organization == null)
                return NotFound();

            return Ok(organization);
        }

        /// <summary>
        /// Get an organization by identifier.
        /// </summary>
        [HttpGet("by-identifier/{identifier}")]
        public async Task<ActionResult<OrganizationDTO>> GetByIdentifier(string identifier)
        {
            var organization = await _organizationService.GetByIdentifierAsync(identifier);
            if (organization == null)
                return NotFound();

            return Ok(organization);
        }

        /// <summary>
        /// Get all organizations.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrganizationDTO>>> GetAll()
        {
            var organizations = await _organizationService.GetAllAsync();
            return Ok(organizations);
        }

        /// <summary>
        /// US 2.2.5: Update organization details.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<OrganizationDTO>> Update(long id, [FromBody] UpdateOrganizationDTO dto)
        {
            try
            {
                var organization = await _organizationService.UpdateAsync(id, dto);
                if (organization == null)
                    return NotFound();

                return Ok(organization);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
