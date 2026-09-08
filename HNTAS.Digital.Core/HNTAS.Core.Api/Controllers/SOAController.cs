using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Extensions;
using HNTAS.Core.Api.Helpers;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models;
using HNTAS.Core.Api.Models.Soa;
using Microsoft.AspNetCore.Mvc;

namespace HNTAS.Core.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SOAController : ControllerBase
    {
        private readonly ISoaService _soaService;
        private readonly ILogger<SOAController> _logger;
        private readonly IEmailService _emailService;
        private readonly IHeatNetworkService _heatNetworkService;
        private readonly IUserService _userService;
        private readonly IAuditService _auditService;
        private readonly INotificationHistoryService _notificationHistoryService;
        private readonly IInvitationService _invitationService;
        public SOAController(ISoaService soaProjectService, ILogger<SOAController> logger, IEmailService emailService, IHeatNetworkService heatNetworkService, IUserService userService, IAuditService auditService, INotificationHistoryService notificationHistoryService, IInvitationService invitationService)
        {
            _soaService = soaProjectService;
            _logger = logger;
            _emailService = emailService;
            _heatNetworkService = heatNetworkService;
            _userService = userService;
            _auditService = auditService;
            _notificationHistoryService = notificationHistoryService;
            _invitationService = invitationService;
        }        

        [HttpPatch("update-soa-status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateSoaStatus([FromBody] ElementSoaStatusUpdateRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid SaveDocument request: {@Errors}",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Saving statuses for HN ID: {HnId}, Element:{ElementId}, Stage: {Stage}, UpdatedBy: {UpdatedBy}",
                 StringFormatter.Sanitize(request.HnId), StringFormatter.Sanitize(request.ElementId!), request.Stage, StringFormatter.Sanitize(request.SoaStatusUpdatedBy!));

            try
            {
                var existingHeatNetwork = await _heatNetworkService.GetByHnIdAsync(request.HnId);
                if (existingHeatNetwork == null)
                {
                    _logger.LogInformation("No heat network found for HnId: {HnId}", StringFormatter.Sanitize(request.HnId));
                    return NotFound($"No heat network found for HnId '{request.HnId}'.");
                }

                await _soaService.UpdateSoaStatus(request.HnId, request.ElementType, request.Stage, request.SoaStatuses!, request.SoaStatusUpdatedBy!, request.ElementSoaStatus);

                _logger.LogInformation("Updated statuses successfully for HN ID: {HnId}, Element:{ElementId}, Stage: {Stage}, UpdatedBy: {UpdatedBy}",
                 StringFormatter.Sanitize(request.HnId), StringFormatter.Sanitize(request.ElementId!), request.Stage, StringFormatter.Sanitize(request.SoaStatusUpdatedBy!));

                var isRegistrationEnabledString = Environment.GetEnvironmentVariable("IS_REGISTRATION_ENABLED");
                if (!string.IsNullOrEmpty(isRegistrationEnabledString) &&
                    isRegistrationEnabledString.ToLower() == "true")
                {
                    var updatedHeatNetwork = await _heatNetworkService.GetByHnIdAsync(request.HnId);

                    await _auditService.SaveAuditAsync<HeatNetwork>(
                        entryType: "SOA- Element status updated",
                        actorId: request.SoaStatusUpdatedBy!,
                        entityId: existingHeatNetwork.HnId!,
                        oldState: existingHeatNetwork,
                        newState: updatedHeatNetwork,
                        elementName: HeatNetworkHelper.GetNetworkElementLabelByElementId(request.ElementType.ToString()),
                        phase: request.SoaPhase!,
                        stage: request.Stage.ToString()
                    );
                }


                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update statuses for HN ID: {HnId}, Element:{ElementId}, Stage: {Stage}, UpdatedBy: {UpdatedBy}",
                 StringFormatter.Sanitize(request.HnId), StringFormatter.Sanitize(request.ElementId!), request.Stage, StringFormatter.Sanitize(request.SoaStatusUpdatedBy!));
                throw;
            }
        }

        [HttpPatch("update-soa-status-for-existing-network")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateSoaStatusForExistingNetwork([FromBody] ElementSoaStatusUpdateRequestForExistingNetwork request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid SaveDocument request: {@Errors}",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Saving statuses for existing HN ID: {HnId}, Element:{ElementId}, Milestone: {Milestone}, UpdatedBy: {UpdatedBy}",
                 StringFormatter.Sanitize(request.HnId), StringFormatter.Sanitize(request.ElementId!), request.Milestone, StringFormatter.Sanitize(request.SoaStatusUpdatedBy!));

            try
            {
                var existingHeatNetwork = await _heatNetworkService.GetByHnIdAsync(request.HnId);
                if (existingHeatNetwork == null)
                {
                    _logger.LogInformation("No heat network found for existing HnId: {HnId}", StringFormatter.Sanitize(request.HnId));
                    return NotFound($"No heat network found for HnId '{request.HnId}'.");
                }

                await _soaService.UpdateSoaStatusForExistingNetwork(request.HnId, request.ElementType, request.Milestone, request.SoaStatuses!, request.SoaStatusUpdatedBy!, request.ElementSoaStatus);

                _logger.LogInformation("Updated statuses successfully for existing HN ID: {HnId}, Element:{ElementId}, Milestone: {Milestone}, UpdatedBy: {UpdatedBy}",
                 StringFormatter.Sanitize(request.HnId), StringFormatter.Sanitize(request.ElementId!), request.Milestone, StringFormatter.Sanitize(request.SoaStatusUpdatedBy!));

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update statuses for HN ID: {HnId}, Element:{ElementId}, Milestone: {Milestone}, UpdatedBy: {UpdatedBy}",
                 StringFormatter.Sanitize(request.HnId), StringFormatter.Sanitize(request.ElementId!), request.Milestone, StringFormatter.Sanitize(request.SoaStatusUpdatedBy!));
                throw;
            }
        }

        [HttpPatch("soa-assign-assessor")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SoaAssignAssessor([FromBody] ElementSoaAssignAssessorRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid SaveDocument request: {@Errors}",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Saving Assessor Assigned for HN ID: {HnId}, UpdatedBy: {UpdatedBy}",
                StringFormatter.Sanitize(request.HnId), StringFormatter.Sanitize(request.UpdatedBy));

            try
            {
                var existingHeatNetwork = await _heatNetworkService.GetByHnIdAsync(request.HnId);
                if (existingHeatNetwork == null)
                {
                    _logger.LogInformation("No heat network found for HnId: {HnId}", StringFormatter.Sanitize(request.HnId));
                    return NotFound($"No heat network found for HnId '{request.HnId}'.");
                }

                var networkElements = existingHeatNetwork.NetworkElements;

                await _soaService.UpdateAssignAssessor(request, networkElements!, existingHeatNetwork.Phase, true);
                existingHeatNetwork = await _heatNetworkService.GetByHnIdAsync(request.HnId);
                networkElements = existingHeatNetwork.NetworkElements;
                var elementModelToUpdate = await _soaService.UpdateAssignAssessor(request, networkElements!, existingHeatNetwork.Phase, false);
                existingHeatNetwork.NetworkElements = elementModelToUpdate;
                await _heatNetworkService.UpdateAsync(request.HnId, existingHeatNetwork);
                await NotificationHistoryForAssigningAssessor(request, existingHeatNetwork);
                _logger.LogInformation("Saved Assessor Assigned for HN ID: {HnId}, UpdatedBy: {UpdatedBy}",
                StringFormatter.Sanitize(request.HnId), StringFormatter.Sanitize(request.UpdatedBy));

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save Assessor Assigned for HN ID: {HnId}, UpdatedBy: {UpdatedBy}",
                StringFormatter.Sanitize(request.HnId), StringFormatter.Sanitize(request.UpdatedBy));
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while assigning assessor to the network.");
            }
        }

        [HttpPatch("soa-assign-assessor-for-existing-network")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SoaAssignAssessorForExistingNetwork([FromBody] ElementSoaAssignAssessorRequestForExistingNetwork request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid SaveDocument request: {@Errors}",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Saving Assessor Assigned for existing HN ID: {HnId}, UpdatedBy: {UpdatedBy}",
                StringFormatter.Sanitize(request.HnId), StringFormatter.Sanitize(request.UpdatedBy));

            try
            {
                var existingHeatNetwork = await _heatNetworkService.GetByHnIdAsync(request.HnId);
                if (existingHeatNetwork == null)
                {
                    _logger.LogInformation("No heat network found for existing HnId: {HnId}", StringFormatter.Sanitize(request.HnId));
                    return NotFound($"No heat network found for existing HnId '{request.HnId}'.");
                }

                var networkElements = existingHeatNetwork.NetworkElements;

                await _soaService.UpdateAssignAssessorForExistingNetwork(request, networkElements!, existingHeatNetwork.Phase!, true);
                existingHeatNetwork = await _heatNetworkService.GetByHnIdAsync(request.HnId);
                networkElements = existingHeatNetwork.NetworkElements;
                var elementModelToUpdate = await _soaService.UpdateAssignAssessorForExistingNetwork(request, networkElements!, existingHeatNetwork.Phase!, false);
                existingHeatNetwork.NetworkElements = elementModelToUpdate;
                await _heatNetworkService.UpdateAsync(request.HnId, existingHeatNetwork);                
                _logger.LogInformation("Saved Assessor Assigned for existing HN ID: {HnId}, UpdatedBy: {UpdatedBy}",
                StringFormatter.Sanitize(request.HnId), StringFormatter.Sanitize(request.UpdatedBy));

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save Assessor Assigned for existing HN ID: {HnId}, UpdatedBy: {UpdatedBy}",
                StringFormatter.Sanitize(request.HnId), StringFormatter.Sanitize(request.UpdatedBy));
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while assigning assessor to the existing network.");
            }
        }


        [HttpPut("update-soa-status")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateSoaStatus([FromBody] UpdateSoaStatusRequest request)
        {
            _logger.LogInformation("Updating SOA status to {Status} for HN ID: {HnId} by {UpdatedBy}", request.Status, request.HnId.ToSafeLog(), request.UpdatedBy.ToSafeLog());

            if (string.IsNullOrWhiteSpace(request.HnId))
                return BadRequest("Heat Network ID is required.");

            if (string.IsNullOrWhiteSpace(request.HnName))
                return BadRequest("Heat Network Name is required.");

            if (string.IsNullOrWhiteSpace(request.UpdatedBy))
                return BadRequest("UpdatedBy is required.");

            if (!Enum.IsDefined(typeof(SoaStatus), request.Status))
                return BadRequest($"Invalid SOA status: {request.Status}");

            var soa = await _soaService.UpdateStatusAsync(request.HnId, request.Status, request.UpdatedBy);
            var user = await _userService.GetUserWithDetailsAsync(request.UpdatedBy);

            if (soa == null)
            {
                _logger.LogWarning("No SOA found to update for HN ID: {HnId}", request.HnId.ToSafeLog());
                return BadRequest("SOA not found.");
            }

            var users = await _userService.GetAssessorsByHnIdAsync(request.HnId);
            var assessor = users.FirstOrDefault();
            if (assessor == null)
            {
                _logger.LogWarning("No assessor found for HN ID: {HnId}", request.HnId.ToSafeLog());
                return NotFound();
            }
            
            await _emailService.TrySendAssessorEmailAsync(
                     emailAddress: assessor.EmailId,
                     hnName: request.HnName,
                     hnId: request.HnId,
                     contributorName: user?.FullName
                 );            

            return NoContent();
        }        

        private async Task NotificationHistoryForAssigningAssessor(ElementSoaAssignAssessorRequest request, HeatNetwork heatNetwork)
        {
            // Get user's email and role
            var currentUser = await _userService.GetUserWithDetailsAsync(request.UpdatedBy);
            var rpUserId = "";
            var nmUserId = "";
            if (currentUser.Roles!.Contains(UserRole.ResponsibleParty))
            {
                rpUserId = currentUser.Id!;
            }
            else
            {
                var invitaions = await _invitationService.GetByInvitedEmailAsync(currentUser.EmailId!);
                nmUserId = currentUser.Id!;
                rpUserId = invitaions.InviterUserId;
            }

            var users = await _userService.GetUsersAssociatedByHnIdAsync(request.HnId);
            // get distinct user emailIds from the list of users
            var emailIds = users.Select(u => u.EmailId).Distinct().ToList();
            var userIds = users.Select(u => u.Id).Distinct().ToList();

            var acceptedInvitations = await _invitationService.GetByEmailsAndHnIdAsync(emailIds, request.HnId);
            var invitorUserIds = acceptedInvitations.Select(i => i.InviterUserId).Distinct().ToList();

            // merge userIds and invitorUserIds and get distinct list of userIds to be notified
            var actors = userIds.Union(invitorUserIds).Distinct().ToList();
            actors.Add(rpUserId);
            if (!string.IsNullOrEmpty(nmUserId))
            {
                actors.Add(nmUserId);
            }

            var description = "";
            var assessorDetails = request.AssessorAssessmentForElements.Select(a => a.AssessorAssessments).FirstOrDefault();
            if (assessorDetails != null)
            {
                var assessor = assessorDetails.FirstOrDefault();
                description = $"{assessor?.AssessorFirstName} {assessor?.AssessorLastName} Assigned to {heatNetwork.HnId}-{heatNetwork.Name}";
            }

            var eligibleRoles = new List<string> { ContributorRole.ResponsibleParty.ToString()
                , ContributorRole.NetworkManager.ToString(),
                ContributorRole.DesignatedDutyHolder.ToString(),
                ContributorRole.Contributor.ToString()};
            var notificationHistory = new NotificationHistory
            {
                NotificationType = NotificationHistoryType.AssessorAssignsToHeatNetwork,
                ActorsId = actors!,
                Subject = NotificationHistorySubjects.AssessorAssignedToHN,
                Description = description,
                Timestamp = DateTime.UtcNow,
                Action = NotificationHistoryActions.HeatNetworkDetails,
                HeatNetworkId = heatNetwork.HnId,
                CreatedBy = request.UpdatedBy,
                EligibleRoles = eligibleRoles,
                Stage = request.SoaStage
            };

            await _notificationHistoryService.CreateAsync(notificationHistory);
        }
    }
}