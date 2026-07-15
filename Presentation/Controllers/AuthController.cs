using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
[Authorize]
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
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken token)
    {
        var tokenResult = await _userService.LoginAsync(request, token);

        return Ok(new
        {
            message = "Пользователь успешно авторизован",
            token = tokenResult
        });
    }
}