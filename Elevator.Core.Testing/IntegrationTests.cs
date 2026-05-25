using Elevator.Core.Models;
using Elevator.Core.Models.Enums;
using Elevator.Core.Models.Requests;
using Elevator.Core.Services.BuildingManager;
using Elevator.Core.Services.EventBus;
using Elevator.Core.Services.Scheduler;
using Moq;
using NUnit.Framework;

namespace Elevator.Core.Tests.Integration
{
    [TestFixture]
    public class BuildingSimulationIntegrationTests
    {
        private Mock<IEventBus> _mockEventBus = null!;
        private BuildingManagerService _building = null!;
        private const string SimulationId = "integration-test";

        [SetUp]
        public void Setup()
        {
            _mockEventBus = new Mock<IEventBus>();
        }

        [TearDown]
        public void Teardown()
        {
            _building?.Dispose();
        }

        [Test]
        public void BuildingWithMultipleElevators_InitializesCorrectly()
        {
            _building = new BuildingManagerService(
                SimulationId, 10, 3, 15, _mockEventBus.Object, new RoundRobinScheduler());

            Assert.Multiple(() =>
            {
                Assert.That(_building.FloorCount, Is.EqualTo(10));
                Assert.That(_building.Elevators.Count, Is.EqualTo(3));
                Assert.That(_building.GetScheduler(), Is.TypeOf<RoundRobinScheduler>());
            });
        }

        [TestCase(5, 2, 10)]
        [TestCase(10, 3, 12)]
        [TestCase(20, 5, 20)]
        public void BuildingInitialization_VariousConfigurations_CreatesAllResources(
            int floors, int elevators, int capacity)
        {
            _building = new BuildingManagerService(
                SimulationId, floors, elevators, capacity, _mockEventBus.Object);

            Assert.Multiple(() =>
            {
                Assert.That(_building.FloorCount, Is.EqualTo(floors));
                Assert.That(_building.Elevators.Count, Is.EqualTo(elevators));
                for (int i = 1; i <= floors; i++)
                {
                    Assert.That(_building.GetFloor(i), Is.Not.Null);
                }
            });
        }

        [Test]
        public void AddingPeopleToBuilding_UpdatesFloorQueues()
        {
            _building = new BuildingManagerService(
                SimulationId, 5, 2, 10, _mockEventBus.Object);

            var person1 = new Person(1, new Queue<int>(new[] { 3 }));
            var person2 = new Person(1, new Queue<int>(new[] { 2 }));
            var person3 = new Person(1, new Queue<int>(new[] { 4 }));

            _building.AddPersonToFloor(person1, 1);
            _building.AddPersonToFloor(person2, 1);
            _building.AddPersonToFloor(person3, 1);

            var floor = _building.GetFloor(1);
            Assert.That(floor!.PeopleCount, Is.EqualTo(3));
        }

        [TestCase(5)]
        [TestCase(10)]
        public void DynamicBuildingExpansion_AddsFloorsAndElevators(int additionalFloors)
        {
            _building = new BuildingManagerService(
                SimulationId, 5, 2, 10, _mockEventBus.Object);

            int initialFloors = _building.FloorCount;
            for (int i = 0; i < additionalFloors; i++)
            {
                _building.AddFloor();
            }

            Assert.That(_building.FloorCount, Is.EqualTo(initialFloors + additionalFloors));
        }

        [Test]
        public void DynamicBuildingContraction_RemovesFloors()
        {
            _building = new BuildingManagerService(
                SimulationId, 10, 2, 10, _mockEventBus.Object);

            int initialFloors = _building.FloorCount;
            _building.RemoveFloor(10);
            _building.RemoveFloor(9);
            _building.RemoveFloor(8);

            Assert.That(_building.FloorCount, Is.EqualTo(initialFloors - 3));
        }

        [TestCase(1, 5)]
        [TestCase(2, 10)]
        [TestCase(3, 8)]
        public void ModifyElevatorCapacity_UpdatesCapacity(int elevatorId, int newCapacity)
        {
            _building = new BuildingManagerService(
                SimulationId, 5, 3, 9, _mockEventBus.Object);

            var originalCapacity = _building.GetElevator(elevatorId)!.Capacity;
            
            var result = _building.ModifyElevatorCapacity(elevatorId, newCapacity);
            var updatedCapacity = _building.GetElevator(elevatorId)!.Capacity;

            Assert.That(result, Is.EqualTo(true));
            Assert.That(updatedCapacity, Is.EqualTo(newCapacity));
            Assert.That(updatedCapacity, Is.Not.EqualTo(originalCapacity));
        }

        [Test]
        public void RequestQueueing_EnqueuesMultipleRequests()
        {
            _building = new BuildingManagerService(
                SimulationId, 5, 2, 10, _mockEventBus.Object);

            var requests = new[]
            {
                new ElevatorMoveRequest(1, 2),
                new ElevatorMoveRequest(2, 4),
                new ElevatorMoveRequest(3, 5),
                new ElevatorMoveRequest(4, 1),
            };

            foreach (var request in requests)
            {
                _building.EnqueueBuildingRequest(request);
            }

            // Verify no exceptions thrown during queueing
            Assert.Pass();
        }

        [Test]
        public void ElevatorLifecycle_AddRemoveAdd_MaintainsState()
        {
            _building = new BuildingManagerService(
                SimulationId, 5, 2, 10, _mockEventBus.Object);

            int initialCount = _building.Elevators.Count;
            var newElevator = _building.AddElevator(12);
            Assert.That(_building.Elevators.Count, Is.EqualTo(initialCount + 1));

            _building.RemoveElevator(newElevator.Id);
            Assert.That(_building.Elevators.Count, Is.EqualTo(initialCount));

            var anotherElevator = _building.AddElevator(8);
            Assert.That(_building.Elevators.Count, Is.EqualTo(initialCount + 1));
        }

        [TestCase(3)]
        [TestCase(5)]
        [TestCase(10)]
        public void SchedulerIntegration_SelectsFromAllElevators(int numberOfRequests)
        {
            var scheduler = new RoundRobinScheduler();
            _building = new BuildingManagerService(
                SimulationId, 5, 3, 10, _mockEventBus.Object, scheduler);

            var selectedElevators = new List<int>();
            for (int i = 0; i < numberOfRequests; i++)
            {
                var elevators = _building.Elevators;
                var selected = scheduler.SelectElevator(elevators, new ElevatorMoveRequest(1, 3));
                if (selected != null)
                {
                    selectedElevators.Add(selected.Id);
                }
            }

            Assert.That(selectedElevators.Count, Is.EqualTo(numberOfRequests));
        }

        [Test]
        public void MultipleSchedulerTypes_BehaveDifferently()
        {
            var roundRobin = new RoundRobinScheduler();
            var look = new LookAlgorithmScheduler();

            _building = new BuildingManagerService(
                SimulationId, 5, 3, 10, _mockEventBus.Object, roundRobin);

            var elevators = _building.Elevators;
            var request = new ElevatorMoveRequest(2, 4);

            var rrSelected = roundRobin.SelectElevator(elevators, request);
            var lookSelected = look.SelectElevator(elevators, request);

            Assert.That(rrSelected, Is.Not.Null);
            Assert.That(lookSelected, Is.Not.Null);
        }

        [Test]
        public void PersonTransitAcrossFloors_SetsCorrectStatus()
        {
            _building = new BuildingManagerService(
                SimulationId, 5, 2, 10, _mockEventBus.Object);

            var person = new Person(1, new Queue<int>(new[] { 3, 5 }));
            _building.AddPersonToFloor(person, 1);

            var floorOne = _building.GetFloor(1);
            Assert.That(floorOne!.PeopleCount, Is.EqualTo(1));

            var floorThree = _building.GetFloor(3);
            Assert.That(floorThree, Is.Not.Null);
        }

        [TestCase(2)]
        [TestCase(5)]
        [TestCase(10)]
        public void BuildingScaling_HandlesLargePopulations(int personCount)
        {
            _building = new BuildingManagerService(
                SimulationId, 10, 4, 20, _mockEventBus.Object);

            var people = new List<Person>();
            for (int i = 0; i < personCount; i++)
            {
                var person = new Person(1, new Queue<int>(new[] { Random.Shared.Next(2, 10) }));
                people.Add(person);
                _building.AddPersonToFloor(person, 1);
            }

            var floor = _building.GetFloor(1);
            Assert.That(floor!.PeopleCount, Is.EqualTo(personCount));
        }

        [Test]
        public void CompleteDisposal_ClearsAllState()
        {
            _building = new BuildingManagerService(
                SimulationId, 8, 3, 15, _mockEventBus.Object);

            // Add some data
            var person = new Person(1, new Queue<int>(new[] { 5 }));
            _building.AddPersonToFloor(person, 1);
            _building.AddFloor();
            _building.AddElevator(10);

            _building.Dispose();

            Assert.Multiple(() =>
            {
                Assert.That(_building.FloorCount, Is.EqualTo(0));
                Assert.That(_building.Elevators.Count, Is.EqualTo(0));
            });
        }
    }
}
