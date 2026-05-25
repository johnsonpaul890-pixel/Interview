namespace Elevator.Core.Services.EventBus
{
    public interface IEventHandler
    {

    }
    public interface IEventHandler<T> : IEventHandler
        where T : class
    {
        Task HandleAsync(T @event);
    }
}
