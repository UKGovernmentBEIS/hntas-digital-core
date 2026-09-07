using AutoMapper;
using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Controllers;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Data.Models.Arms.Configuration;
using HNTAS.Core.Api.Data.Models.Arms.Submission;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models;
using HNTAS.Core.Api.Models.Arms.Dashboard;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using Moq;

namespace HNTAS.Digital.Core.Tests.Controllers
{
    public class ArmsDashboardControllerTests
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IHeatNetworkService> _mockHeatNetworkService;
        private readonly Mock<IArmsKpiService> _mockArmsKpiService;
        private readonly Mock<IKpiSubmissionAuditService> _mockAuditService;
        private readonly Mock<ILogger<ArmsDashboardController>> _mockLogger;
        private readonly Mock<ISuperUserService> _mockSuperUserService;
        private readonly Mock<IOptions<ArmsSettings>> _mockArmsSettings;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IUnitService> _mockUnitService;
        private readonly ArmsDashboardController _sut;

        public ArmsDashboardControllerTests()
        {
            _mockUserService = new Mock<IUserService>();
            _mockHeatNetworkService = new Mock<IHeatNetworkService>();
            _mockArmsKpiService = new Mock<IArmsKpiService>();
            _mockAuditService = new Mock<IKpiSubmissionAuditService>();
            _mockLogger = new Mock<ILogger<ArmsDashboardController>>();
            _mockSuperUserService = new Mock<ISuperUserService>();
            _mockMapper = new Mock<IMapper>();
            _mockArmsSettings = new Mock<IOptions<ArmsSettings>>();
            _mockUnitService = new Mock<IUnitService>();


            _mockArmsSettings.Setup(x => x.Value).Returns(new ArmsSettings
            {
                EnableExtendedValidation = true,
                AllowSuperUserAccess = true
            });

            _sut = new ArmsDashboardController(
                _mockUserService.Object,
                _mockHeatNetworkService.Object,
                _mockArmsKpiService.Object,
                _mockMapper.Object,
                _mockAuditService.Object,
                _mockLogger.Object,
                _mockSuperUserService.Object,
                _mockArmsSettings.Object,
                _mockUnitService.Object);

            _mockUnitService.Setup(x => x.GetUnit(It.IsAny<string>())).Returns((string kpiId) =>
            {
                var units = new Dictionary<string, string>
                {
                    { "kpi-1", "°C" },
                    { "kpi-2", "m³/h" }
                };
                return units.TryGetValue(kpiId, out var unit) ? unit : null;
            });
        }

        [Fact]
        public async Task GetKpiNetworksByRpUser_RpUser_ReturnsOkResult()
        {
            // Arrange
            var userId = "testUserId";
            var kpiNetworks = new List<string> { "Network1", "Network2" };
            _mockUserService.Setup(x => x.GetUserWithDetailsAsync(It.IsAny<string>())).ReturnsAsync(new UserDetailsResult { Roles = new List<UserRole> { UserRole.ResponsibleParty }, EmailId = "trest", HnRoleMappings = new List<HnRoleMappingsUserResult> { new HnRoleMappingsUserResult { HeatNetwork = new HeatNetworkUserResponse { HnId = "HN1000002" }, Role = "ResponsibleParty" } } });

            _mockSuperUserService.Setup(x => x.IsSuperUserAsync(It.IsAny<string>())).ReturnsAsync(false);

            _mockArmsKpiService.Setup(x => x.GetSubmissionsAsync(It.IsAny<List<string>>(), It.IsAny<string>()))
                .ReturnsAsync(new List<KpiSubmission>() { new KpiSubmission { Id = "test", UpdatedAt = new DateTime(), MetaData = new KpiMetadata { NetworkId = "trest", PeriodStart = "20/12/2025" } } });
            // Act
            var result = await _sut.GetKpiNetworksByRpUser(userId, 2, 3);
            // Assert
            Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetKpiNetworksBy_BadRequest()
        {
            // Arrange
            var userId = "testUserId";
            var kpiNetworks = new List<string> { "Network1", "Network2" };
            _mockUserService.Setup(x => x.GetUserWithDetailsAsync(It.IsAny<string>())).ReturnsAsync(new UserDetailsResult { Roles = new List<UserRole> { UserRole.NetworkManager }, EmailId = "trest", HnRoleMappings = new List<HnRoleMappingsUserResult> { new HnRoleMappingsUserResult { HeatNetwork = new HeatNetworkUserResponse { HnId = "HN1000002" }, Role = "ResponsibleParty" } } });

            // Act
            var result = await _sut.GetKpiNetworksByRpUser(userId, 2, 3);
            // Assert
            Assert.IsType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetKpiNetworksBy_UserNotFound()
        {
            // Arrange
            var userId = "testUserId";
            var kpiNetworks = new List<string> { "Network1", "Network2" };
            _mockUserService.Setup(x => x.GetUserWithDetailsAsync(It.IsAny<string>())).ReturnsAsync((UserDetailsResult)null!);

            // Act
            var result = await _sut.GetKpiNetworksByRpUser(userId, 2, 3);
            // Assert
            Assert.IsType<Microsoft.AspNetCore.Mvc.NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetKpiNetworksByRpUser_RpUser_NoAuthorizedNetwork_ReturnsOkResult()
        {
            // Arrange
            var userId = "testUserId";
            var kpiNetworks = new List<string> { "Network1", "Network2" };
            _mockUserService.Setup(x => x.GetUserWithDetailsAsync(It.IsAny<string>())).ReturnsAsync(new UserDetailsResult { Roles = new List<UserRole> { UserRole.ResponsibleParty }, EmailId = "trest" });

            _mockSuperUserService.Setup(x => x.IsSuperUserAsync(It.IsAny<string>())).ReturnsAsync(false);

            _mockArmsKpiService.Setup(x => x.GetSubmissionsAsync(It.IsAny<List<string>>(), It.IsAny<string>()))
                .ReturnsAsync(new List<KpiSubmission>() { new KpiSubmission { Id = "test", UpdatedAt = new DateTime(), MetaData = new KpiMetadata { NetworkId = "trest", PeriodStart = "20/12/2025" } } });
            // Act
            var result = await _sut.GetKpiNetworksByRpUser(userId, 2, 3);
            // Assert
            Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetKpiNetworksByRpUser_SuperUser_ReturnsOkResult()
        {
            // Arrange
            var userId = "testUserId";
            var kpiNetworks = new List<string> { "Network1", "Network2" };
            _mockUserService.Setup(x => x.GetUserWithDetailsAsync(It.IsAny<string>())).ReturnsAsync(new UserDetailsResult { Roles = new List<UserRole> { UserRole.NetworkManager }, EmailId = "trest", HnRoleMappings = new List<HnRoleMappingsUserResult> { new HnRoleMappingsUserResult { HeatNetwork = new HeatNetworkUserResponse { HnId = "HN1000002" }, Role = "ResponsibleParty" } } });

            _mockSuperUserService.Setup(x => x.IsSuperUserAsync(It.IsAny<string>())).ReturnsAsync(true);

            _mockArmsKpiService.Setup(x => x.GetSubmissionsAsync(It.IsAny<List<string>>(), It.IsAny<string>()))
                .ReturnsAsync(new List<KpiSubmission>() { new KpiSubmission { Id = "test", UpdatedAt = new DateTime(), MetaData = new KpiMetadata { NetworkId = "trest", PeriodStart = "20/12/2025" } } });

            _mockHeatNetworkService.Setup(x => x.GetAsync()).ReturnsAsync(new List<HeatNetwork> { new HeatNetwork { HnId = "HN1000002", Name = "Test Network" } });
            // Act
            var result = await _sut.GetKpiNetworksByRpUser(userId, 2, 3);
            // Assert
            Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetKpiNetworkDetailsByRpUser_ReturnsOkResult()
        {
            // Arrange
            var submissionId = "testSubmissionId";
            var networkId = "HN1000002";
            var filterType = "testFilterType";

            _mockArmsKpiService.Setup(x => x.GetSubmissionByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new KpiSubmission
                {
                    Id = "test",
                    UpdatedAt = new DateTime(),
                    MetaData = new KpiMetadata { NetworkId = networkId, PeriodStart = "20-12-2025" },
                    Elements = new List<NetworkElement> { new NetworkElement {ElementId = "test", Type = HeatNetworkElementType.EnergyCentre, Kpis = new Dictionary<string, KpiValue>()
                            {
                                { "kpi-1", new KpiValue { Value = 10, AssessmentStatus = KPIAssessmentStatus.OutsideLimit } }
                            } } },
                    ConsumerConnectionAggregatedKpis = new Dictionary<string, KpiValueAggregated> { { "someKey", new KpiValueAggregated { AssessmentStatus = new KPIAssessmentStatus { }, Value = 20.2 } } },
                    CarbonCalculatorInputs = new Dictionary<string, Dictionary<string, CCKpiValue>> { { "someKey", new Dictionary<string, CCKpiValue> { { "someAddKey", new CCKpiValue { IsImputed = true, ImputationDetails = "test" } } } } }
                });

            _mockHeatNetworkService.Setup(x => x.GetByHnIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new HeatNetwork { HnId = networkId, Name = "Test Network" });

            _mockArmsKpiService.Setup(x => x.GetConfigurationAsync(It.IsAny<string>()))
                .ReturnsAsync(new KpiConfiguration { CarbonCalculator = new CarbonCalculatorConfig { Defaults = new Dictionary<string, BsonValue> { { "key", BsonValue.Create("someValue") } } } });
            // Act
            var result = await _sut.GetKpiNetworkDetailsByRpUser(submissionId, networkId, filterType);
            // Assert
            Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetKpiNetworkDetailsByRpUser_Filter_ReturnsOkResult()
        {
            // Arrange
            var submissionId = "testSubmissionId";
            var networkId = "OutsideLimit";
            var filterType = "testFilterType,EnergyCentre";

            _mockArmsKpiService.Setup(x => x.GetSubmissionByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new KpiSubmission
                {
                    Id = "test",
                    UpdatedAt = new DateTime(),
                    MetaData = new KpiMetadata { NetworkId = networkId, PeriodStart = "20-12-2025" },
                    Elements = new List<NetworkElement> { new NetworkElement {ElementId = "test", Type = HeatNetworkElementType.EnergyCentre, Kpis = new Dictionary<string, KpiValue>()
                            {
                                { "kpi-1", new KpiValue { Value = 10, AssessmentStatus = KPIAssessmentStatus.OutsideLimit } }
                            } } },
                    ConsumerConnectionAggregatedKpis = new Dictionary<string, KpiValueAggregated> { { "someKey", new KpiValueAggregated { AssessmentStatus = new KPIAssessmentStatus { }, Value = 20.2 } } },
                    CarbonCalculatorInputs = new Dictionary<string, Dictionary<string, CCKpiValue>> { { "someKey", new Dictionary<string, CCKpiValue> { { "someAddKey", new CCKpiValue { IsImputed = true, ImputationDetails = "test" } } } } }
                });

            _mockHeatNetworkService.Setup(x => x.GetByHnIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new HeatNetwork { HnId = networkId, Name = "Test Network" });

            _mockArmsKpiService.Setup(x => x.GetConfigurationAsync(It.IsAny<string>()))
                .ReturnsAsync(new KpiConfiguration { CarbonCalculator = new CarbonCalculatorConfig { Defaults = new Dictionary<string, BsonValue> { { "key", BsonValue.Create("someValue") } } } });
            // Act
            var result = await _sut.GetKpiNetworkDetailsByRpUser(submissionId, networkId, filterType);
            // Assert
            Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetKpiNetworkDetailsByRpUser_SubmissionNotFound()
        {
            // Arrange
            var submissionId = "testSubmissionId";
            var networkId = "HN1000002";
            var filterType = "testFilterType";

            _mockArmsKpiService.Setup(x => x.GetSubmissionByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((KpiSubmission)null!);

            // Act
            var result = await _sut.GetKpiNetworkDetailsByRpUser(submissionId, networkId, filterType);
            // Assert
            Assert.IsType<Microsoft.AspNetCore.Mvc.NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetKpiNetworkDetailsByRpUser_NetworkNotFound()
        {
            // Arrange
            var submissionId = "testSubmissionId";
            var networkId = "HN1000002";
            var filterType = "testFilterType";

            _mockArmsKpiService.Setup(x => x.GetSubmissionByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new KpiSubmission
                {
                    Id = "test",
                    UpdatedAt = new DateTime(),
                    MetaData = new KpiMetadata { NetworkId = networkId, PeriodStart = "20-12-2025" },
                    Elements = new List<NetworkElement> { new NetworkElement {ElementId = "test", Type = HeatNetworkElementType.EnergyCentre, Kpis = new Dictionary<string, KpiValue>()
                            {
                                { "kpi-1", new KpiValue { Value = 10, AssessmentStatus = KPIAssessmentStatus.OutsideLimit } }
                            } } },
                    ConsumerConnectionAggregatedKpis = new Dictionary<string, KpiValueAggregated> { { "someKey", new KpiValueAggregated { AssessmentStatus = new KPIAssessmentStatus { }, Value = 20.2 } } },
                    CarbonCalculatorInputs = new Dictionary<string, Dictionary<string, CCKpiValue>> { { "someKey", new Dictionary<string, CCKpiValue> { { "someAddKey", new CCKpiValue { IsImputed = true, ImputationDetails = "test" } } } } }
                });

            _mockHeatNetworkService.Setup(x => x.GetByHnIdAsync(It.IsAny<string>()))
                .ReturnsAsync((HeatNetwork)null!);

            // Act
            var result = await _sut.GetKpiNetworkDetailsByRpUser(submissionId, networkId, filterType);
            // Assert
            Assert.IsType<Microsoft.AspNetCore.Mvc.NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetSubmissionHistory_ReturnOkObject()
        {
            _mockAuditService.Setup(x => x.GetHistoryBySubmissionIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<KpiHistoryResponse> { new KpiHistoryResponse { } });

            // Act
            var result = await _sut.GetSubmissionHistory("submissionId");
            // Assert
            Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);
        }

        [Fact]
        public async Task GetSubmissionHistory_ThrowException()
        {
            _mockAuditService.Setup(x => x.GetHistoryBySubmissionIdAsync(It.IsAny<string>()))
                .Throws(new Exception());

            // Act
            var result = await _sut.GetSubmissionHistory("submissionId");
            // Assert
            var res = Assert.IsType<Microsoft.AspNetCore.Mvc.ObjectResult>(result);
            Assert.Equal(500, res.StatusCode);
        }

        [Fact]
        public async Task GetSubmissionHistory_BadRequest()
        {
            // Act
            var result = await _sut.GetSubmissionHistory("");
            // Assert
            Assert.IsType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>(result);
        }
    }
}
