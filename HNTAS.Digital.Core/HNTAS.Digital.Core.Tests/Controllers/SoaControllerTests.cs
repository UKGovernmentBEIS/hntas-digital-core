using HNTAS.Core.Api.Controllers;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models;
using HNTAS.Core.Api.Models.Soa;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace HNTAS.Digital.Core.Tests.Controllers
{
    public class SoaControllerTests
    {
        private readonly Mock<ISoaService> _mockSoaService;
        private readonly Mock<ILogger<SOAController>> _mockLogger;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IHeatNetworkService> _mockHeatNetworkService;
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IAuditService> _mockAuditService;
        private readonly Mock<INotificationHistoryService> _mockNotificationHistoryService;
        private readonly Mock<IInvitationService> _mockInvitationService;

        private readonly SOAController _controller;

        public SoaControllerTests()
        {
            _mockSoaService = new Mock<ISoaService>();
            _mockLogger = new Mock<ILogger<SOAController>>();
            _mockEmailService = new Mock<IEmailService>();
            _mockHeatNetworkService = new Mock<IHeatNetworkService>();
            _mockUserService = new Mock<IUserService>();
            _mockAuditService = new Mock<IAuditService>();
            _mockNotificationHistoryService = new Mock<INotificationHistoryService>();
            _mockInvitationService = new Mock<IInvitationService>();
            _controller = new SOAController(_mockSoaService.Object, _mockLogger.Object, _mockEmailService.Object, _mockHeatNetworkService.Object, _mockUserService.Object, _mockAuditService.Object, _mockNotificationHistoryService.Object, _mockInvitationService.Object);
        }

        [Fact]
        public async Task UpdateSoaStatus_WithValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new ElementSoaStatusUpdateRequest
            {
                HnId = "HN0000001",
                ElementId = "00001",
                Stage = SoaStage.Stage1,
                ElementSoaStatus = NetworkDetailsStatus.InProgress,
                SoaStatusUpdatedBy = "testuser",
                SoaStatuses = new List<SoaStatusWithCount>(),
                ElementType = ElementTypeInShort.EC
            };

            _mockSoaService
                .Setup(s => s.UpdateSoaStatus(
                    It.IsAny<string>(),
                    It.IsAny<ElementTypeInShort>(),
                    It.IsAny<SoaStage>(),
                    It.IsAny<List<SoaStatusWithCount>>(),
                    It.IsAny<string>(),
                    It.IsAny<NetworkDetailsStatus>()))
                .Returns(Task.CompletedTask);

            _mockHeatNetworkService.Setup(s => s.GetByHnIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new HeatNetwork { HnId = "HN0000001", Name = "Test Heat Network" });

            // Act
            var result = await _controller.UpdateSoaStatus(request);

            // Assert
            var okResult = Assert.IsType<OkResult>(result);
            Assert.NotNull(okResult);
        }

        [Fact]
        public async Task UpdateSoaStatus_WithInvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            _controller.ModelState.AddModelError("HnId", "HnId is required");

            var request = new ElementSoaStatusUpdateRequest
            {
                HnId = null
            };

            // Act
            var result = await _controller.UpdateSoaStatus(request);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);

            var errors = Assert.IsAssignableFrom<IDictionary<string, object>>(badRequest.Value);
            Assert.True(errors.ContainsKey("HnId"));

        }

        [Fact]
        public async Task UpdateSoaStatus_ThrowsException_WhenUnexpectedErrorOccurs()
        {
            // Arrange
            var request = new ElementSoaStatusUpdateRequest
            {
                HnId = "HN0000001",
                ElementId = "00001",
                Stage = SoaStage.Stage1,
                ElementSoaStatus = NetworkDetailsStatus.InProgress,
                SoaStatusUpdatedBy = "testuser",
                SoaStatuses = new List<SoaStatusWithCount>(),
                ElementType = ElementTypeInShort.EC
            };

            _mockHeatNetworkService
                .Setup(s => s.GetByHnIdAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("DB failure"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() =>
                _controller.UpdateSoaStatus(request));
        }


        [Fact]
        public async Task UpdateSoaStatus_ReturnsNotFound_WhenHeatNetworkDoesNotExist()
        {
            // Arrange
            var request = new ElementSoaStatusUpdateRequest
            {
                HnId = "HN0000001",
                ElementId = "00001",
                Stage = SoaStage.Stage1,
                ElementSoaStatus = NetworkDetailsStatus.InProgress,
                SoaStatusUpdatedBy = "testuser",
                SoaStatuses = new List<SoaStatusWithCount>(),
                ElementType = ElementTypeInShort.EC
            };

            _mockHeatNetworkService
                .Setup(s => s.GetByHnIdAsync(It.IsAny<string>()))
                .ReturnsAsync((HeatNetwork)null);

            // Act
            var result = await _controller.UpdateSoaStatus(request);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("No heat network found for HnId 'HN0000001'.", notFound.Value);
        }

        [Fact]
        public async Task UpdateSoaStatus_WhenRegistrationEnabled_SavesAudit()
        {
            // Arrange
            Environment.SetEnvironmentVariable("IS_REGISTRATION_ENABLED", "true");

            var request = new ElementSoaStatusUpdateRequest
            {
                HnId = "HN0000001",
                ElementId = "00001",
                Stage = SoaStage.Stage1,
                ElementSoaStatus = NetworkDetailsStatus.InProgress,
                SoaStatusUpdatedBy = "testuser",
                SoaStatuses = new List<SoaStatusWithCount>(),
                ElementType = ElementTypeInShort.EC
            };

            var heatNetwork = new HeatNetwork { HnId = "HN0000001", Name = "Test HN" };

            _mockHeatNetworkService
                .Setup(s => s.GetByHnIdAsync(It.IsAny<string>()))
                .ReturnsAsync(heatNetwork);

            _mockSoaService
                .Setup(s => s.UpdateSoaStatus(
                    It.IsAny<string>(),
                    It.IsAny<ElementTypeInShort>(),
                    It.IsAny<SoaStage>(),
                    It.IsAny<List<SoaStatusWithCount>>(),
                    It.IsAny<string>(),
                    It.IsAny<NetworkDetailsStatus>()))
                .Returns(Task.CompletedTask);

            _mockAuditService
                .Setup(s => s.SaveAuditAsync<HeatNetwork>(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<HeatNetwork>(),
                    It.IsAny<HeatNetwork>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()
                    ))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdateSoaStatus(request);

            // Assert
            Assert.IsType<OkResult>(result);

            _mockAuditService.Verify(s =>
                s.SaveAuditAsync<HeatNetwork>(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<HeatNetwork>(),
                    It.IsAny<HeatNetwork>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()
                    ), Times.Once());
        }

        [Fact]
        public async Task SoaAssignAssessor_UpdateAssessor()
        {
            // Arrange
            var request = new ElementSoaAssignAssessorRequest
            {
                HnId = "HN0000001",
                AssessorAssessmentForElements = new List<AssessorAssessmentForElement>
                {
                    new AssessorAssessmentForElement
                    {
                        AssessorAssessments = new List<AssessorAssessment>
                        {
                            new AssessorAssessment
                            {
                                Assessment = "A0001",
                                AssessorEmail = "test",
                                AssessorFirstName = "test",
                                AssessorLastName = "test"
                            }
                        },
                    }
                },
                UpdatedBy = "testuser"
            };

            _mockHeatNetworkService.Setup(s => s.GetByHnIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new HeatNetwork { HnId = "HN0000001", Name = "Test Heat Network" });

            _mockSoaService
                .Setup(s => s.UpdateAssignAssessor(
                    It.IsAny<ElementSoaAssignAssessorRequest>(),
                    It.IsAny<NetworkElements>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>()))
                .ReturnsAsync(new NetworkElements());

            _mockHeatNetworkService.Setup(s => s.UpdateAsync(It.IsAny<string>(), It.IsAny<HeatNetwork>()))
                .Returns(Task.CompletedTask);

            _mockUserService.Setup(s => s.GetUserWithDetailsAsync(It.IsAny<string>()))
                .ReturnsAsync(new UserDetailsResult { FirstName = "Test", LastName = "User", Roles = new List<UserRole> { UserRole.ResponsibleParty } });

            _mockUserService.Setup(s => s.GetUsersAssociatedByHnIdAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(new List<User> { new User { FirstName = "Test", LastName = "User", EmailId = "test", Id = "test" } }));

            _mockInvitationService.Setup(s => s.GetByEmailsAndHnIdAsync(It.IsAny<List<string>>(), It.IsAny<string>()))
                .ReturnsAsync(new List<Invitation> { new Invitation { InviterUserId = "test1" } });

            _mockNotificationHistoryService.Setup(s => s.CreateAsync(It.IsAny<NotificationHistory>()))
                .Returns(Task.CompletedTask);
            // Act
            var result = await _controller.SoaAssignAssessor(request);

            // Assert
            _mockHeatNetworkService.Verify(s => s.UpdateAsync(It.IsAny<string>(), It.IsAny<HeatNetwork>()), Times.Once);
        }

        [Fact]
        public async Task SoaAssignAssessor_UpdateAssessor_UpdateNotification()
        {
            // Arrange
            var request = new ElementSoaAssignAssessorRequest
            {
                HnId = "HN0000001",
                AssessorAssessmentForElements = new List<AssessorAssessmentForElement>
                {
                    new AssessorAssessmentForElement
                    {
                        AssessorAssessments = new List<AssessorAssessment>
                        {
                            new AssessorAssessment
                            {
                                Assessment = "A0001",
                                AssessorEmail = "test",
                                AssessorFirstName = "test",
                                AssessorLastName = "test"
                            }
                        },
                    }
                },
                UpdatedBy = "testuser"
            };

            _mockHeatNetworkService.Setup(s => s.GetByHnIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new HeatNetwork { HnId = "HN0000001", Name = "Test Heat Network" });

            _mockSoaService
                .Setup(s => s.UpdateAssignAssessor(
                    It.IsAny<ElementSoaAssignAssessorRequest>(),
                    It.IsAny<NetworkElements>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>()))
                .ReturnsAsync(new NetworkElements());

            _mockHeatNetworkService.Setup(s => s.UpdateAsync(It.IsAny<string>(), It.IsAny<HeatNetwork>()))
                .Returns(Task.CompletedTask);

            _mockUserService.Setup(s => s.GetUserWithDetailsAsync(It.IsAny<string>()))
                .ReturnsAsync(new UserDetailsResult { FirstName = "Test", LastName = "User", Roles = new List<UserRole> { UserRole.NetworkManager } });

            _mockUserService.Setup(s => s.GetUsersAssociatedByHnIdAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(new List<User> { new User { FirstName = "Test", LastName = "User", EmailId = "test", Id = "test" } }));

            _mockInvitationService.Setup(s => s.GetByEmailsAndHnIdAsync(It.IsAny<List<string>>(), It.IsAny<string>()))
                .ReturnsAsync(new List<Invitation> { new Invitation { InviterUserId = "test1" } });

            _mockInvitationService.Setup(s => s.GetByInvitedEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(new Invitation { InviterUserId = "test1" });

            _mockNotificationHistoryService.Setup(s => s.CreateAsync(It.IsAny<NotificationHistory>()))
                .Returns(Task.CompletedTask);
            // Act
            var result = await _controller.SoaAssignAssessor(request);

            // Assert
            _mockHeatNetworkService.Verify(s => s.UpdateAsync(It.IsAny<string>(), It.IsAny<HeatNetwork>()), Times.Once);
        }

        [Fact]
        public async Task SoaAssignAssessor_UpdateAssessor_NoNetworkFound()
        {
            // Arrange
            var request = new ElementSoaAssignAssessorRequest
            {
                HnId = "HN0000001",
                AssessorAssessmentForElements = new List<AssessorAssessmentForElement>
                {
                    new AssessorAssessmentForElement
                    {
                        AssessorAssessments = new List<AssessorAssessment>
                        {
                            new AssessorAssessment
                            {
                                Assessment = "A0001",
                                AssessorEmail = "test",
                                AssessorFirstName = "test",
                                AssessorLastName = "test"
                            }
                        },
                    }
                },
                UpdatedBy = "testuser"
            };

            _mockHeatNetworkService.Setup(s => s.GetByHnIdAsync(It.IsAny<string>()))
                .ReturnsAsync((HeatNetwork)null!);


            // Act
            var result = await _controller.SoaAssignAssessor(request);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"No heat network found for HnId")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
                Times.Once);
        }

        [Fact]
        public async Task SoaAssignAssessor_UpdateAssessor_Exception()
        {
            // Arrange
            var request = new ElementSoaAssignAssessorRequest
            {
                HnId = "HN0000001",
                AssessorAssessmentForElements = new List<AssessorAssessmentForElement>
                {
                    new AssessorAssessmentForElement
                    {
                        AssessorAssessments = new List<AssessorAssessment>
                        {
                            new AssessorAssessment
                            {
                                Assessment = "A0001",
                                AssessorEmail = "test",
                                AssessorFirstName = "test",
                                AssessorLastName = "test"
                            }
                        },
                    }
                },
                UpdatedBy = "testuser"
            };

            _mockHeatNetworkService.Setup(s => s.GetByHnIdAsync(It.IsAny<string>()))
                .Throws(new Exception());


            // Act
            var result = await _controller.SoaAssignAssessor(request);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to save Assessor Assigned for HN ID")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task SoaAssignAssessor_UpdateAssessor_ModelStateError()
        {
            // Arrange
            var request = new ElementSoaAssignAssessorRequest
            {
                HnId = "HN0000001",
                AssessorAssessmentForElements = new List<AssessorAssessmentForElement>
                {
                    new AssessorAssessmentForElement
                    {
                        AssessorAssessments = new List<AssessorAssessment>
                        {
                            new AssessorAssessment
                            {
                                Assessment = "A0001",
                                AssessorEmail = "test",
                                AssessorFirstName = "test",
                                AssessorLastName = "test"
                            }
                        },
                    }
                },
                UpdatedBy = "testuser"
            };


            // Simulate a model state error
            _controller.ModelState.AddModelError("AssessorAssessmentForElements", "The AssessorAssessmentForElements field is required.");

            // Act
            var result = await _controller.SoaAssignAssessor(request);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Invalid SaveDocument request")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task UpdateSoaStatus_Success()
        {
            var request = new UpdateSoaStatusRequest
            {
                HnId = "HN0000001",
                HnName = "Test Heat Network",
                UpdatedBy = "testuser",
                Status = SoaStatus.InProgress
            };

            _mockSoaService
                .Setup(s => s.UpdateStatusAsync(
                    It.IsAny<string>(),
                    It.IsAny<SoaStatus>(),
                    It.IsAny<string>()))
                .Returns(Task.FromResult(new Soa() { CreatedBy = "test" }));

            _mockUserService.Setup(s => s.GetUserWithDetailsAsync(It.IsAny<string>()))
                .ReturnsAsync(new UserDetailsResult { FirstName = "Test", LastName = "User", Roles = new List<UserRole> { UserRole.ResponsibleParty } });

            _mockUserService.Setup(s => s.GetAssessorsByHnIdAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(new List<User> { new User { FirstName = "Test", LastName = "User", EmailId = "test", Id = "test" } }));

            _mockEmailService.Setup(s => s.TrySendAssessorEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdateSoaStatus(request);

            // Assert
            _mockSoaService.Verify(s => s.UpdateStatusAsync(
                It.IsAny<string>(),
                It.IsAny<SoaStatus>(),
                It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task UpdateSoaStatus_NotFound()
        {
            var request = new UpdateSoaStatusRequest
            {
                HnId = "HN0000001",
                HnName = "Test Heat Network",
                UpdatedBy = "testuser",
                Status = SoaStatus.InProgress
            };

            _mockSoaService
                .Setup(s => s.UpdateStatusAsync(
                    It.IsAny<string>(),
                    It.IsAny<SoaStatus>(),
                    It.IsAny<string>()))
                .Returns(Task.FromResult(new Soa() { CreatedBy = "test" }));

            _mockUserService.Setup(s => s.GetUserWithDetailsAsync(It.IsAny<string>()))
                .ReturnsAsync(new UserDetailsResult { FirstName = "Test", LastName = "User", Roles = new List<UserRole> { UserRole.ResponsibleParty } });

            _mockUserService.Setup(s => s.GetAssessorsByHnIdAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(new List<User>()));

            // Act
            var result = await _controller.UpdateSoaStatus(request);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("No assessor found for HN ID")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task UpdateSoaStatus_NoSoaFound()
        {
            var request = new UpdateSoaStatusRequest
            {
                HnId = "HN0000001",
                HnName = "Test Heat Network",
                UpdatedBy = "testuser",
                Status = SoaStatus.InProgress
            };

            _mockSoaService
                .Setup(s => s.UpdateStatusAsync(
                    It.IsAny<string>(),
                    It.IsAny<SoaStatus>(),
                    It.IsAny<string>()))
                .Returns(Task.FromResult((Soa)null));


            // Act
            var result = await _controller.UpdateSoaStatus(request);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("No SOA found to update for HN ID")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [Theory]
        [InlineData("", "Test Heat Network", "testuser", SoaStatus.InProgress)]
        [InlineData("HN0000002", "", "testuser2", SoaStatus.InProgress)]
        [InlineData("HN0000002", "Test Heat Network", "", SoaStatus.InProgress)]
        [InlineData("HN0000002", "Test Heat Network", "testuser2", null)]
        public async Task UpdateSoaStatus_BadRequest(string hnId, string hnName, string updatedBy, SoaStatus status)
        {
            var request = new UpdateSoaStatusRequest
            {
                HnId = hnId,
                HnName = hnName,
                UpdatedBy = updatedBy,
                Status = status
            };


            // Act
            var result = await _controller.UpdateSoaStatus(request);

            // Assert bad request
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}