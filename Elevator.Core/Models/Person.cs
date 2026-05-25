using Elevator.Core.Models.Enums;
using Elevator.Core.Utility;

namespace Elevator.Core.Models
{
    /// <summary>
    /// Represents a person in the elevator simulation.
    /// Each person has a destination path (list of floors to visit).
    /// </summary>
    public class Person
    {
        private readonly Queue<int> _remainingPath;
        private PersonStatus _status;
        private readonly object _lock = new();

        public string Id { get; }
        public int CurrentFloor { get; private set; }
        public PersonStatus Status => _status;
        public DateTime CreatedAt { get; }

        public List<(DateTime, DateTime?)> QueueTimeSpans { get; }

        /// <summary>
        /// Creates a new person with a specified travel path.
        /// </summary>
        /// <param name="startFloor">The floor where person starts</param>
        /// <param name="path">Queue of floors to visit (in order)</param>
        /// <exception cref="ArgumentException">If path is null or empty</exception>
        public Person(int startFloor, Queue<int> path)
        {
            if (path.Count == 0)
                throw new ArgumentException("Person must have at least one destination floor", nameof(path));
            if (startFloor < 1)
                throw new ArgumentException("Start floor must be >= 1", nameof(startFloor));

            Id = Guid.NewGuid().ToString();
            CurrentFloor = startFloor;
            _remainingPath = new Queue<int>(path);
            _status = PersonStatus.Idle;
            CreatedAt = DateTime.UtcNow;
            QueueTimeSpans = new List<(DateTime, DateTime?)>();
        }

        public static Person CreateRandom(int maxFloor)
        {
            var random = new Random();
            var randomPath = RandomPath.Create(maxFloor + 1).ToList();

            var path = new Queue<int>(RandomPath.Create(maxFloor + 1));
            int startFloor = random.Next(1, maxFloor + 1);
            while (startFloor == path.Peek())
            {
                startFloor = random.Next(1, maxFloor + 1);
            }
            return new Person(startFloor, path);
        }

        /// <summary>
        /// Gets the next floor in the person's path without removing it.
        /// Returns null if the path is empty (journey complete).
        /// </summary>
        public int? GetNextFloor()
        {
            lock (_lock)
            {
                return _remainingPath.Count > 0 ? _remainingPath.Peek() : null;
            }
        }

        /// <summary>
        /// Removes and returns the next floor from the path.
        /// Returns null if path is empty.
        /// </summary>
        public int? DequeueNextFloor()
        {
            lock (_lock)
            {
                return _remainingPath.Count > 0 ? _remainingPath.Dequeue() : null;
            }
        }

        /// <summary>
        /// Moves person to the specified floor and updates their status.
        /// </summary>
        public void MoveToFloor(int floor)
        {
            lock (_lock)
            {
                CurrentFloor = floor;
                _status = PersonStatus.Idle;
            }
        }

        /// <summary>
        /// Sets the person's status to WaitingUp.
        /// </summary>
        public void SetWaitingUp()
        {
            lock (_lock)
            {
                _status = PersonStatus.WaitingUp;
                QueueTimeSpans.Add((DateTime.UtcNow, null));
            }
        }

        /// <summary>
        /// Sets the person's status to WaitingDown.
        /// </summary>
        public void SetWaitingDown()
        {
            lock (_lock)
            {
                _status = PersonStatus.WaitingDown;
                QueueTimeSpans.Add((DateTime.UtcNow, null));
            }
        }

        /// <summary>
        /// Sets the person's status to InElevator.
        /// </summary>
        public void SetInElevator()
        {
            lock (_lock)
            {
                _status = PersonStatus.InElevator;
                if (QueueTimeSpans.Count > 0)
                {
                    QueueTimeSpans[^1] = (QueueTimeSpans[^1].Item1, DateTime.UtcNow);
                }
            }
        }

        /// <summary>
        /// Sets the person's status to Exiting.
        /// </summary>
        public void SetExiting()
        {
            lock (_lock)
            {
                _status = PersonStatus.Exiting;
            }
        }

        /// <summary>
        /// Returns true if the person has completed their journey.
        /// </summary>
        public bool HasCompletedJourney()
        {
            lock (_lock)
            {
                return _remainingPath.Count == 0;
            }
        }

        public override string ToString() => $"Person {Id[..8]} (Floor {CurrentFloor}, Status: {Status})";
    }
}
