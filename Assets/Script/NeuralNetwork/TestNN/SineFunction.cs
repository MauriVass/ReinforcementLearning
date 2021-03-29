using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SineFunction : MonoBehaviour
{
    //Test the NN trying to approximate square sine function: f(x) = sin(x)^2
    int training, inputNeuron, nHiddenLayer, hiddenNeuron, outputNeuron;
    public float learningRate, momentum, output, error, maxError, epoch;

    NeuralNetwork nn;
    private float[] inputTraining;
    private float[] outputTraining;
    private float[] inputTest;

    // Start is called before the first frame update
    void Start()
    {
        training = 17;
        inputNeuron = 1;
        nHiddenLayer = 1;
        hiddenNeuron = 10;
        outputNeuron = 1;
        
        learningRate = 0.05f;
        momentum = 0.9f;
        maxError = 0.018f;
        error = 999;

        nn = new NeuralNetwork(inputNeuron, nHiddenLayer, hiddenNeuron, outputNeuron, learningRate, momentum);
        
        inputTraining = new float[] { -Mathf.PI, -Mathf.PI * 5f / 6f, -Mathf.PI * 3f / 4f, -Mathf.PI * 2f / 3f, -Mathf.PI / 2f, -Mathf.PI / 3f, -Mathf.PI / 4f, -Mathf.PI / 6f, 0f,
                                       Mathf.PI / 6f, Mathf.PI / 4f, Mathf.PI / 3f, Mathf.PI / 2f, Mathf.PI * 2f / 3f, Mathf.PI * 3f / 4f, Mathf.PI * 5f / 6f, Mathf.PI };

        outputTraining = new float[training];
        for (int i = 0; i < training; i++)
        {
            outputTraining[i] = Mathf.Pow(Mathf.Sin(inputTraining[i]), 2);
        }
        outputTraining[0] = 0.001f;
        outputTraining[training - 1] = 0.001f;

        inputTest = new float[360];
        for (int i = -180; i < 180; i++)
        {
            inputTest[i + 180] = i / 180f * Mathf.PI;
        }
        Visualize();
    }
    bool succeded;
    bool canStart;
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S) || canStart)
        {
            if (error < maxError && !succeded)
            {
                succeded = true;
                print("Succeded!! Error: " + error + " Epoch: " + epoch);
                print("Time Elapsed: " + Time.realtimeSinceStartup);

                NetworkSucceded();
                print("Error: " + error);
            }
            else
            {
                TrainingLoop();
                epoch++;
            }
        }

        if (Input.GetKeyDown(KeyCode.A))
            canStart = true;
    }
    
    void TrainingLoop()
    {
        error = 0;
        for (int t = 0; t < training; t++)
        {
            nn.StepsForward(new float[] { inputTraining[t] });
            output = nn.GetOutputByActionIndex(0);
            error += 0.5f * Mathf.Pow((outputTraining[t] - output), 2);
            nn.StepsBackward(new float[] { outputTraining[t] });
        }
    }

    void NetworkSucceded()
    {
        float outp;
        for (int t = 0; t < inputTest.Length; t++)
        {
            nn.StepsForward(new float[] { inputTest[t]});
            outp = nn.GetOutputByActionIndex(0);
            float realOut = Mathf.Pow(Mathf.Sin(inputTest[t]), 2);
            error += 0.5f * Mathf.Pow( realOut - outp, 2);
            SketchGraph(inputTest[t],outp);
            print("Aspected: " + Mathf.Pow(Mathf.Sin(inputTest[t]), 2) + " actuall: " + outp + " distance: " + (realOut - outp));
        }
    }
    
    public GameObject neuron, canvas;
    GameObject tmp;
    void Visualize()
    {
        Vector2 start = new Vector2(100,700);
        float v = 100 * neuron.transform.localScale.x;
        for (int i = 0; i < nn.input.Length; i++)
        {
            tmp = Instantiate(neuron,canvas.transform);
            tmp.name = "InputNeuron " + (i + 1);
            tmp.transform.position = start + new Vector2(0,- i*v);
            Neuron n = tmp.GetComponent<Neuron>();
            n.nn = nn;
            n.i = i;
            n.s = "i";
            n.InitializeWeight(nn.sizeHiddenLayer);
        }

        for (int i = 0; i < nn.hiddenLayer.Length; i++)
        {
            for (int j = 0; j < nn.hiddenLayer[i].Length; j++)
            {
                tmp = Instantiate(neuron, canvas.transform);
                tmp.name = "HiddenNeuron " + (i + 1) + " " + (j+1);
                tmp.transform.position = start + new Vector2((i+1)*v, - j * v);
                Neuron n = tmp.GetComponent<Neuron>();
                n.nn = nn;
                n.i = i;
                n.j = j;
                n.s = "h";
                if (i == nn.hiddenLayer.Length - 1) {
                    n.InitializeWeight(nn.nOutput);
                    n.output = true;
                }
                else n.InitializeWeight(nn.sizeHiddenLayer);
            }
        }

        for (int i = 0; i < nn.output.Length; i++)
        {
            tmp = Instantiate(neuron, canvas.transform);
            tmp.name = "OutputNeuron " + (i + 1);
            tmp.transform.position = start + new Vector2(v*(1+nHiddenLayer), - i * v);
            Neuron n = tmp.GetComponent<Neuron>();
            n.nn = nn;
            n.i = i;
            n.s = "o";
        }
    }

    public LineRenderer graphSine, graphApprox;
    void SketchGraph(float input, float output)
    {
        graphApprox.positionCount++;
        graphApprox.SetPosition(graphApprox.positionCount-1,new Vector2(input,output));
        graphApprox.startColor = Color.red;
        graphSine.positionCount++;
        graphSine.SetPosition(graphSine.positionCount-1, new Vector2(input,Mathf.Pow( Mathf.Sin(input),2)));//Mathf.Sin(input)
        graphSine.startColor = Color.green;
    }
}
