using HNTAS.Core.Api.Models;

namespace HNTAS.Core.Api.Interfaces
{
    public interface IHeatNetworkService
    {
        Task CreateAsync(HeatNetwork newHeatNetwork);
    }
}
