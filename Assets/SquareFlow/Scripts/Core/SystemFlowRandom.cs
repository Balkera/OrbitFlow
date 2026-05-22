using System;

namespace SquareFlow.Core
{
    public sealed class SystemFlowRandom : IFlowRandom
    {
        private readonly Random random;

        public SystemFlowRandom(int? seed = null)
        {
            random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public double Value()
        {
            return random.NextDouble();
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            return random.Next(minInclusive, maxExclusive);
        }
    }
}
