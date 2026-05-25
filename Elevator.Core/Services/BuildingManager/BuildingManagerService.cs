using Elevator.Core.Models;
using Elevator.Core.Models.Requests;
using Elevator.Core.Services.ElevatorService;
using Elevator.Core.Services.EventBus;
using Elevator.Core.Services.FloorManager;
using Elevator.Core.Services.RequestProcessor;
using Elevator.Core.Services.Scheduler;
using Elevator.Core.Services.Simulation;
using System.Collections.Concurrent;

namespace Elevator.Core.Services.BuildingManager
{
    public class BuildingManagerService : IBuildingManagerService, ISimulationStart
    {
        public string SimulationId => _simulationId;
        public int FloorCount => _floors.Count;
        public IReadOnlyList<Lift> Elevators => _elevators.Values
            .ToList()
            .AsReadOnly();

        public IReadOnlyDictionary<int, Floor> Floors => _floors
            .ToDictionary() // immediately snapshot the current floors to avoid concurrent modification issues
            .AsReadOnly();

        private readonly ConcurrentDictionary<int, Lift> _elevators;
        private readonly ConcurrentDictionary<int, Floor> _floors;
        private readonly ConcurrentQueue<BaseRequest> _requestQueue;
        private readonly IEventBus _eventBus;
        private readonly IScheduler _scheduler;
        private readonly IRequestProcessor _requestProcessor;
        private readonly string _simulationId;
        private bool _isRunning;
        private Task? _requestProcessorTask;
        private readonly object _lock = new();
        private int _nextElevatorId;
        private readonly BuildingContext sharedContext;

        public BuildingManagerService(string simulationId, int floorCount, int elevatorCount, int elevatorCapacity, IEventBus eventBus, IScheduler? scheduler = null)
        {
            if (floorCount < 2)
                throw new ArgumentException("Building must have at least 2 floors", nameof(floorCount));
            if (elevatorCount < 1)
                throw new ArgumentException("Building must have at least 1 elevator", nameof(elevatorCount));

            _eventBus = eventBus;
            _scheduler = scheduler ?? new ShortestWaitTimeScheduler();
            _requestQueue = new ConcurrentQueue<BaseRequest>();
            _elevators = new ConcurrentDictionary<int, Lift>();
            _floors = new ConcurrentDictionary<int, Floor>();
            _simulationId = simulationId;
            _nextElevatorId = 1;
            sharedContext = new BuildingContext(this, _eventBus);
            _requestProcessor = new BuildingRequestProcessor(sharedContext);

            // Create floors
            for (int i = 1; i <= floorCount; i++)
            {
                _floors.TryAdd(i, new Floor(sharedContext, i));
            }

            // Create elevators
            int j = 0;
            for (; j < elevatorCount; j++)
            {
                var lift = new Lift(SimulationId, j + 1, elevatorCapacity, floorCount, sharedContext);
                _elevators.TryAdd(j + 1, new Lift(SimulationId, j + 1, elevatorCapacity, floorCount, sharedContext));
            }
            _nextElevatorId = j;

        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _isRunning = true;
            // Start all elevators
            var elevatorTasks = _elevators.ToList()
                .Select(e => e.Value.StartAsync(cancellationToken)).ToList();

            // Start request processor
            _requestProcessorTask = ProcessRequestsAsync(cancellationToken);

            try
            {
                await Task.WhenAll(elevatorTasks.Union(new[] { _requestProcessorTask }));
            }
            finally
            {
                _isRunning = false;
                foreach (var elevator in _elevators.ToList().Select(x => x.Value))
                    elevator.Stop();
            }
        }

        /// <summary>
        /// Get the current scheduler.
        /// </summary>
        public IScheduler GetScheduler() => _scheduler;

        private async Task ProcessRequestsAsync(CancellationToken cancellationToken)
        {
            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                if (_requestQueue.TryDequeue(out var request))
                {
                    try
                    {
                        await _requestProcessor.ProcessAsync(request, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue processing
                        System.Diagnostics.Debug.WriteLine($"Error processing request: {ex.Message}");
                    }
                }
                else
                {
                    await Task.Delay(50, cancellationToken);
                }

                RandomlyAddPeopleToQueueOnFloor();
            }
        }
        public void Stop()
        {
            _isRunning = false;
        }

        private static Random Random = new();
        private void RandomlyAddPeopleToQueueOnFloor()
        {
            foreach (var floor in _floors.Values)
            {
                if (floor.UpQueueCount == 0 && floor.DownQueueCount == 0
                    && floor.IdlePeopleCount != 0)
                {
                    foreach (var person in floor.GetAllPeople())
                    {
                        var nextFloor = person.GetNextFloor();
                        if (nextFloor.HasValue)
                        {
                            bool addToQueue = (Random.Next(1, 4) % 3 == 0);
                            if (addToQueue)
                            {
                            }
                        }
                    }
                }
            }
        }
        public void EnqueueBuildingRequest(BaseRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            _requestQueue.Enqueue(request);
        }

        public Lift? GetElevator(int elevatorId)
        {
            lock (_lock)
            {
                return Elevators.FirstOrDefault(e => e.Id == elevatorId);
            }
        }
        public void AddFloor()
        {
            lock (_lock)
            {
                int nextFloorNumber = _floors.Count + 1;
                var newFloor = new Floor(sharedContext, nextFloorNumber);
                _floors.TryAdd(nextFloorNumber, newFloor);

                // Update all elevators with new floor count
                foreach (var elevator in Elevators)
                {
                    elevator.UpdateFloorCount(nextFloorNumber);
                }
            }
        }
        public void AddPersonToFloor(Person person, int floorNumber)
        {
            lock (_lock)
            {
                if (!_floors.TryGetValue(floorNumber, out var floor))
                    throw new ArgumentException($"Floor {floorNumber} does not exist", nameof(floorNumber));

                person.MoveToFloor(floorNumber);
                floor.AddPerson(person);
            }
        }
        public Floor? GetFloor(int floorNumber)
        {
            _floors.TryGetValue(floorNumber, out var floor);
            return floor;
        }
        public void RemoveFloor(int floorId)
        {
            _floors.TryRemove(floorId, out _);

            // Update all elevators with new floor count
            foreach (var elevator in Elevators)
            {
                elevator.UpdateFloorCount(FloorCount);
            }
        }
        public Lift AddElevator(int capacity)
        {
            var newElevator = new Lift(SimulationId, _nextElevatorId++, capacity, FloorCount, sharedContext);
            _elevators.AddOrUpdate(_nextElevatorId, newElevator, (_, _) => newElevator);


            return newElevator;
        }
        public bool RemoveElevator(int elevatorId)
        {
            if (_elevators.TryRemove(elevatorId, out var elevator))
            {
                elevator.Stop();
                return true;
            }
            return false;
        }
        public bool ModifyElevatorCapacity(int elevatorId, int newCapacity)
        {
            if (newCapacity < 1)
                throw new ArgumentException("Capacity must be at least 1", nameof(newCapacity));

            if (_elevators.TryGetValue(elevatorId, out var elevator))
            {
                elevator.UpdateCapacity(newCapacity);
                return true;
            }
            return false;
        }

        public void Dispose()
        {
            foreach (var elevator in _elevators.Values)
            {
                elevator.Stop();
            };
            _floors.Clear();
            _elevators.Clear();

            Stop();
        }
    }

}
