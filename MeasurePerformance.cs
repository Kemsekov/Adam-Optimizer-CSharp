namespace GradientDescentSharp;
public static class MeasurePerformance
{
    public static Func<IDataAccess<double>, double>[] Functions ={
                (d)=>Math.Abs(d[0]-d[1]*d[2]+Math.Exp(d[0]*d[1]*d[2])-Math.Sin(d[0]+d[1]+d[2])),
                (d)=>Math.Abs(d[0]-d[1]*d[2]),
                (d)=>Math.Abs(d[0]*d[1]*d[2]-d[0]*Math.Sin(d[2])+Math.Cos(d[0]*d[2])+d[2]),
                (d)=>Math.Abs(d[0]*Math.Cosh(d[0]*d[1]+Math.Sin(d[2]))),
                (d)=>Math.Abs(Math.Tan(d[0]*d[1]+d[2])-Math.Sin(d[1]*d[2]+d[0])),
                (d)=>Math.Abs(Math.Pow(d[0]*d[0],d[1])-d[2]*Math.Exp(d[0])*Math.Log(d[2]*d[2]))
            };
    public static void FindMatrixInverse()
    {
        var dimensions = 3;
        var m2 = DenseMatrix.Create(dimensions,dimensions,(x,y)=>Random.Shared.NextDouble()*2-1);
        m2.Inverse();
        var identity = DenseMatrix.CreateIdentity(dimensions);
        var errorFunction = (IDataAccess<double> x) =>
        {
            var factory = new ComplexObjectsFactory(x);
            var m1 = factory.TakeMatrix(dimensions, dimensions);
            var m3 = m1 * m2;
            var diff = identity-m3;
            var error = 0.0;
            for(int i = 0;i<dimensions;i++)
            for(int j = 0;j<dimensions;j++){
                error+=Math.Pow(diff[i,j],2);
            }
            return error;
        };

        var init = (IDataAccess<double> x)=>{
            for(int i = 0;i<x.Length;i++)
                x[i] = Random.Shared.NextDouble()*6-3;
        };

        var finder = new BestSolutionFinder(
            variablesLength: dimensions*dimensions,
            data => new MineDescent(data,errorFunction){Theta=0.000001,DescentRate=1}
        ){
            SolutionsCount = 100,
            DescentIterations=20,
            Logger = new ConsoleLogger(),
            Init = init
        };
        var result = finder.TryToFindBestSolution(errorFunction);
        var factory = new ComplexObjectsFactory(result);
        var m1 = factory.TakeMatrix(dimensions, dimensions);
        var m3 = m1 * m2;
        System.Console.WriteLine("Actual inverse matrix");
        System.Console.WriteLine(m2.Inverse());
        System.Console.WriteLine("Converged inverse matrix");
        Console.WriteLine(m1);
        System.Console.WriteLine("Original matrix");
        Console.WriteLine(m2);
        Console.WriteLine("Their product is");
        Console.WriteLine(m3);
        /*
        As you can see, if in actual inverse matrix it's values is way too big, 
        like 14, 25, 10, etc,
        it just only make sense that random matrix that we created will converge way before
        it hits such big values, so it make sense to create a wide range of matrix
        values (like in range from -20, to 20) and try to search local minima in this
        space, yet it is very consuming. 
        So if you at least +- know in which boundaries the actual answer is, I recommend
        to generate initial values for variables in such boundaries
        */
    }
    public static void PrintBothAdamAndMinePerformance(Func<IDataAccess<double>, double> func, int variablesLength, Action<IDataAccess<double>>? init = null)
    {
        //choose function to find it's minima
        // var func = (double a, double b, double c)=>Math.Abs(Functions.Sum(x=>x(a,b,c)));

        ArrayDataAccess<double> variables1 = new double[variablesLength];
        ArrayDataAccess<double> variables2 = new double[variablesLength];

        if (init is null)
            for (int i = 0; i < variablesLength; i++)
                variables1[i] = Random.Shared.NextDouble() * 2 - 1;
        init?.Invoke(variables1);

        variables1.Array.CopyTo(variables2, 0);
        var maxIterations = 500;
        var descentRate = 0.1;
        var theta = 0.0001;
        var logger = new ConsoleLogger();
        var adamDescent = new AdamDescent(variables1, func)
        {
            Theta = theta,
            DescentRate = descentRate,
            Logger = logger
        };
        var mineDescent = new MineDescent(variables2, func)
        {
            Theta = theta,
            DescentRate = descentRate,
            Logger = logger
        };

        var before = func(variables1);
        adamDescent.Descent(maxIterations);
        mineDescent.Descent(maxIterations);

        var after1 = func(variables1);
        var after2 = func(variables2);

        Console.WriteLine("Before " + before);
        Console.WriteLine("Adam " + after1);
        Console.WriteLine("Mine " + after2);
        System.Console.WriteLine("Adam Point is [" + String.Join(' ', variables1.Select(x => x.ToString("0.000"))) + "]");
        System.Console.WriteLine("Mine Point is [" + String.Join(' ', variables2.Select(x => x.ToString("0.000"))) + "]");
    }
    public static void MeasureAvg()
    {
        var results = Functions.Select(x => Measure(x, 3)).ToArray();
        var (adam, mine) = results.Aggregate((x1, x2) => (x1.adam + x2.adam, x1.mine + x2.mine));
        mine /= results.Length;
        adam /= results.Length;
        System.Console.WriteLine("Average adam " + adam);
        System.Console.WriteLine("Average mine " + mine);
    }
    public static (int adam, int mine) Measure(Func<IDataAccess<double>, double> func, int variablesLength)
    {
        ArrayDataAccess<double> variables1 = new double[] { Random.Shared.NextDouble(), Random.Shared.NextDouble(), Random.Shared.NextDouble() };
        ArrayDataAccess<double> variables2 = new double[variablesLength];

        for (int i = 0; i < variablesLength; i++) variables1[i] = Random.Shared.NextDouble() * 2 - 1;
        variables1.Array.CopyTo(variables2, 0);

        var adamDescent = new AdamDescent(variables1, func);
        var mineDescent = new MineDescent(variables2, func);

        double before;

        var maxIterations = 40;
        var descentRate = 0.05;
        var theta = 0.0001;
        int adamCount = 0;
        int mineCount = 0;

        for (int i = 0; i < 1000; i++)
        {
            variables1 = new double[3];
            variables2 = new double[3];

            for (int k = 0; k < variables1.Length; k++)
                variables1[k] = Random.Shared.NextDouble() * 2 - 1;

            variables1.Array.CopyTo(variables2, 0);

            before = func(variables1);
            adamDescent = new AdamDescent(variables1, func)
            {
                Theta = theta,
                DescentRate = descentRate
            };
            mineDescent = new MineDescent(variables2, func)
            {
                Theta = theta,
                DescentRate = descentRate
            };

            adamDescent.Descent(maxIterations);
            mineDescent.Descent(maxIterations);
            var adam = func(variables1);
            var mine = func(variables2);
            if (adam <= mine)
                adamCount++;
            else
                mineCount++;
        }
        System.Console.WriteLine("Adam : " + adamCount);
        System.Console.WriteLine("Mine : " + mineCount);
        System.Console.WriteLine("----------------------");
        return (adamCount, mineCount);
    }
    
    record Model(CustomMatrix[] layers, IDataAccess<double> freeCoefficients);
    public static void TryingML()
    {
        MathNet.Numerics.LinearAlgebra.Vector<double> ActivationFunction(MathNet.Numerics.LinearAlgebra.Vector<double> input)
        {
            input.MapInplace(x => 1.0 / (1 + Math.Exp(-x)));
            return input;
        }
        Model BuildModel(IDataAccess<double> x)
        {
            //outputSize1,inputSize
            //outputSize2, outputSize1
            //and so on

            var factory = new ComplexObjectsFactory(x);
            var layers = new[]{
                    factory.TakeMatrix(5, 3),
                    factory.TakeMatrix(6, 5),
                    factory.TakeMatrix(3, 6),
                    factory.TakeMatrix(1, 3)
                };
            var freeCoefficients = factory.TakeDouble(layers.Length);
            return new(layers, freeCoefficients);
        }
        double Compute(IDataAccess<double> x, Vector input)
        {
            System.Console.WriteLine("-----------");
            var model = BuildModel(x);
            var freeCoefficients = model.freeCoefficients;
            System.Console.WriteLine(input);
            for (int i = 0; i < model.layers.Length; i++)
            {
                var layer = model.layers[i];
                input = (Vector)(ActivationFunction(layer * input) + freeCoefficients[i]);
                System.Console.WriteLine(input);
            }
            return input[0];
        }
        var functionToImitate = Functions[1];
        var inputs = Enumerable.Range(0, 9 * 27).Select(x => DenseVector.Create(3, 0)).ToArray();
        MathUtils.DistributeData(inputs, 2, DenseVector.Create(3, -1));
        var func = (IDataAccess<double> x) =>
        {
            var error = 0.0;
            for (int i = 0; i < inputs.Length; i++)
            {
                var input = inputs[i];
                var expected = functionToImitate(VectorDataAccess.Of(input));
                var result = Compute(x, input);
                error += Math.Pow(result - expected, 2);
            }
            return Math.Sqrt(error);
        };
        void initWeights(IDataAccess<double> x)
        {
            var model = BuildModel(x);
            for (int i = 0; i < model.freeCoefficients.Length; i++)
            {
                model.freeCoefficients[i] = Random.Shared.NextDouble() * 2 - 1;
            }
            foreach (var layer in model.layers)
            {
                layer.MapInplace(x => Random.Shared.NextDouble() * 2 - 1);
                var elementsSum = layer.ReduceColumns((a, b) => a + b).Sum();
                layer.MapInplace(x => x / elementsSum);
            }
        }
        var finder = new BestSolutionFinder(
            variablesLength:70,
            gradientDescentFactory: x=> new MineDescent(x,func){DescentRate=0.5,Theta=0.001},
            init: initWeights
        );
        var solution = finder.TryToFindBestSolution(func);
        System.Console.WriteLine("Running tests");
        var errorSum = 0.0;
        for (int i = 0; i < 20; i++)
        {
            VectorDataAccess input = DenseVector.Create(3, x => Random.Shared.NextDouble());
            var expected = functionToImitate(input);
            var actual = Compute(solution, input);
            var diff = Math.Abs(expected - actual);
            errorSum += diff;
            System.Console.WriteLine($"Expected: {expected.ToString("0.00")}, Actual: {actual.ToString("0.00")}");
            // System.Console.WriteLine("Diff " + diff);
        }
        System.Console.WriteLine("Average error " + errorSum / 20);
        System.Console.WriteLine("-----------");
    }
}