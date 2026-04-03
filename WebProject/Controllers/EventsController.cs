using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;
    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
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

    [HttpGet("{id:int}")]
    public ActionResult<Event> GetEvent(int id)
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

    [HttpPut("{id:int}")]
    public IActionResult UpdateEvent([FromBody] EventDto newEventData, int id)
    {
        _eventService.UpdateEvent(newEventData, id);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        _eventService.DeleteEvent(id);

        return Ok(new { message = $"Event with ID {id} successfully deleted" });
    }
}