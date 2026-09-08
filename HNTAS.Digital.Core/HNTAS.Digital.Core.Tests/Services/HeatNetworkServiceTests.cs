using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Data.Models.External;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models.AssignedAssessor;
using HNTAS.Core.Api.Models.HeatNetwork;
using HNTAS.Core.Api.Models.Users;
using HNTAS.Core.Api.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;

namespace HNTAS.Digital.Core.Tests.Services
{
    public class HeatNetworkServiceTests
    {
        private readonly Mock<IMongoCollection<HeatNetwork>> _mockCollection;
        private readonly Mock<IMongoDatabase> _mockDatabase;
        private readonly Mock<IAuditService> _mockAuditService;
        private readonly Mock<ILogger<HeatNetworkService>> _mockLogger;
        private readonly Mock<IOptions<AWSDocDbSettings>> _mockDbSettings;
        private readonly Mock<IUserService> _mockUserService;
        private readonly HeatNetworkService _sut;

        public HeatNetworkServiceTests()
        {
            _mockCollection = new Mock<IMongoCollection<HeatNetwork>>();
            _mockDatabase = new Mock<IMongoDatabase>();
            _mockAuditService = new Mock<IAuditService>();
            _mockLogger = new Mock<ILogger<HeatNetworkService>>();
            _mockDbSettings = new Mock<IOptions<AWSDocDbSettings>>();
            _mockUserService = new Mock<IUserService>();
            var settings = new AWSDocDbSettings
            {
                HeatNetworksCollectionName = "HeatNetworks"
            };

            _mockDbSettings.Setup(s => s.Value).Returns(settings);
            // Setup the mock database to return the mock collection
            _mockDatabase.Setup(db => db.GetCollection<HeatNetwork>(It.IsAny<string>(), null))
                .Returns(_mockCollection.Object);
            _sut = new HeatNetworkService(_mockDbSettings.Object, _mockDatabase.Object, _mockLogger.Object, _mockAuditService.Object, _mockUserService.Object);
        }

        [Fact]
        public async Task CreateAsync_ShouldInsertNewHeatNetwork()
        {
            // Arrange
            var heatNetwork = new HeatNetwork
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test Heat Network",
            };
            var isNewHeatNetwork = true;
            // Act
            await _sut.CreateAsync(heatNetwork, isNewHeatNetwork);
            // Assert
            _mockCollection.Verify(c => c.InsertOneAsync(heatNetwork, null, default), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateExistingHeatNetwork()
        {
            // Arrange
            var heatNetwork = new HeatNetwork
            {
                Id = "hn1",
                Name = "Updated Heat Network",
            };
            // Act
            await _sut.UpdateAsync("hn1", heatNetwork);
            // Assert
            _mockCollection.Verify(c => c.ReplaceOneAsync(
                It.IsAny<FilterDefinition<HeatNetwork>>(),
                heatNetwork,
                It.IsAny<ReplaceOptions>(),
                default), Times.Once);
        }        

        [Fact]
        public async Task GetByHnIdAsync_ShouldReturnHeatNetwork()
        {
            _mockCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<HeatNetwork>>(),
                It.IsAny<FindOptions<HeatNetwork, HeatNetwork>>(),
                default))
                .ReturnsAsync(Mock.Of<IAsyncCursor<HeatNetwork>>(cursor =>
                    cursor.Current == new List<HeatNetwork> { new HeatNetwork { Id = "hn1", Name = "Test Heat Network" } } &&
                    cursor.MoveNext(It.IsAny<CancellationToken>()) == true &&
                    cursor.MoveNextAsync(It.IsAny<CancellationToken>()).Result == true));
            await _sut.GetByHnIdAsync("hn1");
            _mockCollection.Verify(c => c.FindAsync(
                It.IsAny<FilterDefinition<HeatNetwork>>(),
                It.IsAny<FindOptions<HeatNetwork, HeatNetwork>>(),
                default), Times.Once);
        }

        [Fact]
        public async Task GetByHnIdsAsync_ShouldReturnMatchingHeatNetworks()
        {
            // Arrange
            var hnIds = new List<string> { "HN001", "HN002", "HN003" };
            var expectedHeatNetworks = new List<HeatNetwork>
            {
                new HeatNetwork
                {
                    Id = "1",
                    HnId = "HN001",
                    Name = "Heat Network 1",
                    CreatedAt = DateTime.UtcNow
                },
                new HeatNetwork
                {
                    Id = "2",
                    HnId = "HN002",
                    Name = "Heat Network 2",
                    CreatedAt = DateTime.UtcNow
                },
                new HeatNetwork
                {
                    Id = "3",
                    HnId = "HN003",
                    Name = "Heat Network 3",
                    CreatedAt = DateTime.UtcNow
                }
            };

            // Setup mock cursor
            var mockCursor = new Mock<IAsyncCursor<HeatNetwork>>();
            mockCursor.Setup(c => c.Current).Returns(expectedHeatNetworks);
            mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            // Setup mock for FindAsync
            _mockCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<HeatNetwork>>(),
                It.IsAny<FindOptions<HeatNetwork, HeatNetwork>>(),
                default))
                .ReturnsAsync(mockCursor.Object);

            // Act
            var result = await _sut.GetByHnIdsAsync(hnIds);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedHeatNetworks.Count, result.Count);
            Assert.Equal("HN001", result[0].HnId);
            Assert.Equal("HN002", result[1].HnId);
            Assert.Equal("HN003", result[2].HnId);

            _mockCollection.Verify(c => c.FindAsync(
                It.IsAny<FilterDefinition<HeatNetwork>>(),
                It.IsAny<FindOptions<HeatNetwork, HeatNetwork>>(),
                default), Times.Once);
        }

        [Fact]
        public async Task GetByDateRangeAsync_ShouldReturnMatchingHeatNetworks()
        {
            // Arrange
            var startDate = new DateTime(2023, 1, 1);
            var endDate = new DateTime(2023, 12, 31);
            var expectedHeatNetworks = new List<HeatNetwork>
            {
                new HeatNetwork
                {
                    Id = "1",
                    HnId = "HN001",
                    Name = "Heat Network 1",
                    CreatedAt = new DateTime(2023, 5, 15)
                },
                new HeatNetwork
                {
                    Id = "2",
                    HnId = "HN002",
                    Name = "Heat Network 2",
                    CreatedAt = new DateTime(2023, 8, 20)
                }
            };
            // Setup mock cursor
            var mockCursor = new Mock<IAsyncCursor<HeatNetwork>>();
            mockCursor.Setup(c => c.Current).Returns(expectedHeatNetworks);
            mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            // Setup mock for FindAsync
            _mockCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<HeatNetwork>>(),
                It.IsAny<FindOptions<HeatNetwork, HeatNetwork>>(),
                default))
                .ReturnsAsync(mockCursor.Object);
            // Act
            var result = await _sut.GetByDateRangeAsync(startDate, endDate);
            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedHeatNetworks.Count, result.Count);
            Assert.All(result, hn => Assert.InRange(hn.CreatedAt, startDate, endDate));
            _mockCollection.Verify(c => c.FindAsync(
                It.IsAny<FilterDefinition<HeatNetwork>>(),
                It.IsAny<FindOptions<HeatNetwork, HeatNetwork>>(),
                default), Times.Once);
        }

        [Fact]
        public async Task GetDetailsByHnIdAsync_HeatNetworkExists_ShouldReturnHeatNetworkDetails()
        {
            // Arrange
            var hnId = "HN001";           

            var expectedHeatNetworkExternalResponse = new HeatNetworkExternalResponse
            {
                Id = "1",
                HnId = hnId,
                CreatedAt = DateTime.UtcNow
            };
            
            _mockCollection.Setup(c => c.Aggregate(
                It.IsAny<PipelineDefinition<HeatNetwork, HeatNetworkExternalResponse>>(),
                It.IsAny<AggregateOptions>(), It.IsAny<CancellationToken>()))
                .Returns(Mock.Of<IAsyncCursor<HeatNetworkExternalResponse>>(cursor =>
                    cursor.Current == new List<HeatNetworkExternalResponse> { expectedHeatNetworkExternalResponse } &&
                    cursor.MoveNext(It.IsAny<CancellationToken>()) == true &&
                    cursor.MoveNextAsync(It.IsAny<CancellationToken>()).Result == true));


            // Act
            var result = await _sut.GetDetailsByHnIdAsync(hnId);
            // Assert
            _mockCollection.Verify(c => c.Aggregate(
                It.IsAny<PipelineDefinition<HeatNetwork, HeatNetworkExternalResponse>>(),
                It.IsAny<AggregateOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetDetailsAsync_HeatNetworkExists_ShouldReturnHeatNetworkDetails()
        {
            // Arrange
            var hnId = "HN001";

            var expectedHeatNetworkExternalResponse = new HeatNetworkExternalResponse
            {
                Id = "1",
                HnId = hnId,
                CreatedAt = DateTime.UtcNow
            };

            // Setup mock cursor - ToListAsync() uses MoveNextAsync() and Current internally
            var mockCursor = new Mock<IAsyncCursor<HeatNetworkExternalResponse>>();
            mockCursor.Setup(c => c.Current).Returns(new List<HeatNetworkExternalResponse> { expectedHeatNetworkExternalResponse });
            mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)   // First call returns true (data available)
                .ReturnsAsync(false);  // Second call returns false (end of data)

            _mockCollection.Setup(c => c.Aggregate(
                It.IsAny<PipelineDefinition<HeatNetwork, HeatNetworkExternalResponse>>(),
                It.IsAny<AggregateOptions>(), It.IsAny<CancellationToken>()))
                .Returns(mockCursor.Object);

            // Act
            var result = await _sut.GetDetailsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(expectedHeatNetworkExternalResponse.Id, result[0].Id);
            Assert.Equal(expectedHeatNetworkExternalResponse.HnId, result[0].HnId);

            _mockCollection.Verify(c => c.Aggregate(
                It.IsAny<PipelineDefinition<HeatNetwork, HeatNetworkExternalResponse>>(),
                It.IsAny<AggregateOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetDetailsByDateRangeAsync_HeatNetworkExists_ShouldReturnHeatNetworkDetails()
        {
            // Arrange
            var hnId = "HN001";

            var expectedHeatNetworkExternalResponse = new HeatNetworkExternalResponse
            {
                Id = "1",
                HnId = hnId,
                CreatedAt = DateTime.UtcNow
            };

            // Setup mock cursor - ToListAsync() uses MoveNextAsync() and Current internally
            var mockCursor = new Mock<IAsyncCursor<HeatNetworkExternalResponse>>();
            mockCursor.Setup(c => c.Current).Returns(new List<HeatNetworkExternalResponse> { expectedHeatNetworkExternalResponse });
            mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)   // First call returns true (data available)
                .ReturnsAsync(false);  // Second call returns false (end of data)

            _mockCollection.Setup(c => c.Aggregate(
                It.IsAny<PipelineDefinition<HeatNetwork, HeatNetworkExternalResponse>>(),
                It.IsAny<AggregateOptions>(), It.IsAny<CancellationToken>()))
                .Returns(mockCursor.Object);

            // Act
            var result = await _sut.GetDetailsByDateRangeAsync(DateTime.Now, DateTime.Now);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(expectedHeatNetworkExternalResponse.Id, result[0].Id);
            Assert.Equal(expectedHeatNetworkExternalResponse.HnId, result[0].HnId);

            _mockCollection.Verify(c => c.Aggregate(
                It.IsAny<PipelineDefinition<HeatNetwork, HeatNetworkExternalResponse>>(),
                It.IsAny<AggregateOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetAssignedAssessors_ShouldReturnAssignedAssessorResponse()
        {
            // Arrange
            var request = new AssignedAssessorRequest
            {
                Page = 1,
                PageSize = 10,
                SortBy = "CreatedAt",
                SortDirection = "asc"
            };
            var expectedHeatNetworks = new List<HeatNetwork>
            {
                new HeatNetwork
                {
                    Id = "1",
                    HnId = "HN001",
                    Name = "Heat Network 1",
                    CreatedAt = new DateTime(2023, 5, 15),
                    NetworkElements = new NetworkElements
                    {
                        ElementsGroup = new List<ElementGroup>
                        {
                            new ElementGroup
                            {
                                Count = 1,
                                ElementDisplayType = HeatNetworkElementType.EnergyCentre,
                                ElementType = ElementTypeInShort.EC,
                                SoaStages = new List<SoaStages>
                                {
                                    new SoaStages
                                    {
                                        Assessors = new List<SoaAssessor>
                                        {
                                            new SoaAssessor
                                            {
                                                FirstName = "John",
                                                LastName = "Doe",
                                                Email = "test1@gmail.com"
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                new HeatNetwork
                {
                    Id = "2",
                    HnId = "HN002",
                    Name = "Heat Network 2",
                    CreatedAt = new DateTime(2023, 8, 20),
                    NetworkElements = new NetworkElements
                    {
                        ElementsGroup = new List<ElementGroup>
                        {
                            new ElementGroup
                            {
                                Count = 1,
                                ElementDisplayType = HeatNetworkElementType.EnergyCentre,
                                ElementType = ElementTypeInShort.EC,
                                SoaStages = new List<SoaStages>
                                {
                                    new SoaStages
                                    {
                                        Assessors = new List<SoaAssessor>
                                        {
                                            new SoaAssessor
                                            {
                                                FirstName = "John",
                                                LastName = "Doe",
                                                Email = "test2@gmail.com"
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
            // Setup mock cursor
            var mockCursor = new Mock<IAsyncCursor<HeatNetwork>>();
            mockCursor.Setup(c => c.Current).Returns(expectedHeatNetworks);
            mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            // Setup mock for FindAsync
            _mockCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<HeatNetwork>>(),
                It.IsAny<FindOptions<HeatNetwork, HeatNetwork>>(),
                default))
                .ReturnsAsync(mockCursor.Object);
            // Act
            var result = await _sut.GetAssignedAssessors(request);
            // Assert
            Assert.NotNull(result);
            _mockCollection.Verify(c => c.FindAsync(
                It.IsAny<FilterDefinition<HeatNetwork>>(),
                It.IsAny<FindOptions<HeatNetwork, HeatNetwork>>(),
                default), Times.Once);
        }

        [Fact]
        public async Task GetByHnIdAndRegistrationSourceAsync_ShouldReturnHeatNetwork()
        {
            _mockCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<HeatNetwork>>(),
                It.IsAny<FindOptions<HeatNetwork, HeatNetwork>>(),
                default))
                .ReturnsAsync(Mock.Of<IAsyncCursor<HeatNetwork>>(cursor =>
                    cursor.Current == new List<HeatNetwork> { new HeatNetwork { Id = "hn1", Name = "Test Heat Network" } } &&
                    cursor.MoveNext(It.IsAny<CancellationToken>()) == true &&
                    cursor.MoveNextAsync(It.IsAny<CancellationToken>()).Result == true));
            await _sut.GetByHnIdAndRegistrationSourceAsync("hn1", RegistrationSource.HNTAS);
            _mockCollection.Verify(c => c.FindAsync(
                It.IsAny<FilterDefinition<HeatNetwork>>(),
                It.IsAny<FindOptions<HeatNetwork, HeatNetwork>>(),
                default), Times.Once);
        }
        

        [Fact]
        public async Task GetByOfgemEmailIdAsync_ShouldReturnMatchingHeatNetworks()
        {
            // Arrange
            var expectedHeatNetworks = new List<HeatNetwork>
            {
                new HeatNetwork
                {
                    Id = "1",
                    HnId = "HN001",
                    Name = "Heat Network 1",
                    CreatedAt = DateTime.UtcNow
                },
                new HeatNetwork
                {
                    Id = "2",
                    HnId = "HN002",
                    Name = "Heat Network 2",
                    CreatedAt = DateTime.UtcNow
                },
                new HeatNetwork
                {
                    Id = "3",
                    HnId = "HN003",
                    Name = "Heat Network 3",
                    CreatedAt = DateTime.UtcNow
                }
            };

            // Setup mock cursor
            var mockCursor = new Mock<IAsyncCursor<HeatNetwork>>();
            mockCursor.Setup(c => c.Current).Returns(expectedHeatNetworks);
            mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            // Setup mock for FindAsync
            _mockCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<HeatNetwork>>(),
                It.IsAny<FindOptions<HeatNetwork, HeatNetwork>>(),
                default))
                .ReturnsAsync(mockCursor.Object);

            // Act
            var result = await _sut.GetByOfgemEmailIdAsync("test@gmail.com");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedHeatNetworks.Count, result.Count);
            Assert.Equal("HN001", result[0].HnId);
            Assert.Equal("HN002", result[1].HnId);
            Assert.Equal("HN003", result[2].HnId);

            _mockCollection.Verify(c => c.FindAsync(
                It.IsAny<FilterDefinition<HeatNetwork>>(),
                It.IsAny<FindOptions<HeatNetwork, HeatNetwork>>(),
                default), Times.Once);
        }


        [Fact]
        public async Task GetExistingNetworks_ShouldReturnMatchingHeatNetworks()
        {
            // Arrange

            var request = new ExistingNetworkRequest
            {
                SortBy = "CreatedAt",
                PageSize = 10,
                Page = 1,
                SortDirection = "asc",
                UserId = "test@email.com"
            };
            var expectedHeatNetworks = new List<HeatNetwork>
            {
                new HeatNetwork
                {
                    Id = "1",
                    HnId = "HN001",
                    Name = "Heat Network 1",
                    CreatedAt = DateTime.UtcNow
                },
                new HeatNetwork
                {
                    Id = "2",
                    HnId = "HN002",
                    Name = "Heat Network 2",
                    CreatedAt = DateTime.UtcNow
                },
                new HeatNetwork
                {
                    Id = "3",
                    HnId = "HN003",
                    Name = "Heat Network 3",
                    CreatedAt = DateTime.UtcNow
                }
            };

            // Setup mock cursor
            var mockCursor = new Mock<IAsyncCursor<HeatNetwork>>();
            mockCursor.Setup(c => c.Current).Returns(expectedHeatNetworks);
            mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            // Setup mock for FindAsync
            _mockCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<HeatNetwork>>(),
                It.IsAny<FindOptions<HeatNetwork, HeatNetwork>>(),
                default))
                .ReturnsAsync(mockCursor.Object);

            // Act
            var result = await _sut.GetExistingNetworks(request);

            // Assert
            Assert.NotNull(result);            
        }
    }
}
