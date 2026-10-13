using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RideMatchingSystem.Data;
using RideMatchingSystem.Models;

namespace RideMatchingSystem.Services
{
    public class RiderService
    {
        private readonly AppDbContext _db;

        public RiderService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Rider> CreateRiderAsync(string name, CancellationToken ct = default)
        {
            var rider = new Rider { Name = name };
            _db.Riders.Add(rider);
            await _db.SaveChangesAsync(ct);
            return rider;
        }

        public async Task<Rider?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await _db.Riders.FindAsync(new object[] { id }, ct);
        }

        public async Task<Rider> UpsertRiderAsync(int? id, string name, CancellationToken ct = default)
        {
            if (id.HasValue && id.Value > 0)
            {
                var existing = await _db.Riders.FindAsync(new object[] { id.Value }, ct);
                if (existing != null)
                {
                    existing.Name = name ?? existing.Name;
                    await _db.SaveChangesAsync(ct);
                    return existing;
                }
            }

            return await CreateRiderAsync(name, ct);
        }

        public async Task<bool> DeleteRiderAsync(int id, CancellationToken ct = default)
        {
            // If there are rides referencing this rider, do not delete
            var hasRides = await _db.Rides.AnyAsync(r => r.RiderId == id, ct);
            if (hasRides) return false;

            var rider = await _db.Riders.FindAsync(new object[] { id }, ct);
            if (rider == null) return false;
            _db.Riders.Remove(rider);
            await _db.SaveChangesAsync(ct);
            return true;
        }
    }
}
