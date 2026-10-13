using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RideMatchingSystem.Data;
using RideMatchingSystem.Models;

namespace RideMatchingSystem.Services
{
    public class DriverService
    {
        private readonly AppDbContext _db;
        private readonly IMemoryCache _cache;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private const string AvailableDriversCacheKey = "available_drivers";

        public DriverService(AppDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        public async Task<Driver> CreateDriverAsync(string? name, double? latitude = null, double? longitude = null, bool isAvailable = false, CancellationToken ct = default)
        {
            var driver = new Driver
            {
                Name = name,
                Latitude = latitude,
                Longitude = longitude,
                IsAvailable = isAvailable,
                LastSeenUtc = DateTime.UtcNow
            };

            _db.Drivers.Add(driver);
            await _db.SaveChangesAsync(ct);
            _cache.Remove(AvailableDriversCacheKey);
            return driver;
        }

        public async Task<Driver> UpsertDriverAsync(int? id, string? name, double? latitude = null, double? longitude = null, bool isAvailable = false, CancellationToken ct = default)
        {
            if (id.HasValue && id.Value > 0)
            {
                var existing = await _db.Drivers.FindAsync(new object[] { id.Value }, ct);
                if (existing != null)
                {
                    existing.Name = name ?? existing.Name;
                    existing.Latitude = latitude ?? existing.Latitude;
                    existing.Longitude = longitude ?? existing.Longitude;
                    existing.IsAvailable = isAvailable;
                    existing.LastSeenUtc = DateTime.UtcNow;
                    await _db.SaveChangesAsync(ct);
                    _cache.Remove(AvailableDriversCacheKey);
                    return existing;
                }
            }

            // If not found or no id provided, create new
            return await CreateDriverAsync(name, latitude, longitude, isAvailable, ct);
        }

        public async Task<IEnumerable<Driver>> GetAllAsync(CancellationToken ct = default)
        {
            return await _db.Drivers.AsNoTracking().ToListAsync(ct);
        }

        public async Task<Driver?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await _db.Drivers.FindAsync(new object[] { id }, ct);
        }

        public async Task UpdateLocationAsync(int driverId, double latitude, double longitude, CancellationToken ct = default)
        {
            var driver = await _db.Drivers.FindAsync(new object[] { driverId }, ct);
            if (driver == null) return;

            driver.Latitude = latitude;
            driver.Longitude = longitude;
            driver.LastSeenUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            _cache.Remove(AvailableDriversCacheKey);
        }

        public async Task SetAvailabilityAsync(int driverId, bool isAvailable, CancellationToken ct = default)
        {
            // Use a semaphore to avoid thundering updates to the cache
            await _semaphore.WaitAsync(ct);
            try
            {
                var driver = await _db.Drivers.FindAsync(new object[] { driverId }, ct);
                if (driver == null)
                {
                    // Optionally create driver record if not present
                    driver = new Driver { Id = driverId, IsAvailable = isAvailable, LastSeenUtc = DateTime.UtcNow };
                    _db.Drivers.Add(driver);
                }
                else
                {
                    driver.IsAvailable = isAvailable;
                    driver.LastSeenUtc = DateTime.UtcNow;
                }

                await _db.SaveChangesAsync(ct);
                _cache.Remove(AvailableDriversCacheKey);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<IEnumerable<Driver>> GetAvailableDriversAsync(CancellationToken ct = default)
        {
            if (_cache.TryGetValue<IEnumerable<Driver>>(AvailableDriversCacheKey, out var cached))
            {
                return cached!;
            }

            await _semaphore.WaitAsync(ct);
            try
            {
                // Double-check after acquiring lock
                if (_cache.TryGetValue<IEnumerable<Driver>>(AvailableDriversCacheKey, out cached))
                    return cached!;

                var list = await _db.Drivers.AsNoTracking().Where(d => d.IsAvailable).ToListAsync(ct);
                _cache.Set(AvailableDriversCacheKey, list, TimeSpan.FromSeconds(30));
                return list;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<(bool Deleted, bool Exists)> DeleteDriverAsync(int id, CancellationToken ct = default)
        {
            // If any ride references this driver, deletion is not allowed
            var hasRides = await _db.Rides.AnyAsync(r => r.DriverId == id, ct);
            if (hasRides) return (false, true);

            var existing = await _db.Drivers.FindAsync(new object[] { id }, ct);
            if (existing == null) return (false, false);

            _db.Drivers.Remove(existing);
            await _db.SaveChangesAsync(ct);
            _cache.Remove(AvailableDriversCacheKey);
            return (true, true);
        }
    }
}
