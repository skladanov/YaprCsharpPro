using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;
    private readonly IBookingService _bookingService;
    public EventsController(IEventService eventService, IBookingService bookingService)
    {
        _eventService = eventService;
        _bookingService = bookingService;
    }

    [HttpPost("{id:Guid}/book")]
    public async Task<IActionResult> CreateBooking(Guid id)
    {
        var result = await _bookingService.CreateBookingAsync(id);

        var value = new {
            message = "Бронирование находится в обработке",
            bookingId = result,
            statusCheckUrl = Url.Action("GetBooking", "Bookings", new { id = result })
        };

        return AcceptedAtRoute(result, value);
    }

    [HttpGet]
    public ActionResult<ICollection<Event>> GetAllEvents(
        [FromQuery] string? title = null, 
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, int.MaxValue)] int pageSize = 10)
    {
        return Ok(_eventService.GetAllEvents(page, pageSize, title, from, to));
    }

    [HttpGet("{id:Guid}")]
    public ActionResult<Event> GetEvent(Guid id)
    {
        return Ok(_eventService.GetEvent(id));
    }

    [HttpPost]
    public ActionResult<Event> AddEvent([FromBody] EventDto newEventData)
    {
        var createdEvent = _eventService.AddEvent(newEventData);

        return CreatedAtAction(
            nameof(GetEvent),
            new { id = createdEvent.Id },
            createdEvent
        );
    }

    [HttpPut("{id:Guid}")]
    public IActionResult UpdateEvent([FromBody] EventDto newEventData, Guid id)
    {
        _eventService.UpdateEvent(newEventData, id);

        return NoContent();
    }

    [HttpDelete("{id:Guid}")]
    public IActionResult Delete(Guid id)
    {
        _eventService.DeleteEvent(id);

        return Ok(new { message = $"Event with ID {id} successfully deleted" });
    }
}