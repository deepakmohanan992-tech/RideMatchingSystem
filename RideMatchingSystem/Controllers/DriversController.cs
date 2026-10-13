using System.Linq;
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
    public class DriversController : ControllerBase
    {
        private readonly DriverService _driverService;
        private readonly ILogger<DriversController> _logger;

        public DriversController(DriverService driverService, ILogger<DriversController> logger)
        {
            _driverService = driverService;
            _logger = logger;
        }

        [HttpDelete("{id}")]
        /// <summary>
        /// Delete a driver if they have no associated rides.
        /// </summary>
        /// <param name="id">Driver identifier</param>
        /// <returns>204 NoContent when deleted, 404 if not found, 409 if referenced by rides</returns>
        [SwaggerOperation(Summary = "Delete a driver if no rides reference them.")]
        public async Task<IActionResult> Delete([FromRoute, Range(1, int.MaxValue)] int id)
        {
            var (deleted, exists) = await _driverService.DeleteDriverAsync(id);
            if (!exists) return NotFound(new DTOs.ApiResponse<object?> { Message = "Driver not found", Result = null });
            if (!deleted) return Conflict(new DTOs.ApiResponse<object?> { Message = "Cannot delete driver because one or more rides reference them", Result = null });
            return Ok(new DTOs.ApiResponse<object?> { Message = "Deleted", Result = null });
        }

        [HttpPost]
        /// <summary>
        /// Register a new driver.
        /// </summary>
        /// <param name="dto">Driver details (name, optional initial location)</param>
        /// <returns>201 Created with created driver</returns>
        [SwaggerOperation(Summary = "Create a new driver or update an existing driver.")]
        public async Task<IActionResult> Register([FromBody] DriverDto dto)
        {
            // Model validation is provided by [ApiController] and data annotations on DriverDto/LocationDto.
            var existing = dto.Id > 0 ? await _driverService.GetByIdAsync(dto.Id) : null;
            var upserted = await _driverService.UpsertDriverAsync(dto.Id == 0 ? (int?)null : dto.Id, dto.Name, dto.Location?.Latitude, dto.Location?.Longitude, dto.IsAvailable);
            var result = new DriverDto { Id = upserted.Id, Name = upserted.Name, Location = upserted.Latitude.HasValue ? new LocationDto { Latitude = upserted.Latitude.Value, Longitude = upserted.Longitude!.Value } : null, IsAvailable = upserted.IsAvailable };
            if (existing == null)
                return CreatedAtAction(nameof(Get), new { id = result.Id }, new DTOs.ApiResponse<DriverDto> { Message = "Created", Result = result });
            return Ok(new DTOs.ApiResponse<DriverDto> { Message = "Updated", Result = result });
        }

        [HttpGet]
        /// <summary>
        /// Get all drivers. Primarily used for admin/debug views; returns a list of drivers.
        /// </summary>
        /// <returns>200 OK with list of drivers or 404 if none exist</returns>
        [SwaggerOperation(Summary = "Get all drivers (admin/debug).")]
        public async Task<IActionResult> GetAll()
        {
            var drivers = await _driverService.GetAllAsync();
            var list = drivers.ToList();
            if (!list.Any())
                return NotFound(new DTOs.ApiResponse<object?> { Message = "No drivers found", Result = null });
            return Ok(new DTOs.ApiResponse<object?> { Message = "Success", Result = list });
        }

        [HttpGet("{id}")]
        /// <summary>
        /// Get a single driver by id.
        /// </summary>
        /// <param name="id">Driver identifier</param>
        /// <returns>200 OK with driver details or 404 if not found</returns>
        [SwaggerOperation(Summary = "Get driver details by id.")]
        public async Task<IActionResult> Get([FromRoute, Range(1, int.MaxValue)] int id)
        {
            var driver = await _driverService.GetByIdAsync(id);
            if (driver == null) return NotFound(new DTOs.ApiResponse<object?> { Message = "Driver not found", Result = null });
            return Ok(new DTOs.ApiResponse<object?> { Message = "Success", Result = driver });
        }

        [HttpPut("{id}/location")]
        /// <summary>
        /// Update a driver's current location (latitude/longitude). Drivers should call this periodically.
        /// </summary>
        /// <param name="id">Driver identifier</param>
        /// <param name="location">Location payload with latitude and longitude</param>
        /// <returns>204 NoContent on success</returns>
        [SwaggerOperation(Summary = "Update driver's current GPS location.")]
        public async Task<IActionResult> UpdateLocation([FromRoute, Range(1, int.MaxValue)] int id, [FromBody] LocationDto location)
        {
            // LocationDto has range validation attributes and [ApiController] handles model validation
            await _driverService.UpdateLocationAsync(id, location.Latitude, location.Longitude);
            return NoContent();
        }

        [HttpPost("{id}/availability")]
        /// <summary>
        /// Set a driver's availability (can accept rides or not).
        /// </summary>
        /// <param name="id">Driver identifier</param>
        /// <param name="available">True to mark available, false to mark unavailable</param>
        /// <returns>204 NoContent on success</returns>
        [SwaggerOperation(Summary = "Set driver's availability (available/unavailable).")]
        public async Task<IActionResult> SetAvailability([FromRoute, Range(1, int.MaxValue)] int id, [FromQuery] bool available)
        {
            await _driverService.SetAvailabilityAsync(id, available);
            return NoContent();
        }

        [HttpGet("nearby")]
        /// <summary>
        /// Return nearby available drivers within the specified radius of the given coordinates.
        /// Used by riders to discover drivers for matching.
        /// </summary>
        /// <param name="lat">Latitude to search around</param>
        /// <param name="lng">Longitude to search around</param>
        /// <param name="radius">Search radius in meters (default 2000)</param>
        /// <returns>200 OK with nearby drivers or 404 if none found</returns>
        [SwaggerOperation(Summary = "Find nearby available drivers.")]
        public async Task<IActionResult> GetNearby([FromQuery, Range(-90.0, 90.0)] double lat, [FromQuery, Range(-180.0, 180.0)] double lng, [FromQuery, Range(1, int.MaxValue)] int radius = 2000)
        {
            // Parameter validation is handled via data annotations on parameters and [ApiController]
            var drivers = await _driverService.GetAvailableDriversAsync();
            var now = System.DateTime.UtcNow;

            static double Haversine(double lat1, double lon1, double lat2, double lon2)
            {
                const double R = 6371000;
                double dLat = (lat2 - lat1) * (System.Math.PI / 180);
                double dLon = (lon2 - lon1) * (System.Math.PI / 180);
                double a = System.Math.Sin(dLat / 2) * System.Math.Sin(dLat / 2) +
                           System.Math.Cos(lat1 * (System.Math.PI / 180)) * System.Math.Cos(lat2 * (System.Math.PI / 180)) *
                           System.Math.Sin(dLon / 2) * System.Math.Sin(dLon / 2);
                double c = 2 * System.Math.Atan2(System.Math.Sqrt(a), System.Math.Sqrt(1 - a));
                return R * c;
            }

            var list = drivers
                .Where(d => d.Latitude.HasValue && d.Longitude.HasValue && (now - d.LastSeenUtc).TotalMinutes <= 5)
                .Select(d =>
                {
                    var distance = Haversine(lat, lng, d.Latitude!.Value, d.Longitude!.Value);
                    var loc = new LocationDto { Latitude = d.Latitude!.Value, Longitude = d.Longitude!.Value };
                    return new NearbyDriverSummaryDto(d.Id, distance, loc, 5.0m);
                })
                .Where(x => x.DistanceMeters <= radius)
                .OrderBy(x => x.DistanceMeters)
                .ToList();
            if (!list.Any())
                return NotFound(new DTOs.ApiResponse<object?> { Message = "No nearby drivers found", Result = null });
            return Ok(new DTOs.ApiResponse<object?> { Message = "Success", Result = list });
        }
    }
}
