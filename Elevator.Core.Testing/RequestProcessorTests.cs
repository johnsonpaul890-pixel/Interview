using Elevator.Core.Models.Events;
using Elevator.Core.Models.Requests;
using Elevator.Core.Services.BuildingManager;
using Elevator.Core.Services.EventBus;
using Elevator.Core.Services.RequestProcessor;
using Moq;
using NUnit.Framework;

namespace Elevator.Core.Tests.RequestProcessor
{
    [TestFixture]
    public class RequestProcessorTests
    {
        public class TestException : Exception
        {
            public TestException(string message) : base(message)
            {
            }
        }
        public record TestEvent();
        public record TestRequest() 
            : BaseRequest()
        {
            public override async Task ProcessAsync(BuildingContext context, CancellationToken cancellationToken = default)
            {
                await context.EventBus.PublishAsync(new TestEvent());
            }
        }
        public record TestFailure() 
            : BaseRequest
        {
            public override async Task ProcessAsync(BuildingContext context, CancellationToken cancellationToken = default)
            {
                throw new TestException(nameof(TestFailure));
            }
        }

        private Mock<IBuildingManagerService> _mockBuilding = null!;
        private Mock<IEventBus> _mockEventBus = null!;
        private BuildingContext _context = null!;
        private BuildingRequestProcessor _requestProcessor = null!;

        [SetUp]
        public void Setup()
        {
            _mockBuilding = new Mock<IBuildingManagerService>();
            _mockEventBus = new Mock<IEventBus>();

            _mockBuilding.Setup(b => b.SimulationId).Returns("test-sim");

            _context = new BuildingContext(_mockBuilding.Object, _mockEventBus.Object);

            _requestProcessor = new BuildingRequestProcessor(_context);
        }

        [Test]
        public async Task ProcessAsync_ValidRequest_PublishesReceivedAndProcessedEvents()
        {
            var request = new TestRequest();
            var cancellationToken = CancellationToken.None;

            await _requestProcessor.ProcessAsync(request, cancellationToken);

            _mockEventBus.Verify(eb => eb.PublishAsync(It.IsAny<TestEvent>()), Times.Once);
            _mockEventBus.Verify(eb => eb.PublishAsync(It.IsAny<RequestReceivedEvent>()), Times.Once);
            _mockEventBus.Verify(eb => eb.PublishAsync(It.IsAny<RequestProcessedEvent>()), Times.Once);
        }

        

        [Test]
        public async Task ProcessAsync_RequestRaisesException_PublishesFailureEvent()
        {
            var cancellationToken = CancellationToken.None;

            Assert.ThrowsAsync<TestException>(async () =>
                await _requestProcessor.ProcessAsync(new TestFailure(), cancellationToken));

            _mockEventBus.Verify(
                eb => eb.PublishAsync(It.Is<RequestProcessedEvent>(e =>
                    e.Success == false &&
                    e.ErrorMessage == nameof(TestFailure))),
                Times.Once);
        }

        [Test]
        public async Task ProcessAsync_RequestProcessedEvent_IncludesRequestDetails()
        {
            var request = new TestRequest();
            var cancellationToken = CancellationToken.None;

            await _requestProcessor.ProcessAsync(request, cancellationToken);

            _mockEventBus.Verify(
                eb => eb.PublishAsync(It.Is<RequestProcessedEvent>(e =>
                    e.SimulationId == "test-sim" &&
                    e.Success == true &&
                    e.ErrorMessage == null)),
                Times.Once);
        }


        [TestCase(10, 1)]
        [TestCase(5, 2)]
        [TestCase(20, 10)]
        public async Task ProcessAsync_WithCancellationToken_RespondsToCancel(int requestCount, int cancelAfter)
        {
            var cts = new CancellationTokenSource();
            var requests = new List<TestRequest>();

            for (int i = 1; i <= requestCount; i++)
            {
                requests.Add(new TestRequest());
            }

            int processed = 0;
            foreach (var request in requests)
            {
                if (processed == cancelAfter)
                    cts.Cancel();

                try
                {
                    await _requestProcessor.ProcessAsync(request, cts.Token);
                    processed++;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            Assert.That(processed, Is.GreaterThanOrEqualTo(cancelAfter));
        }

        [Test]
        public async Task ProcessAsync_RequestReceivedEvent_ContainsCorrectData()
        {
            var request = new TestRequest();
            var cancellationToken = CancellationToken.None;

            await _requestProcessor.ProcessAsync(request, cancellationToken);

            _mockEventBus.Verify(
                eb => eb.PublishAsync(It.Is<RequestReceivedEvent>(e =>
                    e.SimulationId == "test-sim" &&
                    e.RequestType == nameof(TestRequest) &&
                    e.RequestId == request.Id)),
                Times.Once);
        }
    }
}
