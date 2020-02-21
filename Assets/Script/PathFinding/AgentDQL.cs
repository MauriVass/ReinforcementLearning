using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Experience
{
    private int state;
    private int action;
    private float reward;
    private int nextState;
    private bool endState;

    public Experience(int state, int action, float reward, int nextState, bool endState)
    {
        this.state = state;
        this.action = action;
        this.reward = reward;
        this.nextState = nextState;
        this.endState = endState;
    }

    public int State { get => state; set => state = value; }
    public int Action { get => action; set => action = value; }
    public int NextState { get => nextState; set => nextState = value; }
    public float Reward { get => reward; set => reward = value; }
    public bool EndState { get => endState; set => endState = value; }
}

public class AgentDQL : MonoBehaviour
{
    public Controller controller;

    //For Deep Q-Learning
    int sizeReplayMemory;
    int indexExperience;
    Experience[] replayMemory;
    int batchReplayMemory;
    List<Experience> batchExperiences;
    public NeuralNetwork policyNetwork, targetNetwork;
    int counterTargetNet, updateTargetNet;
    float rateAveraging;
    bool startLearning;

    [HideInInspector]
    public int n, m;

    void Start()
    {
        n = controller.n;
        m = controller.m;

        //Deep Q-Learning
        sizeReplayMemory = 50;
        indexExperience = 0;
        replayMemory = new Experience[sizeReplayMemory];
        policyNetwork = new NeuralNetwork(1, 2, 4, controller.nActions, 0.45f, 0.6f);
        targetNetwork = new NeuralNetwork(policyNetwork);
        targetNetwork.CopyNN(policyNetwork.hiddenWeights, policyNetwork.outputWeights);
        batchExperiences = new List<Experience>();
        updateTargetNet = 15;
        rateAveraging = 0.01f;
        startLearning = false;
    }

    //Vector2 ChooseActionDQL()
    //{
    //    float n = Random.Range(0f, 1f);
    //    Vector2 action = new Vector2(-1, -1);
    //    if (n < epsilon)
    //    {
    //        action = Action.ChooseRandomAction(isSquare);
    //        if(startLearning)
    //            epsilon = epsilon > minEplison ? epsilon - epsilonDecay : minEplison;
    //        print("Random Action Chosen: " + action);
    //    }
    //    else
    //    {
    //        float maxValue = -999;
    //        FindMaxValueDQL((int)(agentPosition.x + agentPosition.y * n), out action, out maxValue);
    //        print("Action Chosen: " + action + " maxValue: " + maxValue);
    //    }

    //    steps++;
    //    if (steps > maxSteps)
    //    {
    //        end = true;
    //    }
    //    return action;
    //}

    //void FindMaxValueDQL(int index, out Vector2 action, out float maxValue)
    //{
    //    maxValue = -999;
    //    int indexAction = -1;

    //    //Action future that maximize the reward
    //    Vector2 maxValueAction = new Vector2(-1, -1);

    //    policyNetwork.StepsForward(new float[] { index });
    //    float[] output = policyNetwork.getOutput();
    //    for (int i = 0; i < nActions; i++)
    //    {
    //        if (maxValue <  output[i])
    //        {
    //            maxValue = output[i];
    //            indexAction = i;
    //        }
    //    }

    //    action = Action.GetVectorByIndex(indexAction, isSquare);
    //}

    //void PerformActionDeepQ(Vector2 action)
    //{

    //    int indexAction = Action.GetIndexByVector(action, isSquare);
    //    float reward;
    //    float maxNextValue;

    //    int curretStateIndex = (int)(agentPosition.x + agentPosition.y * n);
    //    agentPosition += action;
    //    int nextStateIndex = (int)(agentPosition.x + agentPosition.y * n);

    //    int x = (int)agentPosition.x;
    //    int y = (int)agentPosition.y;
    //    if (checkIndexBoundaries(x, y))
    //    {
    //        previousPlatform = currentPlatform;
    //        currentPlatform = platforms[x, y];

    //        Vector3 pos = currentPlatform.transform.position + Vector3.back;
    //        agent.transform.position = pos;

    //        reward = currentPlatform.reward;

    //        //The value 'any' is not important
    //        Vector2 any;
    //        FindMaxValue(nextStateIndex, out any, out maxNextValue);
    //    }
    //    else
    //    {
    //        //Out of the platform boundaries
    //        end = true;

    //        if (DQLearning)
    //            reward = -1;
    //        else
    //            reward = -10;
    //        maxNextValue = 0;

    //        print("Out of B");
    //    }

    //    //Create new Experience
    //    Experience e = new Experience(curretStateIndex,indexAction,reward,nextStateIndex,end);
    //    //Save Experience in the ReplayMemory (also overwritting the existining ones)
    //    replayMemory[indexExperience % sizeReplayMemory] = e;
    //    indexExperience++;

    //    //Do random actions while the replayMemory is not full
    //    if (indexExperience >= sizeReplayMemory)
    //    {
    //        startLearning = true;
    //    }
    //    if (startLearning) { 
    //        //Choose a random batch size
    //        batchReplayMemory = Random.Range(sizeReplayMemory/10, indexExperience > sizeReplayMemory ? sizeReplayMemory : indexExperience);
    //        //Empty batch experiences
    //        batchExperiences.Clear();
    //        //Select (different) random Experiences
    //        while (batchExperiences.Count < batchReplayMemory)
    //        {
    //            int randExp = Random.Range(0, indexExperience > sizeReplayMemory ? sizeReplayMemory : indexExperience);
    //            if (!batchExperiences.Contains(replayMemory[randExp]))
    //                batchExperiences.Add(replayMemory[randExp]);
    //        }

    //        float error = 0;
    //        for (int i = 0; i < batchExperiences.Count; i++)
    //        {
    //            //Pass the current state through the Policy Network
    //            float[] currentState = new float[] { batchExperiences[i].State };
    //            policyNetwork.StepsForward(currentState);
    //            //Get the output given the choosen Action
    //            int currentAction = batchExperiences[i].Action;
    //            float currentQValue = policyNetwork.GetOutputByActionIndex(currentAction);

    //            //Pass the next state through the Target Network
    //            float[] nextState = new float[] { batchExperiences[i].NextState };
    //            targetNetwork.StepsForward(nextState);
    //            //Get the max output
    //            float nextQValue = targetNetwork.GetMaxOutput();

    //            //Calculate the loss between the choosen Action and the best one
    //            float theta = batchExperiences[i].EndState ? batchExperiences[i].Reward : batchExperiences[i].Reward + learningRate * nextQValue;
    //            float loss = 0.5f * Mathf.Pow(theta - currentQValue, 2);
    //            //Calculate Error's mean
    //            error += loss / batchExperiences.Count;


    //            //Calculate delta loss for backpropagation w.r.t currentQValue
    //            float deltaLoss = (theta - currentQValue);

    //            Debug.LogWarning("Loss: " + loss + " deltaLoss: " + deltaLoss);

    //            for (int j = 0; j < 4; j++)
    //            {
    //                Debug.LogWarning("Actual: " + policyNetwork.getOutput()[j] + " Target: " + targetNetwork.getOutput()[j] + " diff: " + (targetNetwork.getOutput()[j]-policyNetwork.getOutput()[j]));
    //            }

    //            //BackPropagate error and update weights
    //            policyNetwork.StepsBackward(targetNetwork.getOutput(), deltaLoss);//targetNetwork.getOutput()
    //        }

    //        counterTargetNet++;
    //        if (counterTargetNet > updateTargetNet && startLearning)
    //        {
    //            //Update Target Net weights with the Policy Net ones
    //            //Instead of changing each weights and biases
    //            //It is faster (and the same) to make the 2 Nets equals
    //            //DO NOT USE
    //            //targetNetwork = policyNetwork;

    //            //It seem to give problem with the method above, so use this method
    //            targetNetwork.CopyNN(policyNetwork.hiddenWeights,policyNetwork.outputWeights);

    //            counterTargetNet = 0;
    //        }
    //    }

    //    end = end ? end : currentPlatform.CheckGameState();
    //    //print(oldQvalue + " " + qTable[curretStateIndex, indexAction]);
    //}
}
