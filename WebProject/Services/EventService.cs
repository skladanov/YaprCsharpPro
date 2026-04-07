using AutoMapper;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using System;
using System.Linq.Expressions;
using System.Runtime.InteropServices;

public class EventService : IEventService
{
    private readonly IEventRepository _repository;

    public EventService(IEventRepository repository)
    {
        _repository = repository;
    }

    public async Task<PaginatedResult<Event>> GetAllEventsAsync(int page = 1, int pageSize = 10, string? title = null, DateTime? from = null, DateTime? to = null)
    {
        Expression<Func<Event, bool>> predicate = e =>
        (string.IsNullOrEmpty(title) ||
            e.Title.Contains(title, StringComparison.OrdinalIgnoreCase)) &&
        (!from.HasValue || e.StartAt >= from.Value) &&
        (!to.HasValue || e.EndAt <= to.Value);

        ICollection<Event> allEvents = await _repository.GetAllEventsAsync(predicate);

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

        return new PaginatedResult<Event>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Event?> GetEventAsync(Guid id)
    {
        var existingEvent = await _repository.GetEventAsync(id);

        if (existingEvent == null)
            throw new EventNotFoundException(id);

        return existingEvent;
    }

    public async Task<Event> AddEventAsync(EventDto newEventData)
    {
        ValidateRequestEvent(newEventData);

        var createdEvent = await _repository.AddEventAsync(newEventData);

        if (createdEvent == null)
            throw new ExternalException("Failed to create event");

        return createdEvent;
    }

    public async Task UpdateEventAsync(EventDto newEventData, Guid id)
    {
        ValidateRequestEvent(newEventData);

        if (await GetEventAsync(id) == null)
            throw new EventNotFoundException(id);

        if (!await _repository.UpdateEventAsync(newEventData, id))
            throw new ExternalException("Failed to update event");
    }

    public async Task DeleteEventAsync(Guid id)
    {
        if (await GetEventAsync(id) == null)
            throw new EventNotFoundException(id);

        if (!await _repository.DeleteEventAsync(id))
            throw new ExternalException("Failed to delete event");
    }

    private void ValidateRequestEvent(EventDto newEventData)
    {
        if (newEventData == null)
            throw new ValidationException("", "Request body is empty");

        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(newEventData.Title))
        {
            errors["title"] = new[] { "Title is requaried" };
        }

        if (newEventData.StartAt == default)
        {
            errors["startAt"] = new[] { "The start date is required" };
        }
        else if (newEventData.StartAt < DateTime.UtcNow)
        {
            errors["startAt"] = new[] { "The start date cannot be in the past" };
        }

        if (newEventData.EndAt == default)
        {
            errors["endAt"] = new[] { "The end date is required" };
        }
        if (newEventData.EndAt <= newEventData.StartAt)
        {
            errors["endAt"] = new[] { "The end date must be later than the start date" };
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }
}