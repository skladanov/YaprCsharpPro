using AutoMapper;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WebProject.DataAccess; 

public class EventRepository : IEventRepository
{
    private readonly AppDbContext _context;

    public EventRepository(AppDbContext context)
    {
        _context = context;
    }
    public async Task<ICollection<Event>> GetAllEventsAsync(Expression<Func<Event, bool>> predicate, CancellationToken token)
    {
        return await _context.events.AsQueryable().Where(predicate).ToListAsync(token);
    }

    public async Task<Event?> GetEventAsync(Guid eventId, CancellationToken token)
    {
        return await _context.events.Where(e => e.Id == eventId).FirstOrDefaultAsync(token);
    }

    public async Task AddEventAsync(Event eventItem, CancellationToken token)
    {
        _context.events.AddAsync(eventItem);
        await _context.SaveChangesAsync(token);
    }

    public async Task UpdateEventAsync(Event eventItem, CancellationToken token)
    {
        _context.events.Update(eventItem);
        await _context.SaveChangesAsync(token);
    }

    public async Task DeleteEventAsync(Event existingEvent, CancellationToken token)
    {
        _context.events.Remove(existingEvent!);
        await _context.SaveChangesAsync(token);
    }
}