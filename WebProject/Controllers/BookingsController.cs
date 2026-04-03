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

    [HttpGet("{id:int}")]
    public ActionResult<Booking> GetBooking(int id)
    {
        return Ok(Task.FromResult(_bookingService.GetBookingByIdAsync(id)));
    }
}