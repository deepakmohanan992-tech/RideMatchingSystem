using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace RideMatchingSystem.Services
{
    public record OfferInfo(string OfferId, int RideId, int DriverId, DateTime ExpiresAt);

    public interface IOfferStore
    {
        void Add(OfferInfo offer);
        bool TryGet(string offerId, out OfferInfo? offer);
        void Remove(string offerId);
    }

    public class InMemoryOfferStore : IOfferStore
    {
        private readonly ConcurrentDictionary<string, OfferInfo> _offers = new();

        public void Add(OfferInfo offer)
        {
            _offers[offer.OfferId] = offer;
        }

        public bool TryGet(string offerId, out OfferInfo? offer)
        {
            if (_offers.TryGetValue(offerId, out var o))
            {
                if (o.ExpiresAt < DateTime.UtcNow)
                {
                    _offers.TryRemove(offerId, out _);
                    offer = null;
                    return false;
                }
                offer = o;
                return true;
            }
            offer = null;
            return false;
        }

        public void Remove(string offerId)
        {
            _offers.TryRemove(offerId, out _);
        }
    }
}
