using Elevator.Core.Models;

namespace Elevator.Core.Services.FloorManager
{
    /// <summary>
    /// Represents a floor in the building.
    /// Manages people queued for elevators in up/down directions, and idle people.
    /// Autonomously calls elevators when people are waiting.
    /// </summary>
    public interface IFloor
    {
        int Number { get; }
        int PeopleCount { get; }
        int UpQueueCount { get; }
        int DownQueueCount { get; }
        int IdlePeopleCount { get; }

        /// <summary>
        /// Adds a person to the floor. If they need to move, queues them in appropriate direction.
        /// </summary>
        void AddPerson(Person person);

        /// <summary>
        /// Removes a person from the floor (by any means).
        /// </summary>
        bool RemovePerson(string personId);

        /// <summary>
        /// Called when a person exits an elevator on this floor.
        /// Moves them from in-elevator state to appropriate queuing state or idle state.
        /// </summary>
        void PersonArrived(Person person);

        /// <summary>
        /// Called when a person boards an elevator on this floor.
        /// </summary>
        void PersonBoarded(Person person);

        /// <summary>
        /// Peeks at the first person in the up queue without removing them.
        /// </summary>
        Person? TryPeekFirstUpPerson();

        /// <summary>
        /// Peeks at the first person in the down queue without removing them.
        /// </summary>
        Person? TryPeekFirstDownPerson();

        /// <summary>
        /// Removes and returns the first person from the up queue.
        /// </summary>
        Person? DequeueUpPerson();

        /// <summary>
        /// Removes and returns the first person from the down queue.
        /// </summary>
        Person? DequeueDownPerson();

        /// <summary>
        /// Gets all people currently on this floor (queued + idle).
        /// </summary>
        IReadOnlyCollection<Person> GetAllPeople();
    }
}
