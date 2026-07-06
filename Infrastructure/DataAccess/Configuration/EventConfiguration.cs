using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(e => e.Title)
            .HasColumnName("title")
            .IsRequired()
            .HasMaxLength(20);
        
        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(200);
        
        builder.Property(e => e.StartAt)
            .IsRequired()
            .HasColumnName("start_at");
        
        builder.Property(e => e.EndAt)
            .IsRequired()
            .HasColumnName("end_at");
        
        builder.Property(e => e.TotalSeats)
            .IsRequired()
            .HasColumnName("total_seats");
        
        builder.Property(e => e.AvailableSeats)
            .HasColumnName("available_seats");
        
        builder.HasMany(e => e.Bookings)
            .WithOne(b => b.Event)
            .HasForeignKey(b => b.EventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
    
}