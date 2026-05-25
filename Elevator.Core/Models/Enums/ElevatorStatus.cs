namespace Elevator.Core.Models.Enums
{
    [Flags]
    public enum ElevatorStatus
    {
        Broken = int.MinValue,
        Idle = 1<<0,
        Moving = 1 << 1,
        DoorsOpen = 1 << 2,
        DoorsClosed = 1 << 3,
        Queued = 1 << 4,

        Available = Moving | Idle,
    }
}
