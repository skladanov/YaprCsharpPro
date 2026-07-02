using Application.Services;
using Domain.Exceptions;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

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
        public async Task<IActionResult> Register([FromBody] string Login, string Password, string? Role, CancellationToken token)
        {
            try
            {
                await _userService.RegisterAsync(
                    login: Login,
                    role: Role ?? "User", // по умолчанию User
                    password: Password,
                    token: token);

                return Ok(new { message = "Пользователь успешно зарегистрирован" });
            }

            // Обрабатываем бизнес-ошибку «логин уже занят»
            catch (DuplicateLoginException)
            {
                return Conflict(new { message = "Логин уже занят" });
            }

            // Любая другая ошибка — 500 (в проде лучше логировать и возвращать обезличенное сообщение)
            catch (Exception ex)
            {
                // В тестах можно оставить ex.Message, в проде — убрать
                return StatusCode(500, new { message = "Внутренняя ошибка сервера", debug: ex.Message });
            }
        }
    }
}
