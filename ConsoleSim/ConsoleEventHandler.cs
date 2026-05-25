using Elevator.Core.Models.Events;
using Elevator.Core.Services.EventBus;

namespace Elevator.ConsoleClient
{

    public class ConsoleEventHandler :
        IEventHandler<ElevatorMovedEvent>,
        IEventHandler<ElevatorArrivedEvent>,
        IEventHandler<DoorsOpenedEvent>,
        IEventHandler<DoorsClosedEvent>,
        IEventHandler<PassengersEnteredEvent>,
        IEventHandler<PassengersExitedEvent>,
        IEventHandler<RequestQueuedEvent>,
        IEventHandler<ElevatorIdleEvent>
    {
        private readonly object _lockObject = new();

        private Task LockAndLog(ConsoleColor color, string log, int delay = 100)
        {
            lock (_lockObject)
            {
                Console.ForegroundColor = color;
                Console.WriteLine(log);
                Console.ResetColor();
            }
            return Task.Delay(100);
        }
        public Task HandleAsync(ElevatorMovedEvent @event) =>
            LockAndLog(ConsoleColor.Cyan, $"[{@event.Timestamp:HH:mm:ss.fff}] Elevator {@event.ElevatorId} moving {@event.Direction} | Floor {@event.FromFloor} -> {@event.ToFloor}");
        
        public Task HandleAsync(ElevatorArrivedEvent @event) =>
            LockAndLog(ConsoleColor.Cyan, $"[{@event.Timestamp:HH:mm:ss.fff}] Elevator {@event.ElevatorId} arrived at floor {@event.Floor}");
        public Task HandleAsync(DoorsOpenedEvent @event) => 
            LockAndLog(ConsoleColor.Yellow, $"[{@event.Timestamp:HH:mm:ss.fff}] Elevator {@event.ElevatorId} doors OPEN at floor {@event.Floor}");

        public Task HandleAsync(DoorsClosedEvent @event) => 
            LockAndLog(ConsoleColor.Yellow, $"[{@event.Timestamp:HH:mm:ss.fff}] Elevator {@event.ElevatorId} doors CLOSE at floor {@event.Floor}");

        public Task HandleAsync(PassengersEnteredEvent @event)=> 
            LockAndLog(ConsoleColor.Magenta, $"[{@event.Timestamp:HH:mm:ss.fff}] Elevator {@event.ElevatorId}: {+@event.PassengerCount} passenger(s) entered | Total: {@event.TotalPassengers}");

        public Task HandleAsync(PassengersExitedEvent @event) =>
            LockAndLog(ConsoleColor.Magenta, $"[{@event.Timestamp:HH:mm:ss.fff}] Elevator {@event.ElevatorId}: {@event.PassengerCount} passenger(s) exited | Remaining: {@event.RemainingPassengers}");

        public Task HandleAsync(RequestQueuedEvent @event) =>
            LockAndLog(ConsoleColor.Blue, $"[{@event.Timestamp:HH:mm:ss.fff}] Request: Floor {@event.FromFloor} -> {@event.ToFloor} | Queue Length: {@event.QueueLength}");

        public Task HandleAsync(ElevatorIdleEvent @event) => 
            LockAndLog(ConsoleColor.DarkGray, $"[{@event.Timestamp:HH:mm:ss.fff}] Elevator {@event.ElevatorId} IDLE at floor {@event.Floor}");

    }
}
