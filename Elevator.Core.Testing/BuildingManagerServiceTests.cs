using Elevator.Core.Models;
using Elevator.Core.Models.Enums;
using Elevator.Core.Models.Requests;
using Elevator.Core.Services.BuildingManager;
using Elevator.Core.Services.EventBus;
using Elevator.Core.Services.Scheduler;
using Moq;
using NUnit.Framework;

namespace Elevator.Core.Tests.BuildingManager
{
    [TestFixture]
    public class BuildingManagerServiceTests
    {
        private Mock<IEventBus> _mockEventBus = null!;
        private BuildingManagerService _buildingManager = null!;
        private const string SimulationId = "test-sim-1";

        [SetUp]
        public void Setup()
        {
            _mockEventBus = new Mock<IEventBus>();
            _buildingManager = new BuildingManagerService(SimulationId, 5, 2, 10, _mockEventBus.Object);
        }

        [TearDown]
        public void Teardown()
        {
            _buildingManager?.Dispose();
        }

        [Test]
        public void Constructor_ValidParameters_InitializesCorrectly()
        {
            Assert.Multiple(() =>
            {
                Assert.That(_buildingManager.SimulationId, Is.EqualTo(SimulationId));
                Assert.That(_buildingManager.FloorCount, Is.EqualTo(5));
                Assert.That(_buildingManager.Elevators.Count, Is.EqualTo(2));
            });
        }

        [TestCase(1)]
        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_InvalidFloorCount_ThrowsException(int floorCount)
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                new BuildingManagerService(SimulationId, floorCount, 1, 10, _mockEventBus.Object));
            Assert.That(ex!.ParamName, Is.EqualTo("floorCount"));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_InvalidElevatorCount_ThrowsException(int elevatorCount)
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                new BuildingManagerService(SimulationId, 5, elevatorCount, 10, _mockEventBus.Object));
            Assert.That(ex!.ParamName, Is.EqualTo("elevatorCount"));
        }

        [Test]
        public void GetFloor_ValidFloorNumber_ReturnsFloor()
        {
            var floor = _buildingManager.GetFloor(3);
            Assert.That(floor, Is.Not.Null);
            Assert.That(floor!.Number, Is.EqualTo(3));
        }

        [TestCase(0)]
        [TestCase(6)]
        [TestCase(10)]
        public void GetFloor_InvalidFloorNumber_ReturnsNull(int floorNumber)
        {
            var floor = _buildingManager.GetFloor(floorNumber);
            Assert.That(floor, Is.Null);
        }

        [Test]
        public void GetElevator_ValidElevatorId_ReturnsElevator()
        {
            var elevator = _buildingManager.GetElevator(1);
            Assert.That(elevator, Is.Not.Null);
            Assert.That(elevator!.Id, Is.EqualTo(1));
        }

        [TestCase(0)]
        [TestCase(3)]
        [TestCase(100)]
        public void GetElevator_InvalidElevatorId_ReturnsNull(int elevatorId)
        {
            var elevator = _buildingManager.GetElevator(elevatorId);
            Assert.That(elevator, Is.Null);
        }

        [Test]
        public void AddFloor_IncreasesFloorCount()
        {
            var initialCount = _buildingManager.FloorCount;
            _buildingManager.AddFloor();
            
            Assert.That(_buildingManager.FloorCount, Is.EqualTo(initialCount + 1));
            Assert.That(_buildingManager.GetFloor(initialCount + 1), Is.Not.Null);
        }

        [TestCase(5)]
        [TestCase(10)]
        [TestCase(20)]
        public void AddFloor_Multiple_CreatesAllFloors(int floorCountToAdd)
        {
            int initialCount = _buildingManager.FloorCount;
            for (int i = 0; i < floorCountToAdd; i++)
            {
                _buildingManager.AddFloor();
            }

            Assert.That(_buildingManager.FloorCount, Is.EqualTo(initialCount + floorCountToAdd));
        }

        [Test]
        public void RemoveFloor_ReducesFloorCount()
        {
            var initialCount = _buildingManager.FloorCount;
            _buildingManager.RemoveFloor(1);
            
            Assert.That(_buildingManager.FloorCount, Is.EqualTo(initialCount - 1));
        }

        [Test]
        public void AddPerson_ValidFloorAndPerson_AddsPerson()
        {
            var person = new Person(1, new Queue<int>(new[] { 2, 3 }));
            _buildingManager.AddPersonToFloor(person, 1);
            
            var floor = _buildingManager.GetFloor(1);
            Assert.That(floor!.PeopleCount, Is.EqualTo(1));
        }

        [Test]
        public void AddPerson_InvalidFloor_ThrowsException()
        {
            var person = new Person(1, new Queue<int>(new[] { 2 }));
            Assert.Throws<ArgumentException>(() => _buildingManager.AddPersonToFloor(person, 10));
        }

        [Test]
        public void EnqueueBuildingRequest_ValidRequest_Enqueues()
        {
            var request = new ElevatorMoveRequest(1, 3);
            Assert.DoesNotThrow(() => _buildingManager.EnqueueBuildingRequest(request));
        }

        [Test]
        public void AddElevator_CreatesNewElevator()
        {
            var initialCount = _buildingManager.Elevators.Count;
            var newElevator = _buildingManager.AddElevator(8);
            
            Assert.That(_buildingManager.Elevators.Count, Is.EqualTo(initialCount + 1));
            Assert.That(newElevator, Is.Not.Null);
        }

        [TestCase(5)]
        [TestCase(15)]
        [TestCase(20)]
        public void AddElevator_VariousCapacities_CreatesElevator(int capacity)
        {
            var newElevator = _buildingManager.AddElevator(capacity);
            Assert.That(newElevator.Capacity, Is.EqualTo(capacity));
        }

        [Test]
        public void RemoveElevator_ExistingElevator_RemovesIt()
        {
            var initialCount = _buildingManager.Elevators.Count;
            var result = _buildingManager.RemoveElevator(1);
            
            Assert.That(result, Is.True);
            Assert.That(_buildingManager.Elevators.Count, Is.EqualTo(initialCount - 1));
        }

        [Test]
        public void RemoveElevator_NonExistentElevator_ReturnsFalse()
        {
            var result = _buildingManager.RemoveElevator(999);
            Assert.That(result, Is.False);
        }

        [TestCase(1, 5)]
        [TestCase(1, 20)]
        [TestCase(999, 10)]
        public void ModifyElevatorCapacity_VariousIds_ReturnsCorrectResult(int elevatorId, int newCapacity)
        {
            var result = _buildingManager.ModifyElevatorCapacity(elevatorId, newCapacity);
            var isValidId = elevatorId <= _buildingManager.Elevators.Count && elevatorId > 0;
            Assert.That(result, Is.EqualTo(isValidId));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void ModifyElevatorCapacity_InvalidCapacity_ThrowsException(int capacity)
        {
            Assert.Throws<ArgumentException>(() => _buildingManager.ModifyElevatorCapacity(1, capacity));
        }

        [Test]
        public void Dispose_ClearsResources()
        {
            var manager = new BuildingManagerService(SimulationId, 5, 2, 10, _mockEventBus.Object);
            manager.Dispose();
            
            Assert.That(manager.FloorCount, Is.EqualTo(0));
            Assert.That(manager.Elevators.Count, Is.EqualTo(0));
        }
    }
}
