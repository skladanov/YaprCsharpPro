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
    public async Task<IActionResult> CreateBooking(Guid id, CancellationToken token)
    {
        var result = await _bookingService.CreateBookingAsync(id, token);

        return AcceptedAtRoute(
            nameof(BookingsController.GetBooking),
            new { id = result },
            result
        );
    }

    [HttpGet]
    public async Task<ActionResult<ICollection<Event>>> GetAllEvents(
        [FromQuery] string? title = null, 
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, int.MaxValue)] int pageSize = 10,
        CancellationToken token = default)
    {
        var result = await _eventService.GetAllEventsAsync(page, pageSize, title, from, to, token);
        return Ok(result);
    }

    [HttpGet("{id:Guid}")]
    public async Task<ActionResult<Event>> GetEvent(Guid id, CancellationToken token)
    {
        var result = await _eventService.GetEventAsync(id, token);

        if (result == null)
            throw new EventNotFoundException(id);

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<Event>> AddEvent([FromBody] CreateEvent newEventData, CancellationToken token)
    {
        var newEventId = await _eventService.AddEventAsync(newEventData, token);

        return CreatedAtAction(
            nameof(GetEvent),
            new { id = newEventId },
            newEventId
        );
    }

    [HttpPut("{id:Guid}")]
    public async Task<IActionResult> UpdateEvent([FromBody] UpdateEvent newEventData, Guid id, CancellationToken token)
    {
        await _eventService.UpdateEventAsync(newEventData, id, token);

        return NoContent();
    }

    [HttpDelete("{id:Guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken token)
    {
        await _eventService.DeleteEventAsync(id, token);

        return Ok(new { message = $"Event with ID {id} successfully deleted" });
    }
}