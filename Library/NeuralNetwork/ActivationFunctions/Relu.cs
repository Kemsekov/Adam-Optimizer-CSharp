namespace GradientDescentSharp.NeuralNetwork.ActivationFunction;

///<inheritdoc/>
public class Relu : IActivationFunction
{
    ///<inheritdoc/>
    public IWeightsInit WeightsInit { get; set; } = new He2Normal();
    ///<inheritdoc/>
    public FVector Activation(FVector x)
    {
        return x.Map(x => Math.Max(0, x));
    }

    ///<inheritdoc/>
    public FVector ActivationDerivative(FVector x)
    {
        return x.Map(x => x > 0 ? 1.0f : 0.0f);
    }
}
