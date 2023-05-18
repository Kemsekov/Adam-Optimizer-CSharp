using MathNet.Numerics.LinearAlgebra;

namespace GradientDescentSharp.NeuralNetwork;
public class BackpropResult{
    private ILayer[] layers;
    public BackpropResult(ILayer[] layers){
        this.layers= layers;
    }
    public void Unlearn(){
        foreach(var l in layers)
            l.Unlearn();
    }
}
public abstract class NNBase
{
    public ILayer[] Layers{get;}
    public FloatType LearningRate = 0.05;
    public NNBase(params ILayer[] layers)
    {
        Layers = layers;
    }
    public Vector Forward(Vector input)
    {
        ILayer layer;
        for (int i = 0; i < Layers.Length; i++)
        {
            layer = Layers[i];
            input = layer.Forward(input);
        }
        return input;
    }
    Vector<FloatType> ComputeErrorDerivative(Vector input, Vector expected){
        return (Forward(input)-expected)*2;
    }
    public double Error(Vector input, Vector expected){
        return (Forward(input)-expected).Sum(x=>x*x);
    }
    /// <returns>Error value before training</returns>
    public BackpropResult Backwards(Vector input, Vector expected)
    {
        // Forward pass
        var error = ComputeErrorDerivative(input,expected);
        var totalError = error.Sum(x=>x*x);
        
        for(int i = Layers.Length-1;i>=0;i--){
            var layer = Layers[i];
            
            var totalWeightsSum = layer.Bias.Sum(b=>b*b)+layer.Weights.ToEnumerable().Sum(x=>x*x);
            var learningRate = LearningRate/totalWeightsSum;

            var biases = layer.Bias;
            var layerOutput = layer.RawOutput;
            var activation = layer.Activation.Activation;
            var derivative = layer.Activation.ActivationDerivative;

            var biasesGradient = layerOutput.MapIndexed((j,x)=>derivative(x)*error[j]);
            if(i>0){
                error.MapIndexedInplace((j,x)=>x*derivative(layerOutput[j]));
                error *= layer.Weights;
            }
            var layerInput = i>0 ? Layers[i-1].RawOutput.Map(activation) : input;

            layer.Learn((Vector)biasesGradient,(Vector)layerInput,learningRate);
        }
        return new(Layers);
    }
}