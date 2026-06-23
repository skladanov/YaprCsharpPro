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
    public async Task<ActionResult<Booking>> GetBooking(Guid id, CancellationToken token)
    {
        var result = await _bookingService.GetBookingByIdAsync(id, token);
        return Ok(result);
    }
}