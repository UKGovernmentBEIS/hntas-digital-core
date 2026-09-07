using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace HNTAS.Digital.Core.Tests.Services
{
    public class EmailServiceTests
    {
        private readonly Mock<IGovUkNotifyService> _mockGovUkNotifyService;
        private readonly Mock<IOptions<NotificationSettings>> _mockNotificationSettingsOptions;
        private readonly Mock<IOptions<HntasServiceSettings>> _mockHntasServiceSettingsOptions;
        private readonly Mock<ILogger<EmailService>> _mockLogger;

        private readonly EmailService _sut;

        public EmailServiceTests()
        {
            _mockGovUkNotifyService = new Mock<IGovUkNotifyService>();
            _mockNotificationSettingsOptions = new Mock<IOptions<NotificationSettings>>();
            _mockHntasServiceSettingsOptions = new Mock<IOptions<HntasServiceSettings>>();
            _mockLogger = new Mock<ILogger<EmailService>>();
            var notificationSettings = new NotificationSettings
            {
                OrgCreatedEmailTemplateId = "org-created-template-id",
            };
            var hntasServiceSettings = new HntasServiceSettings
            {
                InvitationPath = "/invitation"
            };


            Environment.SetEnvironmentVariable(
                    "WEB_BASE_URL",
                    "https://example.com");

            _mockNotificationSettingsOptions.Setup(s => s.Value).Returns(notificationSettings);
            _mockHntasServiceSettingsOptions.Setup(s => s.Value).Returns(hntasServiceSettings);
            _sut = new EmailService(
                _mockLogger.Object,
                _mockGovUkNotifyService.Object,
                _mockNotificationSettingsOptions.Object,
                _mockHntasServiceSettingsOptions.Object
                );
        }

        [Fact]
        public async Task TrySendOrgCreatedEmailAsync_ShouldSendEmail()
        {
            // Arrange
            var user = new User
            {
                FirstName = "John",
                LastName = "Doe",
                EmailId = "test@gmail.com",
                OrgId = "org-123",
                Id = "user-123"
            };

            var org = new Organisation
            {
                Id = "org-123",
                Name = "Test Org",
                RegisteredAddress = new RegisteredAddress
                {
                    AddressLine1 = "123 Test St",
                    AddressLine2 = "Test City",
                    Postcode = "TE5 7ST"
                }
            };

            _mockGovUkNotifyService.Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>(), It.IsAny<string>()))
                .Returns(Task.FromResult(true));

            // Act
            await _sut.TrySendOrgCreatedEmailAsync(user, org);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Email sent successfully")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task TrySendOrgCreatedEmailAsync_ShouldNotSendEmail()
        {
            // Arrange
            var user = new User
            {
                FirstName = "John",
                LastName = "Doe",
                EmailId = "test@gmail.com",
                OrgId = "org-123",
                Id = "user-123"
            };

            var org = new Organisation
            {
                Id = "org-123",
                Name = "Test Org",
                RegisteredAddress = new RegisteredAddress
                {
                    AddressLine1 = "123 Test St",
                    AddressLine2 = "Test City",
                    Postcode = "TE5 7ST"
                }
            };

            _mockGovUkNotifyService.Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>(), It.IsAny<string>()))
                .Returns(Task.FromResult(false));

            // Act
            await _sut.TrySendOrgCreatedEmailAsync(user, org);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Email failed to send")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task TrySendOrgCreatedEmailAsync_ShouldSkipEmail()
        {
            // Arrange
            User user = null;
            Organisation org = null;

            // Act
            await _sut.TrySendOrgCreatedEmailAsync(user, org);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Skipping email")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task TrySendOrgUpdatedEmailAsync_ShouldSendEmail()
        {
            // Arrange
            var fullName = "John Doe";
            var email = "test@gmail.com";
            var oldNameAndAddress = "Old Org Name, Old Address";
            var newNameAndAddress = "New Org Name, New Address";

            _mockGovUkNotifyService.Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>(), It.IsAny<string>()))
                .Returns(Task.FromResult(true));

            // Act
            await _sut.TrySendOrgUpdatedEmailAsync(fullName, email, oldNameAndAddress, newNameAndAddress);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Organisation-updated email sent successfully")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task TrySendOrgUpdatedEmailAsync_ShouldNotSendEmail()
        {
            // Arrange
            var fullName = "John Doe";
            var email = "test@gmail.com";
            var oldNameAndAddress = "Old Org Name, Old Address";
            var newNameAndAddress = "New Org Name, New Address";

            _mockGovUkNotifyService.Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>(), It.IsAny<string>()))
                .Returns(Task.FromResult(false));

            // Act
            await _sut.TrySendOrgUpdatedEmailAsync(fullName, email, oldNameAndAddress, newNameAndAddress);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Organisation-updated email failed")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task TrySendHeatNetworkRegistrationEmailAsync_ShouldSendEmail()
        {
            // Arrange
            var fullName = "John Doe";
            var email = "test@gmail.com";
            var hnId = "HN100";
            var hnName = "Test";

            _mockGovUkNotifyService.Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>(), It.IsAny<string>()))
                .Returns(Task.FromResult(true));

            // Act
            await _sut.TrySendHeatNetworkRegistrationEmailAsync(email, fullName, hnId, hnName);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Heat network registered email sent successfully")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task TrySendHeatNetworkRegistrationEmailAsync_ShouldNotSendEmail()
        {
            // Arrange
            var fullName = "John Doe";
            var email = "test@gmail.com";
            var hnId = "HN100";
            var hnName = "Test";

            _mockGovUkNotifyService.Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>(), It.IsAny<string>()))
                .Returns(Task.FromResult(false));

            // Act
            await _sut.TrySendHeatNetworkRegistrationEmailAsync(email, fullName, hnId, hnName);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Heat network registered email failed")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task TrySendHeatNetworkInvitationEmailAsync_ShouldSendEmail()
        {
            // Arrange
            var invitation = new Invitation
            {
                Id = "invitation-123",
                InviterUserId = "user-123",
                InvitedEmail = "test@gmail.com"
            };
            var token = "test-token";
            var hnName = "hn";

            _mockGovUkNotifyService.Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>(), It.IsAny<string>()))
                .Returns(Task.FromResult(true));

            // Act
            await _sut.TrySendHeatNetworkInvitationEmailAsync(invitation, token, hnName);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Email sent successfully")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task TrySendHeatNetworkInvitationEmailAsync_ShouldNotSendEmail()
        {
            // Arrange
            var invitation = new Invitation
            {
                Id = "invitation-123",
                InviterUserId = "user-123",
                InvitedEmail = "test@gmail.com"
            };
            var token = "test-token";
            var hnName = "hn";

            _mockGovUkNotifyService.Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>(), It.IsAny<string>()))
                .Returns(Task.FromResult(false));

            // Act
            await _sut.TrySendHeatNetworkInvitationEmailAsync(invitation, token, hnName);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Email failed to send")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task TrySendHeatNetworkInvitationEmailAsync_ShouldSkipSendEmail()
        {
            // Arrange
            Invitation invitation = null;
            var token = "test-token";
            var hnName = "hn";

            // Act
            await _sut.TrySendHeatNetworkInvitationEmailAsync(invitation, token, hnName);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Skipping email: missing Invitation or InvitedEmail for invitation")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task TrySendOrganisationInvitationEmailAsync_ShouldSendEmail()
        {
            // Arrange
            var invitation = new Invitation
            {
                Id = "invitation-123",
                InviterUserId = "user-123",
                InvitedEmail = "test@gmail.com"
            };
            var token = "test-token";
            var orgName = "hn";
            var invitorName = "test";

            _mockGovUkNotifyService.Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>(), It.IsAny<string>()))
                .Returns(Task.FromResult(true));

            // Act
            await _sut.TrySendOrganisationInvitationEmailAsync(invitation, token, orgName, invitorName);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Email sent successfully")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task TrySendOrganisationInvitationEmailAsync_ShouldNotSendEmail()
        {
            // Arrange
            var invitation = new Invitation
            {
                Id = "invitation-123",
                InviterUserId = "user-123",
                InvitedEmail = "test@gmail.com"
            };
            var token = "test-token";
            var orgName = "hn";
            var invitorName = "test";

            _mockGovUkNotifyService.Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>(), It.IsAny<string>()))
                .Returns(Task.FromResult(false));

            // Act
            await _sut.TrySendOrganisationInvitationEmailAsync(invitation, token, orgName, invitorName);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Email failed to send")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task TrySendOrganisationInvitationEmailAsync_ShouldSkipSendEmail()
        {
            // Arrange
            Invitation invitation = null;
            var token = "test-token";
            var orgName = "hn";
            var invitorName = "test";

            // Act
            await _sut.TrySendOrganisationInvitationEmailAsync(invitation, token, orgName, invitorName);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Skipping email")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task TrySendAssessorEmailAsync_ShouldSendEmail()
        {
            // Arrange
            var email = "test@gmail.com";
            var hnName = "test";
            var hnId = "test";
            var contributorName = "test";

            _mockGovUkNotifyService.Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>(), It.IsAny<string>()))
                .Returns(Task.FromResult(true));

            // Act
            await _sut.TrySendAssessorEmailAsync(email, hnName, hnId, contributorName);

            // Assert
            _mockGovUkNotifyService.Verify(a => a.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task TrySendAssessorAssessmentEmailAsync_ShouldSendEmail()
        {
            // Arrange
            var email = "test@gmail.com";
            var hnName = "test";
            var hnId = "test";
            var assessmentResult = "test";

            _mockGovUkNotifyService.Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>(), It.IsAny<string>()))
                .Returns(Task.FromResult(true));

            // Act
            await _sut.TrySendAssessorAssessmentEmailAsync(email, hnName, hnId, assessmentResult);

            // Assert
            _mockGovUkNotifyService.Verify(a => a.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task TrySendCertificationCompleteEmailAsync_ShouldSendEmail()
        {
            // Arrange
            var email = "test@gmail.com";
            var hnName = "test";
            var hnId = "test";


            _mockGovUkNotifyService.Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>(), It.IsAny<string>()))
                .Returns(Task.FromResult(true));

            // Act
            await _sut.TrySendCertificationCompleteEmailAsync(email, hnName, hnId);

            // Assert
            _mockGovUkNotifyService.Verify(a => a.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task TrySendHNDiscontinedEmailAsync_ShouldSendEmail()
        {
            // Arrange
            var user = new User { EmailId = "test@gmail.com", FirstName = "test", LastName = "test" };
            var hnName = "test";
            var contributorRole = ContributorRole.ResponsibleParty;


            _mockGovUkNotifyService.Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>(), It.IsAny<string>()))
                .Returns(Task.FromResult(true));

            // Act
            await _sut.TrySendHNDiscontinedEmailAsync(user, hnName, contributorRole);

            // Assert
            _mockGovUkNotifyService.Verify(a => a.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task TrySendOfgemDataForExistingOrgOrRpEmailAsync_ShouldSendEmail()
        {
            // Arrange
            var ofgemData = new OfgemDataModelForNotification
            {
                HeatNetworkIds = new List<string> { "hn1", "hn2" },
                OrganisationName = "testOrg",
                UserEmailId = "test@gmail.com"
            };


            _mockGovUkNotifyService.Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>(), It.IsAny<string>()))
                .Returns(Task.FromResult(true));

            // Act
            await _sut.TrySendOfgemDataForExistingOrgOrRpEmailAsync(ofgemData);

            // Assert
            _mockGovUkNotifyService.Verify(a => a.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task TrySendOfgemDataForNewRpEmailAsync_ShouldSendEmail()
        {
            // Arrange
            var ofgemData = new OfgemDataModelForNotification
            {
                HeatNetworkIds = new List<string> { "hn1", "hn2" },
                OrganisationName = "testOrg",
                UserEmailId = "test@gmail.com"
            };


            _mockGovUkNotifyService.Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>(), It.IsAny<string>()))
                .Returns(Task.FromResult(true));

            // Act
            await _sut.TrySendOfgemDataForNewRpEmailAsync(ofgemData);

            // Assert
            _mockGovUkNotifyService.Verify(a => a.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, dynamic>>(), It.IsAny<string>()), Times.Once);
        }
    }
}
