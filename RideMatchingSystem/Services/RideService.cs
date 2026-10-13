using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RideMatchingSystem.Data;
using RideMatchingSystem.DTOs;
using RideMatchingSystem.Models;

namespace RideMatchingSystem.Services
{
    public class RideService
    {
        private readonly AppDbContext _db;

        public RideService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<bool> CancelRideAsync(int rideId, string cancelledBy, CancellationToken ct = default)
        {
            var ride = await _db.Rides.FirstOrDefaultAsync(r => r.Id == rideId, ct);
            if (ride == null) return false;
            ride.Status = Models.RideStatus.Cancelled;
            ride.CompletedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> UpdateRideStatusAsync(int rideId, Models.RideStatus newStatus, CancellationToken ct = default)
        {
            var ride = await _db.Rides.FirstOrDefaultAsync(r => r.Id == rideId, ct);
            if (ride == null) return false;
            ride.Status = newStatus;
            if (newStatus == Models.RideStatus.Completed) ride.CompletedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<Ride> CreateRideAsync(RideRequestDto request, CancellationToken ct = default)
        {
            var ride = new Ride
            {
                RiderId = request.RiderId,
                PickupLatitude = request.PickupLocation.Latitude,
                PickupLongitude = request.PickupLocation.Longitude,
                DropoffLatitude = request.DropoffLocation?.Latitude,
                DropoffLongitude = request.DropoffLocation?.Longitude,
                Status = Models.RideStatus.Requested,
                RequestedAtUtc = DateTime.UtcNow
            };

            _db.Rides.Add(ride);
            await _db.SaveChangesAsync(ct);
            return ride;
        }

        public async Task<Ride> UpsertRideAsync(RideRequestDto request, CancellationToken ct = default)
        {
            if (request.Id.HasValue && request.Id.Value > 0)
            {
                var existing = await _db.Rides.FirstOrDefaultAsync(r => r.Id == request.Id.Value, ct);
                if (existing != null)
                {
                    existing.RiderId = request.RiderId;
                    existing.PickupLatitude = request.PickupLocation.Latitude;
                    existing.PickupLongitude = request.PickupLocation.Longitude;
                    existing.DropoffLatitude = request.DropoffLocation?.Latitude;
                    existing.DropoffLongitude = request.DropoffLocation?.Longitude;
                    // Keep status and timestamps as-is for updates; callers can change status via dedicated endpoints
                    await _db.SaveChangesAsync(ct);
                    return existing;
                }
            }

            return await CreateRideAsync(request, ct);
        }

        public async Task<Ride?> GetRideByIdAsync(int id, CancellationToken ct = default)
        {
            return await _db.Rides.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct);
        }

        public async Task<IEnumerable<Ride>> GetPendingRidesAsync(CancellationToken ct = default)
        {
            return await _db.Rides.Where(r => r.Status == Models.RideStatus.Requested).ToListAsync(ct);
        }

        public async Task<bool> TryAssignDriverAsync(int rideId, int driverId, CancellationToken ct = default)
        {
            // Attempt to assign driver in a transaction. Rely on DB checks for concurrency.
            using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                var ride = await _db.Rides.FirstOrDefaultAsync(r => r.Id == rideId, ct);
                if (ride == null || ride.Status != Models.RideStatus.Requested) return false;

                var driver = await _db.Drivers.FirstOrDefaultAsync(d => d.Id == driverId, ct);
                if (driver == null || !driver.IsAvailable) return false;

                // assign
                ride.DriverId = driverId;
                ride.Status = Models.RideStatus.Assigned;
                ride.AssignedAtUtc = DateTime.UtcNow;

                driver.IsAvailable = false;

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                await tx.RollbackAsync(ct);
                return false;
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }
    }
}
