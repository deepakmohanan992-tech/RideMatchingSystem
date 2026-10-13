using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RideMatchingSystem.DTOs;
using Swashbuckle.AspNetCore.Annotations;
using RideMatchingSystem.Services;

namespace RideMatchingSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RidersController : ControllerBase
    {
        private readonly RiderService _riderService;
        private readonly ILogger<RidersController> _logger;

        public RidersController(RiderService riderService, ILogger<RidersController> logger)
        {
            _riderService = riderService;
            _logger = logger;
        }

        [HttpPost]
        [SwaggerOperation(Summary = "Create a new rider or update an existing rider.")]
        public async Task<IActionResult> Register([FromBody] RiderDto dto, CancellationToken ct = default)
        {
            // Model validation via [ApiController]
            var existing = dto.Id > 0 ? await _riderService.GetByIdAsync(dto.Id, ct) : null;
            var upserted = await _riderService.UpsertRiderAsync(dto.Id == 0 ? (int?)null : dto.Id, dto.Name, ct);
            var result = new RiderDto { Id = upserted.Id, Name = upserted.Name };
            if (existing == null)
                return CreatedAtAction(nameof(Get), new { id = result.Id }, new DTOs.ApiResponse<RiderDto> { Message = "Created", Result = result });
            return Ok(new DTOs.ApiResponse<RiderDto> { Message = "Updated", Result = result });
        }

        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Get rider details by id.")]
        public async Task<IActionResult> Get([FromRoute] int id)
        {
            var rider = await _riderService.GetByIdAsync(id);
            if (rider == null) return NotFound(new DTOs.ApiResponse<object?> { Message = "Rider not found", Result = null });
            return Ok(new DTOs.ApiResponse<object?> { Message = "Success", Result = rider });
        }

        [HttpDelete("{id}")]
        [SwaggerOperation(Summary = "Delete a rider if they have no associated rides.")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
        {
            if (id <= 0) return BadRequest(new DTOs.ApiResponse<object?> { Message = "Id must be greater than zero", Result = null });

            var deleted = await _riderService.DeleteRiderAsync(id, ct);
            if (!deleted)
            {
                // Could be because rider not found OR because rides exist; detect which
                var exists = await _riderService.GetByIdAsync(id, ct) != null;
                if (exists)
                    return Conflict(new DTOs.ApiResponse<object?> { Message = "Cannot delete rider because one or more rides reference them", Result = null });
                return NotFound(new DTOs.ApiResponse<object?> { Message = "Rider not found", Result = null });
            }

            return Ok(new DTOs.ApiResponse<object?> { Message = "Deleted", Result = null });
        }
    }
}
