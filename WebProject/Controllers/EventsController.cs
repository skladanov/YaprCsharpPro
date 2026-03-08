using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

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
    public ActionResult<ICollection<Event>> GetAllEvents()
    {
        return Ok(_eventService.GetAllEvents());
    }

    [HttpGet("{id:int}")]
    public ActionResult<Event> GetEvent(int id)
    {
        var eventItem = _eventService.GetEvent(id);
        if (eventItem == null) return NotFound();
        return Ok(eventItem);
    }

    [HttpPost]
    public ActionResult AddEvent([FromBody] Event eventItem)
    {
        if (eventItem == null)
            return BadRequest("Event data is required");

        if (!IsValidEventTime(eventItem))
            return BadRequest(new { error = "StartAt must be less than EndAt" });

        _eventService.AddEvent(eventItem);

        // Указываем URL и возвращаем объект
        return CreatedAtAction(nameof(GetEvent), new { id = eventItem.Id }, eventItem);
    }

    [HttpPut("{id:int}")]
    public ActionResult UpdateEvent([FromBody] Event eventItem, int id)
    {
        if (eventItem == null)
            return NotFound();

        if (!IsValidEventTime(eventItem))
            return BadRequest(new { error = "StartAt must be less than EndAt" });

        _eventService.UpdateEvent(eventItem, id);
        return new CreatedResult();
    }


    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        if(_eventService.DeleteEvent(id)) return new OkResult();
        return NotFound();
    }

    private bool IsValidEventTime(Event eventItem)
    {
        return (eventItem.StartAt < eventItem.EndAt);
    }
}