using Elevator.Core.Models;
using Elevator.Core.Models.Requests;
using Elevator.Core.Services.BuildingManager;
using System.Collections.Concurrent;

namespace Elevator.Core.Services.FloorManager
{
    public class Floor : IFloor
    {
        private readonly BuildingContext _context;
        private readonly ConcurrentQueue<Person> _upQueue;
        private readonly ConcurrentQueue<Person> _downQueue;
        private readonly ConcurrentDictionary<string, Person> _idlePeople;
        private readonly object _lock = new();

        public int Number { get; }

        public int PeopleCount => UpQueueCount + DownQueueCount + IdlePeopleCount;
        public int UpQueueCount => _upQueue.Count;
        public int DownQueueCount => _downQueue.Count;
        public int IdlePeopleCount => _idlePeople.Count;

        public Floor(BuildingContext context, int floorNumber)
        {
            if (floorNumber < 1)
                throw new ArgumentException("Floor number must be >= 1", nameof(floorNumber));

            Number = floorNumber;
            _context = context;
            _upQueue = new ConcurrentQueue<Person>();
            _downQueue = new ConcurrentQueue<Person>();
            _idlePeople = new ConcurrentDictionary<string, Person>();
        }

        public void AddPerson(Person person)
        {
            lock (_lock)
            {
                if (person.CurrentFloor != Number)
                    throw new InvalidOperationException($"Person is on floor {person.CurrentFloor}, not floor {Number}");

                // Check if person needs to move
                var nextFloor = person.GetNextFloor();
                if (!nextFloor.HasValue)
                {
                    // Person has completed their journey, add to idle
                    _idlePeople.TryAdd(person.Id, person);
                    person.SetExiting();
                }
                else if (nextFloor > Number)
                {
                    // Person needs to go up
                    person.SetWaitingUp();
                    _upQueue.Enqueue(person);
                    CallElevatorIfNeeded(nextFloor.Value);
                }
                else if (nextFloor < Number)
                {
                    // Person needs to go down
                    person.SetWaitingDown();
                    _downQueue.Enqueue(person);
                    CallElevatorIfNeeded(nextFloor.Value);
                }
                else
                {
                    // Next floor is current floor (shouldn't happen, but handle it)
                    _idlePeople.TryAdd(person.Id, person);
                    person.SetExiting();
                }
            }
        }

        public bool RemovePerson(string personId)
        {
            if (string.IsNullOrEmpty(personId))
                throw new ArgumentException("Person ID cannot be null or empty", nameof(personId));

            lock (_lock)
            {
                // Try to remove from idle people first
                if (_idlePeople.TryRemove(personId, out _))
                    return true;

                bool removed = false;
                // Try to remove from queues (by reconstructing them without the person)
                var upList = new List<Person>();
                while (_upQueue.TryDequeue(out var person))
                {
                    if (person.Id != personId)
                    {  
                        upList.Add(person);
                    }
                    else
                    {
                        removed = true;
                    }
                }
                foreach (var p in upList)
                    _upQueue.Enqueue(p);

                var downList = new List<Person>();
                while (_downQueue.TryDequeue(out var person))
                {
                    if (person.Id != personId)
                    {
                        downList.Add(person);
                    }
                    else
                    {
                        removed = true;
                    }
                }
                foreach (var p in downList)
                    _downQueue.Enqueue(p);

                return removed;
            }
        }

        public void PersonArrived(Person person)
        {
            if (person == null)
                throw new ArgumentNullException(nameof(person));

            lock (_lock)
            {
                person.MoveToFloor(Number);
                person.DequeueNextFloor(); // Remove the floor we just arrived at

                // Check if person needs to move again
                var nextFloor = person.GetNextFloor();
                if (!nextFloor.HasValue)
                {
                    // Journey complete
                    _idlePeople.TryAdd(person.Id, person);
                }
                else if (nextFloor > Number)
                {
                    // Need to go up
                    person.SetWaitingUp();
                    _upQueue.Enqueue(person);
                    CallElevatorIfNeeded(nextFloor.Value);
                }
                else if (nextFloor < Number)
                {
                    // Need to go down
                    person.SetWaitingDown();
                    _downQueue.Enqueue(person);
                    CallElevatorIfNeeded(nextFloor.Value);
                }
            }
        }

        public void PersonBoarded(Person person) => person.SetInElevator();

        public Person? TryPeekFirstUpPerson()
        {
            _upQueue.TryPeek(out var person);
            return person;
        }

        public Person? TryPeekFirstDownPerson()
        {
            _downQueue.TryPeek(out var person);
            return person;
        }
        public Person? DequeueUpPerson()
        {
            _upQueue.TryDequeue(out var person);
            return person;
        }

        public Person? DequeueDownPerson()
        {
            _downQueue.TryDequeue(out var person);
            return person;
        }

        public IReadOnlyCollection<Person> GetAllPeople()
        {
            var all = new List<Person>();
            all.AddRange(_upQueue);
            all.AddRange(_downQueue);
            all.AddRange(_idlePeople.Values);
            return all.AsReadOnly();
        }

        private void CallElevatorIfNeeded(int nextFloor)
        {
            _context.Building.EnqueueBuildingRequest(new ElevatorMoveRequest(Number, nextFloor));
        }

        public override string ToString() => $"Floor {Number} (People: {PeopleCount}, Up: {UpQueueCount}, Down: {DownQueueCount}, Idle: {IdlePeopleCount})";
    }
}
