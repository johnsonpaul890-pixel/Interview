using Elevator.Core.Models;
using Elevator.Core.Models.Enums;
using Elevator.Core.Models.Events;
using Elevator.Core.Models.Requests;
using Elevator.Core.Services.BuildingManager;
using Elevator.Core.Services.EventBus;
using Elevator.Core.Services.FloorManager;
using System.Collections.Concurrent;

namespace Elevator.Core.Services.ElevatorService
{
    public class Lift : ILift
    {
        public const int TransitTimePerFloorMs = 500;
        public const int DoorsMoveTimePerMs = 100;
        public const int PassengerMovementTimePerMs = 200;

        public int Id { get; }
        public int CurrentFloor { get; private set; }
        public Direction Direction { get; private set; }
        public int PassengerCount => _people.Count;
        public int Capacity { get; private set; }
        public ElevatorStatus Status { get; private set; }
        public int FloorCount => _floorCount;
        public int[] Destinations => _destinationFloors.Keys.Order().ToArray();

        private readonly IEventBus _eventBus;
        private readonly IBuildingManagerService _building;
        private readonly ConcurrentDictionary<int, bool> _destinationFloors = new();
        private readonly SortedSet<int> _upDestinations = new();
        private readonly SortedSet<int> _downDestinations = new();
        private readonly ConcurrentDictionary<string, Person> _people = new();
        private int _floorCount;
        private bool _isRunning;
        private readonly string _simulationId;
        private readonly object _stateLock = new();

        public Lift(string simulationId, int id, int capacity, int floorCount, BuildingContext context)
        {
            Id = id;
            Capacity = capacity;
            _floorCount = floorCount;
            _eventBus = context.EventBus;
            _building = context.Building;
            CurrentFloor = Random.Shared.Next(1, floorCount); // spread randomly at start
            Direction = Direction.Idle;
            Status = ElevatorStatus.Idle;
            _simulationId = simulationId;
        }

        public async Task AssignRequestAsync(ElevatorMoveRequest request)
        {
            if (request.FromFloor != CurrentFloor)
            {
                if (request.FromFloor < CurrentFloor)
                    _downDestinations.Add(request.FromFloor);
                else
                    _upDestinations.Add(request.FromFloor);
            }

            _destinationFloors.TryAdd(request.FromFloor, true);

            if (request.ToFloor < request.FromFloor)
                _downDestinations.Add(request.ToFloor);
            else
                _upDestinations.Add(request.ToFloor);

            if (Status == ElevatorStatus.Idle)
            {
                Status = ElevatorStatus.Queued;
            }
            await _eventBus.PublishAsync(new RequestQueuedEvent(
                _simulationId,
                Id,
                request.FromFloor,
                request.ToFloor,
                _destinationFloors.Count,
                DateTime.UtcNow));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _isRunning = true;

            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                // If no destinations, stay idle
                if (_destinationFloors.IsEmpty)
                {
                    Status = ElevatorStatus.Idle;
                    Direction = Direction.Idle;
                    if (PassengerCount == 0)
                    {
                        await Task.Delay(1000, cancellationToken); // Check every second for new requests
                    }
                    continue;
                }

                // Determine direction if idle
                if (Direction == Direction.Idle)
                {
                    if (_destinationFloors.Keys.FirstOrDefault() is int destinationFloor)
                    {
                        Direction = destinationFloor > CurrentFloor ? Direction.Up : Direction.Down;
                    }
                }

                // Move to next floor
                int nextFloor = GetNextFloor();
                if (nextFloor != CurrentFloor)
                {
                    await _eventBus.PublishAsync(new ElevatorMovedEvent(
                        _simulationId,
                        Id, CurrentFloor, nextFloor, Direction, DateTime.UtcNow));

                    await Task.Delay(TransitTimePerFloorMs * Math.Abs(nextFloor - CurrentFloor) , cancellationToken);

                    CurrentFloor = nextFloor;
                }

                // Handle doors and passenger exchange
                await HandleFloorAsync(cancellationToken);
            }

            _isRunning = false;
        }

        private int GetNextFloor()
        {
            if (Direction == Direction.Up)
            {
                if (_upDestinations.Count > 0)
                    return Math.Min(CurrentFloor + 1, _floorCount);

                if (_downDestinations.Count > 0)
                {
                    Direction = Direction.Down;
                    return Math.Max(CurrentFloor - 1, 1);
                }
            }
            else if (Direction == Direction.Down)
            {
                if (_downDestinations.Count > 0)
                    return Math.Max(CurrentFloor - 1, 1);

                if (_upDestinations.Count > 0)
                {
                    Direction = Direction.Up;
                    return Math.Min(CurrentFloor + 1, _floorCount);
                }
            }

            return CurrentFloor;
        }

        private async Task HandleFloorAsync(CancellationToken cancellationToken)
        {
            var floor = _building.Floors[CurrentFloor];

            // People exit
            var peopleToExit = GetPeople().Where(p => p.GetNextFloor() == CurrentFloor).ToList();
            var stopAtFloor = peopleToExit.Count > 0 || floor.DownQueueCount + floor.UpQueueCount > 0;

            if (stopAtFloor)
            {
                await _eventBus.PublishAsync(new ElevatorArrivedEvent(_simulationId, Id, CurrentFloor, DateTime.UtcNow));
                await Task.Delay(DoorsMoveTimePerMs, cancellationToken);

                Status = ElevatorStatus.DoorsOpen;

                await _eventBus.PublishAsync(new DoorsOpenedEvent(_simulationId, Id, CurrentFloor, DateTime.UtcNow));

                int exiting = peopleToExit.Count;
                if (exiting > 0)
                {
                    await HandleExitingAsync(floor, peopleToExit, cancellationToken);
                }

                if (PassengerCount == 0)
                {
                    Direction = Direction.Idle;
                }

                (int waitingNumber, Direction direction) = (Direction) switch
                {
                    Direction.Up => (floor.UpQueueCount, Direction.Up),
                    Direction.Down => (floor.DownQueueCount, Direction.Down),
                    _ => floor.DownQueueCount >= floor.UpQueueCount ? (floor.DownQueueCount, Direction.Down) : (floor.UpQueueCount, Direction.Up),
                };

                int enteringCapacity = Math.Max(0, Capacity - PassengerCount);
                if (enteringCapacity > 0 && waitingNumber > 0)
                {
                    await HandleEntering(floor, enteringCapacity, waitingNumber, direction, cancellationToken);
                }

                if (PassengerCount != 0)
                {
                    Direction = direction;
                }

                await Task.Delay(DoorsMoveTimePerMs, cancellationToken);

                Status = ElevatorStatus.DoorsClosed;
                await _eventBus.PublishAsync(new DoorsClosedEvent(_simulationId, Id, CurrentFloor, DateTime.UtcNow));
            }
        }

        private async Task HandleExitingAsync(Floor floor, List<Person> peopleToExit, CancellationToken cancellationToken)
        {
            foreach (var person in peopleToExit)
            {
                if (ExitPerson(person))
                {
                    person.SetExiting();
                    floor.PersonArrived(person);

                    await _eventBus.PublishAsync(new PersonExitedElevatorEvent(_simulationId,
                        person.Id, Id, CurrentFloor, DateTime.UtcNow));
                    await Task.Delay(PassengerMovementTimePerMs, cancellationToken);
                }
            }

            await _eventBus.PublishAsync(new PassengersExitedEvent(_simulationId,
                Id, CurrentFloor, peopleToExit.Count, PassengerCount, DateTime.UtcNow));
        }

        private async Task HandleEntering(Floor floor, int enteringCapacity, int waitingNumber, Direction direction, CancellationToken cancellationToken)
        {
            int entering = 0;
            while (enteringCapacity > 0 && waitingNumber > 0)
            {
                var person = direction is Direction.Down
                    ? floor.DequeueDownPerson()
                    : floor.DequeueUpPerson()
                        ?? (direction is Direction.Idle
                            ? floor.DequeueDownPerson()
                            : null);
                if (person != null)
                {
                    if (BoardPerson(person))
                    {
                        person.SetInElevator();

                        enteringCapacity--;
                        waitingNumber--;
                        entering++;

                        await _eventBus.PublishAsync(new PersonBoardedElevatorEvent(_simulationId,
                            person.Id, Id, CurrentFloor, DateTime.UtcNow));
                        await Task.Delay(PassengerMovementTimePerMs, cancellationToken);
                    }
                }
            }
            await _eventBus.PublishAsync(new PassengersEnteredEvent(_simulationId,
                Id, CurrentFloor, entering, PassengerCount, DateTime.UtcNow));
        }

        /// <summary>
        /// Update the floor count when new floors are added to the building.
        /// </summary>
        public void UpdateFloorCount(int newFloorCount)
        {
            lock (_stateLock)
            {
                _floorCount = newFloorCount;
            }
        }

        /// <summary>
        /// Update the elevator's capacity.
        /// </summary>
        public void UpdateCapacity(int newCapacity)
        {
            lock (_stateLock)
            {
                if (newCapacity < 1)
                    throw new ArgumentException("Capacity must be at least 1", nameof(newCapacity));
                Capacity = newCapacity;
            }
        }

        /// <summary>
        /// removes person from the elevator.
        /// </summary>
        public bool RemovePerson(string personId)
        {
            if (_people.TryRemove(personId, out _))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Boards a person into the elevator.
        /// </summary>
        public bool BoardPerson(Person person)
        {
            if (PassengerCount >= Capacity)
                return false; // Elevator is full

            if (_people.TryAdd(person.Id, person))
            {
                AddPersonsFloorRequest(person);

                person.SetInElevator();
                return true;
            }
            return false;
        }

        public bool AddPersonsFloorRequest(Person person)
        {
            if (!_people.ContainsKey(person.Id))
                return false; // Person not in elevator
            var nextFloor = person.GetNextFloor();
            if (nextFloor.HasValue)
            {
                _destinationFloors.TryAdd(nextFloor.Value, true);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes a person from the elevator.
        /// </summary>
        public bool ExitPerson(Person person)
        {
            if (_people.TryRemove(person.Id, out _))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets all people currently in the elevator.
        /// </summary>
        public IReadOnlyCollection<Person> GetPeople()
        {
            return _people.ToList().Select(x => x.Value).ToList().AsReadOnly();
        }


        public void Stop() => _isRunning = false;
    }

}
