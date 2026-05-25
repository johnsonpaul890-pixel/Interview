using Elevator.ConsoleClient;
using Elevator.Core.Services.Simulation;
using Elevator.Core.Utility;

int floorCount = ReadInt("How Many Floors");
int elevatorCount = ReadInt("How Many Elevators");
int elevatorCapacity = ReadInt("Elevator's Capacity");
int stopTime = ReadInt("Simulation Runtime (seconds)");

var service = new ElevatorSimulationService(
    Guid.NewGuid().ToString(),
    floorCount,
    elevatorCount,
    elevatorCapacity);

var building = service.GetBuilding();
var eventBus = service.EventBus;

// Subscribe to events
var consoleHandler = new ConsoleEventHandler();
eventBus.SubscribeAll(consoleHandler);

// Generate random requests
var requestGenerator = new RandomRequestGenerator(building, floorCount);

using var cts = new CancellationTokenSource();

// Start simulation
var simTask = service.StartSimulationAsync(cts.Token);

// Generate requests for 30 seconds
var generatorTask = requestGenerator.GenerateRequestsAsync(30, cts.Token);

await Task.Delay(stopTime * 1000);
cts.Cancel();

try
{
    await Task.WhenAll(simTask, generatorTask);
}
catch (OperationCanceledException)
{
    // Expected
}

Console.WriteLine("\n\nSimulation completed.");

static int ReadInt(string prompt)
{
    int value = 0;
    Console.WriteLine($"{prompt}:");
    while (Console.ReadLine() is string rawValue 
        && int.TryParse(rawValue, out value) == false)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"{rawValue} is not a valid integer. Try again.");
        Console.ResetColor();
    }
    return value;
}