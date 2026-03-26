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

    public PaginatedResult<Event> GetAllEvents(int page = 1, int pageSize = 10, string? title = null, DateTime? from = null, DateTime? to = null)
    {
        Expression<Func<Event, bool>> predicate = e =>
        (string.IsNullOrEmpty(title) ||
            e.Title.Contains(title, StringComparison.OrdinalIgnoreCase)) &&
        (!from.HasValue || e.StartAt >= from.Value) &&
        (!to.HasValue || e.EndAt <= to.Value);

        ICollection<Event> allEvents = _repository.GetAllEvents(predicate);

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

    public Event? GetEvent(int id)
    {
        var existingEvent = _repository.GetEvent(id);

        if (existingEvent == null)
            throw new EventNotFoundException(id);

        return existingEvent;
    }

    public Event AddEvent(EventDto newEventData)
    {
        ValidateRequestEvent(newEventData);

        var createdEvent = _repository.AddEvent(newEventData);

        if (createdEvent == null)
            throw new ExternalException("Failed to create event");

        return createdEvent;
    }

    public void UpdateEvent(EventDto newEventData, int id)
    {
        ValidateRequestEvent(newEventData);

        if (GetEvent(id) == null)
            throw new EventNotFoundException(id);

        if (!_repository.UpdateEvent(newEventData, id))
            throw new ExternalException("Failed to update event");
    }

    public void DeleteEvent(int id)
    {
        if (GetEvent(id) == null)
            throw new EventNotFoundException(id);

        if (!_repository.DeleteEvent(id))
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