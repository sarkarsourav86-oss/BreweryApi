using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BreweryApi.Domain.ValueObjects
{
    public sealed class Coordinates
    {
        public double Latitude { get; }
        public double Longitude { get; }

        public Coordinates(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }
    }
}
