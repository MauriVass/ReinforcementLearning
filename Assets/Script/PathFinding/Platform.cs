using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Platform : MonoBehaviour
{
    //Platform Object
    public bool isCube;

    //These tell what is the nature of each platform:
    //StartingPoint is the platform from where the agent starts
    //MaxRewardPoint is the platform with the max reward (Game ends when agent is here)
    //MinRewardPoint is the platform with some reward (Game does not end when agent is here)
    //PunishmentPoint is the platform with max negative reward (Game ends when agent is here)
    //If the platform is not any of these is an 'empty' platform and this can be 0 or some negative reward (Depends on the policy used)
    public bool startingPoint, maxRewardPoint, minRewardPoint, punishmentPoint;
    //Reward change based on the nature of the platform
    public float reward;
    //Coordinate position (x,y)
    public Vector2 point;

    public Canvas canvas;
    //Texts to show the qTable values
    public Text[] text;
    //Spacing between each text and the center
    public float minValue;

    public Controller controller;
    public AgentQTable agentQTable;
    public AgentDQL agentDQL;

    //Default rewards
    public float maxReward, minReward, punishment, empty;

    void Start()
    {
        agentQTable = controller.agentQTable;
        agentDQL = controller.agentDQL;

        //Reward multiplier
        int r = 10;
        //Rewards should be normalized between -1 and 1
        if (controller.DQLearning)
            r = 1;
        maxReward = 1 * r;
        minReward = 0.5f * r;
        punishment = -1 * r;
        //Larger is the platform size lower should be this value
        //Negative reward for each step -> minimize the steps required to reach the goal point
        empty = -0.1f * r;

        if (maxRewardPoint)
            reward = maxReward;
        else if (minRewardPoint)
            reward = minReward;
        else if (punishmentPoint)
            reward = punishment;
        else
            reward = empty;

        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;

        if (isCube)
        {
            //Set the position of the texts of each platform in the 4 positions (Up,Right,Down,Left)
            //minvalue is the half size of a platform
            //Greater is the factor, farther is the text from the center
            float factor = 0.25f;
            text[0].transform.position = transform.position + new Vector3(0, minValue * factor);
            text[1].transform.position = transform.position + new Vector3(minValue * factor, 0);
            text[2].transform.position = transform.position + new Vector3(0, -minValue * factor);
            text[3].transform.position = transform.position + new Vector3(-minValue * factor, 0);

            text[4].enabled = false;
            //text[4].transform.position = transform.position;
            //text[4].text = (point.y * controller.size + point.x).ToString();
        }
        else
        {
            //Set the position of the texts of each platform in the 6 positions (Up,RightUp,RightDown,Down,LeftDown,LeftUp)
            //minvalue is the half size of a platform
            //Greater is the factor, farther is the text from the center
            float factorTD = 0.35f;
            float factorLR = 0.2f;
            text[0].transform.position = transform.position + new Vector3(0, minValue * factorTD);
            text[1].transform.position = transform.position + new Vector3(minValue * factorLR, minValue * factorLR);
            text[2].transform.position = transform.position + new Vector3(minValue * factorLR, -minValue * factorLR);
            text[3].transform.position = transform.position + new Vector3(0, -minValue * factorTD);
            text[4].transform.position = transform.position + new Vector3(-minValue * factorLR, -minValue * factorLR);
            text[5].transform.position = transform.position + new Vector3(-minValue * factorLR, minValue * factorLR);

            text[6].enabled = false;
            text[6].transform.position = transform.position;
            text[6].text = (point.y * controller.n + point.x).ToString() + point;
        }

        for (int i = 0; i < text.Length; i++)
        {
            text[i].fontSize = 8;
        }
    }

    //Not best solution
    void LateUpdate()
    {
        int c = 0;
        //Update text values
        if (!punishmentPoint && !maxRewardPoint)
        {
            for (int i = 0; i < text.Length-1; i++)
            {
                if (controller.DQLearning)
                {
                    if (agentDQL.policyNetwork != null)
                    {
                        //agentDQL.policyNetwork.StepsForward(new float[] { point.y * agentDQL.n + point.x });
                        //agentDQL.targetNetwork.StepsForward(new float[] { point.y * agentDQL.n + point.x });
                        //text[i].text = "P: " + agentDQL.policyNetwork.output[i].ToString("F2") + "\nT: " + agentDQL.targetNetwork.output[i].ToString("F2");
                    }
                }
                else
                    text[i].text = (agentQTable.qTable[(int)(point.y * controller.n + point.x), i]).ToString("F2");
            }
        }
        else
        {
            canvas.enabled = false;
        }
    }

    Color minRewardColor = Color.yellow;
    //Change platform's color according with its type (StartingPoint, MaxRewardPoint, PunishmentPoint)
    public void Visualize()
    {
        if (startingPoint)
            GetComponent<SpriteRenderer>().color = Color.cyan;
        else if (maxRewardPoint)
            GetComponent<SpriteRenderer>().color = Color.green;
        else if(minRewardPoint)
            GetComponent<SpriteRenderer>().color = minRewardColor;
        else if (punishmentPoint)
            GetComponent<SpriteRenderer>().color = Color.red;
    }

    //Check if the game should be ended
    public bool CheckGameState()
    {
        if (punishmentPoint || maxRewardPoint)
            return true;
        else return false;
    }

    public void SetObstaclePoint()
    {
        punishmentPoint = true;
        Visualize();
        //This make invisible the canvas in order to avoid visualzing the Q-Table value
        canvas.enabled = false;
    }
    public void SetMaxRewardPoint()
    {
        maxRewardPoint = true;
        Visualize();
        //This make invisible the canvas in order to avoid visualzing the Q-Table value
        canvas.enabled = false;
    }
    public void SetMinRewardPoint()
    {
        minRewardPoint = true;
        Visualize();
    }
    public void SetStartingPoint()
    {
        startingPoint = true;
        Visualize();
    }

    public void DisableMinReward()
    {
        if (minRewardPoint)
        {
            reward = empty;
            GetComponent<SpriteRenderer>().color = Color.white;
        }
    }
    public void EnableMinReward()
    {
        if (minRewardPoint)
        {
            reward = minReward;
            GetComponent<SpriteRenderer>().color = minRewardColor;
        }
    }
}
