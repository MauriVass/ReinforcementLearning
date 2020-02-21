using System.Collections;
using System.Collections.Generic;
using UnityEngine;

static class ActionSquare
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
        Vector2 r = new Vector2(-1, -1);
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
static class ActionHex
{
    static public int nAction = 6;

    static public Vector2 up = 2 * Vector2.up;
    static public Vector2[] rightUp = new Vector2[] { Vector2.up, Vector2.up + Vector2.right };
    static public Vector2[] rightDown = new Vector2[] { Vector2.down, Vector2.down + Vector2.right };
    static public Vector2 down = 2 * Vector2.down;
    static public Vector2[] leftDown = new Vector2[] { Vector2.left + Vector2.down, Vector2.down };
    static public Vector2[] leftUp = new Vector2[] { Vector2.left + Vector2.up, Vector2.up };

    static public Vector2 ChooseRandomAction(int remainder)
    {
        int i = Random.Range(0, nAction);
        Vector2 r = GetVectorByIndex(i, remainder);
        return r;
    }

    //'remainder' is the reminder of the division of the y position by 2 (r=y%2)
    static public Vector2 GetVectorByIndex(int index, int remainder)
    {
        Vector2 r = new Vector2(-1, -1);
        switch (index)
        {
            case 0:
                r = up;
                break;
            case 1:
                r = rightUp[remainder];
                break;
            case 2:
                r = rightDown[remainder];
                break;
            case 3:
                r = down;
                break;
            case 4:
                r = leftDown[remainder];
                break;
            case 5:
                r = leftUp[remainder];
                break;
        }
        return r;
    }

    static public int GetIndexByVector(Vector2 v, int remainder)
    {
        int r = -1;

        if (v == up)
            r = 0;
        else if (v == rightUp[remainder])
            r = 1;
        else if (v == rightDown[remainder])
            r = 2;
        else if (v == down)
            r = 3;
        else if (v == leftDown[remainder])
            r = 4;
        else if (v == leftUp[remainder])
            r = 5;

        return r;
    }
}

public class AgentQTable : MonoBehaviour
{
    public Controller controller;

    //Q-Table with first index the length of the states (n*m) and second index the possible actions (Up,Right,Down,Left)
    //At the beginning it is initialized to zero 
    public float[,] qTable;

    int n, m;

    void Start()
    {
        n = controller.n;
        m = controller.m;
        //Create QTable: rows are the states (n * m) and columns are the actions
        qTable = new float[n * m, controller.nActions];
    }

    public Vector2 ChooseAction()
    {
        //Choose a random number n between 0 and 1
        float v = Random.Range(0f, 1f);
        Vector2 action = new Vector2(-1, -1);
        if (v < controller.epsilon)
        {
            //If n is less than the epsilon choose a random action
            action = controller.isSquare ? ActionSquare.ChooseRandomAction() : ActionHex.ChooseRandomAction((int)(controller.agentPosition.y) % 2);
            controller.epsilon = controller.epsilon > controller.minEplison ? controller.epsilon - controller.epsilonDecay : controller.minEplison;
            print("Random Action Chosen: " + action);
        }
        else
        {
            //Otherwise choose an action based on the max value in the QTable of the current state(platform position)
            float maxValue = -999;
            FindMaxValue((int)(controller.agentPosition.x + controller.agentPosition.y * n), out action, out maxValue);
            print("Action Chosen: " + action);
        }

        controller.steps++;
        if (controller.steps > controller.maxSteps)
        {
            controller.end = true;
        }
        return action;
    }

    public void FindMaxValue(int index, out Vector2 action, out float maxValue)
    {
        maxValue = -999;
        int indexAction = -1;

        //Future action that maximize the reward
        Vector2 maxValueAction = new Vector2(-1, -1);

        for (int i = 0; i < controller.nActions; i++)
        {
            if (maxValue < qTable[index, i])
            {
                maxValue = qTable[index, i];
                indexAction = i;
            }
        }

        action = controller.isSquare ? ActionSquare.GetVectorByIndex(indexAction) : ActionHex.GetVectorByIndex(indexAction, (int)(controller.agentPosition.y) % 2);
    }

    //Move the Agent and update the Q-Value for the current state
    public void PerformAction(Vector2 action)
    {
        Vector2 agentPos = controller.agentPosition;
        int indexAction = -1;
        indexAction = controller.isSquare ? ActionSquare.GetIndexByVector(action) : ActionHex.GetIndexByVector(action, (int)(agentPos.y) % 2);
        float reward;
        float maxNextValue;

        //Transfrom 2D position (x,y) in 1D index (x+y*n)
        int curretStateIndex = (int)(agentPos.x + agentPos.y * controller.n);
        //Update Agent position (current position + new action)
        agentPos += action;
        int nextStateIndex = (int)(agentPos.x + agentPos.y * controller.n);

        //Update Agent position in the controller
        controller.agentPosition += action;

        int x = (int)agentPos.x;
        int y = (int)agentPos.y;
        if (controller.checkIndexBoundaries(x, y))
        {
            controller.previousPlatform = controller.currentPlatform;
            controller.currentPlatform = controller.platforms[x, y];

            Vector3 pos = controller.currentPlatform.transform.position + Vector3.back * 0.1f;
            controller.agent.transform.position = pos;

            reward = controller.currentPlatform.reward;
            controller.totalReward += reward;

            //The min reward can be taken only once
            if (controller.currentPlatform.minRewardPoint)
                controller.currentPlatform.DisableMinReward();

            Vector2 any;
            FindMaxValue(nextStateIndex, out any, out maxNextValue);
        }
        else
        {
            //Out of the platform boundaries
            controller.end = true;

            reward = -10;
            maxNextValue = 0;

            print("Out of Boundary");
        }

        float oldQvalue = qTable[curretStateIndex, indexAction];
        //Bellman Equation
        qTable[curretStateIndex, indexAction] = oldQvalue + controller.learningRate * (reward + controller.discountRate * maxNextValue - oldQvalue);
        controller.end = controller.end ? controller.end : controller.currentPlatform.CheckGameState();
    }
}
