using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Neuron : MonoBehaviour
{
    public Text text;
    [HideInInspector]
    public NeuralNetwork nn = new NeuralNetwork();
    [HideInInspector]
    public string s;
    [HideInInspector]
    public int i, j, k;

    public bool input, output;
    public float[] weight;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        switch (s)
        {
            case "i":
                text.text = nn.input[i].ToString("F3");
                weight = nn.hiddenWeights[0][i];
                break;
            case "h":
                text.text = nn.hiddenLayer[i][j].ToString("F3");
                if (output)
                    weight = nn.outputWeights[j];
                else
                    weight = nn.hiddenWeights[1+i][j];
                break;
            case "o":
                text.text = nn.output[i].ToString("F3");
                break;
            default:
                break;
        }

    }

    public void InitializeWeight(int size)
    {
        weight = new float[size];
    }
}
