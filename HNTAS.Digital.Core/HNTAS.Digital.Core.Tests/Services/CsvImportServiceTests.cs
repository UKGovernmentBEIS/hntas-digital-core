using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace HNTAS.Digital.Core.Tests.Services
{
    public class CsvImportServiceTests
    {
        private readonly Mock<ILogger<CsvImportService>> _mockLogger;
        private readonly Mock<IOrganisationService> _mockOrganisationService;
        private readonly Mock<IHeatNetworkService> _mockHeatNetworkService;
        private readonly Mock<IUserService> _mockUserService;
        private readonly CsvImportService _sut;

        public CsvImportServiceTests()
        {
            _mockLogger = new Mock<ILogger<CsvImportService>>();
            _mockOrganisationService = new Mock<IOrganisationService>();
            _mockHeatNetworkService = new Mock<IHeatNetworkService>();
            _mockUserService = new Mock<IUserService>();
            _sut = new CsvImportService(_mockOrganisationService.Object, _mockUserService.Object, _mockHeatNetworkService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task ImportCsv_CompanyHouseNumber_HeatNetworkExist_ShouldReturnNoHeatNetworkAdded()
        {
            // Arrange
            var csvContent = "EmailId,OneLoginId,OrganisationName,OrgStreetAddress,OrgTown,OrgPostcode,PhoneNumber,CompaniesHouseNo,DateOfRegistration,HnId,HnName,DateOfHnRegistration,RegistrationSource,EcStreetAddress,EcTown,EcPostcode,ECLatitude,ECLongitude\r\nkuldeep@mailinator.com,urn:fdc:gov.uk:2022:VfexA9AeHRYrpqu6DzQLAO1tHJSz4iRO625rE3Phjp8,COMPANY 42188207 LIMITED,Crownway,Cardiff,CF14 3UZ,1212121212,42188207,,HN1000002,importedHN1,14/06/2026,Ofgem,\"Flat 10, Friars mews, Wesleyan court\",Lincoln,LN2 5DB,52.9343097,-1.252217489";
            _mockOrganisationService.Setup(x => x.GetByCompanyHouseNumberAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(new Organisation
                {
                    OrgId = "ORG123",
                    Name = "Test Organisation",
                    CompaniesHouseNumber = "42188207",
                    RpUserId = "user123",
                }));

            _mockHeatNetworkService.Setup(x => x.GetByHnIdAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(new HeatNetwork
                {
                    HnId = "HN1000002",
                    Name = "Test Heat Network",                    
                }));

            // Act
            var result = await _sut.ImportFromCsvAsync(csvContent);

            // Assert
            Assert.Equal(1, result.RowsProcessed);
            Assert.Equal(0, result.HeatNetworksInserted);
        }

        [Fact]
        public async Task ImportCsv_CompanyHouseNumber_NoHeatNetwork_ShouldReturnOneHeatNetworkAdded()
        {
            // Arrange
            var csvContent = "EmailId,OneLoginId,OrganisationName,OrgStreetAddress,OrgTown,OrgPostcode,PhoneNumber,CompaniesHouseNo,DateOfRegistration,HnId,HnName,DateOfHnRegistration,RegistrationSource,EcStreetAddress,EcTown,EcPostcode,ECLatitude,ECLongitude\r\nkuldeep@mailinator.com,urn:fdc:gov.uk:2022:VfexA9AeHRYrpqu6DzQLAO1tHJSz4iRO625rE3Phjp8,COMPANY 42188207 LIMITED,Crownway,Cardiff,CF14 3UZ,1212121212,42188207,,HN1000002,importedHN1,14/06/2026,Ofgem,\"Flat 10, Friars mews, Wesleyan court\",Lincoln,LN2 5DB,52.9343097,-1.252217489";
            _mockOrganisationService.Setup(x => x.GetByCompanyHouseNumberAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(new HNTAS.Core.Api.Data.Models.Organisation
                {
                    OrgId = "ORG123",
                    Name = "Test Organisation",
                    CompaniesHouseNumber = "42188207",
                    RpUserId = "user123",
                }));
            _mockUserService.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(new User { Id = "test123", EmailId = "test@c.com" });

            // Act
            var result = await _sut.ImportFromCsvAsync(csvContent);

            // Assert
            Assert.Equal(1, result.RowsProcessed);
            Assert.Equal(1, result.HeatNetworksInserted);
        }

        [Fact]
        public async Task ImportCsv_NoOrg_RpExist_NoHeatNetwork_ShouldReturnOneHeatNetworkAdded()
        {
            // Arrange
            var csvContent = "EmailId,OneLoginId,OrganisationName,OrgStreetAddress,OrgTown,OrgPostcode,PhoneNumber,CompaniesHouseNo,DateOfRegistration,HnId,HnName,DateOfHnRegistration,RegistrationSource,EcStreetAddress,EcTown,EcPostcode,ECLatitude,ECLongitude\r\nkuldeep@mailinator.com,urn:fdc:gov.uk:2022:VfexA9AeHRYrpqu6DzQLAO1tHJSz4iRO625rE3Phjp8,COMPANY 42188207 LIMITED,Crownway,Cardiff,CF14 3UZ,1212121212,42188207,,HN1000002,importedHN1,14/06/2026,Ofgem,\"Flat 10, Friars mews, Wesleyan court\",Lincoln,LN2 5DB,52.9343097,-1.252217489";
            
            _mockUserService.Setup(x => x.GetByEmailAsync(It.IsAny<string>()))
                .Returns(Task.FromResult<User?>(new User
                {
                    Roles = new List<UserRole>
                    {
                        UserRole.ResponsibleParty
                    },

                }));

            _mockOrganisationService.Setup(x => x.GetByOrgIdAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(new Organisation { Name = "org1"}));

            // Act
            var result = await _sut.ImportFromCsvAsync(csvContent);

            // Assert
            Assert.Equal(1, result.RowsProcessed);
            Assert.Equal(1, result.HeatNetworksInserted);
        }

        [Fact]
        public async Task ImportCsv_NoOrg_NoRp_NoHeatNetwork_ShouldReturnOneHeatNetworkAdded()
        {
            // Arrange
            var csvContent = "EmailId,OneLoginId,OrganisationName,OrgStreetAddress,OrgTown,OrgPostcode,PhoneNumber,CompaniesHouseNo,DateOfRegistration,HnId,HnName,DateOfHnRegistration,RegistrationSource,EcStreetAddress,EcTown,EcPostcode,ECLatitude,ECLongitude\r\nkuldeep@mailinator.com,urn:fdc:gov.uk:2022:VfexA9AeHRYrpqu6DzQLAO1tHJSz4iRO625rE3Phjp8,COMPANY 42188207 LIMITED,Crownway,Cardiff,CF14 3UZ,1212121212,42188207,,HN1000002,importedHN1,14/06/2026,Ofgem,\"Flat 10, Friars mews, Wesleyan court\",Lincoln,LN2 5DB,52.9343097,-1.252217489";

            _mockUserService.Setup(x => x.GetByEmailAsync(It.IsAny<string>()))
                .Returns(Task.FromResult<User?>(new User
                {
                    Roles = new List<UserRole>
                    {
                        UserRole.NetworkManager
                    },

                }));

            _mockOrganisationService.Setup(x => x.GetByOrgIdAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(new Organisation { Name = "org1" }));

            // Act
            var result = await _sut.ImportFromCsvAsync(csvContent);

            // Assert
            Assert.Equal(1, result.RowsProcessed);
            Assert.Equal(1, result.HeatNetworksInserted);
        }

        [Fact]
        public async Task ImportCsv_NoOrg_NoRp_HeatNetworkExist_ShouldReturnNoHeatNetworkAdded()
        {
            // Arrange
            var csvContent = "EmailId,OneLoginId,OrganisationName,OrgStreetAddress,OrgTown,OrgPostcode,PhoneNumber,CompaniesHouseNo,DateOfRegistration,HnId,HnName,DateOfHnRegistration,RegistrationSource,EcStreetAddress,EcTown,EcPostcode,ECLatitude,ECLongitude\r\nkuldeep@mailinator.com,urn:fdc:gov.uk:2022:VfexA9AeHRYrpqu6DzQLAO1tHJSz4iRO625rE3Phjp8,COMPANY 42188207 LIMITED,Crownway,Cardiff,CF14 3UZ,1212121212,42188207,,HN1000002,importedHN1,14/06/2026,Ofgem,\"Flat 10, Friars mews, Wesleyan court\",Lincoln,LN2 5DB,52.9343097,-1.252217489";

            _mockUserService.Setup(x => x.GetByEmailAsync(It.IsAny<string>()))
                .Returns(Task.FromResult<User?>(new User
                {
                    Roles = new List<UserRole>
                    {
                        UserRole.NetworkManager
                    },

                }));

            _mockOrganisationService.Setup(x => x.GetByOrgIdAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(new Organisation { Name = "org1" }));

            _mockHeatNetworkService.Setup(x => x.GetByHnIdAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(new HeatNetwork
                {
                    HnId = "HN1000002",
                    Name = "Test Heat Network",
                }));

            // Act
            var result = await _sut.ImportFromCsvAsync(csvContent);

            // Assert
            Assert.Equal(1, result.RowsProcessed);
            Assert.Equal(0, result.HeatNetworksInserted);
        }

        [Fact]
        public async Task ImportCsv_ParserError()
        {
            // Arrange
            var csvContent = "";            

            // Act
            var result = await _sut.ImportFromCsvAsync(csvContent);

            // Assert
            Assert.Equal(0, result.RowsProcessed);    
            Assert.Equal(1, result.Errors.Count);
        }
    }
}
