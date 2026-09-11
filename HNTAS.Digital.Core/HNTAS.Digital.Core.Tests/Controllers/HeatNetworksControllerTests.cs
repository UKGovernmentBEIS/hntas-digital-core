using AutoMapper;
using HNTAS.Core.Api.Controllers;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Data.Models.External;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models;
using HNTAS.Core.Api.Models.HeatNetwork;
using HNTAS.Core.Api.Models.Soa;
using HNTAS.Core.Api.Models.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace HNTAS.Digital.Core.Tests.Controllers
{
    public class HeatNetworksControllerTests
    {
        private readonly Mock<IHeatNetworkService> _mockHnService;
        private readonly Mock<ICounterService> _mockCounterService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<HeatNetworksController>> _mockLogger;
        private readonly Mock<IAuditService> _mockAuditService;
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<INotificationHistoryService> _mockNotificationHistoryService;
        private readonly Mock<IInvitationService> _mockInvitationService;
        private readonly Mock<IOrganisationService> _mockOrganisationService;
        private readonly HeatNetworksController _controller;

        public HeatNetworksControllerTests()
        {
            _mockHnService = new Mock<IHeatNetworkService>();
            _mockCounterService = new Mock<ICounterService>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<HeatNetworksController>>();
            _mockAuditService = new Mock<IAuditService>();
            _mockUserService = new Mock<IUserService>();
            _mockEmailService = new Mock<IEmailService>();
            _mockInvitationService = new Mock<IInvitationService>();
            _mockOrganisationService = new Mock<IOrganisationService>();
            _mockNotificationHistoryService = new Mock<INotificationHistoryService>();

            // Assuming these dependencies are injected via the constructor in your partial class
            _controller = new HeatNetworksController(_mockHnService.Object, _mockLogger.Object, _mockCounterService.Object, _mockMapper.Object, _mockUserService.Object, _mockEmailService.Object, _mockInvitationService.Object, _mockAuditService.Object, _mockOrganisationService.Object, _mockNotificationHistoryService.Object);
        }

        private HeatNetwork SampleHeatNetwork(string id = "1", string? hnId = null)
        {
            return new HeatNetwork
            {
                Id = id,
                HnId = hnId,
                //Location = "LocationA",
                Name = "Network A",
                Pathway = "Pathway X",
                CreatedBy = "tester",
                CreatedAt = DateTime.UtcNow
            };
        }

        private HeatNetworkResponse SampleHeatNetworkResponse(string id = "1", string hnId = "HN0000001")
        {
            return new HeatNetworkResponse
            {
                Id = id,
                HnId = hnId,
                //Location = "LocationA",
                Name = "Network A",
                Pathway = "Pathway X",
                Soa = null
            };
        }

        // 1) GetHeatNetworks - Positive
        [Fact]
        public async Task GetHeatNetworks_ReturnsOk_WithList()
        {
            // Arrange
            var domainList = new List<HeatNetwork> { SampleHeatNetwork("1", "HN0000001") };
            var responseList = new List<HeatNetworkResponse> { SampleHeatNetworkResponse("1", "HN0000001") };

            _mockHnService.Setup(s => s.GetAsync()).ReturnsAsync(domainList);
            _mockMapper.Setup(m => m.Map<List<HeatNetworkResponse>>(It.IsAny<List<HeatNetwork>>()))
                       .Returns(responseList);

            // Act
            var result = await _controller.GetHeatNetworks();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<List<HeatNetworkResponse>>(ok.Value);
            Assert.Single(returned);
            Assert.Equal("HN0000001", returned[0].HnId);
        }

        // 1) GetHeatNetworks - Negative (exception -> 500)
        [Fact]
        public async Task GetHeatNetworks_OnException_Returns500()
        {
            // Arrange
            _mockHnService.Setup(s => s.GetAsync()).ThrowsAsync(new Exception("DB failure"));

            // Act
            var result = await _controller.GetHeatNetworks();

            // Assert
            var objResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objResult.StatusCode);
        }

        // 2) GetHeatNetworksByHnIds - Positive
        [Fact]
        public async Task GetHeatNetworksByHnIds_WithValidIds_ReturnsOk()
        {
            // Arrange
            var hnIdsString = "HN0000001,HN0000002";
            var ids = hnIdsString.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            var domainList = new List<HeatNetwork> { SampleHeatNetwork("1", "HN0000001") };
            var responseList = new List<HeatNetworkResponse> { SampleHeatNetworkResponse("1", "HN0000001") };

            _mockHnService.Setup(s => s.GetByHnIdsAsync(It.Is<List<string>>(l => l.SequenceEqual(ids))))
                          .ReturnsAsync(domainList);

            _mockMapper.Setup(m => m.Map<List<HeatNetworkResponse>>(It.IsAny<List<HeatNetwork>>()))
                       .Returns(responseList);

            // Act
            var result = await _controller.GetHeatNetworksByHnIds(hnIdsString);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<List<HeatNetworkResponse>>(ok.Value);
            Assert.Single(returned);
            Assert.Equal("HN0000001", returned[0].HnId);
        }

        // 2) GetHeatNetworksByHnIds - Negative (bad request when no ids)
        [Fact]
        public async Task GetHeatNetworksByHnIds_WithNoIds_ReturnsBadRequest()
        {
            // Arrange
            string hnIdsString = null; // controller will interpret as no ids

            // Act
            var result = await _controller.GetHeatNetworksByHnIds(hnIdsString);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequest.Value);
        }

        [Fact]
        public async Task GetHeatNetworksByHnIds_NetworkNotFound()
        {
            // Arrange
            var hnIdsString = "HN0000001,HN0000002";
            var ids = hnIdsString.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            var responseList = new List<HeatNetworkResponse> { SampleHeatNetworkResponse("1", "HN0000001") };

            _mockHnService.Setup(s => s.GetByHnIdsAsync(It.Is<List<string>>(l => l.SequenceEqual(ids))))
                          .ReturnsAsync((List<HeatNetwork>)null!);


            // Act
            var result = await _controller.GetHeatNetworksByHnIds(hnIdsString);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetHeatNetworksByHnIds_ThrowException()
        {
            // Arrange
            var hnIdsString = "HN0000001,HN0000002";
            var ids = hnIdsString.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            var responseList = new List<HeatNetworkResponse> { SampleHeatNetworkResponse("1", "HN0000001") };

            _mockHnService.Setup(s => s.GetByHnIdsAsync(It.Is<List<string>>(l => l.SequenceEqual(ids))))
                          .Throws(new Exception());


            // Act
            var result = await _controller.GetHeatNetworksByHnIds(hnIdsString);

            // Assert
            var res = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, res.StatusCode);
        }

        // 3) GetHeatNetworkByHnId - Positive
        [Fact]
        public async Task GetHeatNetworkByHnId_WithValidId_ReturnsOk()
        {
            // Arrange
            var hnId = "HN0000001";
            var domain = SampleHeatNetwork("1", hnId);
            var response = SampleHeatNetworkResponse("1", hnId);

            _mockHnService.Setup(s => s.GetByHnIdAsync(hnId)).ReturnsAsync(domain);
            _mockMapper.Setup(m => m.Map<HeatNetworkResponse>(It.IsAny<HeatNetwork>())).Returns(response);

            // Act
            var result = await _controller.GetHeatNetworkByHnId(hnId);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<HeatNetworkResponse>(ok.Value);
            Assert.Equal(hnId, returned.HnId);
        }

        // 3) GetHeatNetworkByHnId - Negative (invalid input -> BadRequest)
        [Fact]
        public async Task GetHeatNetworkByHnId_WithEmptyId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetHeatNetworkByHnId(string.Empty);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequest.Value);
        }

        [Fact]
        public async Task GetHeatNetworkByHnId_NetworkNotFound()
        {
            // Arrange
            var hnId = "HN0000001";

            _mockHnService.Setup(s => s.GetByHnIdAsync(hnId)).ReturnsAsync((HeatNetwork)null!);

            // Act
            var result = await _controller.GetHeatNetworkByHnId(hnId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);

        }

        [Fact]
        public async Task GetHeatNetworkByHnId_ThrowException()
        {
            // Arrange
            var hnId = "HN0000001";
            var domain = SampleHeatNetwork("1", hnId);


            _mockHnService.Setup(s => s.GetByHnIdAsync(hnId)).Throws(new Exception());


            // Act
            var result = await _controller.GetHeatNetworkByHnId(hnId);

            // Assert
            var res = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, res.StatusCode);

        }

        // 4) AddHeatNetwork - Positive (generates HnId and creates)
        [Fact]
        public async Task AddHeatNetwork_WithoutHnId_Rp_GeneratesHnIdAndReturnsCreated()
        {
            // Arrange
            var input = SampleHeatNetwork("1", hnId: null); // no HnId set
            _mockCounterService.Setup(c => c.GetNextSequenceValue(It.IsAny<string>())).ReturnsAsync(20);
            _mockHnService.Setup(s => s.CreateAsync(It.IsAny<HeatNetwork>(), It.IsAny<bool>())).Returns(Task.CompletedTask);
            _mockUserService.Setup(s => s.GetUserWithDetailsAsync(It.IsAny<string>())).ReturnsAsync(new UserDetailsResult
            {
                Id = "tester",
                EmailId = "user@example.com",
                FirstName = "Test",
                LastName = "User",
                Roles = new List<UserRole> { UserRole.ResponsibleParty },
            });

            _mockUserService.Setup(s => s.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(new User
            {
                Id = "tester",
                EmailId = "test",
                HnRoleMappings = new List<HnRoleMapping>
                {
                    new HnRoleMapping { HnId = "HN0000001", Role = ContributorRole.ResponsibleParty }
                }
            });

            //_mockUserService.Setup(s => s.UpdateAsync(It.IsAny<string>(), It.IsAny<User>())).Returns(Task.CompletedTask);
            _mockInvitationService.Setup(i => i.GetNetworkManagersByInviterUserId(It.IsAny<string>())).ReturnsAsync(new List<Invitation>() { new Invitation { Status = InvitationStatus.Accepted, InvitedEmail = "test" } });
            _mockUserService.Setup(u => u.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(new User { Id = "user1", EmailId = "test" });
            _mockUserService.Setup(u => u.UpdateAsync(It.IsAny<string>(), It.IsAny<User>())).Returns(Task.CompletedTask);
            _mockEmailService.Setup(e => e.TrySendHeatNetworkRegistrationEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            // Act
            var result = await _controller.AddHeatNetwork(input);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returned = Assert.IsAssignableFrom<HeatNetwork>(created.Value);
            Assert.NotNull(returned.HnId);
            Assert.StartsWith("HN", returned.HnId);
        }

        [Fact]
        public async Task AddHeatNetwork_WithoutHnId_NetworkManager_GeneratesHnIdAndReturnsCreated()
        {
            // Arrange
            var input = SampleHeatNetwork("1", hnId: null); // no HnId set
            _mockCounterService.Setup(c => c.GetNextSequenceValue(It.IsAny<string>())).ReturnsAsync(20);
            _mockHnService.Setup(s => s.CreateAsync(It.IsAny<HeatNetwork>(), It.IsAny<bool>())).Returns(Task.CompletedTask);
            _mockUserService.Setup(s => s.GetUserWithDetailsAsync(It.IsAny<string>())).ReturnsAsync(new UserDetailsResult
            {
                Id = "tester",
                EmailId = "user@example.com",
                FirstName = "Test",
                LastName = "User",
                Roles = new List<UserRole> { UserRole.NetworkManager },
            });

            _mockUserService.Setup(s => s.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(new User
            {
                Id = "tester",
                EmailId = "test",
                HnRoleMappings = new List<HnRoleMapping>
                {
                    new HnRoleMapping { HnId = "HN0000001", Role = ContributorRole.NetworkManager }
                }
            });

            _mockInvitationService.Setup(i => i.GetByInvitedEmailAsync(It.IsAny<string>())).ReturnsAsync(new Invitation { Status = InvitationStatus.Accepted, InvitedEmail = "test", InviterUserId = "test" });
            _mockUserService.Setup(s => s.UpdateAsync(It.IsAny<string>(), It.IsAny<User>())).Returns(Task.CompletedTask);
            _mockInvitationService.Setup(i => i.GetNetworkManagersByInviterUserId(It.IsAny<string>())).ReturnsAsync(new List<Invitation>() { new Invitation { Status = InvitationStatus.Accepted, InvitedEmail = "test" } });
            _mockOrganisationService.Setup(o => o.GetByOrgIdAsync(It.IsAny<string>())).ReturnsAsync(new Organisation { Id = "org1", Name = "Org 1", RpUserId = "rpid" });
            _mockEmailService.Setup(e => e.TrySendHeatNetworkRegistrationEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            // Act
            var result = await _controller.AddHeatNetwork(input);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returned = Assert.IsAssignableFrom<HeatNetwork>(created.Value);
            Assert.NotNull(returned.HnId);
            Assert.StartsWith("HN", returned.HnId);
        }

        // 4) AddHeatNetwork - Negative (service throws -> 500)
        [Fact]
        public async Task AddHeatNetwork_OnCreateException_Returns500()
        {
            // Arrange
            var input = SampleHeatNetwork("1", "HN0000001");
            _mockHnService.Setup(s => s.CreateAsync(It.IsAny<HeatNetwork>(), It.IsAny<bool>())).ThrowsAsync(new Exception("write failed"));

            // Act
            var result = await _controller.AddHeatNetwork(input);

            // Assert
            var objResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objResult.StatusCode);
            Assert.IsType<ProblemDetails>(objResult.Value);
        }

        #region GetExternalHeatNetworks Tests

        [Fact]
        public async Task GetExternalHeatNetworks_ReturnsOk_WithData()
        {
            // Arrange
            var mockResponse = new List<HeatNetworkExternalResponse> { new HeatNetworkExternalResponse() };

            // Updated to call GetDetailsAsync as per new controller logic
            _mockHnService.Setup(s => s.GetDetailsAsync()).ReturnsAsync(mockResponse);

            // Act
            var result = await _controller.GetExternalHeatNetworks();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(mockResponse, okResult.Value);
        }

        [Fact]
        public async Task GetExternalHeatNetworks_Returns500_OnException()
        {
            // Arrange
            _mockHnService.Setup(s => s.GetDetailsAsync()).ThrowsAsync(new System.Exception());

            // Act
            var result = await _controller.GetExternalHeatNetworks();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("Internal Server Error", statusCodeResult.Value);
        }

        #endregion

        #region GetExternalHeatNetworkById Tests

        [Fact]
        public async Task GetExternalHeatNetworkById_ReturnsNotFound_WhenNull()
        {
            // Arrange
            _mockHnService.Setup(s => s.GetDetailsByHnIdAsync("HN001")).ReturnsAsync((HeatNetworkExternalResponse)null);

            // Act
            var result = await _controller.GetExternalHeatNetworkById("HN001");

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetExternalHeatNetworkById_ReturnsOk_WhenFound()
        {
            // Arrange
            var response = new HeatNetworkExternalResponse { HnId = "HN001" };
            _mockHnService.Setup(s => s.GetDetailsByHnIdAsync("HN001")).ReturnsAsync(response);

            // Act
            var result = await _controller.GetExternalHeatNetworkById("HN001");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(response, okResult.Value);
        }

        #endregion

        #region GetExternalHeatNetworksByDate Tests

        [Fact]
        public async Task GetExternalHeatNetworksByDate_ReturnsBadRequest_WhenDatesInvalid()
        {
            // Arrange
            var from = DateTime.Now.AddDays(1);
            var to = DateTime.Now;

            // Act
            var result = await _controller.GetExternalHeatNetworksByDate(from, to);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            // Note: Updated error message to match snake_case used in controller
            Assert.Equal("The 'from_date' cannot be after the 'to_date'.", badRequestResult.Value);
        }

        [Fact]
        public async Task GetExternalHeatNetworksByDate_ReturnsOk_WithValidRange()
        {
            // Arrange
            var from = DateTime.Now.AddDays(-1);
            var to = DateTime.Now;
            var mockList = new List<HeatNetworkExternalResponse> { new HeatNetworkExternalResponse() };

            _mockHnService.Setup(s => s.GetDetailsByDateRangeAsync(from, to))
                         .ReturnsAsync(mockList);

            // Act
            var result = await _controller.GetExternalHeatNetworksByDate(from, to);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(mockList, okResult.Value);
        }

        #endregion

        [Fact]
        public async Task GetExistingNetworksByUserId_ReturnsSuccess()
        {

            var req = new ExistingNetworkRequest
            {
                UserId = "user1"
            };
            _mockHnService.Setup(s => s.GetExistingNetworks(It.IsAny<ExistingNetworkRequest>())).ReturnsAsync(new ExistingNetworkResponse
            {
                UserId = "tewt"
            });

            var result = await _controller.GetExistingNetworksByUserId(req);

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetExistingNetworksByUserId_BadRequest()
        {

            var req = new ExistingNetworkRequest
            {
                UserId = ""
            };

            var result = await _controller.GetExistingNetworksByUserId(req);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetExistingNetworksByUserId_ThrowException()
        {

            var req = new ExistingNetworkRequest
            {
                UserId = "user1"
            };
            _mockHnService.Setup(s => s.GetExistingNetworks(It.IsAny<ExistingNetworkRequest>())).Throws(new Exception());

            var result = await _controller.GetExistingNetworksByUserId(req);

            // Assert
            var res = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, res.StatusCode);
        }

        [Fact]
        public async Task GetExistingNetworksByUserId_NetworkNotFound()
        {

            var req = new ExistingNetworkRequest
            {
                UserId = "user1"
            };
            _mockHnService.Setup(s => s.GetExistingNetworks(It.IsAny<ExistingNetworkRequest>())).ReturnsAsync((ExistingNetworkResponse)null!);


            var result = await _controller.GetExistingNetworksByUserId(req);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task UpdateNetworkElements_SingleElement_ReturnsSuccess()
        {
            var request = new NetworkElements
            {
                CreatedAt = DateTime.Now,
                CreatedBy = "tester",
                Elements = new List<Element> { new Element { ElementId = "test", ElementType = ElementTypeInShort.DDN } },
                ElementsGroup = new List<ElementGroup> { new ElementGroup { Count = 1, ElementType = ElementTypeInShort.DDN, ElementDisplayType = HeatNetworkElementType.DistrictDistribution } },
                ElementSoaStatus = NetworkDetailsStatus.ReadyToStart,
                UpdatedAt = DateTime.Now,
                UpdatedBy = "tester",
            };
            var hnId = "HN0000001";
            var domain = SampleHeatNetwork("1", hnId);
            var response = SampleHeatNetworkResponse("1", hnId);

            _mockHnService.Setup(s => s.GetByHnIdAsync(hnId)).ReturnsAsync(domain);
            _mockHnService.Setup(s => s.UpdateAsync(It.IsAny<string>(), It.IsAny<HeatNetwork>())).Returns(Task.CompletedTask);
            _mockAuditService.Setup(a => a.SaveAuditAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HeatNetwork>(), It.IsAny<HeatNetwork>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            var result = await _controller.UpdateNetworkElements(request, hnId);
            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task UpdateNetworkElements_MultipleElements_ReturnsSuccess()
        {
            var request = new NetworkElements
            {
                CreatedAt = DateTime.Now,
                CreatedBy = "tester",
                Elements = new List<Element>
                {
                    new Element { ElementId = "test", ElementType = ElementTypeInShort.DDN },
                    new Element { ElementId = "test", ElementType = ElementTypeInShort.SS },
                    new Element { ElementId = "test", ElementType = ElementTypeInShort.EC },
                    new Element { ElementId = "test", ElementType = ElementTypeInShort.CDN },
                    new Element { ElementId = "test", ElementType = ElementTypeInShort.CC }
                },
                ElementsGroup = new List<ElementGroup>
                {
                    new ElementGroup { Count = 2, ElementType = ElementTypeInShort.DDN, ElementDisplayType = HeatNetworkElementType.DistrictDistribution },
                    new ElementGroup { Count = 2, ElementType = ElementTypeInShort.CC, ElementDisplayType = HeatNetworkElementType.ConsumerConnection },
                    new ElementGroup { Count = 2, ElementType = ElementTypeInShort.CDN, ElementDisplayType = HeatNetworkElementType.CommunalDistribution },
                    new ElementGroup { Count = 2, ElementType = ElementTypeInShort.EC, ElementDisplayType = HeatNetworkElementType.EnergyCentre },
                    new ElementGroup { Count = 2, ElementType = ElementTypeInShort.SS, ElementDisplayType = HeatNetworkElementType.Substation }
                },
                ElementSoaStatus = NetworkDetailsStatus.ReadyToStart,
                UpdatedAt = DateTime.Now,
                UpdatedBy = "tester",
            };
            var hnId = "HN0000001";
            var domain = SampleHeatNetwork("1", hnId);
            var response = SampleHeatNetworkResponse("1", hnId);

            _mockHnService.Setup(s => s.GetByHnIdAsync(hnId)).ReturnsAsync(domain);
            _mockHnService.Setup(s => s.UpdateAsync(It.IsAny<string>(), It.IsAny<HeatNetwork>())).Returns(Task.CompletedTask);
            _mockAuditService.Setup(a => a.SaveAuditAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HeatNetwork>(), It.IsAny<HeatNetwork>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            var result = await _controller.UpdateNetworkElements(request, hnId);
            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task UpdateNetworkElements_ThrowException()
        {
            var request = new NetworkElements
            {
                CreatedAt = DateTime.Now,
                CreatedBy = "tester",
                Elements = new List<Element> { new Element { ElementId = "test", ElementType = ElementTypeInShort.DDN } },
                ElementsGroup = new List<ElementGroup> { new ElementGroup { Count = 1, ElementType = ElementTypeInShort.DDN, ElementDisplayType = HeatNetworkElementType.DistrictDistribution } },
                ElementSoaStatus = NetworkDetailsStatus.ReadyToStart,
                UpdatedAt = DateTime.Now,
                UpdatedBy = "tester",
            };
            var hnId = "HN0000001";

            _mockHnService.Setup(s => s.GetByHnIdAsync(hnId)).Throws(new Exception());
            var result = await _controller.UpdateNetworkElements(request, hnId);
            // Assert
            var res = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, res.StatusCode);
        }

        [Fact]
        public async Task UpdateNetworkElements_NetworkId_BadRequest()
        {
            var request = new NetworkElements
            {
                CreatedAt = DateTime.Now,
                CreatedBy = "tester",
                Elements = new List<Element> { new Element { ElementId = "test", ElementType = ElementTypeInShort.DDN } },
                ElementsGroup = new List<ElementGroup> { new ElementGroup { Count = 1, ElementType = ElementTypeInShort.DDN, ElementDisplayType = HeatNetworkElementType.DistrictDistribution } },
                ElementSoaStatus = NetworkDetailsStatus.ReadyToStart,
                UpdatedAt = DateTime.Now,
                UpdatedBy = "tester",
            };
            var hnId = "";


            var result = await _controller.UpdateNetworkElements(request, hnId);
            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task UpdateNetworkElements_NoNetworkElement_BadRequest()
        {
            NetworkElements request = null;
            var hnId = "HN100001";


            var result = await _controller.UpdateNetworkElements(request, hnId);
            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task UpdateNetworkElements_NetworkNotFound()
        {
            var request = new NetworkElements
            {
                CreatedAt = DateTime.Now,
                CreatedBy = "tester",
                Elements = new List<Element> { new Element { ElementId = "test", ElementType = ElementTypeInShort.DDN } },
                ElementsGroup = new List<ElementGroup> { new ElementGroup { Count = 1, ElementType = ElementTypeInShort.DDN, ElementDisplayType = HeatNetworkElementType.DistrictDistribution } },
                ElementSoaStatus = NetworkDetailsStatus.ReadyToStart,
                UpdatedAt = DateTime.Now,
                UpdatedBy = "tester",
            };
            var hnId = "HN0000001";

            _mockHnService.Setup(s => s.GetByHnIdAsync(hnId)).Returns(Task.FromResult((HeatNetwork)null!));
            var result = await _controller.UpdateNetworkElements(request, hnId);
            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task RegisterOfgemNetwork_ReturnsOk_WithData()
        {
            var request = new HeatNetwork { Id = "1", HnId = "HN000001", CreatedBy = "testuser" };
            _mockHnService.Setup(h => h.UpdateAsync(It.IsAny<string>(), It.IsAny<HeatNetwork>())).Returns(Task.CompletedTask);
            _mockUserService.Setup(u => u.GetUserWithDetailsAsync(It.IsAny<string>())).ReturnsAsync(new UserDetailsResult { Roles = new List<UserRole> { UserRole.ResponsibleParty } });
            _mockInvitationService.Setup(i => i.GetNetworkManagersByInviterUserId(It.IsAny<string>())).ReturnsAsync(new List<Invitation>() { new Invitation { Status = InvitationStatus.Accepted, InvitedEmail = "test" } });
            _mockUserService.Setup(u => u.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(new User { Id = "user1", EmailId = "test" });

            var result = await _controller.RegisterOfgemNetwork(request);
            Assert.NotNull(result);
            var resultValue = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status200OK, resultValue.StatusCode);
        }

        [Fact]
        public async Task RegisterOfgemNetwork_BadRequest()
        {
            var request = new HeatNetwork { CreatedBy = "testuser" };

            var result = await _controller.RegisterOfgemNetwork(request);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task RegisterOfgemNetwork_ThrowException()
        {
            var request = new HeatNetwork { Id = "1", HnId = "HN000001", CreatedBy = "testuser" };
            _mockHnService.Setup(h => h.UpdateAsync(It.IsAny<string>(), It.IsAny<HeatNetwork>())).Throws(new Exception("DB failure"));


            var result = await _controller.RegisterOfgemNetwork(request);
            Assert.NotNull(result);
            var resultValue = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, resultValue.StatusCode);
        }

        #region GetHeatNetworksByUserIdPaginated Tests

        [Fact]
        public async Task GetHeatNetworksByUserIdPaginated_ReturnsOk_WithPagedResult()
        {
            // Arrange
            var userId = "user-123";
            var registrationSource = RegistrationSource.HNTAS;
            var pageNumber = 1;
            var pageSize = 10;
            var sortBy = "Name";
            var sortDirection = "asc";

            var userDetails = new User
            {
                Id = userId,
                HnRoleMappings = new List<HnRoleMapping>
                {
                    new HnRoleMapping { HnId = "HN0000001" }
                }
            };

            var domainList = new List<UserNetworkDetailsResponse>
            {
                new UserNetworkDetailsResponse
                {
                    HnId = "HN0000001",
                    Name = "Network A",
                    OrganisationName = "Organisation A",
                    OrgId = "ORG0000001"
                }
            };

            long totalCount = 1;

            _mockUserService
                .Setup(s => s.GetByIdAsync(userId))
                .ReturnsAsync(userDetails);

            _mockHnService
                .Setup(s => s.GetByHnIdsAndRegistrationSourcePaginatedAsync(
                    It.Is<List<string>>(ids => ids.Contains("HN0000001")),
                    registrationSource,
                    pageNumber,
                    pageSize,
                    sortBy,
                    sortDirection))
                .ReturnsAsync((domainList, totalCount));

            // Act
            var actionResult = await _controller.GetHeatNetworksByUserIdPaginated(
                userId, registrationSource, pageNumber, pageSize, sortBy, sortDirection);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var pagedResult = Assert.IsType<PagedResult<UserNetworkDetailsResponse>>(okResult.Value);

            Assert.Equal(1, pagedResult.TotalCount);
            Assert.Equal(1, pagedResult.TotalPages);
            Assert.Equal(pageNumber, pagedResult.PageNumber);
            Assert.Equal(pageSize, pagedResult.PageSize);
            Assert.Single(pagedResult.Items);
            Assert.Equal("HN0000001", pagedResult.Items[0].HnId);
        }

        [Fact]
        public async Task GetHeatNetworksByUserIdPaginated_ReturnsBadRequest_WhenUserIdIsEmpty()
        {
            // Act
            var result = await _controller.GetHeatNetworksByUserIdPaginated(string.Empty);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Please provide a valid user Id.", badRequest.Value);
        }

        [Fact]
        public async Task GetHeatNetworksByUserIdPaginated_ReturnsNotFound_WhenUserHasNoMappings()
        {
            // Arrange
            var userId = "user-123";
            _mockUserService
                .Setup(s => s.GetByIdAsync(userId))
                .ReturnsAsync((User)null);

            // Act
            var result = await _controller.GetHeatNetworksByUserIdPaginated(userId);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal("User or user role mappings not found.", notFound.Value);
        }

        [Fact]
        public async Task GetHeatNetworksByUserIdPaginated_ReturnsInternalServerError_OnException()
        {
            // Arrange
            var userId = "user-123";
            _mockUserService
                .Setup(s => s.GetByIdAsync(userId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetHeatNetworksByUserIdPaginated(userId);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusResult.StatusCode);
            Assert.Equal("An unexpected error occurred while retrieving the heat networks.", statusResult.Value);
        }

        #endregion
    }
}