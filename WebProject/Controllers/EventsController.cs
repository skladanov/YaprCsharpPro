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
    public ActionResult<ICollection<Event>> GetAllEvents(
        [FromQuery] string? title, 
        [FromQuery] DateTime? from, 
        [FromQuery] DateTime? to)
    {
        return Ok(_eventService.GetAllEvents(title, from, to));
    }

    [HttpGet("{id:int}")]
    public ActionResult<Event> GetEvent(int id)
    {
        var eventItem = _eventService.GetEvent(id);
        if (eventItem == null) 
            return NotFound(new { error = $"Event with ID {id} not found" });
        return Ok(eventItem);
    }

    [HttpPost]
    public ActionResult<Event> AddEvent([FromBody] EventDto newEventData)
    {
        if (newEventData == null)
            return BadRequest(new { error = "Event data is required" });

        var validationResult = ValidateEvent(newEventData);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        try
        {
            var createdEvent = _eventService.AddEvent(newEventData);

            if(_eventService.GetEvent(createdEvent.Id) == null)
            {
                return StatusCode(500, new
                {
                    error = "Failed to create event"
                });
            }

            // Возвращаем 201 Created с URL и объектом
            return CreatedAtAction(
                nameof(GetEvent),           // Имя действия для генерации URL
                new { id = createdEvent.Id }, // Параметры маршрута
                createdEvent                 // Тело ответа
            );
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                error = "Failed to create event",
                details = ex.Message
            });
        }
    }

    [HttpPut("{id:int}")]
    public IActionResult UpdateEvent([FromBody] EventDto newEventData, int id)
    {
        if (newEventData == null)
            return BadRequest(new { error = "Event data is required" });

        var validationResult = ValidateEvent(newEventData);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        if (!_eventService.UpdateEvent(newEventData, id))
            return NotFound(new { error = $"Event with ID {id} not found" });


        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        if(!_eventService.DeleteEvent(id))
                return NotFound(new { error = $"Event with ID {id} not found" });

        var removingEvent = _eventService.GetEvent(id);

        return Ok(new { message = $"Event with ID {id} successfully deleted" });
    }

    private ValidationResult ValidateEvent(EventDto eventItem)
    {
        var errors = new List<string>();

        if (eventItem.StartAt >= eventItem.EndAt)
            errors.Add("StartAt must be less than EndAt");

        if (string.IsNullOrWhiteSpace(eventItem.Title))
            errors.Add("Title is required");

        return new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors
        };
    }

    private class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}