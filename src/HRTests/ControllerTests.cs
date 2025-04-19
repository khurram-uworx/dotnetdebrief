using HRApis.Controllers;
using HRApis.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using UWorx.HR.Repositories;

namespace HRTests;

public class ControllerTests
{
    const string TestUser = "test@email.com";

    HRUserInfo testUser1 = new()
    {
        UserIndex = 1,
        UserGuid = Guid.NewGuid(),
        UserEmail = TestUser,
        FirstName = "firstName",
        MiddleName = "middleName",
        LastName = "lastName"
    };

    [Test]
    public void CreateUserTest()
    {
        //Arrange
        var logger = new Mock<ILogger<UsersController>>();
        var controller = new UsersController(logger.Object);
        var service = new Mock<IUsersService>();

        //Action
        var result = controller.CreateUser(service.Object, testUser1);

        //Assert
        Assert.IsInstanceOf(typeof(BadRequestResult), result);
        //logger.Verify(
        //        x => x.Log(
        //            It.IsAny<LogLevel>(),
        //            It.IsAny<EventId>(),
        //            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Your expected log message")),
        //            It.IsAny<Exception>(),
        //            It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
        //        Times.Once);
    }
}
