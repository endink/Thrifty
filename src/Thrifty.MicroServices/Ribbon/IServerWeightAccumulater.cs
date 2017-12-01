namespace Thrifty.MicroServices.Ribbon
{
    public interface IServerWeightAccumulater
    {
        double[] AccumulatedWeights { get; }
    }
}   
