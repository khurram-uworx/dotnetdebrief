using Moq;
using UWorx.HR.Api.Services;
using UWorx.HR.Implementations;
using UWorx.HR.Repositories;

namespace UWorx.HR.Api.Tests
{
    public class UsersServiceTests
    {
        const string TestUser = "test@email.com";

        HRUserInfo testUser1 = new ()
        {
            UserIndex = 1, UserGuid = Guid.NewGuid(),
            UserEmail = TestUser,
            FirstName = "firstName", MiddleName = "middleName", LastName = "lastName"
        };
        Mock<IHRUsersRepository> repositoryMock;
        IUsersService serviceUnderTest() => new UserService(this.repositoryMock.Object);

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void WhenMockIsReturningNothing()
        {
            //Arrange
            this.repositoryMock = new Mock<IHRUsersRepository>();

            //Act
            var result = this.serviceUnderTest().GetUserInformationByEmail(TestUser);

            //Assert
            Assert.IsTrue(!result.Succeeded);
            
            this.repositoryMock.Verify(s => s.GetUsers(), Times.Once);
        }

        [Test]
        public void WhenMockIsReturningSomething()
        {
            //Arrange
            this.repositoryMock = new Mock<IHRUsersRepository>();
            this.repositoryMock.Setup(s => s.GetUsers()).Returns(new[] { testUser1 });

            //Act
            var result = this.serviceUnderTest().GetUserInformationByEmail(TestUser);
            var data = result.Succeeded ? result as HRDataResult<HRUserResponse> : null;

            //Assert
            Assert.IsTrue(result.Succeeded);
            Assert.IsTrue(null != data && data.Succeeded && null != data.Data && data.Data.FirstName.Length > 0);
            
            this.repositoryMock.Verify(s => s.GetUsers(), Times.Once);
        }
    }
}