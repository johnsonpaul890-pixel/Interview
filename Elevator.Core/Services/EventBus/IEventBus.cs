namespace Elevator.Core.Services.EventBus
{
    public interface IEventBus
    {
        void Subscribe<T>(IEventHandler<T> handler) where T : class;
        void Unsubscribe<T>(IEventHandler<T> handler) where T : class;
        void SubscribeAll<T>(T handler) where T : IEventHandler;
        void UnsubscribeAll<T>(T handler) where T : IEventHandler;
        Task PublishAsync<T>(T @event) where T : class;
    }
}
