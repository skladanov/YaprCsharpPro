using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAutoMapper(cfg=> { },typeof(EventMappingProfile));
builder.Services.AddSingleton<IEventService, EventService>();
builder.Services.AddSingleton<IBookingService, BookingService>();
builder.Services.AddSingleton<IEventRepository, LocalEventRepository>();
builder.Services.AddSingleton<IBookingRepository, LocalBookingRepository>();
builder.Services.AddHostedService<BookingProcessService>();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();
