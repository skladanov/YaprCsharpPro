using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;
    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    [HttpGet]
    [AllowAnonymous]
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

    [HttpGet("top")]
    [AllowAnonymous]
    public async Task<ActionResult<List<ReturnedEvent>>> GetTop10Popular(CancellationToken token)
    {
        var result = await _eventService.GetTop10PopularAsync(token);
        return Ok(result);
    }

    [HttpGet("{id:Guid}")]
    [Authorize]
    public async Task<ActionResult<ReturnedEvent>> GetEvent(Guid id, CancellationToken token)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Пользователь не авторизован" });

        var result = await _eventService.GetEventAsync(id, token);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
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
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateEvent([FromBody] UpdateEvent newEventData, Guid id, CancellationToken token)
    {
        await _eventService.UpdateEventAsync(newEventData, id, token);

        return NoContent();
    }

    [HttpDelete("{id:Guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken token)
    {
        await _eventService.DeleteEventAsync(id, token);

        return Ok(new { message = $"Event with ID {id} successfully deleted" });
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);

        if (claim?.Value is not string idString)
            return null;

        return Guid.TryParse(idString, out var id) ? id : null;
    }
}