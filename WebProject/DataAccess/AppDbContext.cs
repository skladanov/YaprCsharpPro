using Microsoft.EntityFrameworkCore;

namespace WebProject.DataAccess;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) :  base(options){}
    
    public DbSet<Booking>? bookings => Set<Booking>();
    public DbSet<Event>? events => Set<Event>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}