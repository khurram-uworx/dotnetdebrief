using Microsoft.AspNetCore.Mvc;
using UWorx.HR.Api.Services;
using UWorx.HR.Repositories;

namespace UWorx.HR.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UsersController : ControllerBase
    {
        readonly ILogger<UsersController> _logger;

        public UsersController(ILogger<UsersController> logger)
        {
            _logger = logger;
        }

        [HttpPut]
        public IActionResult CreateUser(
            [FromServices] IUsersService service,
            [FromBody] HRUserInfo userInfo)
        {
            return base.BadRequest();
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
}
