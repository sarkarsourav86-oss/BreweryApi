using BreweryApi.Application.Interfaces;
using BreweryApi.Application.Models.Responses;
using BreweryApi.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BreweryApi.Infrastructure.Mapping
{
    public sealed class BreweryMapper : IBreweryMapper
    {
        public BreweryDto Map(Brewery brewery, double? distanceMiles = null)
        {
            return new BreweryDto
            {
                Id = brewery.Id,
                Name = brewery.Name,
                City = brewery.City,
                Phone = brewery.Phone,
                DistanceMiles = distanceMiles
            };
        }
    }
}
