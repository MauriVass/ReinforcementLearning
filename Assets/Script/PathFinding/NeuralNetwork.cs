using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class NeuralNetwork
{
    //Number of Inputs = Number of States
    int nInput;
    //Input States (States are integer, in this case, but to avoid casting in propagation(Forward and Backward) float type is used)
    public float[] input;

    //Number of hidden layers
    public int nHiddenLayer;
    //The size of each hidden layer (How many neurons)
    public int sizeHiddenLayer;
    //The value of each Hidden Layer Neurons
    public float[][] hiddenLayer;
    //All Hidden Layer weights and biases
    public float[][][] hiddenWeights;
    float[][][] oldHiddenWeights;
    float[][] hiddenDelta;

    // Number of Output = Number of Actions
    public int nOutput;
    //Output Q-Value Actions
    public float[] output;
    //Output Layer weights and biases
    public float[][] outputWeights;
    float[][] oldoutputWeights;
    float[] outputDelta;

    public float learningRate, momentum, error;
    float value;

    public NeuralNetwork()
    {
        //Empty constructor
    }
    //Create a new neural network object copying an existing one
    public NeuralNetwork(NeuralNetwork nn)
    {
        this.nInput = nn.nInput;
        this.nHiddenLayer = nn.nHiddenLayer;
        this.sizeHiddenLayer = nn.sizeHiddenLayer;
        this.nOutput = nn.nOutput;
        this.learningRate = nn.learningRate;
        this.momentum = nn.momentum;

        InitializeVariables();

        CopyNN(nn.hiddenWeights, nn.outputWeights);
    }
    //Create a new neural network object passing each parameters
    public NeuralNetwork(int nInput, int nHiddenLayer, int sizeHiddenLayer, int nOutput, float learningRate, float momentum)
    {
        this.nInput = nInput;
        this.nHiddenLayer = nHiddenLayer;
        this.sizeHiddenLayer = sizeHiddenLayer;
        this.nOutput = nOutput;
        this.learningRate = learningRate;
        this.momentum = momentum;

        InitializeVariables();
    }

    void InitializeVariables()
    {
        if (nInput == 0 || nHiddenLayer == 0 || sizeHiddenLayer == 0 || nOutput == 0)
        {
            Debug.LogError("Error Initialization");
        }

        input = new float[nInput];

        /*****Hidden Layers*****/
        hiddenLayer = new float[nHiddenLayer][];
        for (int i = 0; i < hiddenLayer.Length; i++)
        {
            hiddenLayer[i] = new float[sizeHiddenLayer];
        }
        //Store memory for all the weights
        hiddenWeights = new float[nHiddenLayer][][];
        oldHiddenWeights = new float[nHiddenLayer][][];
        hiddenDelta = new float[nHiddenLayer][];
        for (int i = 0; i < hiddenWeights.Length; i++)
        {
            // 'sizeHiddenLayer + 1', the '+ 1' is for the bias
            if (i > 0)
            {
                hiddenWeights[i] = new float[sizeHiddenLayer + 1][];
                oldHiddenWeights[i] = new float[sizeHiddenLayer + 1][];
            }
            else
            {
                //The first index is for the input weights
                hiddenWeights[i] = new float[nInput + 1][];
                oldHiddenWeights[i] = new float[nInput + 1][];
            }
            hiddenDelta[i] = new float[sizeHiddenLayer];
            for (int j = 0; j < hiddenWeights[i].Length; j++)
            {
                hiddenWeights[i][j] = new float[sizeHiddenLayer];
                oldHiddenWeights[i][j] = new float[sizeHiddenLayer];
            }
            InitializeWeights(hiddenWeights[i]);
        }

        /*****Output Layer*****/
        output = new float[nOutput];
        outputWeights = new float[sizeHiddenLayer + 1][];
        oldoutputWeights = new float[sizeHiddenLayer + 1][];
        outputDelta = new float[nOutput];
        for (int i = 0; i < outputWeights.Length; i++)
        {
            outputWeights[i] = new float[nOutput];
            oldoutputWeights[i] = new float[nOutput];
        }
        InitializeWeights(outputWeights);
    }
    void InitializeWeights(float[][] weight)
    {
        for (int i = 0; i < weight.Length; i++)
        {
            for (int j = 0; j < weight[i].Length; j++)
            {
                float max = 100f;
                //Choose a random value for the weight with only 2 decimal digits
                float r = Random.Range(-max, max) / max;
                weight[i][j] = r;
            }
        }
    }

    void ForwardPropagate(float[][] inputWeight, float[] _input, float[] _output, bool outputLayer)
    {
        //For Sine test
        Func<float, float> activationFunctionOutputL = new Func<float, float>(SigmoidFunction);
        Func<float, float> activationFunctionHiddenL = new Func<float, float>(SigmoidFunction);

        //For Path Finding test
        //Func<float, float> activationFunctionOutputL = new Func<float, float>(Linear);
        //Func<float, float> activationFunctionHiddenL = new Func<float, float>(RELU);


        for (int i = 0; i < _output.Length; i++)
        {
            value = 0;
            for (int j = 0; j < _input.Length; j++)
            {
                value += _input[j] * inputWeight[j][i];
            }
            value += inputWeight[inputWeight.Length - 1][i];

            //Calculate the activation fuction for the different layers
            if (outputLayer)
                _output[i] = activationFunctionOutputL(value);
            else
                _output[i] = activationFunctionHiddenL(value);
        }
    }

    public void StepsForward(float[] input)
    {
        this.input = input;
        ForwardPropagate(hiddenWeights[0], input, hiddenLayer[0], false);
        for (int i = 0; i < hiddenWeights.Length - 1; i++)
        {
            ForwardPropagate(hiddenWeights[i + 1], hiddenLayer[i], hiddenLayer[i + 1], false);
        }
        ForwardPropagate(outputWeights, hiddenLayer[hiddenLayer.Length - 1], output, true);
    }
    public void StepsBackward(float[] targetOutput, float loss = 1)
    {
        //For Sine test
        Func<float, float> activationFunctionDevOutputL = new Func<float, float>(SigmoidFunctionDev);
        Func<float, float> activationFunctionDevHiddenL = new Func<float, float>(SigmoidFunctionDev);

        //For Path Finding test (Deep Q Learning)
        //Func<float, float> activationFunctionDevOutputL = new Func<float, float>(LinearDev);
        //Func<float, float> activationFunctionDevHiddenL = new Func<float, float>(RELUdev);

        error = 0;
        //Calculate errors
        for (int i = 0; i < output.Length; i++)
        {
            outputDelta[i] = (targetOutput[i] - output[i]) * activationFunctionDevOutputL(output[i]) * loss;

            //Calculate quadratic error
            error += 0.5f * Mathf.Pow(targetOutput[i] - output[i], 2);
        }

        //Backpropagate errors to the last hidden layer
        for (int i = 0; i < sizeHiddenLayer; i++)
        {
            value = 0;
            for (int j = 0; j < nOutput; j++)
            {
                value += outputWeights[i][j] * outputDelta[j];
            }
            float v = value * activationFunctionDevHiddenL(hiddenLayer[nHiddenLayer - 1][i]);
            hiddenDelta[nHiddenLayer - 1][i] = v;
        }

        //Backpropagate errors from the second last hidden layer to the first one (input layer --> first H L)
        for (int i = nHiddenLayer - 1; i > 0; i--)
        {
            for (int j = 0; j < sizeHiddenLayer; j++)
            {
                value = 0;
                for (int t = 0; t < sizeHiddenLayer; t++)
                {
                    value += hiddenWeights[i][j][t] * hiddenDelta[i][t];
                }
                hiddenDelta[i-1][j] = value * activationFunctionDevHiddenL(hiddenLayer[i-1][j]);
            }
        }

        //Backpropagate errors from first hidden layer to input layer
        //for (int i = 0; i < nInput; i++)
        //{
        //    value = 0;
        //    for (int j = 0; j < sizeHiddenLayer; j++)
        //    {
        //        value += hiddenWeights[0][i][j] * hiddenDelta[0][j];
        //    }
        //    hiddenDelta[0][i] = value;
        //}

        //Update Inner weights -> first Hidden Layer Weights 
        for (int i = 0; i < sizeHiddenLayer; i++)
        {
            oldHiddenWeights[0][nInput][i] = learningRate * hiddenDelta[0][i] + momentum * oldHiddenWeights[0][nInput][i];
            hiddenWeights[0][nInput][i] += oldHiddenWeights[0][nInput][i];
            for (int j = 0; j < nInput; j++)
            {
                oldHiddenWeights[0][j][i] = learningRate * input[j] * hiddenDelta[0][i] + momentum * oldHiddenWeights[0][j][i];
                hiddenWeights[0][j][i] += oldHiddenWeights[0][j][i];
                //Debug.Log(string.Format("i: {0} j: {1}",i,j));
            }
        }

        //Update Hidden Layer Weights 
        for (int i = 1; i < nHiddenLayer; i++)
        {
            for (int j = 0; j < sizeHiddenLayer; j++)
            {
                oldHiddenWeights[i][sizeHiddenLayer][j] = learningRate * hiddenDelta[i][j] + momentum * oldHiddenWeights[i][sizeHiddenLayer][j];
                hiddenWeights[i][sizeHiddenLayer][j] += oldHiddenWeights[i][sizeHiddenLayer][j];
                for (int k = 0; k < sizeHiddenLayer; k++)
                {
                    oldHiddenWeights[i][k][j] = learningRate * hiddenLayer[i - 1][j] * hiddenDelta[i][j] + momentum * oldHiddenWeights[i][k][j];
                    hiddenWeights[i][k][j] += oldHiddenWeights[i][k][j];
                }
            }
        }

        //Update last Hidden Layer Weights -> Output Layer Weights
        for (int i = 0; i < nOutput; i++)
        {
            oldoutputWeights[sizeHiddenLayer][i] = learningRate * outputDelta[i] + momentum * oldoutputWeights[sizeHiddenLayer][i];
            outputWeights[sizeHiddenLayer][i] += oldoutputWeights[sizeHiddenLayer][i];
            for (int j = 0; j < sizeHiddenLayer; j++)
            {
                oldoutputWeights[j][i] = learningRate * hiddenLayer[nHiddenLayer - 1][j] * outputDelta[i] + momentum * oldoutputWeights[j][i];
                outputWeights[j][i] += oldoutputWeights[j][i];
            }
        }
    }

    public float SigmoidFunction(float input)
    {
        return 1f / (1f + Mathf.Exp(-input));
    }
    public float SigmoidFunctionDev(float input)
    {
        //The input has already passed through the SigmoidFunction
        float r = input * (1 - input);
        //This avoids return 0
        return Mathf.Abs(r) < 0.0001f ? 0.0001f : r;
    }

    float RELU(float input)
    {
        if (input <= 0)
            return 0.0001f;
        else return input;
    }
    float RELUdev(float input)
    {
        if (input <= 0)
            return 0.0001f;
        else return 1f;
    }

    float Linear(float input)
    {
        return input;
    }
    float LinearDev(float input)
    {
        return Mathf.Abs(input) < 0.0001f ? 0.0001f : 1f;
    }

    public float TanH(float input)
    {
        //Since it's hard to evaluate exp() for large numbers, it's better to check the number before evaluate it
        float maxV = 5f;
        //If the number is too large return 1
        if (input > maxV)
            return 1;
        //Same if it is too small
        else if (input < -maxV)
            return -1;
        else
            return (Mathf.Exp(input) - Mathf.Exp(-input)) / (Mathf.Exp(input) + Mathf.Exp(-input));
    }
    float TanHDev(float input)
    {
        return input - Mathf.Pow(input,2);
    }

    public float GetOutputByActionIndex(int index)
    {
        return output[index];
    }

    public float GetMaxOutput()
    {
        return Mathf.Max(output);
    }
    public float[] getOutput()
    {
        float[] outp = new float[nOutput];
        for (int i = 0; i < nOutput; i++)
        {
            outp[i] = output[i];
        }
        return outp;
    }

    public void CopyNN(float[][][] _hiddenWeight, float[][] _outputWeight)
    {
        for (int i = 0; i < _hiddenWeight.Length; i++)
        {
            for (int j = 0; j < _hiddenWeight[i].Length; j++)
            {
                for (int k = 0; k < _hiddenWeight[i][j].Length; k++)
                {
                    hiddenWeights[i][j][k] = _hiddenWeight[i][j][k];
                }
            }
        }
        Debug.LogError("Changed");

        for (int i = 0; i < _outputWeight.Length; i++)
        {
            for (int j = 0; j < _outputWeight[i].Length; j++)
            {
                _outputWeight[i][j] = _outputWeight[i][j];
            }
        }
    }
}
