using AutoMapper;

public class EventMappingProfile : Profile
{
    public EventMappingProfile()
    {
        CreateMap<EventDto, Event>();
        CreateMap<Event, EventDto>();
    }
}