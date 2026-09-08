using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Data.Models.External;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Models.AssignedAssessor;
using HNTAS.Core.Api.Models.HeatNetwork;
using HNTAS.Core.Api.Models.Users;

namespace HNTAS.Core.Api.Interfaces
{
    public interface IHeatNetworkService
    {
        Task<List<HeatNetwork>> GetAsync();
        Task<List<HeatNetwork>> GetByHnIdsAsync(List<string> ids);
        Task<HeatNetwork> GetByHnIdAsync(string hnId);
        Task CreateAsync(HeatNetwork newHeatNetwork, bool isNewHeatNetwork = false);
        Task UpdateAsync(string id, HeatNetwork updatedHn);
        Task<List<HeatNetwork>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate);


        // --- Enriched Detail Methods (External/Response DTOs) ---
        Task<HeatNetworkExternalResponse> GetDetailsByHnIdAsync(string hnId);
        Task<List<HeatNetworkExternalResponse>> GetDetailsAsync();
        Task<List<HeatNetworkExternalResponse>> GetDetailsByDateRangeAsync(DateTime fromDate, DateTime toDate);
        //Task UpdateMeteringAndMonitoringStrategyAsync(string hnId, NetworkDetailsDocument document);
        //Task UpdateAssessmentPlanAsync(string hnId, NetworkDetailsDocument document);
        //Task UpdateDesignConstructionLogAsync(string hnId, NetworkDetailsDocument document);
        Task<AssignedAssessorResponse> GetAssignedAssessors(AssignedAssessorRequest request);
        Task<List<HeatNetwork>> GetByOfgemEmailIdAsync(string ofgemEmailId);
        Task<HeatNetwork> GetByHnIdAndRegistrationSourceAsync(string hnId, RegistrationSource registrationSource);
        Task<ExistingNetworkResponse> GetExistingNetworks(ExistingNetworkRequest existingNetworkRequest);

        Task<(List<UserNetworkDetailsResponse> Items, long TotalCount)> GetByHnIdsAndRegistrationSourcePaginatedAsync(
               List<string> hnIds,
               RegistrationSource registrationSource,
               int pageNumber,
               int pageSize,
               string sortBy,
               string sortDirection);
    }
}
