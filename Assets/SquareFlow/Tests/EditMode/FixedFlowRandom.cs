using System.Collections.Generic;
using SquareFlow.Core;

namespace SquareFlow.Tests
{
    public sealed class FixedFlowRandom : IFlowRandom
    {
        private readonly Queue<double> values = new Queue<double>();
        private readonly double fallback;

        public FixedFlowRandom(double fallback, params double[] sequence)
        {
            this.fallback = fallback;
            foreach (double value in sequence) values.Enqueue(value);
        }

        public double Value()
        {
            return values.Count > 0 ? values.Dequeue() : fallback;
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            double raw = Value();
            int width = maxExclusive - minInclusive;
            return minInclusive + UnityEngine.Mathf.Clamp((int)(raw * width), 0, width - 1);
        }
    }
}
