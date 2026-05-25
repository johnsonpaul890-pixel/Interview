using Elevator.Core.Models.Enums;
using Elevator.Core.Models.Requests;
using Elevator.Core.Services.ElevatorService;
using Elevator.Core.Services.Scheduler;
using Moq;
using NUnit.Framework;

namespace Elevator.Core.Tests.Scheduler
{
    [TestFixture]
    public class SchedulerTests
    {
        private List<ILift> CreateMockElevators(int count)
        {
            var elevators = new List<ILift>();
            for (int i = 1; i <= count; i++)
            {
                var mock = new Mock<ILift>();
                mock.Setup(e => e.Id).Returns(i);
                mock.Setup(e => e.CurrentFloor).Returns(i);
                mock.Setup(e => e.Direction).Returns(Direction.Idle);
                elevators.Add(mock.Object);
            }
            return elevators;
        }

        [TestFixture]
        public class RoundRobinSchedulerTests : SchedulerTests
        {
            [Test]
            public void SelectElevator_EmptyList_ReturnsNull()
            {
                var scheduler = new RoundRobinScheduler();
                var result = scheduler.SelectElevator(new List<ILift>(), new ElevatorMoveRequest(1, 3));
                Assert.That(result, Is.Null);
            }

            [TestCase(1)]
            [TestCase(2)]
            [TestCase(5)]
            public void SelectElevator_SingleOrMultipleElevators_SelectsRoundRobin(int elevatorCount)
            {
                var scheduler = new RoundRobinScheduler();
                var elevators = CreateMockElevators(elevatorCount);
                var request = new ElevatorMoveRequest(1, 3);

                var first = scheduler.SelectElevator(elevators, request);
                Assert.That(first, Is.Not.Null);
                Assert.That(first!.Id, Is.EqualTo(1));
            }

            [Test]
            public void SelectElevator_MultipleRequests_RotatesElevators()
            {
                var scheduler = new RoundRobinScheduler();
                var elevators = CreateMockElevators(3);
                var request1 = new ElevatorMoveRequest(1, 3);
                var request2 = new ElevatorMoveRequest(2, 4);
                var request3 = new ElevatorMoveRequest(3, 5);
                var request4 = new ElevatorMoveRequest(4, 2);

                var elevator1 = scheduler.SelectElevator(elevators, request1);
                var elevator2 = scheduler.SelectElevator(elevators, request2);
                var elevator3 = scheduler.SelectElevator(elevators, request3);
                var elevator4 = scheduler.SelectElevator(elevators, request4);

                Assert.Multiple(() =>
                {
                    Assert.That(elevator1!.Id, Is.EqualTo(1));
                    Assert.That(elevator2!.Id, Is.EqualTo(2));
                    Assert.That(elevator3!.Id, Is.EqualTo(3));
                    Assert.That(elevator4!.Id, Is.EqualTo(1)); // Cycles back
                });
            }

            [TestCase(5)]
            [TestCase(10)]
            public void SelectElevator_ManyRequests_CyclesCorrectly(int requestCount)
            {
                var scheduler = new RoundRobinScheduler();
                var elevators = CreateMockElevators(3);
                
                var selectedIds = new List<int>();
                for (int i = 0; i < requestCount; i++)
                {
                    var request = new ElevatorMoveRequest(1, 3);
                    var selected = scheduler.SelectElevator(elevators, request);
                    selectedIds.Add(selected!.Id);
                }

                for (int i = 0; i < requestCount; i++)
                {
                    int expectedId = (i % 3) + 1;
                    Assert.That(selectedIds[i], Is.EqualTo(expectedId), $"Request {i} should be assigned to elevator {expectedId}");
                }
            }
        }

        [TestFixture]
        public class LookAlgorithmSchedulerTests : SchedulerTests
        {
            [Test]
            public void SelectElevator_EmptyList_ReturnsNull()
            {
                var scheduler = new LookAlgorithmScheduler();
                var result = scheduler.SelectElevator(new List<ILift>(), new ElevatorMoveRequest(1, 3));
                Assert.That(result, Is.Null);
            }

            [Test]
            public void SelectElevator_AllIdleElevators_SelectsClosest()
            {
                var scheduler = new LookAlgorithmScheduler();
                var elevators = new List<ILift>();

                var elevator1 = new Mock<ILift>();
                elevator1.Setup(e => e.Id).Returns(1);
                elevator1.Setup(e => e.CurrentFloor).Returns(1);
                elevator1.Setup(e => e.Direction).Returns(Direction.Idle);

                var elevator2 = new Mock<ILift>();
                elevator2.Setup(e => e.Id).Returns(2);
                elevator2.Setup(e => e.CurrentFloor).Returns(3);
                elevator2.Setup(e => e.Direction).Returns(Direction.Idle);

                var elevator3 = new Mock<ILift>();
                elevator3.Setup(e => e.Id).Returns(3);
                elevator3.Setup(e => e.CurrentFloor).Returns(5);
                elevator3.Setup(e => e.Direction).Returns(Direction.Idle);

                elevators.AddRange(new[] { elevator1.Object, elevator2.Object, elevator3.Object });

                var request = new ElevatorMoveRequest(4, 2);
                var selected = scheduler.SelectElevator(elevators, request);

                Assert.That(selected!.Id, Is.EqualTo(2)); // Closest to floor 4
            }

            [TestCase(1)]
            [TestCase(3)]
            [TestCase(5)]
            public void SelectElevator_SingleOrMultipleElevators_ReturnsOne(int elevatorCount)
            {
                var scheduler = new LookAlgorithmScheduler();
                var elevators = CreateMockElevators(elevatorCount);
                var request = new ElevatorMoveRequest(2, 4);

                var selected = scheduler.SelectElevator(elevators, request);
                Assert.That(selected, Is.Not.Null);
            }

            [Test]
            public void SelectElevator_ElevatorMovingTowards_PreferredOverMovingAway()
            {
                var scheduler = new LookAlgorithmScheduler();
                var elevators = new List<ILift>();

                var movingTowards = new Mock<ILift>();
                movingTowards.Setup(e => e.Id).Returns(1);
                movingTowards.Setup(e => e.CurrentFloor).Returns(2);
                movingTowards.Setup(e => e.Direction).Returns(Direction.Up);

                var movingAway = new Mock<ILift>();
                movingAway.Setup(e => e.Id).Returns(2);
                movingAway.Setup(e => e.CurrentFloor).Returns(2);
                movingAway.Setup(e => e.Direction).Returns(Direction.Down);

                elevators.AddRange(new[] { movingTowards.Object, movingAway.Object });

                var request = new ElevatorMoveRequest(4, 2);
                var selected = scheduler.SelectElevator(elevators, request);

                Assert.That(selected!.Id, Is.EqualTo(1)); // Elevator moving up is preferred
            }
        }

        [TestFixture]
        public class ScanAlgorithmSchedulerTests : SchedulerTests
        {
            [Test]
            public void SelectElevator_EmptyList_ReturnsNull()
            {
                var scheduler = new ScanAlgorithmScheduler();
                var result = scheduler.SelectElevator(new List<ILift>(), new ElevatorMoveRequest(1, 3));
                Assert.That(result, Is.Null);
            }

            [TestCase(1)]
            [TestCase(2)]
            [TestCase(5)]
            public void SelectElevator_SingleOrMultipleElevators_ReturnsOne(int elevatorCount)
            {
                var scheduler = new ScanAlgorithmScheduler();
                var elevators = CreateMockElevators(elevatorCount);
                var request = new ElevatorMoveRequest(1, 5);

                var selected = scheduler.SelectElevator(elevators, request);
                Assert.That(selected, Is.Not.Null);
            }

            [Test]
            public void SelectElevator_IdleElevators_PreferredOverBusy()
            {
                var scheduler = new ScanAlgorithmScheduler();
                var elevators = new List<ILift>();

                var idle = new Mock<ILift>();
                idle.Setup(e => e.Id).Returns(1);
                idle.Setup(e => e.Direction).Returns(Direction.Idle);

                var busy = new Mock<ILift>();
                busy.Setup(e => e.Id).Returns(2);
                busy.Setup(e => e.Direction).Returns(Direction.Up);

                elevators.AddRange(new[] { idle.Object, busy.Object });

                var request = new ElevatorMoveRequest(1, 3);
                var selected = scheduler.SelectElevator(elevators, request);

                Assert.That(selected!.Id, Is.EqualTo(1)); // Idle elevator is preferred
            }
        }

        [TestFixture]
        public class ShortestWaitTimeSchedulerTests : SchedulerTests
        {
            [Test]
            public void SelectElevator_EmptyList_ReturnsNull()
            {
                var scheduler = new ShortestWaitTimeScheduler();
                var result = scheduler.SelectElevator(new List<ILift>(), new ElevatorMoveRequest(1, 3));
                Assert.That(result, Is.Null);
            }

            [TestCase(1)]
            [TestCase(2)]
            [TestCase(5)]
            public void SelectElevator_SingleOrMultipleElevators_ReturnsOne(int elevatorCount)
            {
                var scheduler = new ShortestWaitTimeScheduler();
                var elevators = CreateMockElevators(elevatorCount);
                var request = new ElevatorMoveRequest(1, 3);

                var selected = scheduler.SelectElevator(elevators, request);
                Assert.That(selected, Is.Not.Null);
            }
        }
    }
}
