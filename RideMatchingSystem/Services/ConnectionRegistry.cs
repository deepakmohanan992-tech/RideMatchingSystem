using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace RideMatchingSystem.Services
{
    public interface IConnectionRegistry
    {
        void Add(int userId, string connectionId);
        void RemoveByConnection(string connectionId);
        IEnumerable<string> GetConnections(int userId);
    }

    // Simple in-memory registry mapping user id (driverId/riderId) to connection ids.
    public class ConnectionRegistry : IConnectionRegistry
    {
        private readonly ConcurrentDictionary<int, ConcurrentDictionary<string, byte>> _map = new();

        public void Add(int userId, string connectionId)
        {
            var set = _map.GetOrAdd(userId, _ => new ConcurrentDictionary<string, byte>());
            set[connectionId] = 0;
        }

        public void RemoveByConnection(string connectionId)
        {
            foreach (var kv in _map)
            {
                if (kv.Value.TryRemove(connectionId, out _))
                {
                    if (kv.Value.IsEmpty)
                        _map.TryRemove(kv.Key, out _);
                    break;
                }
            }
        }

        public IEnumerable<string> GetConnections(int userId)
        {
            if (_map.TryGetValue(userId, out var set))
                return set.Keys.ToList();
            return Enumerable.Empty<string>();
        }
    }
}
