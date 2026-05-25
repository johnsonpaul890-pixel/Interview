using Elevator.Core.Services.EventBus;
using Moq;
using NUnit.Framework;

namespace Elevator.Core.Tests.EventBus
{
    public class TestEvent
    {
        public string Message { get; set; } = "Test";
    }

    public class AnotherTestEvent
    {
        public int Value { get; set; } = 42;
    }

    [TestFixture]
    public class DefaultEventBusTests
    {
        private DefaultEventBus _eventBus = null!;

        [SetUp]
        public void Setup()
        {
            _eventBus = new DefaultEventBus();
        }

        [Test]
        public async Task Subscribe_SubscriberHandlesEvent_PublishNotifies()
        {
            var handler = new Mock<IEventHandler<TestEvent>>();
            handler.Setup(h => h.HandleAsync(It.IsAny<TestEvent>()))
                .Returns(Task.CompletedTask);

            _eventBus.Subscribe(handler.Object);

            var @event = new TestEvent { Message = "Hello" };
            await _eventBus.PublishAsync(@event);

            handler.Verify(h => h.HandleAsync(It.Is<TestEvent>(e => e.Message == "Hello")), Times.Once);
        }

        [Test]
        public async Task Subscribe_MultipleSubscribers_AllNotified()
        {
            var handler1 = new Mock<IEventHandler<TestEvent>>();
            var handler2 = new Mock<IEventHandler<TestEvent>>();

            handler1.Setup(h => h.HandleAsync(It.IsAny<TestEvent>())).Returns(Task.CompletedTask);
            handler2.Setup(h => h.HandleAsync(It.IsAny<TestEvent>())).Returns(Task.CompletedTask);

            _eventBus.Subscribe(handler1.Object);
            _eventBus.Subscribe(handler2.Object);

            var @event = new TestEvent();
            await _eventBus.PublishAsync(@event);

            handler1.Verify(h => h.HandleAsync(It.IsAny<TestEvent>()), Times.Once);
            handler2.Verify(h => h.HandleAsync(It.IsAny<TestEvent>()), Times.Once);
        }

        [Test]
        public async Task Unsubscribe_RemovedSubscriber_NotNotified()
        {
            var handler = new Mock<IEventHandler<TestEvent>>();
            handler.Setup(h => h.HandleAsync(It.IsAny<TestEvent>())).Returns(Task.CompletedTask);

            _eventBus.Subscribe(handler.Object);
            _eventBus.Unsubscribe(handler.Object);

            var @event = new TestEvent();
            await _eventBus.PublishAsync(@event);

            handler.Verify(h => h.HandleAsync(It.IsAny<TestEvent>()), Times.Never);
        }

        [TestCase(1)]
        [TestCase(5)]
        [TestCase(10)]
        public async Task Subscribe_MultipleSubscribersToSameEvent_AllReceiveEvent(int subscriberCount)
        {
            var handlers = new List<Mock<IEventHandler<TestEvent>>>();
            for (int i = 0; i < subscriberCount; i++)
            {
                var handler = new Mock<IEventHandler<TestEvent>>();
                handler.Setup(h => h.HandleAsync(It.IsAny<TestEvent>())).Returns(Task.CompletedTask);
                handlers.Add(handler);
                _eventBus.Subscribe(handler.Object);
            }

            var @event = new TestEvent();
            await _eventBus.PublishAsync(@event);

            foreach (var handler in handlers)
            {
                handler.Verify(h => h.HandleAsync(It.IsAny<TestEvent>()), Times.Once);
            }
        }

        [Test]
        public async Task PublishAsync_DifferentEventTypes_OnlyRelevantSubscriberNotified()
        {
            var testEventHandler = new Mock<IEventHandler<TestEvent>>();
            var anotherEventHandler = new Mock<IEventHandler<AnotherTestEvent>>();

            testEventHandler.Setup(h => h.HandleAsync(It.IsAny<TestEvent>())).Returns(Task.CompletedTask);
            anotherEventHandler.Setup(h => h.HandleAsync(It.IsAny<AnotherTestEvent>())).Returns(Task.CompletedTask);

            _eventBus.Subscribe(testEventHandler.Object);
            _eventBus.Subscribe(anotherEventHandler.Object);

            var testEvent = new TestEvent();
            await _eventBus.PublishAsync(testEvent);

            testEventHandler.Verify(h => h.HandleAsync(It.IsAny<TestEvent>()), Times.Once);
            anotherEventHandler.Verify(h => h.HandleAsync(It.IsAny<AnotherTestEvent>()), Times.Never);
        }

        [Test]
        public async Task PublishAsync_NoSubscribers_CompletesWithoutError()
        {
            var @event = new TestEvent();
            Assert.DoesNotThrowAsync(async () => await _eventBus.PublishAsync(@event));
        }

        [Test]
        public async Task PublishAsync_HandlerThrowsException_PropagatesException()
        {
            var handler = new Mock<IEventHandler<TestEvent>>();
            handler.Setup(h => h.HandleAsync(It.IsAny<TestEvent>()))
                .Throws<InvalidOperationException>();

            _eventBus.Subscribe(handler.Object);

            var @event = new TestEvent();
            Assert.ThrowsAsync<InvalidOperationException>(async () => await _eventBus.PublishAsync(@event));
        }

        [Test]
        public async Task SubscribeAll_GenericHandler_SubscribedToAllImplementedEventTypes()
        {
            var multiHandler = new MultiEventHandler();

            _eventBus.SubscribeAll(multiHandler);

            var testEvent = new TestEvent();
            var anotherEvent = new AnotherTestEvent();

            await _eventBus.PublishAsync(testEvent);
            await _eventBus.PublishAsync(anotherEvent);

            Assert.That(multiHandler.TestEventCount, Is.EqualTo(1));
            Assert.That(multiHandler.AnotherEventCount, Is.EqualTo(1));
        }

        [Test]
        public async Task UnsubscribeAll_GenericHandler_UnsubscribedFromAllEventTypes()
        {
            var multiHandler = new MultiEventHandler();

            _eventBus.SubscribeAll(multiHandler);
            _eventBus.UnsubscribeAll(multiHandler);

            var testEvent = new TestEvent();
            var anotherEvent = new AnotherTestEvent();

            await _eventBus.PublishAsync(testEvent);
            await _eventBus.PublishAsync(anotherEvent);

            Assert.That(multiHandler.TestEventCount, Is.EqualTo(0));
            Assert.That(multiHandler.AnotherEventCount, Is.EqualTo(0));
        }

        [TestCase(10, 1)]
        [TestCase(5, 2)]
        [TestCase(3, 5)]
        public async Task PublishAsync_AsyncHandlers_AwaitAllCompletions(int handlerCount, int delayMs)
        {
            var handlers = new List<AsyncTestHandler>();
            for (int i = 0; i < handlerCount; i++)
            {
                var handler = new AsyncTestHandler(delayMs);
                handlers.Add(handler);
                _eventBus.Subscribe(handler);
            }

            var @event = new TestEvent();
            await _eventBus.PublishAsync(@event);

            foreach (var handler in handlers)
            {
                Assert.That(handler.Handled, Is.True);
            }
        }
    }

    public class MultiEventHandler : IEventHandler<TestEvent>, IEventHandler<AnotherTestEvent>
    {
        public int TestEventCount { get; private set; }
        public int AnotherEventCount { get; private set; }

        public Task HandleAsync(TestEvent @event)
        {
            TestEventCount++;
            return Task.CompletedTask;
        }

        public Task HandleAsync(AnotherTestEvent @event)
        {
            AnotherEventCount++;
            return Task.CompletedTask;
        }
    }

    public class AsyncTestHandler : IEventHandler<TestEvent>
    {
        private readonly int _delayMs;
        public bool Handled { get; private set; }

        public AsyncTestHandler(int delayMs)
        {
            _delayMs = delayMs;
        }

        public async Task HandleAsync(TestEvent @event)
        {
            await Task.Delay(_delayMs);
            Handled = true;
        }
    }
}
