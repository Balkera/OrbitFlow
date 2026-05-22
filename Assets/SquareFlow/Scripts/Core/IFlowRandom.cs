namespace SquareFlow.Core
{
    public interface IFlowRandom
    {
        double Value();
        int Range(int minInclusive, int maxExclusive);
    }
}
