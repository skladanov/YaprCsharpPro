using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {

        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();

        services.AddHostedService<BookingProcessService>();

        return services;
    }
}
