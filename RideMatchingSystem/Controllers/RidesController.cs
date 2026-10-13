using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using RideMatchingSystem.DTOs;
using Swashbuckle.AspNetCore.Annotations;
using RideMatchingSystem.Services;

namespace RideMatchingSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RidesController : ControllerBase
    {
        private readonly RideService _rideService;
        private readonly MatchingService _matchingService;
        private readonly ILogger<RidesController> _logger;

        public RidesController(RideService rideService, MatchingService matchingService, ILogger<RidesController> logger)
        {
            _rideService = rideService;
            _matchingService = matchingService;
            _logger = logger;
        }

        [HttpPost]
        /// <summary>
        /// Create a new ride request. Riders call this to request a ride.
        /// </summary>
        /// <param name="request">Ride request details (pickup/dropoff, rider id, etc.)</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>201 Created with the created ride details</returns>
        [SwaggerOperation(Summary = "Create a new ride or update an existing ride.")]
        public async Task<IActionResult> Create([FromBody] RideRequestDto request, CancellationToken ct = default)
        {
            // Model validation (RideRequestDto and nested LocationDto) is performed automatically by [ApiController]
            var existing = request.Id.HasValue && request.Id.Value > 0 ? await _rideService.GetRideByIdAsync(request.Id.Value, ct) : null;
            var ride = await _rideService.UpsertRideAsync(request, ct);
            var dto = new RideDto { Id = ride.Id, RiderId = ride.RiderId, DriverId = ride.DriverId, Status = ride.Status.ToString() };
            if (existing == null)
                return CreatedAtAction(nameof(GetById), new { id = ride.Id }, new DTOs.ApiResponse<RideDto> { Message = "Created", Result = dto });
            return Ok(new DTOs.ApiResponse<RideDto> { Message = "Updated", Result = dto });
        }

        [HttpGet("{id}")]
        /// <summary>
        /// Get a ride by id.
        /// </summary>
        /// <param name="id">Ride identifier</param>
        /// <returns>200 OK with ride details or 404 if not found</returns>
        [SwaggerOperation(Summary = "Get ride details by id.")]
        public async Task<IActionResult> GetById([FromRoute, Range(1, int.MaxValue)] int id)
        {
            var ride = await _rideService.GetRideByIdAsync(id);
            if (ride == null) return NotFound(new DTOs.ApiResponse<RideDto> { Message = "Ride not found", Result = null });
            var dto = new RideDto { Id = ride.Id, RiderId = ride.RiderId, DriverId = ride.DriverId, Status = ride.Status.ToString() };
            return Ok(new DTOs.ApiResponse<RideDto> { Message = "Success", Result = dto });
        }

        [HttpGet("pending")]
        /// <summary>
        /// Return pending rides that still need a driver assignment.
        /// </summary>
        /// <returns>200 OK with list of pending rides or 404 when none exist</returns>
        [SwaggerOperation(Summary = "Get pending rides awaiting drivers.")]
        public async Task<IActionResult> GetPending()
        {
            var pending = await _rideService.GetPendingRidesAsync();
            var dtos = pending.Select(r => new RideDto { Id = r.Id, RiderId = r.RiderId, DriverId = r.DriverId, Status = r.Status.ToString() });
            var list = dtos.ToList();
            if (!list.Any())
                return NotFound(new DTOs.ApiResponse<object?> { Message = "No pending rides found", Result = null });
            return Ok(new DTOs.ApiResponse<object?> { Message = "Success", Result = list });
        }

        [HttpPost("{id}/match")]
        /// <summary>
        /// Trigger matching for a specific ride. Attempts to assign an available driver.
        /// </summary>
        /// <param name="id">Ride identifier</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>200 OK with updated ride on success or 409 if no driver available</returns>
        [SwaggerOperation(Summary = "Attempt to match a driver to this ride.")]
        public async Task<IActionResult> Match([FromRoute, Range(1, int.MaxValue)] int id, CancellationToken ct = default)
        {
            var ride = await _rideService.GetRideByIdAsync(id, ct);
            if (ride == null) return NotFound();

            var ok = await _matchingService.TryMatchRideAsync(ride, ct);
            if (!ok) return Conflict(new { Message = "No driver could be assigned" });

            // Return updated ride
            var updated = await _rideService.GetRideByIdAsync(id, ct);
            var dto = new RideDto { Id = updated!.Id, RiderId = updated.RiderId, DriverId = updated.DriverId, Status = updated.Status.ToString() };
            return Ok(new DTOs.ApiResponse<RideDto> { Message = "Success", Result = dto });
        }

        [HttpPost("{id}/accept")]
        /// <summary>
        /// Driver accepts assignment for a ride.
        /// </summary>
        /// <param name="id">Ride identifier</param>
        /// <param name="decision">Driver decision payload</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>200 OK with updated ride on success or 409 on failure</returns>
        [SwaggerOperation(Summary = "Driver accepts a ride assignment.")]
        public async Task<IActionResult> Accept([FromRoute, Range(1, int.MaxValue)] int id, [FromBody] DriverDecisionDto decision, CancellationToken ct = default)
        {
            // DriverDecisionDto has validation attributes; [ApiController] will return 400 if invalid
            var ok = await _rideService.TryAssignDriverAsync(id, decision.DriverId, ct);
            if (!ok) return Conflict(new { Message = "Failed to assign driver" });
            var updated = await _rideService.GetRideByIdAsync(id, ct);
            var rideDto = new RideDto { Id = updated!.Id, RiderId = updated.RiderId, DriverId = updated.DriverId, Status = updated.Status.ToString() };
            return Ok(new DTOs.ApiResponse<RideDto> { Message = "Success", Result = rideDto });
        }

        [HttpPost("{id}/decline")]
        /// <summary>
        /// Driver declines an offer. The system will continue matching with other drivers.
        /// </summary>
        /// <param name="id">Ride identifier</param>
        /// <param name="decision">Driver decision payload (offer id, driver id)</param>
        /// <returns>200 OK acknowledgement</returns>
        [SwaggerOperation(Summary = "Driver declines an offer (acknowledgement).")]
        public IActionResult Decline([FromRoute, Range(1, int.MaxValue)] int id, [FromBody] DriverDecisionDto decision)
        {
            // For now, just acknowledge. MatchingService will try other drivers via background worker.
            _logger.LogInformation("Driver {DriverId} declined offer {OfferId} for ride {RideId}", decision.DriverId, decision.OfferId, id);
            return Ok(new { Message = "Declined" });
        }

        [HttpPost("{id}/cancel")]
        /// <summary>
        /// Cancel a ride. Can be called by the rider to cancel a pending/active ride.
        /// </summary>
        /// <param name="id">Ride identifier</param>
        /// <param name="body">Flexible payload with cancellation info</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>200 OK on success or 404 if ride not found</returns>
        [SwaggerOperation(Summary = "Cancel a ride request.")]
        public async Task<IActionResult> Cancel(int id, [FromBody] object body, CancellationToken ct = default)
        {
            // Accept flexible payload { cancelledByUserId, reason }
            var ok = await _rideService.CancelRideAsync(id, "user", ct);
            if (!ok) return NotFound();
            return Ok(new { Message = "Cancelled" });
        }

        [HttpPatch("{id}/status")]
        /// <summary>
        /// Update the status of a ride (e.g., InProgress, Completed).
        /// </summary>
        /// <param name="id">Ride identifier</param>
        /// <param name="dto">Status update payload</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>200 OK on success or 404 if ride not found</returns>
        [SwaggerOperation(Summary = "Update the ride status.")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] RideStatusUpdateDto dto, CancellationToken ct = default)
        {
            if (dto == null) return BadRequest();
            if (!Enum.TryParse<Models.RideStatus>(dto.Status, true, out var newStatus))
                return BadRequest(new { Message = "Unknown status" });

            var ok = await _rideService.UpdateRideStatusAsync(id, newStatus, ct);
            if (!ok) return NotFound();
            return Ok(new { Message = "Status updated" });
        }
    }
}
