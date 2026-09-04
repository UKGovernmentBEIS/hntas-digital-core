using AutoMapper;
using HNTAS.Core.Api.Controllers;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models;
using HNTAS.Core.Api.Models.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;

namespace HNTAS.Digital.Core.Tests.Controllers
{
    public class UsersControllerTests
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IOrganisationService> _mockOrgService;
        private readonly Mock<IInvitationService> _mockInvitationService;
        private readonly Mock<ILogger<UsersController>> _mockLogger;
        private readonly Mock<ICounterService> _mockCounterService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly UsersController _controller;
        private readonly Mock<IHeatNetworkService> _mockHeatNetworkService;
        private readonly Mock<IAuditService> _mockAuditService;
        private readonly Mock<INotificationHistoryService> _mockNotificationHistoryService;

        public UsersControllerTests()
        {
            _mockUserService = new Mock<IUserService>();
            _mockOrgService = new Mock<IOrganisationService>();
            _mockInvitationService = new Mock<IInvitationService>();
            _mockLogger = new Mock<ILogger<UsersController>>();
            _mockCounterService = new Mock<ICounterService>();
            _mockMapper = new Mock<IMapper>();
            _mockEmailService = new Mock<IEmailService>();
            _mockHeatNetworkService = new Mock<IHeatNetworkService>();
            _mockAuditService = new Mock<IAuditService>();
            _mockNotificationHistoryService = new Mock<INotificationHistoryService>();

            _controller = new UsersController(
                _mockUserService.Object,
                _mockOrgService.Object,
                _mockInvitationService.Object,
                _mockLogger.Object,
                _mockCounterService.Object,
                _mockMapper.Object,
                _mockEmailService.Object,
                _mockHeatNetworkService.Object,
                _mockAuditService.Object,
                _mockNotificationHistoryService.Object
            );
        }

        #region GetUsersDetails Tests

        [Fact]
        public async Task GetUsersDetails_ReturnsOk_WithValidUser()
        {
            // Arrange
            string userId = "user-abc";

            // Creating the result object expected by the service
            var mockDetailsResult = new UserDetailsResult
            {
                Id = userId,
                FirstName = "John",
                LastName = "Doe"
                // Add other properties required by your UserDetailsResult model
            };

            var mockResponse = new UserDetailsResponse
            {
                Id = userId,
                FirstName = "John"
            };

            _mockUserService.Setup(s => s.GetUserWithDetailsAsync(userId))
                .ReturnsAsync(mockDetailsResult);

            _mockMapper.Setup(m => m.Map<UserDetailsResponse>(mockDetailsResult))
                .Returns(mockResponse);

            // Act
            var result = await _controller.GetUsersDetails(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedUser = Assert.IsType<UserDetailsResponse>(okResult.Value);
            Assert.Equal(userId, returnedUser.Id);

            // Verify Logger was called (Information)
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Successfully retrieved")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task GetUsersDetails_ReturnsInternalServerError_WhenServiceThrows()
        {
            // Arrange
            string userId = "user-abc";
            _mockUserService.Setup(s => s.GetUserWithDetailsAsync(userId))
                .ThrowsAsync(new System.Exception("Critical Failure"));

            // Act
            var result = await _controller.GetUsersDetails(userId);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
            Assert.Equal("An unexpected error occurred while retrieving users.", objectResult.Value);

            // Verify Error was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("An error occurred while retrieving all users")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        #endregion

        #region GetById Tests

        [Fact]
        public async Task GetById_ReturnsOk_WhenUserExists()
        {
            // Arrange
            string userId = "65f1a2b3c4d5e6f7a8b9c0d1"; // 24-character hex string
            var mockUser = new User { Id = userId, FirstName = "Alice" };
            var mockResponse = new UserResponse { Id = userId, FirstName = "Alice" };

            _mockUserService.Setup(s => s.GetByIdAsync(userId))
                .ReturnsAsync(mockUser);

            _mockMapper.Setup(m => m.Map<UserResponse>(mockUser))
                .Returns(mockResponse);

            // Act
            var result = await _controller.GetById(userId);

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedUser = Assert.IsType<UserResponse>(actionResult.Value);
            Assert.Equal(userId, returnedUser.Id);
        }

        [Fact]
        public async Task GetById_ReturnsNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            string userId = "65f1a2b3c4d5e6f7a8b9c0d1";
            _mockUserService.Setup(s => s.GetByIdAsync(userId))
                .ReturnsAsync((User)null);

            // Act
            var result = await _controller.GetById(userId);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetById_ReturnsInternalServerError_OnException()
        {
            // Arrange
            string userId = "65f1a2b3c4d5e6f7a8b9c0d1";
            _mockUserService.Setup(s => s.GetByIdAsync(userId))
                .ThrowsAsync(new System.Exception("Database connection failed"));

            // Act
            var result = await _controller.GetById(userId);

            // Assert
            var actionResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, actionResult.StatusCode);
            Assert.Equal("An unexpected error occurred while validating the user ID.", actionResult.Value);
        }

        #endregion

        #region GetManagedUsersAsync Tests

        [Fact]
        public async Task GetManagedUsersAsync_ReturnsNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            string userId = "user-123";
            _mockUserService.Setup(s => s.GetUserWithDetailsAsync(userId))
                .ReturnsAsync((UserDetailsResult)null);

            // Act
            var result = await _controller.GetManagedUsersAsync(userId);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetManagedUsersAsync_ReturnsOk_WithCombinedManagedUsers()
        {
            // Arrange
            string userId = "resp-user-id";
            var mainUser = new UserDetailsResult { Id = userId, EmailId = "resp@test.com" };

            var invitedUsersDetail = new List<UserDetailsResult>
            {
                new UserDetailsResult { Id = "reg-user-id", EmailId = "registered@test.com" }
            };

            var invitations = new List<ManagedUserResponse>
            {
                new ManagedUserResponse { EmailId = "invited@test.com", Status = "Invited" }
            };

            // Mock Main User Retrieval
            _mockUserService.Setup(s => s.GetUserWithDetailsAsync(userId))
                .ReturnsAsync(mainUser);

            _mockMapper.Setup(m => m.Map<ManagedUserResponse>(mainUser))
                .Returns(new ManagedUserResponse { Id = userId, EmailId = "resp@test.com" });

            // Mock Invitations
            _mockInvitationService.Setup(s => s.GetInvitedUsersAsRegisteredAsync(userId))
                .ReturnsAsync(invitations);

            // Mock Registered Users from Invitations
            _mockUserService.Setup(s => s.GetUsersByInvitedEmailsWithDetailsAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(invitedUsersDetail);

            _mockMapper.Setup(m => m.Map<List<ManagedUserResponse>>(invitedUsersDetail))
                .Returns(new List<ManagedUserResponse>
                {
            new ManagedUserResponse { Id = "reg-user-id", EmailId = "registered@test.com" }
                });

            // Act
            var result = await _controller.GetManagedUsersAsync(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var managedUsers = Assert.IsType<List<ManagedUserResponse>>(okResult.Value);

            // Should contain: 1 Registered + 1 Invited
            Assert.Equal(2, managedUsers.Count);
            //Assert.Contains(managedUsers, u => u.EmailId == "resp@test.com");
            Assert.Contains(managedUsers, u => u.EmailId == "registered@test.com");
            Assert.Contains(managedUsers, u => u.EmailId == "invited@test.com");
        }

        [Fact]
        public async Task GetManagedUsersAsync_ExcludesResponsibleUserFromFinalResult()
        {
            // Arrange
            string rpUserId = "resp-user-id";
            string contributorEmail = "contributor@test.com";

            var rpUser = new UserDetailsResult { Id = rpUserId, EmailId = "rp@test.com" };

            // Registered users list contains the RP AND a contributor
            var invitedUsersDetail = new List<UserDetailsResult>
            {
                new UserDetailsResult { Id = rpUserId, EmailId = "rp@test.com" }, // RP
                new UserDetailsResult { Id = "other-id", EmailId = contributorEmail } // Contributor
            };

            _mockUserService.Setup(s => s.GetUserWithDetailsAsync(rpUserId)).ReturnsAsync(rpUser);

            // Mocking the list return
            _mockMapper.Setup(m => m.Map<List<ManagedUserResponse>>(invitedUsersDetail))
                .Returns(new List<ManagedUserResponse>
                {
                    new ManagedUserResponse { Id = rpUserId, EmailId = "rp@test.com" },
                    new ManagedUserResponse { Id = "other-id", EmailId = contributorEmail }
                });

            _mockInvitationService.Setup(s => s.GetInvitedUsersAsRegisteredAsync(rpUserId))
                .ReturnsAsync(new List<ManagedUserResponse>());

            _mockUserService.Setup(s => s.GetUsersByInvitedEmailsWithDetailsAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(invitedUsersDetail);

            // Act
            var result = await _controller.GetManagedUsersAsync(rpUserId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var managedUsers = Assert.IsType<List<ManagedUserResponse>>(okResult.Value);

            // Should only have 1 entry (the contributor) because the RP (resp-user-id) is excluded
            Assert.Single(managedUsers);
            Assert.All(managedUsers, u => Assert.NotEqual(rpUserId, u.Id));
            Assert.Contains(managedUsers, u => u.EmailId == contributorEmail);
        }


        [Fact]
        public async Task GetManagedUsersAsync_HandlesRejectedAndRegisteredLogic_Correctly()
        {
            // Arrange
            string rpUserId = "rp-id";
            string networkId = "network-A";
            string email1 = "contributor1@test.com";
            string email2 = "contributor2@test.com";

            var rpUser = new UserDetailsResult { Id = rpUserId, FirstName = "rp", LastName = "user", Roles = new List<UserRole> { UserRole.ResponsibleParty }, Status = UserStatus.Active, EmailId = "rpuser@test.com" };

            // 1. Registered Users (User 1)
            var registeredUsersDetail = new List<UserDetailsResult>
            {
                new UserDetailsResult
                {
                    Id = "u1-reg-id", // Ensure this ID matches what the loop expects
                    FirstName = "User",
                    LastName = "One",
                    EmailId = email1,
                    Status = UserStatus.Active,
                    Roles = new List<UserRole> { UserRole.Contributor },
                    HnRoleMappings = new List<HnRoleMappingsUserResult> { new HnRoleMappingsUserResult { HeatNetwork = new HeatNetworkUserResponse { HnId = networkId }, Role = "DesignatedDesigner" } }
                }
            };

            // 2. Invitations
            var invitations = new List<ManagedUserResponse>
            {
                new ManagedUserResponse { EmailId = email1, Status = "Rejected", InvitedAt = DateTime.Now.AddDays(-5),
                    HeatNetworks = new List<HeatNetworkInfo> { new HeatNetworkInfo { HnId = networkId } } },
                new ManagedUserResponse { EmailId = email1, Status = "Invited", InvitedAt = DateTime.Now.AddDays(-2),
                     HeatNetworks = new List<HeatNetworkInfo> { new HeatNetworkInfo { HnId = networkId } } },
                new ManagedUserResponse { EmailId = email2, Status = "Rejected", InvitedAt = DateTime.Now.AddDays(-1),
                    HeatNetworks = new List<HeatNetworkInfo> { new HeatNetworkInfo { HnId = networkId } } },
            };

            _mockUserService.Setup(s => s.GetUserWithDetailsAsync(rpUserId)).ReturnsAsync(rpUser);
            _mockInvitationService.Setup(s => s.GetInvitedUsersAsRegisteredAsync(rpUserId)).ReturnsAsync(invitations);
            _mockUserService.Setup(s => s.GetUsersByInvitedEmailsWithDetailsAsync(It.IsAny<List<string>>())).ReturnsAsync(registeredUsersDetail);

            // FIX: Ensure the mapped object has the ID so FirstOrDefault(x => x.Id == ruser.Id) works
            _mockMapper.Setup(m => m.Map<List<ManagedUserResponse>>(registeredUsersDetail))
                .Returns(new List<ManagedUserResponse> {
            new ManagedUserResponse {
                Id = "u1-reg-id", // MUST MATCH the Detail ID above
                EmailId = email1,
                Status = UserStatus.Active.ToString()
            }
                });

            // Act
            var result = await _controller.GetManagedUsersAsync(rpUserId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var managedUsers = Assert.IsType<List<ManagedUserResponse>>(okResult.Value);

            // We expect 2 contributors. RP is filtered out by the controller logic.
            Assert.Equal(2, managedUsers.Count);

            // Verify User 1: Registered record exists, invite record is hidden
            Assert.Contains(managedUsers, u => u.EmailId == email1 && u.Status == UserStatus.Active.ToString());
            Assert.DoesNotContain(managedUsers, u => u.EmailId == email1 && u.Status == "Rejected");

            // Verify User 2: Still shows "Rejected" because they are not registered
            Assert.Contains(managedUsers, u => u.EmailId == email2 && u.Status == "Rejected");

            // Verify RP is NOT in the list
            Assert.DoesNotContain(managedUsers, u => u.EmailId == "rpuser@test.com");
        }

        #endregion

        #region IsRpUser

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task IsRpUser_ShouldReturnOk_WithCorrectRoleStatus(bool hasRole)
        {
            // Arrange
            string email = "test@example.com";
            var user = new User
            {
                EmailId = email,
                Roles = hasRole ? new List<UserRole> { UserRole.ResponsibleParty } : new List<UserRole>()
            };

            _mockUserService.Setup(s => s.GetByEmailAsync(email))
                .ReturnsAsync(user);

            // Act
            var result = await _controller.IsRpUser(email);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(hasRole, okResult.Value);
        }


        [Fact]
        public async Task IsRpUser_ShouldReturnNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            string email = "test@example.com";
            _mockUserService.Setup(s => s.GetByEmailAsync(email))
                .ReturnsAsync((User)null);

            // Act
            var result = await _controller.IsRpUser(email);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task IsRpUser_ShouldReturn500_WhenExceptionOccurs()
        {
            // Arrange
            string email = "error@example.com";
            _mockUserService.Setup(s => s.GetByEmailAsync(email))
                .ThrowsAsync(new System.Exception("Database failure"));

            // Act
            var result = await _controller.IsRpUser(email);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        }
        #endregion

        #region IsActiveUser
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task IsActiveUser_ShouldReturnBadRequest_WhenEmailIsInvalid(string email)
        {
            // Act
            var result = await _controller.IsActiveUser(email);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Email ID must be provided.", badRequestResult.Value);
        }

        [Theory]
        [InlineData(UserStatus.Active, true)]
        [InlineData(UserStatus.InActive, false)]
        public async Task IsActiveUser_ShouldReturnOk_WithExpectedActiveStatus(UserStatus status, bool expectedResult)
        {
            // Arrange
            string email = "test@example.com";
            var user = new User { EmailId = email, Status = status };

            _mockUserService.Setup(s => s.GetByEmailAsync(email))
                .ReturnsAsync(user);

            // Act
            var result = await _controller.IsActiveUser(email);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(expectedResult, okResult.Value);
        }

        [Fact]
        public async Task IsActiveUser_ShouldReturnNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            string email = "unknown@example.com";
            _mockUserService.Setup(s => s.GetByEmailAsync(email))
                .ReturnsAsync((User)null);

            // Act
            var result = await _controller.IsActiveUser(email);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }
        #endregion
                
        #region CheckOrganisationExistence
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CheckOrganisationExistence_ShouldReturnOk_WhenServiceReturnsResult(bool serviceResult)
        {
            // Arrange
            string houseNumber = "12345678";
            _mockOrgService.Setup(s => s.IsOrganizationExists(houseNumber))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.CheckOrganisationExistence(houseNumber);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(serviceResult, okResult.Value);
        }

        [Fact]
        public async Task CheckOrganisationExistence_ShouldReturn500_WhenExceptionOccurs()
        {
            // Arrange
            string houseNumber = "12345678";
            _mockOrgService.Setup(s => s.IsOrganizationExists(houseNumber))
                .ThrowsAsync(new System.Exception("Database connection failed"));

            // Act
            var result = await _controller.CheckOrganisationExistence(houseNumber);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
            Assert.Equal("An unexpected error occurred.", objectResult.Value);
        }

        [Fact]
        public async Task CheckOrganisationExistence_BadRequest()
        {
            // Arrange
            string houseNumber = "";
            _mockOrgService.Setup(s => s.IsOrganizationExists(houseNumber))
                .ThrowsAsync(new System.Exception("Database connection failed"));

            // Act
            var result = await _controller.CheckOrganisationExistence(houseNumber);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);            
        }
        #endregion

        #region GetHeatNetworkUsersWithRoles

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task GetHeatNetworkUsersWithRoles_ShouldReturnBadRequest_WhenHnIdIsMissing(string invalidId)
        {
            // Act
            var result = await _controller.GetHeatNetworkUsersWithRoles(invalidId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Heat Network ID must be provided.", badRequestResult.Value);
        }

        [Fact]
        public async Task GetHeatNetworkUsersWithRoles_ShouldReturnOk_WithRpAtFirstPosition()
        {
            // Arrange
            var hnId = "HN123";
            var rpUser = new User { Id = "rp-user-id", EmailId = "rp@test.com" };
            var otherUsers = new List<UserRoleDetailResponse>
        {
            new() { EmailId = "other@test.com" }
        };
            var mappedRp = new UserRoleDetailResponse { EmailId = "rp@test.com" };

            _mockUserService.Setup(s => s.GetResponsiblePartyByHnIdAsync(hnId))
                .ReturnsAsync(rpUser);
            _mockUserService.Setup(s => s.GetHeatNetworkUsersWithRolesAsync(hnId))
                .ReturnsAsync(otherUsers);
            _mockMapper.Setup(m => m.Map<UserRoleDetailResponse>(rpUser))
                .Returns(mappedRp);

            // Act
            var result = await _controller.GetHeatNetworkUsersWithRoles(hnId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var finalData = Assert.IsType<List<UserRoleDetailResponse>>(okResult.Value);

            Assert.Equal(2, finalData.Count);
            Assert.Equal("rp@test.com", finalData[0].EmailId); // Verify RP is at index 0
        }

        [Fact]
        public async Task GetHeatNetworkUsersWithRoles_ShouldWork_WhenNoOtherUsersFound()
        {
            // Arrange
            var hnId = "HN123";
            _mockUserService.Setup(s => s.GetResponsiblePartyByHnIdAsync(hnId))
                .ReturnsAsync(new User());
            _mockUserService.Setup(s => s.GetHeatNetworkUsersWithRolesAsync(hnId))
                .ReturnsAsync((List<UserRoleDetailResponse>)null); // Service returns null
            _mockMapper.Setup(m => m.Map<UserRoleDetailResponse>(It.IsAny<User>()))
                .Returns(new UserRoleDetailResponse());

            // Act
            var result = await _controller.GetHeatNetworkUsersWithRoles(hnId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var finalData = Assert.IsType<List<UserRoleDetailResponse>>(okResult.Value);
            Assert.Single(finalData); // Only the RP should be present
        }

        [Fact]
        public async Task GetHeatNetworkUsersWithRoles_ShouldReturnNotFound_WhenNoRpExists()
        {
            // Arrange
            var hnId = "HN123";
            _mockUserService.Setup(s => s.GetResponsiblePartyByHnIdAsync(hnId))
                .ReturnsAsync((User)null);

            // Act
            var result = await _controller.GetHeatNetworkUsersWithRoles(hnId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Contains("No Responsible Party found", notFoundResult.Value.ToString());
        }
        #endregion

        #region UpdateOrgId

        [Fact]
        public async Task UpdateOrgId_ShouldReturnBadRequest_WhenModelStateIsInvalid()
        {
            // Arrange
            _controller.ModelState.AddModelError("UserId", "Required");
            var request = new UpdateUserOrgIdRequest();

            // Act
            var result = await _controller.UpdateOrgId(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateOrgId_ShouldReturnNoContent_WhenUpdateIsSuccessful()
        {
            // Arrange
            var request = new UpdateUserOrgIdRequest { UserId = "user-123", OrgId = "org-456" };

            // 1. Mock the UpdateResult
            var mockUpdateResult = new Mock<UpdateResult>();
            mockUpdateResult.Setup(r => r.IsAcknowledged).Returns(true);
            mockUpdateResult.Setup(r => r.MatchedCount).Returns(1);

            _mockUserService.Setup(s => s.UpdateOrgIdAsync(request.UserId, request.OrgId))
                .ReturnsAsync(mockUpdateResult.Object);

            // Act
            var result = await _controller.UpdateOrgId(request);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task UpdateOrgId_ShouldReturnNotFound_WhenNoUserMatches()
        {
            // Arrange
            var request = new UpdateUserOrgIdRequest { UserId = "invalid-id", OrgId = "org-1" };

            // Fix: Mock the abstract UpdateResult class
            var mockUpdateResult = new Mock<UpdateResult>();
            mockUpdateResult.Setup(r => r.IsAcknowledged).Returns(true);
            mockUpdateResult.Setup(r => r.MatchedCount).Returns(0);

            _mockUserService.Setup(s => s.UpdateOrgIdAsync(request.UserId, request.OrgId))
                .ReturnsAsync(mockUpdateResult.Object);

            // Act
            var result = await _controller.UpdateOrgId(request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal($"User with ID '{request.UserId}' not found.", notFoundResult.Value);
        }

        [Fact]
        public async Task UpdateOrgId_ShouldReturn500_WhenDatabaseNotAcknowledged()
        {
            // Arrange
            var request = new UpdateUserOrgIdRequest { UserId = "user-1", OrgId = "org-1" };

            // Fix: Mock the abstract UpdateResult class
            var mockUpdateResult = new Mock<UpdateResult>();
            mockUpdateResult.Setup(r => r.IsAcknowledged).Returns(false);

            _mockUserService.Setup(s => s.UpdateOrgIdAsync(request.UserId, request.OrgId))
                .ReturnsAsync(mockUpdateResult.Object);

            // Act
            var result = await _controller.UpdateOrgId(request);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
            Assert.Equal("Database update operation was not acknowledged.", objectResult.Value);
        }
        #endregion

        [Fact]
        public async Task GetUsers_ShouldReturnListOfUsers()
        {
            _mockUserService.Setup(u => u.GetAsync()).Returns(Task.FromResult(new List<User>
            {
                new User { Id = "1", FirstName = "John", LastName = "Doe" },
                new User { Id = "2", FirstName = "Jane", LastName = "Smith" }
            }));

            _mockMapper.Setup(m => m.Map<List<UserResponse>>(It.IsAny<List<User>>()))
                .Returns((List<User> users) => users.Select(u => new UserResponse { Id = u.Id, FirstName = u.FirstName, LastName = u.LastName }).ToList());

            var result = await _controller.GetUsers();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var users = Assert.IsType<List<UserResponse>>(okResult.Value);
            Assert.Equal(2, users.Count);
        }

        [Fact]
        public async Task GetUsers_ThrowException()
        {
            _mockUserService.Setup(u => u.GetAsync()).Throws(new Exception());            

            var result = await _controller.GetUsers();

            var res = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, res.StatusCode);
        }

        [Fact]
        public async Task GetUserByOneLoginId_ShouldReturnListOfUsers()
        {
            _mockUserService.Setup(u => u.GetByUserOneLoginIdAsync(It.IsAny<string>())).Returns(Task.FromResult(new User { Id = "1", FirstName = "John", LastName = "Doe" }));

            _mockMapper.Setup(m => m.Map<UserResponse>(It.IsAny<User>()))
                .Returns((User user) => new UserResponse { Id = user.Id!, FirstName = user.FirstName, LastName = user.LastName });

            var result = await _controller.GetUserByOneLoginId("oneLoginId");

            Assert.IsType<OkObjectResult>(result.Result);            
        }

        [Fact]
        public async Task GetUserByOneLoginId_ThrowException()
        {
            _mockUserService.Setup(u => u.GetByUserOneLoginIdAsync(It.IsAny<string>())).Throws(new Exception());

            var result = await _controller.GetUserByOneLoginId("oneLoginId");

            var res = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, res.StatusCode);
        }

        [Fact]
        public async Task GetUserByOneLoginId_UserNotFound()
        {
            _mockUserService.Setup(u => u.GetByUserOneLoginIdAsync(It.IsAny<string>())).Returns(Task.FromResult((User)null!));

            var result = await _controller.GetUserByOneLoginId("oneLoginId");

            Assert.IsType<NotFoundResult>(result.Result);            
        }

        [Fact]
        public async Task InitialRegisterUser_ReturnsConflict()
        {
            var request = new InitialUserRegistrationRequest
            {
                EmailId = "test",
                OneLoginId = "test",
                Status = UserStatus.Active
            };

            _mockUserService.Setup(u => u.GetByUserOneLoginIdAsync(It.IsAny<string>())).Returns(Task.FromResult(new User { Id = "1", FirstName = "John", LastName = "Doe" }));

            var result = await _controller.InitialRegisterUser(request);

            Assert.IsType<ConflictObjectResult>(result.Result);
        }

        [Fact]
        public async Task InitialRegisterUser_ReturnsOk()
        {
            var request = new InitialUserRegistrationRequest
            {
                EmailId = "test",
                OneLoginId = "test",
                Status = UserStatus.Active
            };

            _mockUserService.Setup(u => u.GetByUserOneLoginIdAsync(It.IsAny<string>())).ReturnsAsync((User)null!);
            _mockUserService.Setup(u => u.CreateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
            var result = await _controller.InitialRegisterUser(request);

            var res = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status201Created, res.StatusCode);
        }

        [Fact]
        public async Task InitialRegisterUser_ThrowException()
        {
            var request = new InitialUserRegistrationRequest
            {
                EmailId = "test",
                OneLoginId = "test",
                Status = UserStatus.Active
            };

            _mockUserService.Setup(u => u.GetByUserOneLoginIdAsync(It.IsAny<string>())).Throws(new Exception());

            var result = await _controller.InitialRegisterUser(request);

            var res = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, res.StatusCode);
        }

        [Fact]
        public async Task InitialRegisterUser_InvalidModelState()
        {
            var request = new InitialUserRegistrationRequest
            {
                EmailId = "test",
                OneLoginId = "test",
                Status = UserStatus.Active
            };

            _controller.ModelState.AddModelError("EmailId", "Required");

            var result = await _controller.InitialRegisterUser(request);

            var res = Assert.IsType<ObjectResult>(result.Result);
            Assert.IsType<ValidationProblemDetails>(res.Value);
        }

        [Fact]
        public async Task UpdateUserAndOrgDetails_Success()
        {
            var request = new UpdateUserOrganisationRequest
            {
                Organisation = new OrganisationRequest { Name = "Test Org" },
                Role = UserRole.Contributor,
                FirstName = "John",
                LastName = "Doe",
                ContactNumberExtension = "123",
                JobTitle = "Engineer",
                LandlineNumber = "123456789",
                MobileNumber = "987654321",
                PreferredContactType = PreferredContactType.PreferNotToSay,
            };

            _mockUserService.Setup(u => u.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(new User { Id = "test", EmailId = "test" });
            _mockOrgService.Setup(o => o.CreateAsync(It.IsAny<Organisation>())).Returns(Task.CompletedTask);
            _mockCounterService.Setup(o => o.GetNextSequenceValue(It.IsAny<string>())).ReturnsAsync(12);            
            _mockMapper.Setup(m => m.Map<RegisteredAddress>(It.IsAny<RegisteredAddress>())).Returns(new RegisteredAddress {AddressLine1 = "test" });
            _mockUserService.Setup(u => u.UpdateAsync(It.IsAny<string>(), It.IsAny<User>())).Returns(Task.CompletedTask);
            _mockHeatNetworkService.Setup(h => h.GetByOfgemEmailIdAsync(It.IsAny<string>())).ReturnsAsync(new List<HeatNetwork> { new HeatNetwork { Id = "test", HnId = "test", OrgId = "test", CreatedBy = "test" } });
            _mockUserService.Setup(u => u.UpdateUserNetwork(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ContributorRole>())).Returns(Task.CompletedTask);
            _mockEmailService.Setup(e => e.TrySendOrgCreatedEmailAsync(It.IsAny<User>(), It.IsAny<Organisation>())).Returns(Task.CompletedTask);

            var result = await _controller.UpdateUserAndOrgDetails("id", request);
            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task UpdateUserAndOrgDetails_NoRoles_Success()
        {
            var request = new UpdateUserOrganisationRequest
            {
                Organisation = new OrganisationRequest { Name = "Test Org" },
                Role = UserRole.Contributor,
                FirstName = "John",
                LastName = "Doe",
                ContactNumberExtension = "123",
                JobTitle = "Engineer",
                LandlineNumber = "123456789",
                MobileNumber = "987654321",
                PreferredContactType = PreferredContactType.PreferNotToSay,
            };

            _mockUserService.Setup(u => u.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(new User { Id = "test", EmailId = "test", Roles = null! });
            _mockOrgService.Setup(o => o.CreateAsync(It.IsAny<Organisation>())).Returns(Task.CompletedTask);
            _mockCounterService.Setup(o => o.GetNextSequenceValue(It.IsAny<string>())).ReturnsAsync(12);
            _mockMapper.Setup(m => m.Map<RegisteredAddress>(It.IsAny<RegisteredAddress>())).Returns(new RegisteredAddress { AddressLine1 = "test" });
            _mockUserService.Setup(u => u.UpdateAsync(It.IsAny<string>(), It.IsAny<User>())).Returns(Task.CompletedTask);
            _mockHeatNetworkService.Setup(h => h.GetByOfgemEmailIdAsync(It.IsAny<string>())).ReturnsAsync(new List<HeatNetwork> { new HeatNetwork { Id = "test", HnId = "test", OrgId = "test", CreatedBy = "test" } });
            _mockUserService.Setup(u => u.UpdateUserNetwork(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ContributorRole>())).Returns(Task.CompletedTask);
            _mockEmailService.Setup(e => e.TrySendOrgCreatedEmailAsync(It.IsAny<User>(), It.IsAny<Organisation>())).Returns(Task.CompletedTask);

            var result = await _controller.UpdateUserAndOrgDetails("id", request);
            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task UpdateUserAndOrgDetails_UserNotFound()
        {
            var request = new UpdateUserOrganisationRequest
            {
                Organisation = new OrganisationRequest { Name = "Test Org" },
                Role = UserRole.Contributor,
                FirstName = "John",
                LastName = "Doe",
                ContactNumberExtension = "123",
                JobTitle = "Engineer",
                LandlineNumber = "123456789",
                MobileNumber = "987654321",
                PreferredContactType = PreferredContactType.PreferNotToSay,
            };

            _mockUserService.Setup(u => u.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((User)null!);            

            var result = await _controller.UpdateUserAndOrgDetails("id", request);
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task UpdateUserAndOrgDetails_ThrowException()
        {
            var request = new UpdateUserOrganisationRequest
            {
                Organisation = new OrganisationRequest { Name = "Test Org" },
                Role = UserRole.Contributor,
                FirstName = "John",
                LastName = "Doe",
                ContactNumberExtension = "123",
                JobTitle = "Engineer",
                LandlineNumber = "123456789",
                MobileNumber = "987654321",
                PreferredContactType = PreferredContactType.PreferNotToSay,
            };

            _mockUserService.Setup(u => u.GetByIdAsync(It.IsAny<string>())).Throws(new Exception());

            var result = await _controller.UpdateUserAndOrgDetails("id", request);
            var res = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, res.StatusCode);
        }

        [Fact]
        public async Task UpdateUserAndOrgDetails_InvalidModelState()
        {
            var request = new UpdateUserOrganisationRequest
            {
                Organisation = new OrganisationRequest { Name = "Test Org" },
                Role = UserRole.Contributor,
                FirstName = "John",
                LastName = "Doe",
                ContactNumberExtension = "123",
                JobTitle = "Engineer",
                LandlineNumber = "123456789",
                MobileNumber = "987654321",
                PreferredContactType = PreferredContactType.PreferNotToSay,
            };

            _controller.ModelState.AddModelError("EmailId", "Required");

            var result = await _controller.UpdateUserAndOrgDetails("id", request);
            var res = Assert.IsType<ObjectResult>(result.Result);
            Assert.IsType<ValidationProblemDetails>(res.Value);
        }

        [Fact]
        public async Task RegisterOrganisationAndLinkUserAsync_Success()
        {
            var request = new OrganisationRequest
            {
                CompaniesHouseNumber = "test",
                Name = "test",
                RegisteredAddress = new RegisteredAddress { AddressLine1 = "add1" },
                Type = OrganisationType.UkCompaniesHouse
            };

            _mockUserService.Setup(u => u.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(new User { Id = "test", EmailId = "test", Roles = new List<UserRole> { UserRole.NetworkManager } });
            _mockCounterService.Setup(o => o.GetNextSequenceValue(It.IsAny<string>())).ReturnsAsync(12);
            _mockInvitationService.Setup(i => i.GetByInvitedEmailAsync(It.IsAny<string>())).ReturnsAsync(new Invitation { Status = InvitationStatus.Accepted, InvitedEmail = "test", InviterUserId = "test" });
            _mockOrgService.Setup(o => o.CreateAsync(It.IsAny<Organisation>())).Returns(Task.CompletedTask);            

            var mockUpdateResult = new Mock<UpdateResult>();
            mockUpdateResult.Setup(r => r.IsAcknowledged).Returns(true);
            mockUpdateResult.Setup(r => r.MatchedCount).Returns(1);
            mockUpdateResult.Setup(r => r.ModifiedCount).Returns(1);

            _mockUserService.Setup(s => s.UpdateOrgIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(mockUpdateResult.Object);

            var result = await _controller.RegisterOrganisationAndLinkUserAsync("6a3aa661be3d3d47c69044d6", request);
            var res = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status201Created, res.StatusCode);
        }

        [Fact]
        public async Task RegisterOrganisationAndLinkUserAsync_InternalServerError()
        {
            var request = new OrganisationRequest
            {
                CompaniesHouseNumber = "test",
                Name = "test",
                RegisteredAddress = new RegisteredAddress { AddressLine1 = "add1" },
                Type = OrganisationType.UkCompaniesHouse
            };

            _mockUserService.Setup(u => u.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(new User { Id = "test", EmailId = "test", Roles = new List<UserRole> { UserRole.NetworkManager } });
            _mockCounterService.Setup(o => o.GetNextSequenceValue(It.IsAny<string>())).ReturnsAsync(12);
            _mockInvitationService.Setup(i => i.GetByInvitedEmailAsync(It.IsAny<string>())).ReturnsAsync(new Invitation { Status = InvitationStatus.Accepted, InvitedEmail = "test", InviterUserId = "test" });
            _mockOrgService.Setup(o => o.CreateAsync(It.IsAny<Organisation>())).Returns(Task.CompletedTask);

            var mockUpdateResult = new Mock<UpdateResult>();
            mockUpdateResult.Setup(r => r.IsAcknowledged).Returns(true);
            mockUpdateResult.Setup(r => r.MatchedCount).Returns(1);
            mockUpdateResult.Setup(r => r.ModifiedCount).Returns(0);

            _mockUserService.Setup(s => s.UpdateOrgIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(mockUpdateResult.Object);

            var result = await _controller.RegisterOrganisationAndLinkUserAsync("6a3aa661be3d3d47c69044d6", request);
            var res = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, res.StatusCode);
        }

        [Fact]
        public async Task RegisterOrganisationAndLinkUserAsync_UserNotFound()
        {
            var request = new OrganisationRequest
            {
                CompaniesHouseNumber = "test",
                Name = "test",
                RegisteredAddress = new RegisteredAddress { AddressLine1 = "add1" },
                Type = OrganisationType.UkCompaniesHouse
            };

            _mockUserService.Setup(u => u.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((User)null!);            

            var result = await _controller.RegisterOrganisationAndLinkUserAsync("6a3aa661be3d3d47c69044d6", request);
            Assert.IsType<NotFoundObjectResult>(result.Result);            
        }

        [Fact]
        public async Task RegisterOrganisationAndLinkUserAsync_ThrowException()
        {
            var request = new OrganisationRequest
            {
                CompaniesHouseNumber = "test",
                Name = "test",
                RegisteredAddress = new RegisteredAddress { AddressLine1 = "add1" },
                Type = OrganisationType.UkCompaniesHouse
            };

            _mockUserService.Setup(u => u.GetByIdAsync(It.IsAny<string>())).Throws(new Exception());

            var result = await _controller.RegisterOrganisationAndLinkUserAsync("6a3aa661be3d3d47c69044d6", request);
            var res = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, res.StatusCode);
        }

        [Fact]
        public async Task RegisterOrganisationAndLinkUserAsync_ModelStateError()
        {
            var request = new OrganisationRequest
            {
                CompaniesHouseNumber = "test",
                Name = "test",
                RegisteredAddress = new RegisteredAddress { AddressLine1 = "add1" },
                Type = OrganisationType.UkCompaniesHouse
            };

            _controller.ModelState.AddModelError("EmailId", "Required");

            var result = await _controller.RegisterOrganisationAndLinkUserAsync("6a3aa661be3d3d47c69044d6", request);
            Assert.IsType<BadRequestObjectResult>(result.Result);            
        }

        [Fact]
        public async Task RegisterOrganisationAndLinkUserAsync_BadRequest()
        {
            var request = new OrganisationRequest
            {
                CompaniesHouseNumber = "test",
                Name = "test",
                RegisteredAddress = new RegisteredAddress { AddressLine1 = "add1" },
                Type = OrganisationType.UkCompaniesHouse
            };

            _controller.ModelState.AddModelError("EmailId", "Required");

            var result = await _controller.RegisterOrganisationAndLinkUserAsync("", request);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task UpdateUserDetails_Success()
        {
            var request = new UpdateUserDetailsRequest
            {
                ContactNumberExtension = "1232",
                FirstName = "John",
                PreferredContactType = PreferredContactType.Mobile,
                MobileNumber = "1111122222",
                Role = UserRole.ResponsibleParty
            };

            _mockUserService.Setup(u => u.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(new User { Id = "test", EmailId = "test", Roles = new List<UserRole> { UserRole.NetworkManager } });
            _mockUserService.Setup(u => u.UpdateAsync(It.IsAny<string>(), It.IsAny<User>())).Returns(Task.CompletedTask);

            var result = await _controller.UpdateUserDetails("id", request);
            Assert.IsType<NoContentResult>(result.Result);
        }

        [Fact]
        public async Task UpdateUserDetails_NoExistingRole_Success()
        {
            var request = new UpdateUserDetailsRequest
            {
                ContactNumberExtension = "1232",
                FirstName = "John",
                PreferredContactType = PreferredContactType.Mobile,
                MobileNumber = "1111122222",
                Role = UserRole.ResponsibleParty
            };

            _mockUserService.Setup(u => u.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(new User { Id = "test", EmailId = "test", Roles = null! });
            _mockUserService.Setup(u => u.UpdateAsync(It.IsAny<string>(), It.IsAny<User>())).Returns(Task.CompletedTask);

            var result = await _controller.UpdateUserDetails("id", request);
            Assert.IsType<NoContentResult>(result.Result);
        }

        [Fact]
        public async Task UpdateUserDetails_ThrowException()
        {
            var request = new UpdateUserDetailsRequest
            {
                ContactNumberExtension = "1232",
                FirstName = "John",
                PreferredContactType = PreferredContactType.Mobile,
                MobileNumber = "1111122222",
                Role = UserRole.ResponsibleParty
            };

            _mockUserService.Setup(u => u.GetByIdAsync(It.IsAny<string>())).Throws(new Exception());
            
            var result = await _controller.UpdateUserDetails("id", request);
            var res = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, res.StatusCode);
        }

        [Fact]
        public async Task UpdateUserDetails_UserNotFound()
        {
            var request = new UpdateUserDetailsRequest
            {
                ContactNumberExtension = "1232",
                FirstName = "John",
                PreferredContactType = PreferredContactType.Mobile,
                MobileNumber = "1111122222",
                Role = UserRole.ResponsibleParty
            };

            _mockUserService.Setup(u => u.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((User)null!);

            var result = await _controller.UpdateUserDetails("id", request);
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task UpdateUserDetails_ModelStateError()
        {
            var request = new UpdateUserDetailsRequest
            {
                ContactNumberExtension = "1232",
                FirstName = "John",
                PreferredContactType = PreferredContactType.Mobile,
                MobileNumber = "1111122222",
                Role = UserRole.ResponsibleParty
            };

            _controller.ModelState.AddModelError("EmailId", "Required");

            var result = await _controller.UpdateUserDetails("id", request);
            var res = Assert.IsType<ObjectResult>(result.Result);
            Assert.IsType<ValidationProblemDetails>(res.Value);
        }

        [Fact]
        public async Task GetContributorRoles_Ok()
        {
            var result = await _controller.GetContributorRoles();
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetUserRoles_Ok()
        {
            var result = await _controller.GetUserRoles();
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task DeleteUser_Ok()
        {
            _mockUserService.Setup(u => u.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(new User { Id = "test", EmailId = "test", Roles = new List<UserRole> { UserRole.NetworkManager } });
            _mockUserService.Setup(u => u.RemoveAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

            var result = await _controller.DeleteUser("uid");
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteUser_UserNotFound()
        {
            _mockUserService.Setup(u => u.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((User)null!);

            var result = await _controller.DeleteUser("uid");
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteUser_ThrowException()
        {
            _mockUserService.Setup(u => u.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(new User { Id = "test", EmailId = "test", Roles = new List<UserRole> { UserRole.NetworkManager } });
            _mockUserService.Setup(u => u.RemoveAsync(It.IsAny<string>())).Throws(new Exception());

            var result = await _controller.DeleteUser("uid");
            var res = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, res.StatusCode);
        }

        [Fact]
        public async Task GetNetworkManagersAsync_Ok()
        {
            _mockUserService.Setup(u => u.GetUserWithDetailsAsync(It.IsAny<string>())).ReturnsAsync(new UserDetailsResult { Id = "test", EmailId = "test", Roles = new List<UserRole> { UserRole.NetworkManager } });
            _mockInvitationService.Setup(u => u.GetNetworkManagersByInviterUserId(It.IsAny<string>())).ReturnsAsync(new List<Invitation>());
            _mockMapper.Setup(s => s.Map<List<InvitedUserResponse>>(It.IsAny<List<Invitation>>())).Returns(new List<InvitedUserResponse>());
            var result = await _controller.GetNetworkManagersAsync("uid");
            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetNetworkManagersAsync_UserNotFound()
        {
            _mockUserService.Setup(u => u.GetUserWithDetailsAsync(It.IsAny<string>())).ReturnsAsync((UserDetailsResult)null!);
            
            var result = await _controller.GetNetworkManagersAsync("uid");
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetRegisteredUsersAsync_Ok()
        {
            _mockUserService.Setup(u => u.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(new User { Id = "test", EmailId = "test", Roles = new List<UserRole> { UserRole.NetworkManager } });
            _mockInvitationService.Setup(u => u.GetByInviterUserIdAsync(It.IsAny<string>())).ReturnsAsync(new List<Invitation> { new Invitation { InvitedEmail = "test"} });
            _mockUserService.Setup(u => u.GetRegisteredUsers(It.IsAny<List<string>>())).ReturnsAsync(new List<User> { new User { Id = "test", EmailId = "test", Roles = new List<UserRole> { UserRole.NetworkManager } } });
            _mockMapper.Setup(s => s.Map<List<UserResponse>>(It.IsAny<List<User>>())).Returns(new List<UserResponse>());
            var result = await _controller.GetRegisteredUsersAsync("uid");
            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetRegisteredUsersAsync_EmptyResponse()
        {
            _mockUserService.Setup(u => u.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(new User { Id = "test", EmailId = "test", Roles = new List<UserRole> { UserRole.NetworkManager } });
            _mockInvitationService.Setup(u => u.GetByInviterUserIdAsync(It.IsAny<string>())).ReturnsAsync(new List<Invitation> { new Invitation { InvitedEmail = "test" } });
            _mockUserService.Setup(u => u.GetRegisteredUsers(It.IsAny<List<string>>())).ReturnsAsync(new List<User>());
            _mockMapper.Setup(s => s.Map<List<UserResponse>>(It.IsAny<List<User>>())).Returns(new List<UserResponse>());
            var result = await _controller.GetRegisteredUsersAsync("uid");
            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetRegisteredUsersAsync_UserNotFound()
        {
            _mockUserService.Setup(u => u.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((User)null!);
            
            var result = await _controller.GetRegisteredUsersAsync("uid");
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetUsersByOrganisation_Ok()
        {
            _mockUserService.Setup(u => u.GetUsersByOrgIdAsync(It.IsAny<string>())).ReturnsAsync(new List<User> { new User { Id = "test", EmailId = "test", Roles = new List<UserRole> { UserRole.NetworkManager } } });            
            _mockMapper.Setup(s => s.Map<List<UserResponse>>(It.IsAny<List<User>>())).Returns(new List<UserResponse>());
            var result = await _controller.GetUsersByOrganisation("uid");
            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetUsersByOrganisation_BadRequest()
        {            
            var result = await _controller.GetUsersByOrganisation("");
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetUsersByOrganisation_UsersNotFound()
        {
            _mockUserService.Setup(u => u.GetUsersByOrgIdAsync(It.IsAny<string>())).ReturnsAsync((List<User>)null!);
            
            var result = await _controller.GetUsersByOrganisation("uid");
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }
    }
}
