using AutoMapper;
using HNTAS.Core.Api.Controllers;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models;
using HNTAS.Core.Api.Models.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace HNTAS.Digital.Core.Tests.Controllers
{
    public class InvitationsControllerTests
    {
        private readonly Mock<IUserService> _mockUserService = new();
        private readonly Mock<IInvitationService> _mockInvitationService = new();
        private readonly Mock<ILogger<InvitationsController>> _mockLogger = new();
        private readonly Mock<IConfiguration> _mockConfiguration = new();
        private readonly Mock<IEmailService> _mockEmailService = new();
        private readonly Mock<IHeatNetworkService> _mockHnService = new();
        private readonly Mock<IOrganisationService> _mockOrganisationService = new();
        private readonly Mock<INotificationHistoryService> _mockNotificationHistory = new();
        private readonly Mock<IMapper> _mockMapper = new();

        private InvitationsController CreateController()
        {
            return new InvitationsController(
                _mockUserService.Object,
                _mockInvitationService.Object,
                _mockLogger.Object,
                _mockConfiguration.Object,
                _mockEmailService.Object,
                _mockHnService.Object,
                _mockMapper.Object,
                _mockOrganisationService.Object,
                _mockNotificationHistory.Object
            );
        }

        [Fact]
        public async Task GetInvitationById_Positive_ReturnsOkWithMappedResponse()
        {
            // Arrange
            var id = "507f1f77bcf86cd799439011";
            var invitation = new Invitation
            {
                Id = id,
                InvitedEmail = "invitee@example.com",
                FirstName = "John",
                LastName = "Doe",
                InvitedRoles = new List<ContributorRole> { ContributorRole.Assessor },
                Status = InvitationStatus.Invited,
                InvitedHnId = "HN-1",
                InvitedOrgId = null,
                InviterUserId = "inviter-1",
                InvitedAt = DateTime.UtcNow
            };

            var expectedResponse = new InvitedUserResponse
            {
                Id = invitation.Id,
                Email = invitation.InvitedEmail,
                FirstName = invitation.FirstName,
                LastName = invitation.LastName,
                Roles = invitation.InvitedRoles,
                Status = invitation.Status,
                InvitedAt = invitation.InvitedAt,
                InvitedHnId = invitation.InvitedHnId,
                InvitedOrgId = invitation.InvitedOrgId,
                InviterUserId = invitation.InviterUserId
            };

            _mockInvitationService.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(invitation);
            _mockMapper.Setup(m => m.Map<InvitedUserResponse>(invitation)).Returns(expectedResponse);

            var controller = CreateController();

            // Act
            var actionResult = await controller.GetInvitationById(id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var value = Assert.IsType<InvitedUserResponse>(okResult.Value);
            Assert.Equal(expectedResponse.Id, value.Id);
            Assert.Equal(expectedResponse.Email, value.Email);
            _mockInvitationService.Verify(s => s.GetByIdAsync(id), Times.Once);
            _mockMapper.Verify(m => m.Map<InvitedUserResponse>(invitation), Times.Once);
        }

        [Fact]
        public async Task GetInvitationById_Negative_NotFound()
        {
            // Arrange
            var id = "nonexistent";
            _mockInvitationService.Setup(s => s.GetByIdAsync(id)).ReturnsAsync((Invitation)null);

            var controller = CreateController();

            // Act
            var actionResult = await controller.GetInvitationById(id);

            // Assert
            Assert.IsType<NotFoundResult>(actionResult.Result);
            _mockInvitationService.Verify(s => s.GetByIdAsync(id), Times.Once);
        }

        [Fact]
        public async Task AddUserInvitation_Positive_CreatesInvitationAndReturnsCreatedId()
        {
            // Arrange
            var inviterId = "inviter-123";
            var existingUser = new User { Id = inviterId, EmailId = "inviter@example.com" };
            _mockUserService.Setup(u => u.GetByIdAsync(inviterId)).ReturnsAsync(existingUser);

            var request = new AddInvitationRequest
            {
                EmailAddress = "newuser@example.com",
                FirstName = "New",
                LastName = "User",
                HnId = null,
                OrgId = null,
                ContributorRoles = new List<ContributorRole> { ContributorRole.Certifier },
                RolesToReplace = new List<ContributorRole>()
            };

            // Capture the invitation and set Id in CreateAsync callback
            _mockInvitationService
                .Setup(i => i.CreateAsync(It.IsAny<Invitation>()))
                .Returns<Invitation>(inv =>
                {
                    inv.Id = "new-invite-id";
                    return Task.CompletedTask;
                });

            var controller = CreateController();

            // Act
            var result = await controller.AddUserInvitation(inviterId, request);

            // Assert
            var createdResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(201, createdResult.StatusCode);
            Assert.Equal("new-invite-id", createdResult.Value);
            _mockInvitationService.Verify(i => i.CreateAsync(It.IsAny<Invitation>()), Times.Once);
            _mockUserService.Verify(u => u.GetByIdAsync(inviterId), Times.Once);
        }

        [Fact]
        public async Task AddUserInvitation_NetworkNotFound()
        {
            // Arrange
            var inviterId = "inviter-123";
            var existingUser = new User { Id = inviterId, EmailId = "inviter@example.com" };
            _mockUserService.Setup(u => u.GetByIdAsync(inviterId)).ReturnsAsync(existingUser);

            var request = new AddInvitationRequest
            {
                EmailAddress = "newuser@example.com",
                FirstName = "New",
                LastName = "User",
                HnId = "HN100001",
                OrgId = null,
                ContributorRoles = new List<ContributorRole> { ContributorRole.Certifier },
                RolesToReplace = new List<ContributorRole>()
            };

            // Capture the invitation and set Id in CreateAsync callback
            _mockInvitationService
                .Setup(i => i.CreateAsync(It.IsAny<Invitation>()))
                .Returns<Invitation>(inv =>
                {
                    inv.Id = "new-invite-id";
                    return Task.CompletedTask;
                });

            _mockHnService.Setup(h => h.GetByHnIdAsync(It.IsAny<string>())).ReturnsAsync((HeatNetwork)null!);

            var controller = CreateController();

            // Act
            var result = await controller.AddUserInvitation(inviterId, request);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);

        }

        [Fact]
        public async Task AddUserInvitation_UserNotFound()
        {
            // Arrange
            var inviterId = "inviter-123";
            var existingUser = new User { Id = inviterId, EmailId = "inviter@example.com" };
            _mockUserService.Setup(u => u.GetByIdAsync(inviterId)).ReturnsAsync(existingUser);

            var request = new AddInvitationRequest
            {
                EmailAddress = "newuser@example.com",
                FirstName = "New",
                LastName = "User",
                HnId = null,
                OrgId = null,
                ContributorRoles = new List<ContributorRole> { ContributorRole.Certifier },
                RolesToReplace = new List<ContributorRole>()
            };

            // Capture the invitation and set Id in CreateAsync callback
            _mockInvitationService
                .Setup(i => i.CreateAsync(It.IsAny<Invitation>()))
                .Returns<Invitation>(inv =>
                {
                    inv.Id = "new-invite-id";
                    return Task.CompletedTask;
                });

            _mockUserService.Setup(h => h.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((User)null!);

            var controller = CreateController();

            // Act
            var result = await controller.AddUserInvitation(inviterId, request);

            // Assert
            Assert.IsType<NotFoundResult>(result);

        }

        [Fact]
        public async Task AddUserInvitation_ThrowException()
        {
            // Arrange
            var inviterId = "inviter-123";
            var existingUser = new User { Id = inviterId, EmailId = "inviter@example.com" };
            _mockUserService.Setup(u => u.GetByIdAsync(inviterId)).Throws(new Exception());

            var request = new AddInvitationRequest
            {
                EmailAddress = "newuser@example.com",
                FirstName = "New",
                LastName = "User",
                HnId = null,
                OrgId = null,
                ContributorRoles = new List<ContributorRole> { ContributorRole.Certifier },
                RolesToReplace = new List<ContributorRole>()
            };

            var controller = CreateController();

            // Act
            var result = await controller.AddUserInvitation(inviterId, request);

            // Assert
            var res = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, res.StatusCode);

        }

        [Theory]
        [InlineData(UserRole.ResponsibleParty, ContributorRole.NetworkManager)]
        [InlineData(UserRole.ResponsibleParty, ContributorRole.DesignatedDutyHolder)]
        [InlineData(UserRole.ResponsibleParty, ContributorRole.Contributor)]
        [InlineData(UserRole.NetworkManager, ContributorRole.DesignatedDutyHolder)]
        [InlineData(UserRole.NetworkManager, ContributorRole.Contributor)]
        [InlineData(UserRole.DesignatedDutyHolder, ContributorRole.Contributor)]
        public async Task AddUserInvitation_UpdateUser(UserRole invitorRole, ContributorRole invitedRole)
        {
            // Arrange
            var inviterId = "inviter-123";
            var existingUser = new User { Id = inviterId, EmailId = "inviter@example.com" };
            _mockUserService.Setup(u => u.GetByIdAsync(inviterId)).ReturnsAsync(existingUser);

            var request = new AddInvitationRequest
            {
                EmailAddress = "newuser@example.com",
                FirstName = "New",
                LastName = "User",
                HnId = null,
                OrgId = null,
                ContributorRoles = new List<ContributorRole> { invitedRole },
                RolesToReplace = new List<ContributorRole> { ContributorRole.Contributor },
                ReplacedUserId = "nonexistent-user"
            };

            // Capture the invitation and set Id in CreateAsync callback
            _mockInvitationService
                .Setup(i => i.CreateAsync(It.IsAny<Invitation>()))
                .Returns<Invitation>(inv =>
                {
                    inv.Id = "new-invite-id";
                    return Task.CompletedTask;
                });

            _mockUserService.Setup(h => h.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(new User { Id = "test", HnRoleMappings = new List<HnRoleMapping> { new HnRoleMapping { HnId = "HN100001", Role = ContributorRole.NetworkManager } }, Roles = new List<UserRole> { invitorRole } });
            _mockUserService.Setup(u => u.UpdateAsync(It.IsAny<string>(), It.IsAny<User>())).Returns(Task.CompletedTask);
            _mockEmailService.Setup(e => e.TrySendHNDiscontinedEmailAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<ContributorRole>())).Returns(Task.CompletedTask);
            var controller = CreateController();

            // Act
            var result = await controller.AddUserInvitation(inviterId, request);

            // Assert
            Assert.IsType<ObjectResult>(result);
        }

        [Fact]
        public async Task AddUserInvitation_Negative_InvalidModel_ReturnsValidationProblem()
        {
            // Arrange
            var inviterId = "inviter-123";
            var request = new AddInvitationRequest
            {
                EmailAddress = "invalid-email",
                FirstName = "",
                LastName = ""
            };

            var controller = CreateController();
            controller.ModelState.AddModelError("EmailAddress", "EmailAddress is required.");

            // Act
            var result = await controller.AddUserInvitation(inviterId, request);

            // Assert
            var badRequest = Assert.IsType<ObjectResult>(result);
            Assert.Equal(null, badRequest.StatusCode);
            _mockInvitationService.Verify(i => i.CreateAsync(It.IsAny<Invitation>()), Times.Never);
        }

        [Fact]
        public async Task SendInvitationEmail_Positive_HeatNetworkInvitation_EmailSentAndNoContentReturned()
        {
            // Arrange
            var invitationId = "invite-1";
            var invitation = new Invitation
            {
                Id = invitationId,
                InvitedHnId = "HN-123",
                Status = InvitationStatus.Invited,
                InvitedEmail = "invitee@hn.com"
            };

            var request = new SendInvitationEmailRequest { Token = "token-123" };

            _mockInvitationService.Setup(s => s.GetByIdAsync(invitationId)).ReturnsAsync(invitation);
            _mockHnService.Setup(h => h.GetByHnIdAsync(invitation.InvitedHnId)).ReturnsAsync(new HeatNetwork { Name = "Test HN", HnId = invitation.InvitedHnId });

            var controller = CreateController();

            // Act
            var result = await controller.SendInvitationEmail(invitationId, request);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockEmailService.Verify(e => e.TrySendHeatNetworkInvitationEmailAsync(invitation, request.Token, "Test HN"), Times.Once);
        }

        [Fact]
        public async Task SendInvitationEmail_Positive_HeatNetworkInvitation_OrgNotNull_EmailSentAndNoContentReturned()
        {
            // Arrange
            var invitationId = "invite-1";
            var invitation = new Invitation
            {
                Id = invitationId,
                InvitedHnId = null,
                Status = InvitationStatus.Invited,
                InvitedEmail = "invitee@hn.com",
                InvitedOrgId = "ORG-123"
            };

            var request = new SendInvitationEmailRequest { Token = "token-123" };

            _mockInvitationService.Setup(s => s.GetByIdAsync(invitationId)).ReturnsAsync(invitation);
            _mockHnService.Setup(h => h.GetByHnIdAsync(invitation.InvitedHnId)).ReturnsAsync(new HeatNetwork { Name = "Test HN", HnId = invitation.InvitedHnId });

            _mockUserService.Setup(u => u.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(new User { Id = "test", EmailId = "test" });
            _mockMapper.Setup(m => m.Map<UserResponse>(It.IsAny<User>())).Returns(new UserResponse() { FullName = "test" });
            _mockOrganisationService.Setup(o => o.GetByOrgIdAsync(It.IsAny<string>())).ReturnsAsync(new Organisation { Name = "Test Org", OrgId = "ORG-1" });
            _mockEmailService.Setup(e => e.TrySendOrganisationInvitationEmailAsync(It.IsAny<Invitation>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            var controller = CreateController();

            // Act
            var result = await controller.SendInvitationEmail(invitationId, request);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockEmailService.Verify(e => e.TrySendOrganisationInvitationEmailAsync(It.IsAny<Invitation>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task SendInvitationEmail_Negative_InvitationNotFound_ReturnsNotFound()
        {
            // Arrange
            var invitationId = "missing";
            var request = new SendInvitationEmailRequest { Token = "token" };
            _mockInvitationService.Setup(s => s.GetByIdAsync(invitationId)).ReturnsAsync((Invitation)null);

            var controller = CreateController();

            // Act
            var result = await controller.SendInvitationEmail(invitationId, request);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockEmailService.Verify(e => e.TrySendHeatNetworkInvitationEmailAsync(It.IsAny<Invitation>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [InlineData(ContributorRole.DesignatedDutyHolder)]
        [InlineData(ContributorRole.Contributor)]
        [InlineData(ContributorRole.NetworkManager)]
        public async Task RejectInvitation_Positive_PendingInvitation_UpdatesAndReturnsNoContent(ContributorRole invitedRole)
        {
            // Arrange
            var invitationId = "to-reject";
            var invitation = new Invitation
            {
                Id = invitationId,
                Status = InvitationStatus.Invited,
                InvitedRoles = new List<ContributorRole> { invitedRole }
            };

            _mockInvitationService.Setup(s => s.GetByIdAsync(invitationId)).ReturnsAsync(invitation);
            _mockInvitationService.Setup(s => s.UpdateAsync(invitationId, It.IsAny<Invitation>())).Returns(Task.CompletedTask)
                .Callback<string, Invitation>((id, updated) =>
                {
                    // ensure state changed in the updated invitation passed into UpdateAsync
                    Assert.Equal(InvitationStatus.Rejected, updated.Status);
                    Assert.NotNull(updated.RejectedAt);
                });

            _mockUserService.Setup(u => u.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(new User { Id = "test", EmailId = "test", Roles = new List<UserRole> { UserRole.NetworkManager } });
            _mockInvitationService.Setup(i => i.GetByInvitedEmailAsync(It.IsAny<string>())).ReturnsAsync(new Invitation() { InviterUserId = "test" });
            var controller = CreateController();

            // Act
            var result = await controller.RejectInvitation(invitationId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockInvitationService.Verify(s => s.GetByIdAsync(invitationId), Times.Once);
            _mockInvitationService.Verify(s => s.UpdateAsync(invitationId, It.IsAny<Invitation>()), Times.Once);
        }

        [Fact]
        public async Task RejectInvitation_Negative_AlreadyAccepted_ReturnsBadRequest()
        {
            // Arrange
            var invitationId = "already-accepted";
            var invitation = new Invitation
            {
                Id = invitationId,
                Status = InvitationStatus.Accepted
            };

            _mockInvitationService.Setup(s => s.GetByIdAsync(invitationId)).ReturnsAsync(invitation);

            var controller = CreateController();

            // Act
            var result = await controller.RejectInvitation(invitationId);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequest.StatusCode);
            _mockInvitationService.Verify(s => s.UpdateAsync(It.IsAny<string>(), It.IsAny<Invitation>()), Times.Never);
        }

        [Fact]
        public async Task AcceptInvitation_Ok()
        {
            var request = new InvitedUserRequest();
            _mockInvitationService.Setup(s => s.AcceptAsync(It.IsAny<InvitedUserRequest>()))
                .ReturnsAsync(new AcceptInvitationResult("test", false, false));

            var controller = CreateController();
            var result = await controller.AcceptInvitation(request);
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task AcceptInvitation_IsCreated()
        {
            var request = new InvitedUserRequest();
            _mockInvitationService.Setup(s => s.AcceptAsync(It.IsAny<InvitedUserRequest>()))
                .ReturnsAsync(new AcceptInvitationResult("test", true, false));

            var controller = CreateController();
            var result = await controller.AcceptInvitation(request);
            Assert.IsType<ObjectResult>(result);
        }

        [Fact]
        public async Task AcceptInvitation_IsNotFound()
        {
            var request = new InvitedUserRequest();
            _mockInvitationService.Setup(s => s.AcceptAsync(It.IsAny<InvitedUserRequest>()))
                .ReturnsAsync(new AcceptInvitationResult("test", false, true));

            var controller = CreateController();
            var result = await controller.AcceptInvitation(request);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task AcceptInvitation_ReturnsNotFound_WhenInvitationNotFound()
        {
            // Arrange
            var request = new InvitedUserRequest
            {
                InvitationId = "invite-123",
                OneLoginId = "onelogin-123"
            };

            _mockInvitationService
                .Setup(s => s.AcceptAsync(It.IsAny<InvitedUserRequest>()))
                .ReturnsAsync(AcceptInvitationResult.NotFound());

            var controller = CreateController();

            // Act
            var result = await controller.AcceptInvitation(request);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task AcceptInvitation_ReturnsCreated_WhenUserIsCreated()
        {
            // Arrange
            var request = new InvitedUserRequest
            {
                InvitationId = "invite-123",
                OneLoginId = "onelogin-123"
            };

            _mockInvitationService
                .Setup(s => s.AcceptAsync(It.IsAny<InvitedUserRequest>()))
                .ReturnsAsync(AcceptInvitationResult.Created("user-001"));

            var controller = CreateController();

            // Act
            var result = await controller.AcceptInvitation(request);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);
            Assert.Equal("user-001", objectResult.Value);
        }

        [Fact]
        public async Task AcceptInvitation_ReturnsOk_WhenUserAlreadyExists()
        {
            // Arrange
            var request = new InvitedUserRequest
            {
                InvitationId = "invite-123",
                OneLoginId = "onelogin-123"
            };

            _mockInvitationService
                .Setup(s => s.AcceptAsync(It.IsAny<InvitedUserRequest>()))
                .ReturnsAsync(AcceptInvitationResult.Updated("user-001"));

            var controller = CreateController();

            // Act
            var result = await controller.AcceptInvitation(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("user-001", okResult.Value);
        }

    }
}