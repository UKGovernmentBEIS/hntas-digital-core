using AutoMapper;
using HNTAS.Core.Api.Constants;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Extensions;
using HNTAS.Core.Api.Helpers;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models;
using HNTAS.Core.Api.Models.HeatNetwork;
using HNTAS.Core.Api.Models.Soa;
using HNTAS.Core.Api.Models.Users;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace HNTAS.Core.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public partial class HeatNetworksController : ControllerBase
    {
        private readonly IHeatNetworkService _hnService;
        private readonly ILogger<HeatNetworksController> _logger;
        private readonly ICounterService _counterService;
        private readonly IMapper _mapper;
        private readonly IAuditService _auditService;
        private readonly IUserService _userService;
        private readonly IEmailService _emailService;
        private readonly IInvitationService _invitationService;
        private readonly IOrganisationService _organisationService;
        private readonly INotificationHistoryService _notificationHistoryService;

        public HeatNetworksController(IHeatNetworkService hnService, ILogger<HeatNetworksController> logger, ICounterService counterService, IMapper mapper, IUserService userService, IEmailService emailService, IInvitationService invitationService, IAuditService auditService, IOrganisationService organisationService, INotificationHistoryService notificationHistoryService)
        {
            _hnService = hnService;
            _logger = logger;
            _counterService = counterService;
            _mapper = mapper;
            _auditService = auditService;
            _userService = userService;
            _emailService = emailService;
            _invitationService = invitationService;
            _notificationHistoryService = notificationHistoryService;
            _organisationService = organisationService;
        }

        /// <summary>
        /// Retrieves a list of all heat networks available in the system.
        /// </summary>
        /// <returns>A list of heat network response objects.</returns>
        [HttpGet] // This defines the route as GET /api/HeatNetworks
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<HeatNetworkResponse>))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<HeatNetworkResponse>>> GetHeatNetworks()
        {
            _logger.LogInformation("Attempting to retrieve all heat networks.");
            try
            {
                var heatNetworks = await _hnService.GetAsync();
                var heatNetworksResponse = _mapper.Map<List<HeatNetworkResponse>>(heatNetworks);
                _logger.LogInformation("Successfully retrieved {HeatNetworkCount} heatNetworks.", heatNetworks.Count);

                return Ok(heatNetworksResponse); // Returns 200 OK with the list of heat networks
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving all heat networks.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while retrieving heat networks.");
            }
        }

        [HttpGet("hnIds")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(List<HeatNetworkResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<HeatNetworkResponse>>> GetHeatNetworksByHnIds([FromQuery] string hnIdsString)
        {
            // Split the comma-separated string of IDs into a List<string>
            List<string> hnIds = hnIdsString?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>();

            // Validate input IDs
            if (hnIds == null || !hnIds.Any())
            {
                _logger.LogWarning("GetHeatNetworksByIds called with no IDs provided in the query string.");
                return BadRequest("Please provide at least one heat network ID in the query string (e.g., /api/heatnetwork/list?ids=id1,id2).");
            }

            try
            {
                var heatNetworks = await _hnService.GetByHnIdsAsync(hnIds);

                if (heatNetworks == null || !heatNetworks.Any())
                {
                    _logger.LogInformation("No heat networks found for the provided IDs: {HeatNetworkIds}", string.Join(", ", hnIds?.Select(x => x.ToSafeLog()).ToArray()!));
                    return NotFound("No heat networks found for the given IDs.");
                }

                var heatNetworksResponse = _mapper.Map<List<HeatNetworkResponse>>(heatNetworks);

                return Ok(heatNetworksResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving heat networks for IDs: {HeatNetworkIds}", string.Join(", ", hnIds.Select(x => x.ToSafeLog()).ToArray()));
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while retrieving heat networks.");
            }
        }

        [HttpGet("{hnId}")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(HeatNetworkResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<HeatNetworkResponse>> GetHeatNetworkByHnId(string hnId)
        {
            // Validate input ID
            if (string.IsNullOrEmpty(hnId))
            {
                _logger.LogWarning("GetHeatNetworkByHnId called with a null or empty ID.");
                return BadRequest("Please provide a valid heat network ID in the URL.");
            }

            try
            {
                var heatNetwork = await _hnService.GetByHnIdAsync(hnId);

                if (heatNetwork == null)
                {
                    _logger.LogInformation("No heat network found for the provided ID: {HeatNetworkId}", StringFormatter.Sanitize(hnId));
                    return NotFound("No heat network found for the given ID.");
                }

                var heatNetworkResponse = _mapper.Map<HeatNetworkResponse>(heatNetwork);

                return Ok(heatNetworkResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving heat network for ID: {HeatNetworkId}", StringFormatter.Sanitize(hnId));
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while retrieving the heat network.");
            }
        }

        [HttpGet("heat-network-by-userId")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(List<HeatNetworkResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<HeatNetworkResponse>>> GetHeatNetworksByUserId(string userId, RegistrationSource registrationSource = RegistrationSource.HNTAS)
        {
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("GetHeatNetworksByUserId called with empty user Id");
                return BadRequest("Please provide a valid user Id.");
            }
            try
            {
                var userDetails = await _userService.GetByIdAsync(userId);
                var heatNetworks = new List<HeatNetworkResponse>();
                foreach (var hnMapping in userDetails.HnRoleMappings)
                {
                    var heatNetwork = await _hnService.GetByHnIdAndRegistrationSourceAsync(hnMapping.HnId, registrationSource);

                    if (heatNetwork == null)
                    {
                        _logger.LogInformation("No heat networks found for the provided ID: {HeatNetworkId}", StringFormatter.Sanitize(hnMapping.HnId));
                    }
                    else
                    {
                        var heatNetworkResponse = _mapper.Map<HeatNetworkResponse>(heatNetwork);
                        heatNetworks.Add(heatNetworkResponse);
                    }
                }
                return heatNetworks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving heat networks for ID: {UserId}", StringFormatter.Sanitize(userId));
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while retrieving the heat networks.");
            }
        }

        [HttpGet("heat-network-by-userId-paginated")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(PagedResult<UserNetworkDetailsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PagedResult<UserNetworkDetailsResponse>>> GetHeatNetworksByUserIdPaginated(
            [FromQuery] string userId,
            [FromQuery] RegistrationSource registrationSource = RegistrationSource.HNTAS,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string sortBy = "Name",
            [FromQuery] string sortDirection = "asc")
        {
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("GetHeatNetworksByUserId called with empty user Id");
                return BadRequest("Please provide a valid user Id.");
            }

            if (pageNumber < 1 || pageSize < 1)
            {
                return BadRequest("Page number and page size must be greater than 0.");
            }

            try
            {
                var userDetails = await _userService.GetByIdAsync(userId);
                if (userDetails == null || userDetails.HnRoleMappings == null || !userDetails.HnRoleMappings.Any())
                {
                    return NotFound("User or user role mappings not found.");
                }

                // Collect all HnIds for the user
                var hnIds = userDetails.HnRoleMappings.Select(x => x.HnId).Distinct().ToList();

                // Fetch paginated data from MongoDB
                var (heatNetworks, totalCount) = await _hnService.GetByHnIdsAndRegistrationSourcePaginatedAsync(
                    hnIds, registrationSource, pageNumber, pageSize, sortBy, sortDirection);

                var result = new PagedResult<UserNetworkDetailsResponse>
                {
                    Items = heatNetworks,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = (int)totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving heat networks for ID: {UserId}", StringFormatter.Sanitize(userId));
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while retrieving the heat networks.");
            }
        }

        [HttpGet("existing-network-by-userId")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(ExistingNetworkResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ExistingNetworkResponse>> GetExistingNetworksByUserId(ExistingNetworkRequest request)
        {
            if (string.IsNullOrEmpty(request.UserId))
            {
                _logger.LogWarning("GetExistingNetworksByUserId called with empty user Id");
                return BadRequest("Please provide a valid user Id.");
            }
            try
            {
                var existingNetworks = await _hnService.GetExistingNetworks(request);
                if (existingNetworks is null)
                {
                    _logger.LogWarning("Existing network records are not found");
                    return NotFound();
                }

                return Ok(existingNetworks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving existing networks");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while retrieving the heat networks.");
            }
        }



        [HttpPost("add-heat-network")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(HeatNetworkResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<HeatNetworkResponse>> AddHeatNetwork([FromBody] HeatNetwork heatNetworkDetails)
        {
            try
            {
                // Create new heat network
                if (String.IsNullOrWhiteSpace(heatNetworkDetails.HnId))
                {
                    var sequenceID = await _counterService.GetNextSequenceValue("heatNetworkId_sequence");
                    var heatNetworkId = $"HN{sequenceID:D7}";
                    heatNetworkDetails.HnId = heatNetworkId;
                    heatNetworkDetails.UHnId = sequenceID.ToString();
                    _logger.LogInformation("Generated new heat network ID: {HeatNetworkId}", heatNetworkDetails.HnId.ToSafeLog());
                }
                UserDetailsResult user = await _userService.GetUserWithDetailsAsync(heatNetworkDetails.CreatedBy);
                await _hnService.CreateAsync(heatNetworkDetails, true);
                _logger.LogInformation("New heat network initially registered: {HNID} (DB Id: {Id})", heatNetworkDetails.HnId.ToSafeLog(), heatNetworkDetails.Id.ToSafeLog());


                ContributorRole role = user.Roles[0] switch
                {
                    UserRole.ResponsibleParty => ContributorRole.ResponsibleParty,
                    UserRole.NetworkManager => ContributorRole.NetworkManager
                };

                // Add Hn Mapping to the user
                User userWithUpdatedHnRoleMapping = await _userService.GetByIdAsync(heatNetworkDetails.CreatedBy);
                userWithUpdatedHnRoleMapping.HnRoleMappings.Add(new HnRoleMapping { HnId = heatNetworkDetails.HnId, Role = role });
                await _userService.UpdateAsync(heatNetworkDetails.CreatedBy, userWithUpdatedHnRoleMapping);

                if (role == ContributorRole.ResponsibleParty)
                {
                    //find the network managers
                    var allNetworkManagers = await _invitationService.GetNetworkManagersByInviterUserId(heatNetworkDetails.CreatedBy);
                    var acceptedNetworkManagers = allNetworkManagers.Where(nm => nm.Status == InvitationStatus.Accepted).ToList();

                    // All networks managers reporting to the rp can access all heat networks the rp adds
                    foreach (var nm in acceptedNetworkManagers)
                    {
                        if (nm.Status == InvitationStatus.Accepted)
                        {
                            User nmWithUpdatedHnRoleMapping = await _userService.GetByEmailAsync(nm.InvitedEmail);
                            nmWithUpdatedHnRoleMapping.HnRoleMappings.Add(new HnRoleMapping { HnId = heatNetworkDetails.HnId, Role = ContributorRole.NetworkManager });
                            await _userService.UpdateAsync(nmWithUpdatedHnRoleMapping.Id, nmWithUpdatedHnRoleMapping);
                        }
                    }
                }
                if (role == ContributorRole.NetworkManager)
                {
                    // The Rp of the organisation that the networks are added to should be able to view them too, irrespective of who added them
                    var orgDetails = await _organisationService.GetByOrgIdAsync(heatNetworkDetails.OrgId);
                    var rpUserId = orgDetails.RpUserId;
                    var rpUser = await _userService.GetByIdAsync(rpUserId);
                    rpUser.HnRoleMappings.Add(new HnRoleMapping { HnId = heatNetworkDetails.HnId, Role = ContributorRole.ResponsibleParty });
                    await _userService.UpdateAsync(rpUser.Id, rpUser);
                }
                _logger.LogInformation("New heat network role mapping updated");

                string userEmail = user.EmailId;
                string fullName = user.FullName;
                string hnId = heatNetworkDetails.HnId;
                string hnName = heatNetworkDetails.Name;
                await _emailService.TrySendHeatNetworkRegistrationEmailAsync(userEmail, fullName, hnId, hnName);
                await NotificationHistoryForAddingHeatNetwork(heatNetworkDetails, user);
                return CreatedAtAction(nameof(AddHeatNetwork), new { id = heatNetworkDetails.Id }, heatNetworkDetails);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Internal Server Error",
                    Detail = "An unexpected error occurred during initial user registration."
                });
            }
        }

        [HttpPut("register-ofgem-network")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(HeatNetworkResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<HeatNetworkResponse>> RegisterOfgemNetwork([FromBody] HeatNetwork heatNetworkDetails)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(heatNetworkDetails.HnId))
                {
                    return BadRequest("Invalid heat network details.");
                }
                await _hnService.UpdateAsync(heatNetworkDetails.HnId, heatNetworkDetails);
                _logger.LogInformation("Heat network initially registered: {HNID} (DB Id: {Id})", heatNetworkDetails.HnId.ToSafeLog(), heatNetworkDetails.Id.ToSafeLog());                
                
                return Ok(heatNetworkDetails);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Internal Server Error",
                    Detail = "An unexpected error occurred during ofgem network registration."
                });
            }
        }

        /// <summary>
        /// Updates the NetworkElements for a given heat network identified by HnId.
        /// </summary>
        [HttpPut("network-elements")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(HeatNetworkResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<HeatNetworkResponse>> UpdateNetworkElements([FromBody] NetworkElements request, string hnId)
        {
            if (string.IsNullOrWhiteSpace(hnId))
            {
                _logger.LogWarning("UpdateNetworkElements called with empty HnId.");
                return BadRequest("Please provide a valid heat network HnId.");
            }

            if (request == null)
            {
                _logger.LogWarning("UpdateNetworkElements called without NetworkElements payload for HnId: {HnId}", StringFormatter.Sanitize(hnId));
                return BadRequest("Please provide NetworkElements to update.");
            }

            try
            {
                var existingHeatNetwork = await _hnService.GetByHnIdAsync(hnId);
                if (existingHeatNetwork == null)
                {
                    _logger.LogInformation("No heat network found for HnId: {HnId}", StringFormatter.Sanitize(hnId));
                    return NotFound($"No heat network found for HnId '{hnId}'.");
                }

                var existingHeatNetworkSnapshot = System.Text.Json.JsonSerializer.Deserialize<HeatNetwork>(
                    System.Text.Json.JsonSerializer.Serialize(existingHeatNetwork)
                )!;

                var requestWithInstances = GenerateElementsInstances(request);
                existingHeatNetwork.NetworkElements = requestWithInstances;

                await _hnService.UpdateAsync(hnId, existingHeatNetwork);

                // Only log an audit event if NetworkElements were previously null, to capture the addition of elements rather than updates to existing elements
                var isRegistrationEnabledString = Environment.GetEnvironmentVariable("IS_REGISTRATION_ENABLED");
                if (!string.IsNullOrEmpty(isRegistrationEnabledString) &&
                    isRegistrationEnabledString.ToLower() == "true" && existingHeatNetworkSnapshot.NetworkElements == null)
                {
                    await _auditService.SaveAuditAsync<HeatNetwork>(
                        entryType: HeatNetworkEvents.NetworkElementsAdded,
                        actorId: existingHeatNetwork.NetworkElements.CreatedBy,
                        entityId: existingHeatNetwork.HnId!,
                        oldState: existingHeatNetworkSnapshot,
                        newState: existingHeatNetwork,
                        elementName: "All Elements",
                        phase: existingHeatNetwork.Phase,
                        stage: HeatNetworkHelper.GetStageFromPhase(existingHeatNetwork.Phase)
                        );
                }

                _logger.LogInformation("Updated NetworkElements for HnId: {HnId}", StringFormatter.Sanitize(hnId));
                var response = CreatedAtAction(nameof(UpdateNetworkElements), new { id = existingHeatNetwork.Id }, existingHeatNetwork);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating NetworkElements for HnId: {HnId}", StringFormatter.Sanitize(hnId));
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while updating the heat network.");
            }
        }

        private async Task NotificationHistoryForAddingHeatNetwork(HeatNetwork heatNetwork, UserDetailsResult user)
        {
            var userRole = user.Roles?.FirstOrDefault();
            var actorIds = new List<string>() { user.Id };
            var eligibleRoles = new List<string>();
            var description = $"{heatNetwork.HnId} - {heatNetwork.Name} registered";
            var notificationType = NotificationHistoryType.NA;
            var subject = NotificationHistorySubjects.NewBuildNetworkRegistered;
            if (userRole == UserRole.ResponsibleParty)
            {
                eligibleRoles.Add(UserRole.ResponsibleParty.ToString());
                notificationType = NotificationHistoryType.RpRegistersHeatNetwork;
            }
            else
            {
                var invitation = await _invitationService.GetByInvitedEmailAsync(user.EmailId!);
                if (invitation != null)
                    actorIds.Add(invitation.InviterUserId);
                eligibleRoles.Add(UserRole.ResponsibleParty.ToString());
                eligibleRoles.Add(UserRole.NetworkManager.ToString());
                notificationType = NotificationHistoryType.NetworkManagerRegistersHeatNetwork;
            }
            var notificationHistory = new NotificationHistory
            {
                NotificationType = notificationType,
                ActorsId = actorIds,
                Subject = subject,
                Description = description,
                Timestamp = DateTime.UtcNow,
                Action = NotificationHistoryActions.HeatNetworkDetails,
                EligibleRoles = eligibleRoles,
                HeatNetworkId = heatNetwork.HnId,
                CreatedBy = user.Id
            };

            await _notificationHistoryService.CreateAsync(notificationHistory);
        }

        private NetworkElements GenerateElementsInstances(NetworkElements networkElements)
        {
            var elementGroups = networkElements.ElementsGroup;
            var elementInstances = new List<Element>();
            var instanceCounter = 1;
            var index = 1;
            foreach (var element in elementGroups!)
            {

                if (element.Count == 1)
                {
                    var ele = new Element();
                    ele.ElementId = (index).ToString("D5");
                    ele.ElementType = element.ElementType;
                    ele.NetworkElementInstanceName = GetNetworkElementLabelByElementType(element.ElementDisplayType);
                    elementInstances.Add(ele);
                    index++;
                    continue;
                }
                instanceCounter = 1;
                for (int i = 0; i < element.Count; i++)
                {
                    elementInstances.Add(
                        new Element
                        {
                            ElementId = (index).ToString("D5"),
                            ElementType = element.ElementType,
                            NetworkElementInstanceName = GetNetworkElementLabelByElementType(element.ElementDisplayType) + " - " + instanceCounter
                        });
                    index++;
                    instanceCounter++;
                }
            }
            networkElements.Elements = elementInstances;
            return networkElements;
        }

        public static string GetNetworkElementLabelByElementType(HeatNetworkElementType elementType)
        {
            return elementType switch
            {
                HeatNetworkElementType.EnergyCentre => "Energy Centre",
                HeatNetworkElementType.Substation => "Substation",
                HeatNetworkElementType.DistrictDistribution => "District Distribution Network",
                HeatNetworkElementType.ConsumerConnection => "Consumer Connection",
                HeatNetworkElementType.CommunalDistribution => "Communal Distribution Network",
                _ => throw new ArgumentOutOfRangeException(nameof(elementType), $"Not expected heat network element ID value: {elementType}")
            };
        }

        //public static string GetNetworkElementLabelByElementType(string elementType)
        //{
        //    return elementType switch
        //    {
        //        "EC" => "Energy Centre",
        //        "SS" => "Substation",
        //        "DDN" => "District Distribution Network",
        //        "CC" => "Consumer Connection",
        //        "CDN" => "Communal Distribution Network",
        //        _ => throw new ArgumentOutOfRangeException(nameof(elementType), $"Not expected heat network element ID value: {elementType}")
        //    };
        //}
    }
}
