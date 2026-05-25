using Elevator.Core.Models.Requests;
using Elevator.Core.Services.ElevatorService;

namespace Elevator.Core.Services.Scheduler
{
    public interface IScheduler
    {
        ILift? SelectElevator(IEnumerable<ILift> elevators, ElevatorMoveRequest request);
    }
}
