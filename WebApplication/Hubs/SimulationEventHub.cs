using Elevator.Core.Models;
using Elevator.Core.Services.Simulation.Manager;
using Elevator.WebApp.Services;
using Microsoft.AspNetCore.SignalR;

namespace Elevator.WebApp.Hubs
{
    public class SimulationEventHub : Hub
    {
        private readonly ConnectionPool _connectionPool;
        private readonly ISimulationCoordinator _simulationCoordinator;

        public SimulationEventHub(ConnectionPool connectionPool, ISimulationCoordinator simulationCoordinator)
        {
            _connectionPool = connectionPool;
            _simulationCoordinator = simulationCoordinator;
        }

        public override async Task OnConnectedAsync()
        {
            if (_connectionPool.TryGetValue(Context.ConnectionId, out var simulationId)
                && _simulationCoordinator.GetSimulation(simulationId) is SimulationManager simulation)
            {
                if (!simulation.IsRunning)
                {
                    await simulation.StartAsync();
                    return;
                }
                else
                {
                    // already running do nothing.
                    return;
                }
            }
            var query = Context.GetHttpContext()?.Request.Query;

            if (query != null
                && query.TryGetValue("f", out var rawFloor) && int.TryParse(rawFloor, out var floors)
                && query.TryGetValue("e", out var rawElevators) && int.TryParse(rawElevators, out var elevators)
                && query.TryGetValue("c", out var rawCapacity) && int.TryParse(rawCapacity, out var capacity)
                && query.TryGetValue("p", out var rawPeople) && int.TryParse(rawPeople, out var people)
                && query.TryGetValue("t", out var rawTimeout) && int.TryParse(rawTimeout, out var timeout))
            {
                if (_connectionPool.TryAdd(Context.ConnectionId, Context.ConnectionId))
                {
                    var manager = _simulationCoordinator.CreateSimulationManager(Context.ConnectionId, floors, elevators, capacity);

                    for (int i = 0; i < people; i++)
                    {
                        var person = Person.CreateRandom(floors);
                        manager.Building.AddPersonToFloor(person, person.CurrentFloor);
                    }

                    await _simulationCoordinator.AddSimulationAsync(Context.ConnectionId, manager, TimeSpan.FromSeconds(timeout));
                    
                    await base.OnConnectedAsync();

                    await Clients.Caller.SendAsync("Connected", Context.ConnectionId);
                    return;
                }
            }

            Context.Abort();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await _simulationCoordinator.RemoveSimulationAsync(Context.ConnectionId);

            await base.OnDisconnectedAsync(exception);
        }
    }
}
