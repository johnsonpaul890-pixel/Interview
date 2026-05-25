using Elevator.Core.Models;
using Elevator.Core.Models.Requests;
using Elevator.Core.Services.BuildingManager;
using Elevator.Core.Services.ElevatorService;
using Elevator.Core.Services.EventBus;
using Elevator.Core.Services.FloorManager;
using Elevator.Core.Services.Scheduler;

namespace Elevator.Core.Services.Simulation.Manager
{
    /// <summary>
    /// Manages the elevator simulation lifecycle and provides a fluent API for configuration and interaction.
    /// Simplifies starting, configuring, and stopping the simulation.
    /// </summary>
    public class SimulationManager : IDisposable
    {
        private readonly ElevatorSimulationService _service;
        private readonly IBuildingManagerService _building;
        private readonly IEventBus _eventBus;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _simulationTask;
        private bool _isRunning;
        private bool _hasStarted;

        /// <summary>
        /// Gets the underlying building (for direct access if needed).
        /// </summary>
        public IBuildingManagerService Building => _building;

        /// <summary>
        /// Gets the event bus (for direct subscription if needed).
        /// </summary>
        public IEventBus EventBus => _eventBus;

        /// <summary>
        /// Gets whether the simulation is currently running.
        /// </summary>
        public bool IsRunning => _isRunning && _cancellationTokenSource?.IsCancellationRequested != true;

        /// <summary>
        /// Gets whether the simulation is has started.
        /// </summary>
        public bool HasStarted => _hasStarted || _cancellationTokenSource is null;

        /// <summary>
        /// Creates a new simulation manager with a custom scheduler and eventBus.
        /// </summary>
        public static SimulationManager Create(string simulationId, int floorCount, int elevatorCount, int elevatorCapacity, IEventBus eventBus)
        {
            return new SimulationManager(simulationId, floorCount, elevatorCount, elevatorCapacity, eventBus: eventBus);
        }

        private SimulationManager(string? simulationId, int floorCount, int elevatorCount, int elevatorCapacity, IScheduler? scheduler = null, IEventBus? eventBus = null)
        {
            if (floorCount < 2)
                throw new ArgumentException("Building must have at least 2 floors", nameof(floorCount));
            if (elevatorCount < 1)
                throw new ArgumentException("Building must have at least 1 elevator", nameof(elevatorCount));
            if (elevatorCapacity < 1)
                throw new ArgumentException("Elevator capacity must be at least 1", nameof(elevatorCapacity));

            _service = new ElevatorSimulationService(simulationId, floorCount, elevatorCount, elevatorCapacity, scheduler, eventBus);
            _building = (BuildingManagerService)_service.GetBuilding();
            _eventBus = _service.EventBus;
            _isRunning = false;
            _hasStarted = false;
        }

        /// <summary>
        /// Starts the simulation asynchronously.
        /// </summary>
        public async Task StartAsync(CancellationToken? externalToken = null)
        {
            if (_simulationTask != null)
                throw new InvalidOperationException("Simulation is already running");

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(externalToken ?? CancellationToken.None);
            _isRunning = true; 
            _hasStarted= true;

            _simulationTask = _service.StartSimulationAsync(_cancellationTokenSource.Token);

            // Don't await here - let simulation run in background
            // User can await later if needed
        }

        /// <summary>
        /// Stops the simulation gracefully and waits for it to complete.
        /// </summary>
        public async Task StopAsync()
        {
            if (!_isRunning)
                return;

            _cancellationTokenSource?.Cancel();
            _isRunning = false;

            if (_simulationTask != null)
            {
                try
                {
                    await _simulationTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                }
            }

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _simulationTask = null;

            Dispose();
        }

        /// <summary>
        /// Waits for the simulation to complete (useful if started without waiting).
        /// </summary>
        public async Task WaitForCompletionAsync()
        {
            if (_simulationTask == null)
                throw new InvalidOperationException("Simulation has not been started");

            try
            {
                await _simulationTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when stopped
            }
        }

        /// <summary>
        /// Enqueues request to the building.
        /// </summary>
        public SimulationManager EnqueueRequest<T>(T request) where T : BaseRequest 
        {
            _building.EnqueueBuildingRequest(request);
            return this;
        }

        /// <summary>
        /// Gets statistics about the simulation state.
        /// </summary>
        public SimulationStatistics GetStatistics()
        {
            var building = _building;
            int totalPeopleOnFloor = 0;
            int totalPassengers = 0;
            int totalQueued = 0;
            double waitTimeSum = 0;
            var date = DateTime.UtcNow;
            foreach (var floor in building.Floors.Values)
            {
                totalPeopleOnFloor += floor.PeopleCount;
                totalQueued += floor.UpQueueCount + floor.DownQueueCount;

                waitTimeSum += floor.GetAllPeople()
                    .Sum(p => p.QueueTimeSpans
                        .Sum(x => ((x.Item2 ?? date) - x.Item1).TotalSeconds));
            }
            foreach (var elevator in building.Elevators)
            {
                totalPassengers += elevator.PassengerCount;
                waitTimeSum += elevator.GetPeople()
                    .Sum(p => p.QueueTimeSpans
                        .Sum(x => ((x.Item2 ?? date) - x.Item1).TotalSeconds));
            }

            return new SimulationStatistics
            {
                FloorCount = building.FloorCount,
                ElevatorCount = building.Elevators.Count,
                TotalPeople = totalPeopleOnFloor + totalPassengers,
                TotalPeopleOnFloor = totalPeopleOnFloor,
                TotalPassengers = totalPassengers,
                TotalQueued = totalQueued,
                TotalIdle = totalPeopleOnFloor - totalQueued,
                AverageWaitTime = waitTimeSum / totalPeopleOnFloor,
                IsRunning = _isRunning
            };
        }

        /// <summary>
        /// Prints statistics to console.
        /// </summary>
        public SimulationManager PrintStatistics()
        {
            var stats = GetStatistics();
            Console.WriteLine($"\n--- Simulation Statistics ---");
            Console.WriteLine($"Floors: {stats.FloorCount}");
            Console.WriteLine($"Elevators: {stats.ElevatorCount}");
            Console.WriteLine($"Total People: {stats.TotalPeople}");
            Console.WriteLine($"Queued: {stats.TotalQueued}");
            Console.WriteLine($"Idle: {stats.TotalIdle}");
            Console.WriteLine($"Running: {stats.IsRunning}");
            Console.WriteLine($"-----------------------------\n");
            return this; // Fluent return
        }

        /// <summary>
        /// Disposes resources used by the simulation.
        /// </summary>
        public void Dispose()
        {
            try
            {
                Building.Dispose();
                StopAsync().GetAwaiter().GetResult();
            }
            catch
            {
                // Ignore errors during disposal
            }
        }
    }
}
