using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Platform : MonoBehaviour
{
    public bool startingPoint, maxRewardPoint, minRewardPoint, punishmentPoint;
    public float reward;
    public Vector2 point;

    public Canvas canvas;
    public Text[] text;

    public float minValue;
    public Controller controller;

    public float maxReward, minReward, punishment, empty;
    public bool free;

    void Start()
    {
        int v = 10;
        //Rewards should be normalized between -1 and 1
        if (controller.DQLearning)
            v = 1;
        maxReward = 1 * v;
        minReward = 0.5f * v;
        punishment = -1 * v;
        empty = -0.25f * v;

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

        //Set the position of the texts of each platform in the 4 positions (Up,Right,Down,Left)
        //minvalue is the half size of a platform
        //Greater is the factor, farther is the text from the center
        float factor = 0.25f;
        text[0].transform.position = transform.position + new Vector3(0, minValue * factor);
        text[1].transform.position = transform.position + new Vector3(minValue * factor, 0);
        text[2].transform.position = transform.position + new Vector3(0, -minValue * factor);
        text[3].transform.position = transform.position + new Vector3(-minValue * factor, 0);
        text[4].enabled = false;
        if (controller.snake)
            text[4].enabled = true;
        //text[4].transform.position = transform.position;
        //text[4].text = (point.y * controller.size + point.x).ToString();

        for (int i = 0; i < text.Length; i++)
        {
            text[i].fontSize = 8;
        }
    }
    
    //Not best solution
    void LateUpdate()
    {
        if (!punishmentPoint && !maxRewardPoint && !controller.snake)//&& !controller.DQLearning
        {
            for (int i = 0; i < text.Length-1; i++)
            {
                if (controller.DQLearning)
                {
                    controller.policyNetwork.StepsForward(new float[] { point.y * controller.size + point.x });
                    controller.targetNetwork.StepsForward(new float[] { point.y * controller.size + point.x });
                    text[i].text = "P: " + controller.policyNetwork.output[i].ToString("F2") + "\nT: " + controller.targetNetwork.output[i].ToString("F2");
                }
                else
                    text[i].text = (controller.qTable[(int)(point.y * controller.size + point.x), i]).ToString("F2");
            }
        }
        else
        {
            canvas.enabled = false;
        }

        if (controller.snake)
        {
            if (!free)
            {
                GetComponent<SpriteRenderer>().color = Color.red;
            }
            else GetComponent<SpriteRenderer>().color = Color.white;
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
