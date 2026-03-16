using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BreweryApi.Domain.Entities
{
    public sealed class Brewery
    {
        public string Id { get; }
        public string Name { get; }
        public string City { get; }
        public string? Phone { get; }
        public string BreweryType { get; }
        public double? Latitude { get; }
        public double? Longitude { get; }

        public Brewery(
            string id,
            string name,
            string city,
            string? phone,
            string breweryType,
            double? latitude,
            double? longitude)
        {
            Id = id;
            Name = name;
            City = city;
            Phone = phone;
            BreweryType = breweryType;
            Latitude = latitude;
            Longitude = longitude;
        }

        public bool IsOpen()
        {
            return !string.Equals(BreweryType, "closed", StringComparison.OrdinalIgnoreCase);
        }
    }
}
