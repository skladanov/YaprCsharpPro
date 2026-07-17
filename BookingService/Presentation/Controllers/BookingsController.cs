using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;
    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpPost("event/{eventId:Guid}/book")]
    [Authorize]
    public async Task<IActionResult> CreateBooking(Guid eventId, CancellationToken token)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Пользователь не авторизован" });

        var result = await _bookingService.CreateBookingAsync(userId.Value, eventId, token);

        return AcceptedAtRoute(
            nameof(BookingsController.GetBooking),
            new { id = result },
            result
        );
    }

    [HttpGet("{id:Guid}", Name = "GetBooking")]
    [Authorize]
    public async Task<ActionResult<Booking>> GetBooking(Guid id, CancellationToken token)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Пользователь не авторизован" });

        var result = await _bookingService.GetBookingByIdAsync(id, token);
        return Ok(result);
    }

    [HttpDelete("{id:Guid}/cancel")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CancelBooking(Guid id, CancellationToken token)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        // Получение роли из claims (пример)
        bool isAdmin = User.IsInRole("Admin");

        await _bookingService.CancelAsync(id, userId.Value, isAdmin, token);
        return NoContent();
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);

        if (claim?.Value is not string idString)
            return null;

        return Guid.TryParse(idString, out var id) ? id : null;
    }
}