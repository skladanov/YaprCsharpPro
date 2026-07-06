using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebProject.Controllers
{
    
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        public AuthController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken token)
        {
            await _userService.RegisterAsync(request, token);

            return Ok(new { message = "Пользователь успешно зарегистрирован" });
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] RegisterRequest request, CancellationToken token)
        {
            await _userService.RegisterAsync(request, token);

            return Ok(new { message = "Пользователь успешно зарегистрирован" });
        }
    }
}
