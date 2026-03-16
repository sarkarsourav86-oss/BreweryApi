using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BreweryApi.Application.Models.Responses
{
    public sealed class BreweryDto
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string City { get; init; } = string.Empty;
        public string? Phone { get; init; }
        public double? DistanceMiles { get; init; }
    }
}
