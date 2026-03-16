using AutoMapper;

public class EventService : IEventService
{
    private readonly IEventRepository _repository;

    public EventService(IEventRepository repository) 
    {
        _repository = repository;
    }
    public PaginatedResult<Event> GetAllEvents(int page, int pageSize, string? title, DateTime? from, DateTime? to)
    {
        var query = _repository.GetAllEvents().AsQueryable();

        if (!string.IsNullOrEmpty(title))
            query = query.Where(e => e.Title.Contains(title, StringComparison.OrdinalIgnoreCase));

        if (from.HasValue)
            query = query.Where(e => e.StartAt >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.EndAt <= to.Value);

        var totalCount = query.Count();
        var offset = (page - 1) * pageSize;

        var items = query
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
        return _repository.GetEvent(id);
    }
    public Event AddEvent(EventDto newEventData)
    {
        return _repository.AddEvent(newEventData);
    }
    public bool UpdateEvent(EventDto newEventData, int id)
    {
        return _repository.UpdateEvent(newEventData, id);
    }
    public bool DeleteEvent(int id)
    {
        return _repository.DeleteEvent(id);
    }
}