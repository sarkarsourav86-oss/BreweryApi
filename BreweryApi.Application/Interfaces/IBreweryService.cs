using BreweryApi.Application.Models.Requests;
using BreweryApi.Application.Models.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BreweryApi.Application.Interfaces
{
    public interface IBreweryService
    {
        Task<PagedResult<BreweryDto>> SearchAsync(BreweryQuery query, CancellationToken cancellationToken);
        Task<IReadOnlyList<AutocompleteItemDto>> AutocompleteAsync(string term, CancellationToken cancellationToken);
    }
}
