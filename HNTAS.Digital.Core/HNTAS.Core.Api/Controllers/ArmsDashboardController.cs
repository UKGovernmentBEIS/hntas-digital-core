using AutoMapper;
using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models.Arms.Submission;
using HNTAS.Core.Api.Extensions;
using HNTAS.Core.Api.Helpers;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models;
using HNTAS.Core.Api.Models.Arms.Dashboard;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Bson;

namespace HNTAS.Core.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArmsDashboardController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IHeatNetworkService _networkService;
        private readonly IArmsKpiService _armsKpiService;
        private readonly IKpiSubmissionAuditService _auditService;
        private readonly ILogger<ArmsDashboardController> _logger;
        private readonly ISuperUserService _superUserService;
        private readonly ArmsSettings _armsSettings;
        private readonly IUnitService _unitService;

        public ArmsDashboardController(IUserService userService,
            IHeatNetworkService networkService,
            IArmsKpiService armsKpiService,
            IMapper mapper,
            IKpiSubmissionAuditService auditService,
            ILogger<ArmsDashboardController> logger,
            ISuperUserService superUserService,
            IOptions<ArmsSettings> armsSettings,
            IUnitService unitService)
        {
            _userService = userService;
            _networkService = networkService;
            _armsKpiService = armsKpiService;
            _auditService = auditService;
            _logger = logger;
            _superUserService = superUserService;
            _armsSettings = armsSettings.Value;
            _unitService = unitService;
        }

        [HttpGet("get-kpi-networks-by-rp-user")]
        [ProducesResponseType(typeof(HeatNetworkDashboardResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<HeatNetworkDashboardResponse>> GetKpiNetworksByRpUser(
        [FromQuery] string userId,
        [FromQuery] int? month,
        [FromQuery] int year,
        [FromQuery] int pageNumber = 1)
        {
            const int pageSize = 10;

            // 1. Validate User and Role
            var userDetails = await _userService.GetUserWithDetailsAsync(userId);
            if (userDetails == null) return NotFound("User not found");

            bool isRpUser = userDetails.Roles?.Contains(HNTAS.Core.Api.Enums.UserRole.ResponsibleParty) ?? false;
            bool isSuperUser = _armsSettings.AllowSuperUserAccess && await _superUserService.IsSuperUserAsync(userDetails.EmailId);
            bool isAuthorized = isRpUser || isSuperUser;

            if (!isAuthorized)
            {
                return BadRequest("Only Responsible Parties can access this endpoint");
            }

            // 2. Get the full list of Authorized Networks (The Master List)
            var authorizedNetworks = new List<HeatNetworkUserResponse>();
            if (isRpUser)
            {
                authorizedNetworks = UserNetworkHelper.GetAuthorizedNetworks(userDetails);
                if (authorizedNetworks == null || !authorizedNetworks.Any())
                {
                    return Ok(new HeatNetworkDashboardResponse());
                }
            }
            else if (isSuperUser)
            {
                var allNetworks = await _networkService.GetAsync();
                authorizedNetworks = allNetworks.Select(n => new HeatNetworkUserResponse
                {
                    HnId = n.HnId,
                    Name = n.Name
                }).ToList();
            }


            // 3. Prepare Period String
            // If month is null, periodStr becomes "2026", triggering the Regex search
            string periodStr = month.HasValue
                ? $"{year}-{month.Value:D2}"
                : $"{year}";

            var allowedHnids = authorizedNetworks.Select(n => n.HnId).ToList();

            // 1. Fetch submissions (if any)
            var submissions = await _armsKpiService.GetSubmissionsAsync(allowedHnids, periodStr)
                              ?? new List<KpiSubmission>();

            // 2. Merge Data - One loop handles both "Has Submissions" and "Zero Submissions"
            // Declaring 'allRows' outside the blocks so it is accessible for pagination
            var allRows = submissions.Select(submission =>
            {
                // Find the network name from your authorized list based on the submission's ID
                var network = authorizedNetworks.FirstOrDefault(n => n.HnId == submission.MetaData.NetworkId);

                string formattedPeriod = "N/A";
                if (DateTime.TryParse(submission.MetaData.PeriodStart, out DateTime periodDate))
                {
                    formattedPeriod = periodDate.ToString("MMMM yyyy");
                }

                return new HeatNetworkDashboardRow
                {
                    HnId = submission.MetaData.NetworkId,
                    NetworkName = network?.Name ?? "Unknown Network", // Fallback if name isn't found
                    Provider = submission.MetaData?.SourceSystem ?? "N/A",
                    DataPeriod = formattedPeriod,
                    SubmissionId = submission.Id.ToString(),
                    LastUpdated = submission.UpdatedAt
                };
            }).ToList();

            // 3. Calculate Pagination Metadata (allRows is now in scope)
            int totalCount = allRows.Count;
            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            // 4. Slice the data for the requested page
            var items = allRows
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(new HeatNetworkDashboardResponse
            {
                Items = items,
                TotalCount = totalCount,
                TotalPages = totalPages,
                CurrentPage = pageNumber
            });
        }

        [HttpGet("get-kpi-network-details")]
        [ProducesResponseType(typeof(HeatNetworkDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<HeatNetworkDetailsResponse>> GetKpiNetworkDetailsByRpUser(
        [FromQuery] string submissionId,
        [FromQuery] string? statusFilter,
        [FromQuery] string? typeFilter,
        [FromQuery] int page = 1)
        {
            const int pageSize = 10;

            var submission = await _armsKpiService.GetSubmissionByIdAsync(submissionId);
            if (submission == null) return NotFound("Submission not found");

            // 1. Process Comma-Separated Status Filters
            var activeStatusFilters = statusFilter?
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToList();
            if (activeStatusFilters?.Count == 0) activeStatusFilters = null;

            // 2. Process Comma-Separated Type Filters
            var activeTypeFilters = typeFilter?
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToList();
            if (activeTypeFilters?.Count == 0) activeTypeFilters = null;

            int displayYear = DateTime.Now.Year;
            int displayMonth = DateTime.Now.Month;

            if (!string.IsNullOrEmpty(submission.MetaData.PeriodStart))
            {
                var parts = submission.MetaData.PeriodStart.Split('-');
                if (parts.Length == 2)
                {
                    int.TryParse(parts[0], out displayYear);
                    int.TryParse(parts[1], out displayMonth);
                }
            }

            var networkInfo = await _networkService.GetByHnIdAsync(submission.MetaData.NetworkId);
            if (networkInfo == null) return NotFound("Network info not found");

            // 1. Map NetworkElements to our DTO while applying the status filter
            var allElements = submission.Elements
                .Where(e => activeTypeFilters == null || activeTypeFilters.Contains(e.Type.ToString()))
                .Select(e => new ElementGroupDto
                {
                    ElementId = e.ElementId,
                    ElementType = e.Type.ToString(),
                    Kpis = e.Kpis
                    .Where(kvp => activeStatusFilters == null || !activeStatusFilters.Any() || activeStatusFilters.Contains(kvp.Value.AssessmentStatus.ToString()))
                    .Select(kvp => new KpiDetailDto
                    {
                        KpiName = kvp.Key,
                        Value = kvp.Value.Value,
                        Status = kvp.Value.AssessmentStatus.GetDescription(),
                        IsImputed = kvp.Value.IsKpiImputed,
                        ImputationDetails = kvp.Value.KpiImputationDetails,
                        Unit = _unitService.GetUnit(kvp.Key)
                    }).ToList()
                })
            // Only include elements that actually have KPIs after filtering
            .Where(e => e.Kpis.Any())
            .OrderBy(e => e.ElementId)
            .ToList();

            // 2. Pagination
            int totalElements = allElements.Count;
            int totalPages = (int)Math.Ceiling(totalElements / (double)pageSize);

            var pagedElements = allElements
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var aggregatedKpis = submission.ConsumerConnectionAggregatedKpis?
                .Where(kvp => activeStatusFilters == null || !activeStatusFilters.Any() || activeStatusFilters.Contains(kvp.Value.AssessmentStatus.ToString()))
                .Select(kvp => new AggregatedKpi
                {
                    KpiName = kvp.Key,
                    Value = kvp.Value.Value,
                    Status = kvp.Value.AssessmentStatus.GetDescription(),
                    Unit = _unitService.GetUnit(kvp.Key)
                }).ToList() ?? null;

            var carbonUiInputs = new Dictionary<string, CarbonInputUiDisplay>();

            //get Config 
            var config = await _armsKpiService.GetConfigurationAsync(submission.MetaData.NetworkId);

            if (submission?.CarbonCalculatorInputs != null)
            {
                // Define your UI label mapping configuration
                var targetKeys = new[]
                {
                    (Section: "chp_totals", Key: "EC-DATA-53", Label: "CHP Useful Heat Generated", IsFromConfig: false),
                    (Section: "chp_totals", Key: "EC-DATA-55", Label: "CHP Electricity Generated", IsFromConfig: false),
                    (Section: "chp_totals", Key: "EC-DATA-57", Label: "CHP Fuel Used", IsFromConfig: false),
                    (Section: "chp_totals", Key: "EC-DATA-63", Label: "CHP Max Heat Output", IsFromConfig: true),
                    (Section: "chp_totals", Key: "EC-DATA-64", Label: "CHP Max Electricity Output", IsFromConfig: true),
                    (Section: "hpm_totals", Key: "EC-DATA-66", Label: "Heat Pump Useful Heat Generated", IsFromConfig: false),
                    (Section: "hpm_totals", Key: "EC-DATA-68", Label: "Heat Pump Energy Used", IsFromConfig: false),
                    (Section: "hpm_totals", Key: "EC-DATA-74", Label: "Heat Pump Max Heat Output", IsFromConfig: true),
                    (Section: "blr_totals", Key: "EC-DATA-84", Label: "Boiler Useful Heat Generated", IsFromConfig: false),
                    (Section: "blr_totals", Key: "EC-DATA-86", Label: "Boiler Fuel Used", IsFromConfig: false),
                    (Section: "blr_totals", Key: "EC-DATA-92", Label: "Boiler Fuel Max Heat Output", IsFromConfig: true)
                };

                // 1. Extract and safeguard the defaults dictionary BEFORE entering the loop
                var defaultsDictionary = config?.CarbonCalculator?.Defaults;

                foreach (var target in targetKeys)
                {
                    BsonValue? rawValue = null;

                    if (target.IsFromConfig)
                    {
                        // 2. Safe, clean dictionary lookup without TryGetValue or LINQ overhead
                        if (defaultsDictionary != null && defaultsDictionary.ContainsKey(target.Key))
                        {
                            rawValue = defaultsDictionary[target.Key];
                        }
                    }
                    else
                    {
                        // Always read from user submission data input
                        if (submission?.CarbonCalculatorInputs != null &&
                            submission.CarbonCalculatorInputs.TryGetValue(target.Section, out var section) &&
                            section.TryGetValue(target.Key, out var kpiValue) && kpiValue?.Value != null)
                        {
                            rawValue = kpiValue.Value;
                        }
                    }

                    // Process and add to output in the exact order requested
                    if (rawValue != null && BsonConversionHelper.TryGetDouble(rawValue, out var numericValue))
                    {
                        carbonUiInputs[target.Key] = new CarbonInputUiDisplay
                        {
                            Label = target.Label,
                            Value = numericValue,
                            Unit = _unitService.GetUnit(target.Key)
                        };
                    }
                }
            }

            return Ok(new HeatNetworkDetailsResponse
            {
                HnId = networkInfo.HnId,
                NetworkName = networkInfo.Name,
                SelectedMonth = displayMonth,
                SelectedYear = displayYear,
                GroupedElements = pagedElements,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalElements = totalElements,
                AggregatedKpis = aggregatedKpis,
                TotalCarbonEmission = submission?.CarbonCalculatorResponse?.TotalCarbonEmission,
                // Pass the mapped inputs straight to the frontend payload block
                CarbonCalculationInputs = carbonUiInputs.Any() ? carbonUiInputs : null
            });
        }

        [HttpGet("{submissionId}/history")]
        [ProducesResponseType(typeof(IEnumerable<KpiHistoryResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetSubmissionHistory(string submissionId)
        {
            // 1. Log the incoming request
            _logger.LogInformation("Received request for KPI history. SubmissionId: {SubmissionId}", submissionId.ToSafeLog());

            if (string.IsNullOrEmpty(submissionId))
            {
                _logger.LogWarning("GetSubmissionHistory called with null or empty SubmissionId.");
                return BadRequest("Submission ID is required.");
            }

            try
            {
                var history = await _auditService.GetHistoryBySubmissionIdAsync(submissionId);

                var historyCount = history?.Count() ?? 0;

                _logger.LogInformation("Successfully retrieved {Count} history records for SubmissionId: {SubmissionId}",
                    historyCount, submissionId.ToSafeLog());

                return Ok(history);
            }
            catch (Exception ex)
            {
                // 4. Capture the exact exception causing the red error in your logs
                _logger.LogError(ex, "Unhandled error fetching KPI history for SubmissionId: {SubmissionId}. Message: {Message}",
                    submissionId.ToSafeLog(), ex.Message);

                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving history.");
            }
        }
    }
}
