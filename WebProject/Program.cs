using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAutoMapper(typeof(EventMappingProfile));
builder.Services.AddSingleton<IEventService, EventService>();
builder.Services.AddSingleton<IEventRepository, LocalEventRepository>();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();
