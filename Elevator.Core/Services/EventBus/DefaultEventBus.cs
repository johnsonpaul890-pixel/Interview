using System.Collections.Concurrent;

namespace Elevator.Core.Services.EventBus
{
    public class DefaultEventBus : IEventBus
    {
        private readonly ConcurrentDictionary<Type, List<object>> _subscribers = new();
        private readonly object _lock = new();

        public void Subscribe<T>(IEventHandler<T> handler) where T : class
        {
            lock (_lock)
            {
                var eventType = typeof(T);
                if (!_subscribers.ContainsKey(eventType))
                    _subscribers[eventType] = new List<object>();

                _subscribers[eventType].Add(handler);
            }
        }

        public void Unsubscribe<T>(IEventHandler<T> handler) where T : class
        {
            lock (_lock)
            {
                var eventType = typeof(T);
                _subscribers[eventType].Remove(handler);
            }
        }

        public void SubscribeAll<T>(T handler) where T : IEventHandler
        {

            lock (_lock)
            {
                var eventTypes = typeof(T).GetInterfaces()
                    .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEventHandler<>))
                    .Select(x => x.GetGenericArguments()[0]);

                foreach (var eventType in eventTypes)
                {
                    if (!_subscribers.ContainsKey(eventType))
                        _subscribers[eventType] = new List<object>();

                    _subscribers[eventType].Add(handler);
                }
            }
        }

        public void UnsubscribeAll<T>(T handler) where T : IEventHandler
        {

            lock (_lock)
            {
                var eventTypes = typeof(T).GetInterfaces()
                    .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEventHandler<>))
                    .Select(x => x.GetGenericArguments()[0]);
                foreach (var eventType in eventTypes)
                {
                    _subscribers[eventType].Remove(handler);
                }
            }
        }

        public async Task PublishAsync<T>(T @event) where T : class
        {
            List<IEventHandler<T>>? handlers;

            lock (_lock)
            {
                var eventType = typeof(T);
                if (!_subscribers.TryGetValue(eventType, out var subscriber))
                    return;

                handlers = subscriber.Cast<IEventHandler<T>>().ToList();
            }

            var tasks = handlers.Select(h => h.HandleAsync(@event));
            await Task.WhenAll(tasks);
        }
    }
}
