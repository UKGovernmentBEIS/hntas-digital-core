using HNTAS.Core.Api.Data.Models;

namespace HNTAS.Core.Api.Interfaces
{
    public interface IHeatNetworkService
    {
        Task<List<HeatNetwork>> GetAsync();
        Task<List<HeatNetwork>> GetByHnIdsAsync(List<string> ids);
        Task CreateAsync(HeatNetwork newHeatNetwork);
    }
}
