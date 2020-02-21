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
    bool canStart;

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

        if (!DQLearning)
            agentQTable = agent.GetComponent<AgentQTable>();
        else agentDQL = agent.GetComponent<AgentDQL>();
        agentQTable.controller = this;


        epsilon = 1f;
        minEplison = 0.1f;
        epsilonDecay = 0.01f / (n + m) / 2;
        learningRate = 0.75f;
        discountRate = 0.85f;
        //3 times the diagonal (more less) length (sqrt(2)*size ~= 1.4*size)
        maxSteps = (n + m) / 2 * 4;
        nActions = isSquare ? ActionSquare.nAction : ActionHex.nAction;

        Restart();
    }

    void Update()
        {
            if (canStart)
            {
                //Change waiting time with the slider at runtime
                maxTimer = 5.1f - slider.value;

                timer += Time.deltaTime;
                if (!end)
                {
                    if (timer > maxTimer)
                    {
                        Vector2 action;

                        if (DQLearning)
                        {
                            //Path Finding with deep network
                            action = agentQTable.ChooseAction();
                            agentQTable.PerformAction(action);
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

    public bool checkIndexBoundaries(int x, int y)
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

}
