namespace Elevator.Core.Utility
{
    public static class RandomPath
    {
        public static Random Random = new Random();
        public static IEnumerable<int> Create(int maxFloor)
        {
            var length = Random.Next(1, 10);

            for (int i = 0; i < length; i++)
            {
                yield return Random.Next(1, maxFloor);
            }
        }
    }
}
