using HRApis.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using UWorx.HR;
using UWorx.HR.Repositories;

namespace HRApis.Controllers;

[ApiController]
[Route("[controller]")]
public class UsersController : ControllerBase
{
    readonly ILogger<UsersController> logger;

    public UsersController(ILogger<UsersController> logger)
    {
        this.logger = logger;
    }

    [HttpPut]
    public IActionResult CreateUser(
        [FromServices] IUsersService service,
        [FromBody] HRUserInfo userInfo)
    {
        Func<HRUserInfo, bool> valid = u => true;

        try
        {
            Guards.Against.Null(userInfo);

            if (!valid(userInfo)) throw new ArgumentException("userInfo is invalid");

            //if (service.Create())
            //  return Ok();
            //else
            return base.BadRequest();
        }
        catch (ArgumentException ex) //Guards and validations should always throw ArgumentException
        {
            // we want to log input?
            this.logger.LogError(ex, "Failed to create due to input");
            return base.ValidationProblem();
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to create due to system failure");
            return base.Problem();
        }
    }

    [HttpPost]
    public IActionResult UpdateUser([FromBody] HRUserInfo userInfo)
    {
        return base.BadRequest();
    }

    [HttpPost]
    [Route("/{userIndex}")]
    public IActionResult UpdateUserByIndex([FromRoute] int userIndex,
        [FromBody] HRUserInfo userInfo)
    {
        return base.BadRequest();
    }

    [HttpPost]
    [Route("/{guid}")]
    public IActionResult UpdateUserByGuid([FromRoute] Guid guid,
        [FromBody] HRUserInfo userInfo)
    {
        return base.BadRequest();
    }
}
