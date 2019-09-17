using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

static class Action
    {
        static public int nAction = 4;

        static public Vector2 up = Vector2.up;
        static public Vector2 right = Vector2.right;
        static public Vector2 down = Vector2.down;
        static public Vector2 left = Vector2.left;

        static public Vector2 ChooseRandomAction()
        {
            int i = Random.Range(0, nAction);
            Vector2 r = GetVectorByIndex(i);
            return r;
        }

        static public Vector2 GetVectorByIndex(int index)
        {
            Vector2 r = new Vector2(-1,-1);
            switch (index)
            {
                case 0:
                    r = up;
                    break;
                case 1:
                    r = right;
                    break;
                case 2:
                    r = down;
                    break;
                case 3:
                    r = left;
                    break;
            }
            return r;
        }

        static public int GetIndexByVector(Vector2 v)
        {
            int r = -1;
            if (v == up)
                r = 0;
            else if (v == right)
                r = 1;
            else if (v == down)
                r = 2;
            else if (v == left)
                r = 3; 
            return r;
        }
    }
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

static class SnakeState
{
    static public int nState = 6;

    //1->presence, 0->no presence

    //Chech if in the r,l,f positions there are obstacles (end of box, snake's body)
    static public int obstacleRight, obstacleLeft, obstacleForward;

    //Position of the food
    static public int foodRight, foodLeft, foodForward;
}

public class Controller : MonoBehaviour
{
    int epoch, steps, maxSteps;

    //Box's side length
    public int size;

    //Grid of Platforms
    public Platform[,] platforms;

    //Q-Table with first index the length of the states (size*size) and second index the possible actions (Up,Right,Down,Left)
    //At the beginning it is initialized to zero 
    public float[,] qTable;

    [HideInInspector]
    public Platform agentStartingPlatform, maxRewardPlatform, minRewardPlatform;
    List<Vector2> obstaclesPoints = new List<Vector2>();
    Platform currentPlatform, previousPlatform;

    //Used to determine the end of an episode
    public bool pause;

    public GameObject agentPrefab;
    GameObject agent;
    Vector2 agentPosition;

    public Text epsilonText, stepsText, epochText;
    public Slider slider;

    float epsilon, minEplison, epsilonDecay, learningRate, discountRate;

    //For Deep Q-Learning
    public bool DQLearning;
    int sizeReplayMemory;
    int indexExperience;
    Experience[] replayMemory;
    int batchReplayMemory;
    List<Experience> batchExperiences;
    public NeuralNetwork policyNetwork, targetNetwork;
    int counterTargetNet, updateTargetNet;
    float rateAveraging;
    bool startLearning;

    //For Snake
    Dictionary<int[], float[]> snakeQtable;
    public bool snake;
    float distance;
    public bool eatFood, spawnedHead;
    int snakeState;

    //Used to have some waiting time between actions
    float timer, maxTimer;
    

    // Start is called before the first frame update
    void Start()
    {
        Starting();
    }

    bool canStart;
    // Update is called once per frame
    void Update()
    {
        if (canStart)
        {
            //Change waiting time with the slider at runtime
            maxTimer = 1.1f + slider.value;

            timer += Time.deltaTime;
            if (!pause)
            {
                if (timer > maxTimer)
                {
                    Vector2 action;

                    if (DQLearning)
                    {
                        //Path Finding with deep network
                        action = ChooseActionDQL();
                        PerformActionDeepQ(action);
                    }
                    else
                    {
                        //Path Finding
                        action = ChooseAction();
                        PerformAction(action);
                    }
                    timer = 0;
                }
            }
            else
            {
                if (timer > maxTimer)
                {
                    Restart();
                    print("Restart");
                    timer = 0;
                }
            }

            //Not best solution
            epsilonText.text = "EPSILON: " + epsilon.ToString("F3");
            stepsText.text = "STEPS: " + steps;
            epochText.text = "EPISODE: " + epoch;

        }

        if (Input.GetKeyDown(KeyCode.A)) canStart = true;
    }

    public void Starting()
    {
        //size = 5; //it is changed in PlatformGenerator class
        if (!snake)
        {
            agent = Instantiate(agentPrefab, new Vector2(-999, -999), Quaternion.identity);
        }
        epsilon = 1f;
        minEplison = 0.1f;
        epsilonDecay = 0.01f/size;
        learningRate = 0.8f;
        discountRate = 0.9f;
        //More than 2 times the diagonal'length (sqrt(2)*size ~= 1.4*size)
        maxSteps = size * 3;

        //Create QTable: rows are the states (size * size) and columns are the actions
        if (snake)
        {
            snakeQtable = new Dictionary<int[], float[]>();
            InizializeQTableSnake();
        }
        else
            qTable = new float[size * size, Action.nAction];

        //Q-Learning
        if (DQLearning)
        {
            sizeReplayMemory = 50;
            indexExperience = 0;
            replayMemory = new Experience[sizeReplayMemory];
            policyNetwork = new NeuralNetwork(1, 2, 4, Action.nAction, 0.45f, 0.6f);
            targetNetwork = new NeuralNetwork(policyNetwork);
            targetNetwork.CopyNN(policyNetwork.hiddenWeights, policyNetwork.outputWeights);
            batchExperiences = new List<Experience>();
            updateTargetNet = 15;
            rateAveraging = 0.01f;
            startLearning = false;
        }

        Restart();
    }

    void Restart()
    {
        agentPosition = agentStartingPlatform.point;
        if(minRewardPlatform)
            minRewardPlatform.EnableMinReward();
        Vector3 pos = agentStartingPlatform.transform.position + Vector3.back;
        if (!snake)
            agent.transform.position = pos;
        else
        {
            agent.transform.position = Vector3.back;
            agentStartingPlatform.free = false;
        }
        currentPlatform = agentStartingPlatform;
        previousPlatform = null;

        pause = false;

        timer = 0;
        maxTimer = 1f;

        steps = 0;
        epoch++;
    }
    
    void InizializeQTableSnake()
    {
        float[] value = new float[Action.nAction];

        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                for (int k = 0; k < 2; k++)
                {
                    for (int l = 0; l < 2; l++)
                    {
                        for (int m = 0; m < 2; m++)
                        {
                            for (int n = 0; n < 2; n++)
                            {
                                snakeQtable.Add(new int[] { i, j, k, l, m, n },value);
                            }
                        }
                    }
                }
            }
        }
    }
    

    Vector2 ChooseAction()
    {
        float n = Random.Range(0f,1f);
        Vector2 action = new Vector2(-1,-1);
        if (n < epsilon)
        {
            action = Action.ChooseRandomAction();
            epsilon = epsilon > minEplison ? epsilon - epsilonDecay : minEplison;
            print("Random Action Chosen: " + action);
        }
        else
        {
            float maxValue = -999;
            FindMaxValue((int)(agentPosition.x + agentPosition.y * size), out action, out maxValue);
            //Since an optimal action was not found, choose a random one
            //It may happen that some actions are never choosen (There should be a check if all the value's actions a 0 then..)
            //if (maxValue == 0)
            //    action = Action.ChooseRandomAction();
            print("Action Chosen: " + action);
        }

        steps++;
        if (steps>maxSteps)
        {
            pause = true;
        }
        return action;
    }
    Vector2 ChooseActionDQL()
    {
        float n = Random.Range(0f, 1f);
        Vector2 action = new Vector2(-1, -1);
        if (n < epsilon)
        {
            action = Action.ChooseRandomAction();
            if(startLearning)
                epsilon = epsilon > minEplison ? epsilon - epsilonDecay : minEplison;
            print("Random Action Chosen: " + action);
        }
        else
        {
            float maxValue = -999;
            FindMaxValueDQL((int)(agentPosition.x + agentPosition.y * size), out action, out maxValue);
            print("Action Chosen: " + action + " maxValue: " + maxValue);
        }

        steps++;
        if (steps > maxSteps)
        {
            pause = true;
        }
        return action;
    }
    int ChooseActionSnake(int[] state)
    {
        float n = Random.Range(0f, 1f);
        int action = -1;
        if (n < epsilon)
        {
            //Select an action between 0 and 3 (0->f, 1->r, 2->d, 3->l)
            action = Random.Range(0,3);
            epsilon = epsilon > minEplison ? epsilon - epsilonDecay : minEplison;
            print("Random Action Chosen: " + action);
        }
        else
        {
            float maxValue = -999;
            FindMaxValueSnake(state, out action, out maxValue);
            //Since an optimal action was not found, choose a random one
            //It may happen that some actions are never choosen (There should be a check if all the value's actions a 0 then..)
            //if (maxValue == 0)
            //    action = Action.ChooseRandomAction();
            print("Action Chosen: " + action + " value: " + maxValue);
        }

        steps++;
        if (steps > maxSteps)
        {
            pause = true;
        }
        return action;
    }

    void FindMaxValue(int index, out Vector2 action, out float maxValue)
    {
        maxValue = -999;
        int indexAction = -1;

        //Action future that maximize the reward
        Vector2 maxValueAction = new Vector2(-1, -1);

        for (int i = 0; i < Action.nAction; i++)
        {
            if (maxValue < qTable[index, i])
            {
                maxValue = qTable[index, i];
                indexAction = i;
            }
        }
        
        action = Action.GetVectorByIndex(indexAction);
    }
    void FindMaxValueDQL(int index, out Vector2 action, out float maxValue)
    {
        maxValue = -999;
        int indexAction = -1;

        //Action future that maximize the reward
        Vector2 maxValueAction = new Vector2(-1, -1);

        policyNetwork.StepsForward(new float[] { index });
        float[] output = policyNetwork.getOutput();
        for (int i = 0; i < Action.nAction; i++)
        {
            if (maxValue <  output[i])
            {
                maxValue = output[i];
                indexAction = i;
            }
        }

        action = Action.GetVectorByIndex(indexAction);
    }
    void FindMaxValueSnake(int[] index, out int action, out float maxValue)
    {
        maxValue = -999;
        int indexAction = -1;
        
        float[] values = new float[Action.nAction];
        
        //snakeQtable.TryGetValue(index,out values);
        int counter = 0;
        foreach (int[] item in snakeQtable.Keys)
        {
            if (Enumerable.SequenceEqual(item,index))
            {
                values = snakeQtable.ElementAt(counter).Value;
            }
            counter++;
        }
        //values = snakeQtable.Where((k,v) => Enumerable.SequenceEqual(k, index));

        for (int i = 0; i < values.Length; i++)
        {
            if (maxValue < values[i])
            {
                maxValue = values[i];
                indexAction = i;
            }
        }

        action = indexAction;
    }

    bool checkIndexBoundaries(int x, int y)
    {
        if (x >= 0 && x < platforms.GetLength(0) && y >= 0 && y < platforms.GetLength(1))
            return true;
        return false;
    }

    //Move the Agent and update the Q-Value for the current state
    void PerformAction(Vector2 action)
    {

        int indexAction = Action.GetIndexByVector(action);
        float reward;
        float maxNextValue;
        
        int curretStateIndex = (int)(agentPosition.x + agentPosition.y * size);
        agentPosition += action;
        int nextStateIndex = (int)(agentPosition.x + agentPosition.y * size);

        int x = (int)agentPosition.x;
        int y = (int)agentPosition.y;
        if (checkIndexBoundaries(x,y))
        {
            previousPlatform = currentPlatform;
            currentPlatform = platforms[x, y];

            Vector3 pos = currentPlatform.transform.position + Vector3.back;
            agent.transform.position = pos;

            reward = currentPlatform.reward;

            //The min reward can be taken only once
            if (currentPlatform.minRewardPoint)
                currentPlatform.DisableMinReward();

            Vector2 any;
            FindMaxValue(nextStateIndex, out any, out maxNextValue);
        }
        else
        {
            //Out of the platform boundaries
            pause = true;

            reward = -10;
            maxNextValue = 0;

            print("Out of B");
        }
       
        float oldQvalue = qTable[curretStateIndex, indexAction];
        //Bellman Equation
        qTable[curretStateIndex, indexAction] = oldQvalue + learningRate * (reward + discountRate * maxNextValue - oldQvalue);
        pause = pause ? pause : currentPlatform.CheckGameState();
        print(oldQvalue + " " + qTable[curretStateIndex, indexAction]);
    }
    void PerformActionDeepQ(Vector2 action)
    {

        int indexAction = Action.GetIndexByVector(action);
        float reward;
        float maxNextValue;

        int curretStateIndex = (int)(agentPosition.x + agentPosition.y * size);
        agentPosition += action;
        int nextStateIndex = (int)(agentPosition.x + agentPosition.y * size);

        int x = (int)agentPosition.x;
        int y = (int)agentPosition.y;
        if (checkIndexBoundaries(x, y))
        {
            previousPlatform = currentPlatform;
            currentPlatform = platforms[x, y];

            Vector3 pos = currentPlatform.transform.position + Vector3.back;
            agent.transform.position = pos;

            reward = currentPlatform.reward;

            //The value 'any' is not important
            Vector2 any;
            FindMaxValue(nextStateIndex, out any, out maxNextValue);
        }
        else
        {
            //Out of the platform boundaries
            pause = true;

            if (DQLearning)
                reward = -1;
            else
                reward = -10;
            maxNextValue = 0;

            print("Out of B");
        }
        
        //Create new Experience
        Experience e = new Experience(curretStateIndex,indexAction,reward,nextStateIndex,pause);
        //Save Experience in the ReplayMemory (also overwritting the existining ones)
        replayMemory[indexExperience % sizeReplayMemory] = e;
        indexExperience++;

        //Do random actions while the replayMemory is not full
        if (indexExperience >= sizeReplayMemory)
        {
            startLearning = true;
        }
        if (startLearning) { 
            //Choose a random batch size
            batchReplayMemory = Random.Range(sizeReplayMemory/10, indexExperience > sizeReplayMemory ? sizeReplayMemory : indexExperience);
            //Empty batch experiences
            batchExperiences.Clear();
            //Select (different) random Experiences
            while (batchExperiences.Count < batchReplayMemory)
            {
                int randExp = Random.Range(0, indexExperience > sizeReplayMemory ? sizeReplayMemory : indexExperience);
                if (!batchExperiences.Contains(replayMemory[randExp]))
                    batchExperiences.Add(replayMemory[randExp]);
            }

            float error = 0;
            for (int i = 0; i < batchExperiences.Count; i++)
            {
                //Pass the current state through the Policy Network
                float[] currentState = new float[] { batchExperiences[i].State };
                policyNetwork.StepsForward(currentState);
                //Get the output given the choosen Action
                int currentAction = batchExperiences[i].Action;
                float currentQValue = policyNetwork.GetOutputByActionIndex(currentAction);

                //Pass the next state through the Target Network
                float[] nextState = new float[] { batchExperiences[i].NextState };
                targetNetwork.StepsForward(nextState);
                //Get the max output
                float nextQValue = targetNetwork.GetMaxOutput();

                //Calculate the loss between the choosen Action and the best one
                float theta = batchExperiences[i].EndState ? batchExperiences[i].Reward : batchExperiences[i].Reward + learningRate * nextQValue;
                float loss = 0.5f * Mathf.Pow(theta - currentQValue, 2);
                //Calculate Error's mean
                error += loss / batchExperiences.Count;


                //Calculate delta loss for backpropagation w.r.t currentQValue
                float deltaLoss = (theta - currentQValue);

                Debug.LogWarning("Loss: " + loss + " deltaLoss: " + deltaLoss);

                for (int j = 0; j < 4; j++)
                {
                    Debug.LogWarning("Actual: " + policyNetwork.getOutput()[j] + " Target: " + targetNetwork.getOutput()[j] + " diff: " + (targetNetwork.getOutput()[j]-policyNetwork.getOutput()[j]));
                }

                //BackPropagate error and update weights
                policyNetwork.StepsBackward(targetNetwork.getOutput(), deltaLoss);//targetNetwork.getOutput()
            }

            counterTargetNet++;
            if (counterTargetNet > updateTargetNet && startLearning)
            {
                //Update Target Net weights with the Policy Net ones
                //Instead of changing each weights and biases
                //It is faster (and the same) to make the 2 Nets equals
                //DO NOT USE
                //targetNetwork = policyNetwork;

                //It seem to give problem with the method above, so use this method
                targetNetwork.CopyNN(policyNetwork.hiddenWeights,policyNetwork.outputWeights);

                counterTargetNet = 0;
            }
        }

        pause = pause ? pause : currentPlatform.CheckGameState();
        //print(oldQvalue + " " + qTable[curretStateIndex, indexAction]);
    }

    void CheckGameState(Vector2 action)
    {
        agentPosition += action;
        Platform platform = platforms[(int)agentPosition.x, (int)agentPosition.y];
        pause = platform.CheckGameState();
    }

    //Debug
    //private void OnGUI()
    //{
    //    for (int i = 0; i < 4; i++)
    //    {
    //        for (int j = 0; j < size * size; j++)
    //        {
    //            int x = 65;
    //            int y = 12;
    //            Rect r = new Rect(750 + i * x, 150 + j * y, 100, 100);
    //            GUI.Label(r, qTable[j, i].ToString("F4"));
    //        }
    //    }
    //}

}
