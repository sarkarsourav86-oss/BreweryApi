using BreweryApi.Application.Models.Responses;
using BreweryApi.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BreweryApi.Application.Interfaces
{
    public interface IBreweryMapper
    {
        BreweryDto Map(Brewery source, double? distanceMiles = null);
    }
}
