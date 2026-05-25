using Elevator.Core.Models;
using Elevator.Core.Models.Enums;
using Elevator.Core.Models.Requests;

namespace Elevator.Core.Services.ElevatorService
{
    public interface ILift
    {
        int Id { get; }
        int CurrentFloor { get; }
        Direction Direction { get; }
        int PassengerCount { get; }
        int Capacity { get; }
        ElevatorStatus Status { get; }

        Task StartAsync(CancellationToken cancellationToken);
        Task AssignRequestAsync(ElevatorMoveRequest request);
    }
}
