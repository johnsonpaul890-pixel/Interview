using Elevator.Core.Services.EventBus;
using Elevator.Hosting;
using Elevator.WebApp.Dispatchers;
using Elevator.WebApp.Hubs;
using Elevator.WebApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddSingleton<ConnectionPool>();
builder.Services.AddSignalR();
builder.Services.AddHttpLogging(_ => { });
builder.Services.AddElevatorSimulation<IEventHandler, EventDispatcher>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseHttpLogging();
app.UseRouting();
app.MapControllers();
app.MapRazorPages();
app.MapHub<SimulationEventHub>("/hub");

app.Run();
