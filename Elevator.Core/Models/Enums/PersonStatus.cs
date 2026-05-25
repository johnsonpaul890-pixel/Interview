namespace Elevator.Core.Models.Enums
{
    /// <summary>
    /// Represents the status of a person in the elevator system.
    /// </summary>
    public enum PersonStatus
    {
        Idle,
        WaitingUp,
        WaitingDown,
        InElevator,
        Exiting
    }
}
