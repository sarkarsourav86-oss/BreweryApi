using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BreweryApi.Application.Interfaces
{
    public interface IDistanceCalculator
    {
        double CalculateMiles(double lat1, double lon1, double lat2, double lon2);
    }
}
