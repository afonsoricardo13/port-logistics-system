using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTO;
using Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RepresentativesController : ControllerBase
    {
        private readonly RepresentativeService _representativeService;

        public RepresentativesController(RepresentativeService representativeService)
        {
            _representativeService = representativeService 
                ?? throw new ArgumentNullException(nameof(representativeService));
        }

        /// <summary>
        /// US 2.2.6: Register a new representative for a shipping agent organization.
        /// </summary>
        [HttpPost("organization/{organizationId}")]
        public async Task<ActionResult<RepresentativeDTO>> Create(long organizationId, [FromBody] CreateRepresentativeDTO dto)
        {
            try
            {
                var representative = await _representativeService.CreateAsync(organizationId, dto);
                return CreatedAtAction(nameof(GetById), new { id = representative.Id }, representative);
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
        /// Get a representative by ID.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<RepresentativeDTO>> GetById(long id)
        {
            var representative = await _representativeService.GetByIdAsync(id);
            if (representative == null)
                return NotFound();

            return Ok(representative);
        }

        /// <summary>
        /// Get all representatives for a specific organization.
        /// </summary>
        [HttpGet("organization/{organizationId}")]
        public async Task<ActionResult<IEnumerable<RepresentativeDTO>>> GetByOrganization(long organizationId)
        {
            var representatives = await _representativeService.GetByOrganizationIdAsync(organizationId);
            return Ok(representatives);
        }

        /// <summary>
        /// Get all representatives.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RepresentativeDTO>>> GetAll()
        {
            var representatives = await _representativeService.GetAllAsync();
            return Ok(representatives);
        }

        /// <summary>
        /// Get representative organization by email.
        /// </summary>
        [HttpGet("by-email/{email}")]
        public async Task<ActionResult<RepresentativeWithOrganizationDTO>> GetByEmail(string email)
        {            var rep = await _representativeService.GetByEmailAsync(email);
            if (rep == null) return NotFound();
            return Ok(rep);
        }

        /// <summary>
        /// US 2.2.6: Update representative details.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<RepresentativeDTO>> Update(long id, [FromBody] UpdateRepresentativeDTO dto)
        {
            try
            {
                var representative = await _representativeService.UpdateAsync(id, dto);
                if (representative == null)
                    return NotFound();

                return Ok(representative);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// US 2.2.6: Deactivate a representative.
        /// Cannot deactivate the last active representative of an organization.
        /// </summary>
        [HttpPatch("{id}/deactivate")]
        public async Task<ActionResult> Deactivate(long id)
        {
            try
            {
                var result = await _representativeService.DeactivateAsync(id);
                if (!result)
                    return NotFound();

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        /// <summary>
        /// US 2.2.6: Reactivate a representative.
        /// </summary>
        [HttpPatch("{id}/reactivate")]
        public async Task<ActionResult> Reactivate(long id)
        {
            var result = await _representativeService.ReactivateAsync(id);
            if (!result)
                return NotFound();

            return NoContent();
        }
    }
}
