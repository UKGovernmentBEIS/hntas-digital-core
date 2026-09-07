using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models;
using HNTAS.Core.Api.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;


namespace HNTAS.Digital.Core.Tests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<ILogger<UserService>> _mockLogger;
        private readonly Mock<IMongoCollection<User>> _mockUserCollection;
        private readonly Mock<IMongoCollection<Organisation>> _mockOrgCollection;
        private readonly Mock<IMongoDatabase> _mockDatabase;
        private readonly Mock<IOptions<AWSDocDbSettings>> _mockSettings;
        private readonly UserService _sut;

        public UserServiceTests()
        {
            _mockLogger = new Mock<ILogger<UserService>>();
            _mockUserCollection = new Mock<IMongoCollection<User>>();
            _mockOrgCollection = new Mock<IMongoCollection<Organisation>>();
            _mockDatabase = new Mock<IMongoDatabase>();
            _mockSettings = new Mock<IOptions<AWSDocDbSettings>>();
            // Setup the mock database to return the mock collections
            _mockDatabase.Setup(db => db.GetCollection<User>(It.IsAny<string>(), It.IsAny<MongoCollectionSettings?>()))
                .Returns(_mockUserCollection.Object);
            _mockDatabase.Setup(db => db.GetCollection<Organisation>(It.IsAny<string>(), It.IsAny<MongoCollectionSettings?>()))
                .Returns(_mockOrgCollection.Object);
            // Setup the mock settings to return a dummy connection string
            var settings = new AWSDocDbSettings
            {
                UsersCollectionName = "Users" ,
                OrganisationsCollectionName = "Organisations"
            };

            _mockSettings.Setup(s => s.Value).Returns(settings);

            // Initialize the UserService with the mocked dependencies
            _sut = new UserService(_mockDatabase.Object, _mockSettings.Object, _mockLogger.Object );
        }

        [Fact]
        public async Task GetAsync_ShouldReturnUsers()
        {
            var expectedUsers = new List<User>
            {
                new User { EmailId = "test" },
                
            };
            var invitedEmails = new List<string> { "test1", "test2" };
            var mockCursor = new Mock<IAsyncCursor<User>>();
            mockCursor.Setup(c => c.Current).Returns(expectedUsers);
            mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            _mockUserCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                default))
                .ReturnsAsync(mockCursor.Object);
            await _sut.GetAsync();
            _mockUserCollection.Verify(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                default), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnUser()
        {            
            _mockUserCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                default))
                .ReturnsAsync(Mock.Of<IAsyncCursor<User>>(cursor =>
                    cursor.Current == new List<User> { new User { Id = "hn1" } } &&
                    cursor.MoveNext(It.IsAny<CancellationToken>()) == true &&
                    cursor.MoveNextAsync(It.IsAny<CancellationToken>()).Result == true));
            await _sut.GetByIdAsync("test");
            _mockUserCollection.Verify(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                default), Times.Once);
        }

        [Fact]
        public async Task GetByEmailAsync_ShouldReturnUser()
        {
            _mockUserCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                default))
                .ReturnsAsync(Mock.Of<IAsyncCursor<User>>(cursor =>
                    cursor.Current == new List<User> { new User { Id = "hn1" } } &&
                    cursor.MoveNext(It.IsAny<CancellationToken>()) == true &&
                    cursor.MoveNextAsync(It.IsAny<CancellationToken>()).Result == true));
            await _sut.GetByEmailAsync("test");
            _mockUserCollection.Verify(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                default), Times.Once);
        }

        [Fact]
        public async Task GetByUserOneLoginIdAsync_ShouldReturnUser()
        {
            _mockUserCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                default))
                .ReturnsAsync(Mock.Of<IAsyncCursor<User>>(cursor =>
                    cursor.Current == new List<User> { new User { Id = "hn1" } } &&
                    cursor.MoveNext(It.IsAny<CancellationToken>()) == true &&
                    cursor.MoveNextAsync(It.IsAny<CancellationToken>()).Result == true));
            await _sut.GetByUserOneLoginIdAsync("test");
            _mockUserCollection.Verify(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                default), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldInsertNewUser()
        {
            // Arrange
            var user = new User
            {
                Id = "test"
            };
            
            // Act
            await _sut.CreateAsync(user);
            // Assert
            _mockUserCollection.Verify(c => c.InsertOneAsync(user, null, default), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateUser()
        {
            // Arrange
            var user = new User
            {
                Id = "test"
            };

            // Act
            await _sut.UpdateAsync("userId",user);
            // Assert
            _mockUserCollection.Verify(c => c.ReplaceOneAsync(It.IsAny<FilterDefinition<User>>(), user, It.IsAny<ReplaceOptions>(), default), Times.Once);
        }

        [Fact]
        public async Task RemoveAsync_ShouldUpdateUser()
        { 

            // Act
            await _sut.RemoveAsync("userId");
            // Assert
            _mockUserCollection.Verify(c => c.DeleteOneAsync(It.IsAny<FilterDefinition<User>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetRegisteredUsers_ShouldReturnUsers()
        {
            var expectedUsers = new List<User>
            {
                new User { EmailId = "test" },

            };
            var request = new List<string> { "email1", "email2" };
            var invitedEmails = new List<string> { "test1", "test2" };
            var mockCursor = new Mock<IAsyncCursor<User>>();
            mockCursor.Setup(c => c.Current).Returns(expectedUsers);
            mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            _mockUserCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                default))
                .ReturnsAsync(mockCursor.Object);
            await _sut.GetRegisteredUsers(request);
            _mockUserCollection.Verify(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                default), Times.Once);
        }

        [Fact]
        public async Task GetAssessorsByHnIdAsync_ShouldReturnUsers()
        {
            var expectedUsers = new List<User>
            {
                new User { EmailId = "test" },

            };
            var request = "test";
            var invitedEmails = new List<string> { "test1", "test2" };
            var mockCursor = new Mock<IAsyncCursor<User>>();
            mockCursor.Setup(c => c.Current).Returns(expectedUsers);
            mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            _mockUserCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                default))
                .ReturnsAsync(mockCursor.Object);
            await _sut.GetAssessorsByHnIdAsync(request);
            _mockUserCollection.Verify(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                default), Times.Once);
        }

        [Fact]
        public async Task GetUsersAssociatedByHnIdAsync_ShouldReturnUsers()
        {
            var expectedUsers = new List<User>
            {
                new User { EmailId = "test" },

            };
            var request = "test";
            var invitedEmails = new List<string> { "test1", "test2" };
            var mockCursor = new Mock<IAsyncCursor<User>>();
            mockCursor.Setup(c => c.Current).Returns(expectedUsers);
            mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            _mockUserCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                default))
                .ReturnsAsync(mockCursor.Object);
            await _sut.GetUsersAssociatedByHnIdAsync(request);
            _mockUserCollection.Verify(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                default), Times.Once);
        }

        [Fact]
        public async Task GetResponsiblePartyByHnIdAsync_ShouldReturnUser()
        {
            _mockOrgCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<Organisation>>(),
                It.IsAny<FindOptions<Organisation, Organisation>>(),
                default))
                .ReturnsAsync(Mock.Of<IAsyncCursor<Organisation>>(cursor =>
                    cursor.Current == new List<Organisation> { new Organisation { Id = "hn1", RpUserId = "test" } } &&
                    cursor.MoveNext(It.IsAny<CancellationToken>()) == true &&
                    cursor.MoveNextAsync(It.IsAny<CancellationToken>()).Result == true));

            _mockUserCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                default))
                .ReturnsAsync(Mock.Of<IAsyncCursor<User>>(cursor =>
                    cursor.Current == new List<User> { new User { Id = "hn1" } } &&
                    cursor.MoveNext(It.IsAny<CancellationToken>()) == true &&
                    cursor.MoveNextAsync(It.IsAny<CancellationToken>()).Result == true));

            await _sut.GetResponsiblePartyByHnIdAsync("test");
            _mockUserCollection.Verify(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                default), Times.Once);
        }

        [Fact]
        public async Task GetResponsiblePartyByHnIdAsync_ShouldNotReturnUser()
        {
            _mockOrgCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<Organisation>>(),
                It.IsAny<FindOptions<Organisation, Organisation>>(),
                default))
                .ReturnsAsync(Mock.Of<IAsyncCursor<Organisation>>(cursor =>
                    cursor.Current == new List<Organisation> { new Organisation { Id = "hn1" } } &&
                    cursor.MoveNext(It.IsAny<CancellationToken>()) == true &&
                    cursor.MoveNextAsync(It.IsAny<CancellationToken>()).Result == true));
            

            await _sut.GetResponsiblePartyByHnIdAsync("test");
            _mockUserCollection.Verify(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                default), Times.Never);
        }

        [Fact]
        public async Task GetContributorsByHnIdAsync_ShouldReturnUsers()
        {
            var expectedUsers = new List<User>
            {
                new User { EmailId = "test" },

            };
            var invitedEmails = new List<string> { "test1", "test2" };
            var mockCursor = new Mock<IAsyncCursor<User>>();
            mockCursor.Setup(c => c.Current).Returns(expectedUsers);
            mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            _mockUserCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                default))
                .ReturnsAsync(mockCursor.Object);
            await _sut.GetContributorsByHnIdAsync("hnid");
            _mockUserCollection.Verify(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                default), Times.Once);
        }

        [Fact]
        public async Task GetHeatNetworkUsersWithRolesAsync_ShouldReturnUserRoleDetailResponse()
        {
            // Arrange
            var hnId = "6a3aa661be3d3d47c69044d6";
            
            var mockCursor = new Mock<IAsyncCursor<BsonDocument>>();
            mockCursor.Setup(c => c.Current).Returns(new List<BsonDocument> {  });
            mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            _mockUserCollection.Setup(c => c.Aggregate(
                It.IsAny<PipelineDefinition<User, BsonDocument>>(),
                It.IsAny<AggregateOptions>(), It.IsAny<CancellationToken>()))
                .Returns(mockCursor.Object);


            // Act
            var result = await _sut.GetHeatNetworkUsersWithRolesAsync(hnId);
            // Assert
            _mockUserCollection.Verify(c => c.Aggregate(
                It.IsAny<PipelineDefinition<User, BsonDocument>>(),
                It.IsAny<AggregateOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateOrgIdAsync_ShouldUpdateUser()
        {
            // Arrange
            var user = new User
            {
                Id = "test"
            };

            // Act
            await _sut.UpdateOrgIdAsync("userId", "orgId");
            // Assert
            _mockUserCollection.Verify(c => c.UpdateOneAsync(It.IsAny<FilterDefinition<User>>(), It.IsAny<UpdateDefinition<User>>(), It.IsAny<UpdateOptions>(), default), Times.Once);
        }

        [Fact]
        public async Task GetUsersByOrgIdAsync_ShouldReturnUsers()
        {
            var expectedUsers = new List<User>
            {
                new User { EmailId = "test" },

            };
            var invitedEmails = new List<string> { "test1", "test2" };
            var mockCursor = new Mock<IAsyncCursor<User>>();
            mockCursor.Setup(c => c.Current).Returns(expectedUsers);
            mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            _mockUserCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                default))
                .ReturnsAsync(mockCursor.Object);
            await _sut.GetUsersByOrgIdAsync("orgId");
            _mockUserCollection.Verify(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                default), Times.Once);
        }

        [Fact]
        public async Task GetUserWithDetailsAsync_ShouldReturnUserDetailsResult()
        {
            // Arrange
            var hnId = "6a3aa661be3d3d47c69044d6";

            var mockCursor = new Mock<IAsyncCursor<UserDetailsResult>>();
            mockCursor.Setup(c => c.Current).Returns(new List<UserDetailsResult> { new UserDetailsResult {EmailId = "test" } });
            mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            _mockUserCollection.Setup(c => c.Aggregate(
                It.IsAny<PipelineDefinition<User, UserDetailsResult>>(),
                It.IsAny<AggregateOptions>(), It.IsAny<CancellationToken>()))
                .Returns(mockCursor.Object);


            // Act
            var result = await _sut.GetUserWithDetailsAsync(hnId);
            // Assert
            _mockUserCollection.Verify(c => c.Aggregate(
                It.IsAny<PipelineDefinition<User, UserDetailsResult>>(),
                It.IsAny<AggregateOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetUsersByInvitedEmailsWithDetailsAsync_ShouldReturnUserDetailsResult()
        {
            // Arrange
            var request = new List<string> { "email1"};

            var mockCursor = new Mock<IAsyncCursor<UserDetailsResult>>();
            mockCursor.Setup(c => c.Current).Returns(new List<UserDetailsResult> { new UserDetailsResult { EmailId = "test" } });
            mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            _mockUserCollection.Setup(c => c.Aggregate(
                It.IsAny<PipelineDefinition<User, UserDetailsResult>>(),
                It.IsAny<AggregateOptions>(), It.IsAny<CancellationToken>()))
                .Returns(mockCursor.Object);


            // Act
            var result = await _sut.GetUsersByInvitedEmailsWithDetailsAsync(request);
            // Assert
            _mockUserCollection.Verify(c => c.Aggregate(
                It.IsAny<PipelineDefinition<User, UserDetailsResult>>(),
                It.IsAny<AggregateOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateUserNetwork_ShouldUpdateUser()
        {
            // Arrange
            var updateResultMock = new Mock<UpdateResult>();
            updateResultMock.Setup(r => r.IsAcknowledged).Returns(true);
            updateResultMock.Setup(r => r.MatchedCount).Returns(1);
            updateResultMock.Setup(r => r.ModifiedCount).Returns(1);

            _mockUserCollection
                .Setup(c => c.UpdateOneAsync(
                    It.IsAny<FilterDefinition<User>>(),
                    It.IsAny<UpdateDefinition<User>>(),
                    It.IsAny<UpdateOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(updateResultMock.Object);
            
            // Act
            await _sut.UpdateUserNetwork("userId", "hnId");
            // Assert
            _mockUserCollection.Verify(c => c.UpdateOneAsync(It.IsAny<FilterDefinition<User>>(), It.IsAny<UpdateDefinition<User>>(), It.IsAny<UpdateOptions>(), default), Times.Once);
        }
    }
}
