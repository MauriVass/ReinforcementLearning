using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;


public class Controller : MonoBehaviour
{
    /*This script controlls the action of the agent*/

    [HideInInspector]
    public int epoch, steps, maxSteps, nActions;

    //Number of platforms in each rows and comumns
    [HideInInspector]
    public int n, m;

    //Grid of Platforms
    public Platform[,] platforms;

    [HideInInspector]
    public Platform agentStartingPlatform, maxRewardPlatform, minRewardPlatform;
    List<Vector2> obstaclesPoints = new List<Vector2>();
    [HideInInspector]
    public Platform currentPlatform, previousPlatform;

    [HideInInspector]
    public bool end, isSquare, DQLearning;

    public GameObject agentPrefab;
    [HideInInspector]
    public GameObject agent;
    public AgentQTable agentQTable;
    public AgentDQL agentDQL;
    [HideInInspector]
    public Vector2 agentPosition;

    public Text epsilonText, stepsText, epochText, totalRewardText;
    public Slider slider;

    [HideInInspector]
    public float epsilon, minEplison, epsilonDecay, learningRate, discountRate, totalReward;

    //Used to have some waiting time between actions
    float timer, maxTimer;

    //Used to start learning after game started (Press key S)
    //todo
    bool canStart = true;

    void Start()
    {
        Starting();
    }

    public void Starting()
    {
        print($"/// Starting Controller with isSquare:{isSquare} ///");
        //Spawn agent
        agent = Instantiate(agentPrefab, new Vector2(-999, -999), Quaternion.identity);

        //Get data from menu
        DQLearning = ManagerScenes.isDeepQN;
        isSquare = ManagerScenes.isSquare;

        if (!DQLearning){
            agentQTable = agent.GetComponent<AgentQTable>();
            agentQTable.enabled = true;
            agentQTable.controller = this;
        }
        else {
            agentDQL = agent.GetComponent<AgentDQL>();
            agentDQL.enabled = true;
            agentDQL.controller = this;
        }


        epsilon = 1f;
        minEplison = 0.1f;
        epsilonDecay = 0.05f / (n + m);
        learningRate = 0.75f;
        discountRate = 0.85f;
        //3 times the diagonal (more less) length (sqrt(2)*size ~= 1.4*size)
        maxSteps = (int) Mathf.Sqrt( (n*n + m*m) ) * 3 ;
        nActions = isSquare ? ActionSquare.nAction : ActionHex.nAction;

        Restart();
    }

    void Update()
        {
            if (canStart)
            {
                //Change waiting time with the slider at runtime
                maxTimer = 5.3f - slider.value;

                timer += Time.deltaTime;
                if (!end)
                {
                    if (timer > maxTimer)
                    {
                        Vector2 action;

                        if (DQLearning)
                        {
                            //Path Finding with deep network
                            action = agentDQL.ChooseAction();
                            agentDQL.PerformAction(action);
                        }
                        else
                        {
                            //Path Finding Q-Table
                            action = agentQTable.ChooseAction();
                            agentQTable.PerformAction(action);
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
                totalRewardText.text = "TOTAL REWARD: " + totalReward;
            }

            if (Input.GetKeyDown(KeyCode.S)) canStart = true;
        }

    void Restart()
    {
        agentPosition = agentStartingPlatform.point;
        if (minRewardPlatform)
            minRewardPlatform.EnableMinReward();
        Vector3 pos = agentStartingPlatform.transform.position + Vector3.back * 0.1f;
        agent.transform.position = pos;
        currentPlatform = agentStartingPlatform;
        previousPlatform = null;

        end = false;

        timer = 0;
        maxTimer = 1f;

        steps = 0;
        totalReward = 0;
        epoch++;
    }

    public bool checkIndexBoundaries(float x, float y)
    {
        if (x >= 0 && x < n && y >= 0 && y < m)
            return true;
        return false;
    }

    public void CheckGameState(Vector2 action)
    {
        agentPosition += action;
        Platform platform = platforms[(int)agentPosition.x, (int)agentPosition.y];
        end = platform.CheckGameState();
    }


    public float[] getCurrentState(int nInput, Vector2 pos)
    {
        float[] state = new float[nInput];

        //1 -> lead to a positive state
        //0 -> lead to a negative state (obstacle, far from objective, ...)
        if (true)//Remove false (when there are only 4 input and you want directions instead of obstacles)
        {
            if (isSquare)
            {
                // up, right, down, left
                state[0] = checkIndexBoundaries(pos.x, pos.y + 1) && !platforms[(int)pos.x, (int)pos.y + 1].punishmentPoint ? 1 : 0;
                state[1] = checkIndexBoundaries(pos.x + 1, pos.y) && !platforms[(int)pos.x + 1, (int)pos.y].punishmentPoint ? 1 : 0;
                state[2] = checkIndexBoundaries(pos.x, pos.y - 1) && !platforms[(int)pos.x, (int)pos.y - 1].punishmentPoint ? 1 : 0;
                state[3] = checkIndexBoundaries(pos.x - 1, pos.y) && !platforms[(int)pos.x - 1, (int)pos.y].punishmentPoint ? 1 : 0;
            }
            else
            {
                // up, right-up, right-down, down, left-down, left-up
                state[0] = checkIndexBoundaries(pos.x, pos.y + 2) ? 1 : 0;
                state[1] = checkIndexBoundaries(pos.x + 1, pos.y) ? 1 : 0;
                state[2] = checkIndexBoundaries(pos.x, pos.y - 1) ? 1 : 0;
                state[3] = checkIndexBoundaries(pos.x - 1, pos.y) ? 1 : 0;
            }
        }

        //Top Down
        if (maxRewardPlatform.point.y > pos.y)
            state[nInput - 4] = 1;
        else if (maxRewardPlatform.point.y < pos.y) state[nInput - 2] = 1;
        //otherwise both state[nInput-4] and state[nInput-2] are 0 (default value)

        //Right left
        if (maxRewardPlatform.point.x > pos.x)
            state[nInput - 3] = 1;
        else if (maxRewardPlatform.point.x < pos.x) state[nInput - 1] = 1;
        //otherwise both state[nInput-3] and state[nInput-1] are 0 (default value)

        return state;
    }

}
