using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;
    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
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

    [HttpPost("{id:Guid}/cancel")]
    [Authorize]
    public async Task<IActionResult> CancelBooking(Guid bookingId, CancellationToken token)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        // Получение роли из claims (пример)
        bool isAdmin = User.IsInRole("Admin");

        try
        {
            await _bookingService.CancelBookingAsync(id, userId.Value, isAdmin, token);
            return NoContent();
        }
        catch (ForbiddenException)
        {
            return Forbid();
        }
        catch (BookingNotFoundException)
        {
            return NotFound();
        }
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst("sub"); // стандартный claim для userId
        return claim?.Value is string sub && Guid.TryParse(sub, out var id) ? id : null;
    }
}