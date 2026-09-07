using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models;
using HNTAS.Core.Api.Models.Users;
using HNTAS.Core.Api.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;

namespace HNTAS.Digital.Core.Tests.Services
{
    public class InvitationServiceTests
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IHeatNetworkService> _mockHeatNetworkService;
        private readonly Mock<IAuditService> _mockAuditService;
        private readonly Mock<INotificationHistoryService> _mockNotificationHistoryService;
        private readonly Mock<ILogger<InvitationService>> _mockLogger;
        private readonly Mock<IMongoCollection<Invitation>> _mockCollection;
        private readonly Mock<IMongoDatabase> _mockDatabase;
        private readonly Mock<IOptions<AWSDocDbSettings>> _mockSettings;
        private readonly InvitationService _sut;

        public InvitationServiceTests()
        {
            _mockUserService = new Mock<IUserService>();
            _mockHeatNetworkService = new Mock<IHeatNetworkService>();
            _mockAuditService = new Mock<IAuditService>();
            _mockNotificationHistoryService = new Mock<INotificationHistoryService>();
            _mockLogger = new Mock<ILogger<InvitationService>>();
            _mockCollection = new Mock<IMongoCollection<Invitation>>();
            _mockDatabase = new Mock<IMongoDatabase>();
            _mockSettings = new Mock<IOptions<AWSDocDbSettings>>();
            var settings = new AWSDocDbSettings
            {
                HeatNetworksCollectionName = "Invitations"
            };

            _mockSettings.Setup(s => s.Value).Returns(settings);

            _mockDatabase.Setup(db => db.GetCollection<Invitation>(It.IsAny<string>(), null))
                .Returns(_mockCollection.Object);

            _sut = new InvitationService(
                _mockDatabase.Object,
                _mockSettings.Object,
                _mockLogger.Object,
                _mockUserService.Object,
                _mockHeatNetworkService.Object,
                _mockAuditService.Object,
                _mockNotificationHistoryService.Object
            );
            // Setup the mock collection to return a mock cursor
            //var mockCursor = new Mock<IAsyncCursor<Invitation>>();
            //mockCursor.Setup(_ => _.Current).Returns(new List<Invitation>());
            //mockCursor.SetupSequence(_ => _.MoveNext(It.IsAny<CancellationToken>())).Returns(true).Returns(false);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnInvitation()
        {
            _mockCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<Invitation>>(),
                It.IsAny<FindOptions<Invitation, Invitation>>(),
                default))
                .ReturnsAsync(Mock.Of<IAsyncCursor<Invitation>>(cursor =>
                    cursor.Current == new List<Invitation> { new Invitation { Id = "hn1" } } &&
                    cursor.MoveNext(It.IsAny<CancellationToken>()) == true &&
                    cursor.MoveNextAsync(It.IsAny<CancellationToken>()).Result == true));
            await _sut.GetByIdAsync("hn1");
            _mockCollection.Verify(c => c.FindAsync(
                It.IsAny<FilterDefinition<Invitation>>(),
                It.IsAny<FindOptions<Invitation, Invitation>>(),
                default), Times.Once);
        }

        [Fact]
        public async Task GetByInviterUserIdAsync_ShouldReturnInvitations()
        {
            var expectedInvitations = new List<Invitation>
            {
                new Invitation { Id = "hn1" },
                new Invitation { Id = "hn2" }
            };
            var mockCursor = new Mock<IAsyncCursor<Invitation>>();
            mockCursor.Setup(c => c.Current).Returns(expectedInvitations);
            mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            _mockCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<Invitation>>(),
                It.IsAny<FindOptions<Invitation, Invitation>>(),
                default))
                .ReturnsAsync(mockCursor.Object);
            await _sut.GetByInviterUserIdAsync("hn1");
            _mockCollection.Verify(c => c.FindAsync(
                It.IsAny<FilterDefinition<Invitation>>(),
                It.IsAny<FindOptions<Invitation, Invitation>>(),
                default), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldInsertNewInvitation()
        {
            // Arrange
            var invitation = new Invitation
            {
                Id = Guid.NewGuid().ToString(),
            };
            var isNewHeatNetwork = true;
            // Act
            await _sut.CreateAsync(invitation);
            // Assert
            _mockCollection.Verify(c => c.InsertOneAsync(invitation, null, default), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldInsertNewInvitation()
        {
            // Arrange
            var invitation = new Invitation
            {
                Id = Guid.NewGuid().ToString(),
            };
            var id = "test-id";
            var isNewHeatNetwork = true;
            // Act
            await _sut.UpdateAsync(id, invitation);
            // Assert
            _mockCollection.Verify(c => c.ReplaceOneAsync(It.IsAny<FilterDefinition<Invitation>>(), invitation, It.IsAny<ReplaceOptions>(), default), Times.Once);
        }

        [Fact]
        public async Task GetByEmailAsync_ShouldReturnInvitation()
        {
            _mockCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<Invitation>>(),
                It.IsAny<FindOptions<Invitation, Invitation>>(),
                default))
                .ReturnsAsync(Mock.Of<IAsyncCursor<Invitation>>(cursor =>
                    cursor.Current == new List<Invitation> { new Invitation { Id = "hn1" } } &&
                    cursor.MoveNext(It.IsAny<CancellationToken>()) == true &&
                    cursor.MoveNextAsync(It.IsAny<CancellationToken>()).Result == true));
            await _sut.GetByEmailAsync("hn1", "hn2");
            _mockCollection.Verify(c => c.FindAsync(
                It.IsAny<FilterDefinition<Invitation>>(),
                It.IsAny<FindOptions<Invitation, Invitation>>(),
                default), Times.Once);
        }

        [Fact]
        public async Task GetByEmailsAndHnIdAsync_ShouldReturnInvitations()
        {
            var expectedInvitations = new List<Invitation>
            {
                new Invitation { Id = "hn1" },
                new Invitation { Id = "hn2" }
            };
            var invitedEmails = new List<string> { "test1", "test2" };
            var mockCursor = new Mock<IAsyncCursor<Invitation>>();
            mockCursor.Setup(c => c.Current).Returns(expectedInvitations);
            mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            _mockCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<Invitation>>(),
                It.IsAny<FindOptions<Invitation, Invitation>>(),
                default))
                .ReturnsAsync(mockCursor.Object);
            await _sut.GetByEmailsAndHnIdAsync(invitedEmails, "hn1");
            _mockCollection.Verify(c => c.FindAsync(
                It.IsAny<FilterDefinition<Invitation>>(),
                It.IsAny<FindOptions<Invitation, Invitation>>(),
                default), Times.Once);
        }

        [Fact]
        public async Task GetInvitedUsersAsRegisteredAsync_ShouldReturnManagedUserResponse()
        {
            // Arrange
            var hnId = "6a3aa661be3d3d47c69044d6";

            var expectedManagedUserResponse = new ManagedUserResponse
            {
                Id = "1",

            };

            var mockCursor = new Mock<IAsyncCursor<ManagedUserResponse>>();
            mockCursor.Setup(c => c.Current).Returns(new List<ManagedUserResponse> { expectedManagedUserResponse });
            mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            _mockCollection.Setup(c => c.Aggregate(
                It.IsAny<PipelineDefinition<Invitation, ManagedUserResponse>>(),
                It.IsAny<AggregateOptions>(), It.IsAny<CancellationToken>()))
                .Returns(mockCursor.Object);


            // Act
            var result = await _sut.GetInvitedUsersAsRegisteredAsync(hnId);
            // Assert
            _mockCollection.Verify(c => c.Aggregate(
                It.IsAny<PipelineDefinition<Invitation, ManagedUserResponse>>(),
                It.IsAny<AggregateOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetByInvitedDetailsAsync_ShouldReturnInvitation()
        {
            _mockCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<Invitation>>(),
                It.IsAny<FindOptions<Invitation, Invitation>>(),
                default))
                .ReturnsAsync(Mock.Of<IAsyncCursor<Invitation>>(cursor =>
                    cursor.Current == new List<Invitation> { new Invitation { Id = "hn1" } } &&
                    cursor.MoveNext(It.IsAny<CancellationToken>()) == true &&
                    cursor.MoveNextAsync(It.IsAny<CancellationToken>()).Result == true));
            await _sut.GetByInvitedDetailsAsync("hn1", "hn2", HNTAS.Core.Api.Enums.ContributorRole.ResponsibleParty);
            _mockCollection.Verify(c => c.FindAsync(
                It.IsAny<FilterDefinition<Invitation>>(),
                It.IsAny<FindOptions<Invitation, Invitation>>(),
                default), Times.Once);
        }

        [Fact]
        public async Task GetByInvitedEmailAsync_ShouldReturnInvitation()
        {
            _mockCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<Invitation>>(),
                It.IsAny<FindOptions<Invitation, Invitation>>(),
                default))
                .ReturnsAsync(Mock.Of<IAsyncCursor<Invitation>>(cursor =>
                    cursor.Current == new List<Invitation> { new Invitation { Id = "hn1" } } &&
                    cursor.MoveNext(It.IsAny<CancellationToken>()) == true &&
                    cursor.MoveNextAsync(It.IsAny<CancellationToken>()).Result == true));
            await _sut.GetByInvitedEmailAsync("hn1");
            _mockCollection.Verify(c => c.FindAsync(
                It.IsAny<FilterDefinition<Invitation>>(),
                It.IsAny<FindOptions<Invitation, Invitation>>(),
                default), Times.Once);
        }

        [Fact]
        public async Task AcceptAsync_ShouldReturnAcceptInvitationResult()
        {
            // Arrange
            var request = new InvitedUserRequest
            {
                InvitationId = "test",
                OneLoginId = "test"
            };

            _mockCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<Invitation>>(),
                It.IsAny<FindOptions<Invitation, Invitation>>(),
                default))
                .ReturnsAsync(Mock.Of<IAsyncCursor<Invitation>>(cursor =>
                    cursor.Current == new List<Invitation> { new Invitation { Id = "hn1" } } &&
                    cursor.MoveNext(It.IsAny<CancellationToken>()) == true &&
                    cursor.MoveNextAsync(It.IsAny<CancellationToken>()).Result == true));

            var response = _sut.AcceptAsync(request);

            Assert.NotNull(response);
        }

        [Fact]
        public async Task AcceptAsync_ShouldReturnAcceptUpdatedInvitationResult()
        {
            // Arrange
            var request = new InvitedUserRequest
            {
                InvitationId = "test",
                OneLoginId = "test"
            };

            _mockUserService.Setup(c => c.GetByUserOneLoginIdAsync(It.IsAny<string>())).Returns(Task.FromResult(new User { Id = "test" }));
            _mockCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<Invitation>>(),
                It.IsAny<FindOptions<Invitation, Invitation>>(),
                default))
                .ReturnsAsync(Mock.Of<IAsyncCursor<Invitation>>(cursor =>
                    cursor.Current == new List<Invitation> { new Invitation { Id = "hn1" } } &&
                    cursor.MoveNext(It.IsAny<CancellationToken>()) == true &&
                    cursor.MoveNextAsync(It.IsAny<CancellationToken>()).Result == true));

            var response = _sut.AcceptAsync(request);

            Assert.NotNull(response);
        }

        [Fact]
        public async Task AddHnMapping_ShouldHandleHnMappingForNetworkManagerRole()
        {
            // Arrange
            var user = new User { HnRoleMappings = new List<HnRoleMapping> { new HnRoleMapping { HnId = "test", Role = ContributorRole.ResponsibleParty } } };
            var invitation = new Invitation { InvitedRoles = new List<ContributorRole> { ContributorRole.NetworkManager }, InviterUserId = "test" };

            _mockUserService.Setup(x => x.GetByIdAsync(It.IsAny<string>())).Returns(Task.FromResult(new User { HnRoleMappings = new List<HnRoleMapping> { new HnRoleMapping { HnId = "test", Role = ContributorRole.ResponsibleParty } } }));
            _mockCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<Invitation>>(),
                It.IsAny<FindOptions<Invitation, Invitation>>(),
                default))
                .ReturnsAsync(Mock.Of<IAsyncCursor<Invitation>>(cursor =>
                    cursor.Current == null &&
                    cursor.MoveNext(It.IsAny<CancellationToken>()) == true &&
                    cursor.MoveNextAsync(It.IsAny<CancellationToken>()).Result == true));

            _sut.AddHnMapping(user, invitation);

            _mockUserService.Verify(x => x.GetByIdAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task AddHnMapping_ShouldHandleHnMappingForOtherRolesNoHnId()
        {
            // Arrange
            var user = new User { HnRoleMappings = new List<HnRoleMapping> { new HnRoleMapping { HnId = "test", Role = ContributorRole.ResponsibleParty } } };
            var invitation = new Invitation { InvitedRoles = new List<ContributorRole> { ContributorRole.ResponsibleParty }, InviterUserId = "test" };

            _mockCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<Invitation>>(),
                It.IsAny<FindOptions<Invitation, Invitation>>(),
                default))
                .ReturnsAsync(Mock.Of<IAsyncCursor<Invitation>>(cursor =>
                    cursor.Current == null &&
                    cursor.MoveNext(It.IsAny<CancellationToken>()) == true &&
                    cursor.MoveNextAsync(It.IsAny<CancellationToken>()).Result == true));

            _sut.AddHnMapping(user, invitation);

            _mockUserService.Verify(x => x.GetByIdAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task AddHnMapping_ShouldHandleHnMappingForOtherRoles()
        {
            // Arrange
            var user = new User { HnRoleMappings = new List<HnRoleMapping> { new HnRoleMapping { HnId = "test", Role = ContributorRole.DesignatedDutyHolder } } };
            var invitation = new Invitation { InvitedRoles = new List<ContributorRole> { ContributorRole.ResponsibleParty }, InviterUserId = "test", InvitedHnId = "test" };

            _mockCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<Invitation>>(),
                It.IsAny<FindOptions<Invitation, Invitation>>(),
                default))
                .ReturnsAsync(Mock.Of<IAsyncCursor<Invitation>>(cursor =>
                    cursor.Current == null &&
                    cursor.MoveNext(It.IsAny<CancellationToken>()) == true &&
                    cursor.MoveNextAsync(It.IsAny<CancellationToken>()).Result == true));

            _sut.AddHnMapping(user, invitation);

            _mockUserService.Verify(x => x.GetByIdAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task AddOrganisation_AddContributionOrg()
        {
            var user = new User { HnRoleMappings = new List<HnRoleMapping> { new HnRoleMapping { HnId = "test", Role = ContributorRole.DesignatedDutyHolder } } };
            var invitation = new Invitation { InvitedRoles = new List<ContributorRole> { ContributorRole.ResponsibleParty }, InviterUserId = "test", InvitedHnId = "test", InvitedOrgId = "test" };

            _sut.AddOrganisation(user, invitation);

            Assert.Single(user.ContributingOrganisations ?? new List<string>());
        }

        [Fact]
        public async Task GetNetworkManagersByInviterUserId_ShouldReturnInvitations()
        {
            var expectedInvitations = new List<Invitation>
            {
                new Invitation { Id = "hn1" },
                new Invitation { Id = "hn2" }
            };
            var mockCursor = new Mock<IAsyncCursor<Invitation>>();
            mockCursor.Setup(c => c.Current).Returns(expectedInvitations);
            mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            _mockCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<Invitation>>(),
                It.IsAny<FindOptions<Invitation, Invitation>>(),
                default))
                .ReturnsAsync(mockCursor.Object);

            var response = await _sut.GetNetworkManagersByInviterUserId("userId");

            Assert.NotNull(response);
        }

        [Fact]
        public async Task MapAndFilterRoles_AddContributionOrg()
        {
            var rolesToMap = new List<ContributorRole> { ContributorRole.ResponsibleParty };

            var res = _sut.MapAndFilterRoles(rolesToMap);

            Assert.Single(res);
        }

        //[Fact]
        //public async Task AuditLogs_ShouldSaveAuditLogs_OtherInvitedRoles()
        //{
        //    var invitation = new Invitation { InvitedRoles = new List<ContributorRole> { ContributorRole.ResponsibleParty }, InviterUserId = "test", InvitedHnId = "test" };
        //    var userId = "uid";
        //    var heatNetwork = new HeatNetwork
        //    {
        //        Phase = "design",
        //        Id = "test"
        //    };

        //    await _sut.AuditLogs(invitation, userId, heatNetwork);

        //    _mockAuditService.Verify(x => x.SaveAuditAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HeatNetwork>(), It.IsAny<HeatNetwork>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        //}


        //[Fact]
        //public async Task AuditLogs_ShouldSaveAuditLogs_DdhInvitedRoles()
        //{
        //    var invitation = new Invitation { InvitedRoles = new List<ContributorRole> { ContributorRole.DesignatedDutyHolder }, InviterUserId = "test", InvitedHnId = "test" };
        //    var userId = "uid";
        //    var heatNetwork = new HeatNetwork
        //    {
        //        Phase = "design",
        //        Id = "test"
        //    };

        //    await _sut.AuditLogs(invitation, userId, heatNetwork);

        //    _mockAuditService.Verify(x => x.SaveAuditAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HeatNetwork>(), It.IsAny<HeatNetwork>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        //}

        //[Fact]
        //public async Task AuditLogs_ShouldSaveAuditLogs_ContributorInvitedRoles()
        //{
        //    var invitation = new Invitation { InvitedRoles = new List<ContributorRole> { ContributorRole.Contributor }, InviterUserId = "test", InvitedHnId = "test" };
        //    var userId = "uid";
        //    var heatNetwork = new HeatNetwork
        //    {
        //        Phase = "design",
        //        Id = "test"
        //    };

        //    await _sut.AuditLogs(invitation, userId, heatNetwork);

        //    _mockAuditService.Verify(x => x.SaveAuditAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HeatNetwork>(), It.IsAny<HeatNetwork>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        //}

        [Fact]
        public async Task NotificationHistoryForAcceptingInvite_NetworkManager()
        {
            var user = new User { HnRoleMappings = new List<HnRoleMapping> { new HnRoleMapping { HnId = "test", Role = ContributorRole.ResponsibleParty } } };
            var invitation = new Invitation { InvitedRoles = new List<ContributorRole> { ContributorRole.NetworkManager }, InviterUserId = "test", InvitedHnId = "test" };
            var heatNetwork = new HeatNetwork
            {
                Phase = "design",
                Id = "test"
            };

            await _sut.NotificationHistoryForAcceptingInvite(invitation, user, heatNetwork);

            _mockNotificationHistoryService.Verify(x => x.CreateAsync(It.IsAny<NotificationHistory>()), Times.Once);
        }

        [Fact]
        public async Task NotificationHistoryForAcceptingInvite_DesignatedDutyHolder()
        {
            var user = new User { HnRoleMappings = new List<HnRoleMapping> { new HnRoleMapping { HnId = "test", Role = ContributorRole.ResponsibleParty } } };
            var invitation = new Invitation { InvitedRoles = new List<ContributorRole> { ContributorRole.DesignatedDutyHolder }, InviterUserId = "test", InvitedHnId = "test" };
            var heatNetwork = new HeatNetwork
            {
                Phase = "design",
                Id = "test"
            };

            _mockCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<Invitation>>(),
                It.IsAny<FindOptions<Invitation, Invitation>>(),
                default))
                .ReturnsAsync(Mock.Of<IAsyncCursor<Invitation>>(cursor =>
                    cursor.Current == new List<Invitation> { new Invitation { Id = "hn1" } } &&
                    cursor.MoveNext(It.IsAny<CancellationToken>()) == true &&
                    cursor.MoveNextAsync(It.IsAny<CancellationToken>()).Result == true));

            _mockUserService.Setup(x => x.GetByIdAsync(It.IsAny<string>())).Returns(
                Task.FromResult(new User
                {
                    HnRoleMappings = new List<HnRoleMapping>
                    { new HnRoleMapping
                    {
                        HnId = "test", Role = ContributorRole.ResponsibleParty
                    }
                    },
                    Roles = new List<UserRole> { UserRole.NetworkManager }
                }));

            await _sut.NotificationHistoryForAcceptingInvite(invitation, user, heatNetwork);

            _mockNotificationHistoryService.Verify(x => x.CreateAsync(It.IsAny<NotificationHistory>()), Times.Once);
        }

        [Fact]
        public async Task NotificationHistoryForAcceptingInvite_Contributor()
        {
            var user = new User { HnRoleMappings = new List<HnRoleMapping> { new HnRoleMapping { HnId = "test", Role = ContributorRole.ResponsibleParty } } };
            var invitation = new Invitation { InvitedRoles = new List<ContributorRole> { ContributorRole.Contributor }, InviterUserId = "test", InvitedHnId = "test" };
            var heatNetwork = new HeatNetwork
            {
                Phase = "design",
                Id = "test"
            };

            _mockCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<Invitation>>(),
                It.IsAny<FindOptions<Invitation, Invitation>>(),
                default))
                .ReturnsAsync(Mock.Of<IAsyncCursor<Invitation>>(cursor =>
                    cursor.Current == new List<Invitation> { new Invitation { Id = "hn1" } } &&
                    cursor.MoveNext(It.IsAny<CancellationToken>()) == true &&
                    cursor.MoveNextAsync(It.IsAny<CancellationToken>()).Result == true));

            _mockUserService.Setup(x => x.GetByIdAsync(It.IsAny<string>())).Returns(
                Task.FromResult(new User
                {
                    HnRoleMappings = new List<HnRoleMapping>
                    { new HnRoleMapping
                    {
                        HnId = "test", Role = ContributorRole.ResponsibleParty
                    }
                    },
                    EmailId = "test",
                    Roles = new List<UserRole> { UserRole.NetworkManager }
                }));

            await _sut.NotificationHistoryForAcceptingInvite(invitation, user, heatNetwork);

            _mockNotificationHistoryService.Verify(x => x.CreateAsync(It.IsAny<NotificationHistory>()), Times.Once);
        }
    }
}
