using AutoMapper;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using static System.Net.WebRequestMethods;

public class EventService : IEventService
{
    private readonly IEventRepository _repository;
    private readonly ILogger<EventService> _logger;

    public EventService(IEventRepository repository, ILogger<EventService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<PaginatedResult<Event>> GetAllEventsAsync(int page = 1, int pageSize = 10, string? title = null, DateTime? from = null, DateTime? to = null, CancellationToken token = default)
    {
        _logger.LogInformation($"Attempting to retrieve events with filters: page={page}, pageSize={pageSize}, title='{title}', from='{from}', to='{to}'");

        Expression<Func<Event, bool>> predicate = e =>
        (string.IsNullOrEmpty(title) ||
            e.Title.Contains(title, StringComparison.OrdinalIgnoreCase)) &&
        (!from.HasValue || e.StartAt >= from.Value) &&
        (!to.HasValue || e.EndAt <= to.Value);

        ICollection<Event> allEvents = await _repository.GetAllEventsAsync(predicate, token);

        if (allEvents == null)
        {
            allEvents = new List<Event>();
        }

        var totalCount = allEvents.Count;
        var offset = (page - 1) * pageSize;

        var items = allEvents
            .OrderBy(e => e.StartAt)
            .Skip(offset)
            .Take(pageSize)
            .ToList();

        _logger.LogInformation($"Successfully retrieved {items.Count} events out of {totalCount} total events.");

        return new PaginatedResult<Event>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Event> GetEventAsync(Guid id, CancellationToken token)
    {
        _logger.LogInformation("Attempting to retrieve event with ID: {EventId}", id);

        var existingEvent = await _repository.GetEventAsync(id, token);

        if(existingEvent == null)
        {
            _logger.LogWarning($"Event with ID {id} not found.");
            throw new EventNotFoundException(id);
        }

        _logger.LogInformation("Successfully retrieved event with ID: {EventId}.", id);

        return existingEvent;
    }

    public async Task<Guid> AddEventAsync(CreateEvent newEventData, CancellationToken token)
    {
        _logger.LogInformation($"Attempting to add a new event with: Title='{newEventData.Title}', Description='{newEventData.Description}', StartAt='{newEventData.StartAt}', EndAt='{newEventData.EndAt}'");

        Event newEventItem = Event.Create(
            Guid.NewGuid(),
            newEventData.Title,
            newEventData.StartAt,
            newEventData.EndAt,
            newEventData.TotalSeats,
            newEventData.Description
        );

        await _repository.AddEventAsync(newEventItem, token);

        _logger.LogInformation("Successfully added new event with ID: {EventId}.", newEventItem.Id);

        return newEventItem.Id;
    }

    public async Task UpdateEventAsync(UpdateEvent newEventData, Guid id, CancellationToken token)
    {
        _logger.LogInformation($"Attempting to update event with ID='{id}' and new data: Title='{newEventData.Title}', Description='{newEventData.Description}', StartAt='{newEventData.StartAt}', EndAt='{newEventData.EndAt}'");

        //ValidateRequestEvent(newEventData);

        Event existsEvent = await GetEventAsync(id, token); // Check event

        existsEvent.Title = newEventData.Title;
        existsEvent.Description = newEventData.Description;
        existsEvent.StartAt = newEventData.StartAt;
        existsEvent.EndAt = newEventData.EndAt;

        await _repository.UpdateEventAsync(existsEvent, token);

        _logger.LogInformation("Successfully updated event with ID: {EventId}.", id);
    }

    public async Task DeleteEventAsync(Guid id, CancellationToken token)
    {
        _logger.LogInformation($"Attempting to delete event with ID='{id}'");

        await GetEventAsync(id, token); // Check event

        await _repository.DeleteEventAsync(id, token);

        _logger.LogInformation("Successfully remove event with ID: {EventId}.", id);
    }

    private void ValidateRequestEvent(CreateEvent newEventData)
    {
        if (newEventData == null)
        {
            _logger.LogError("Request body is empty.");
            throw new ValidationException("", "Request body is empty");
        }

        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(newEventData.Title))
        {
            errors["title"] = new[] { "Title is requaried" };
            _logger.LogWarning("Validation error: Title is required.");
        }

        if (newEventData.StartAt == default)
        {
            errors["startAt"] = new[] { "The start date is required" };
            _logger.LogWarning("Validation error: The start date is required.");
        }
        else if (newEventData.StartAt < DateTime.UtcNow)
        {
            errors["startAt"] = new[] { "The start date cannot be in the past" };
            _logger.LogWarning("Validation error: The start date cannot be in the past.");
        }

        if (newEventData.EndAt == default)
        {
            errors["endAt"] = new[] { "The end date is required" };
            _logger.LogWarning("Validation error: The end date is required.");
        }
        else if (newEventData.EndAt <= newEventData.StartAt)
        {
            errors["endAt"] = new[] { "The end date must be later than the start date" };
            _logger.LogWarning("Validation error: The end date must be later than the start date.");
        }

        if (errors.Count > 0)
        {
            _logger.LogError("Validation failed with {ErrorCount} errors.", errors.Count);
            throw new ValidationException(errors);
        }
    }
}