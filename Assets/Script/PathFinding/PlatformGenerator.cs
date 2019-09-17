using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlatformGenerator : MonoBehaviour
{
    public int n, m;

    //Platform's prefab
    public GameObject platform;

    //The container of all the platforms
    GameObject container;

    Vector2 platformStartingPoint;

    //Platform side's size
    public float minValue;

    Platform[,] platforms;

    //Most important platforms
    Platform agentStartingPoint, maxRewardPoint, minRewardPoint;
    List<Vector2> obstaclesPoints = new List<Vector2>();

    public Controller controller;

    public Vector2 off;
    void Start()
    {
        if (controller.snake)
            controller.size = 19;
        else
            controller.size = 5;
        n = controller.size;
        m = controller.size;
        platforms = new Platform[n, m];
        container = gameObject;
        platform.transform.localScale = Vector3.one * 1.5f * 1.01f;
        minValue = platform.transform.localScale.x;


        int x;
        int y;
        
        platformStartingPoint = new Vector2(Camera.main.ScreenToWorldPoint(Vector3.left).x, Camera.main.ScreenToWorldPoint(Vector3.down).y) + Vector2.one * minValue / 2;
        //Generate platform of size nXm and populate the tha platforms array
        for (int i = 0; i < m; i++)//Columns
        {
            for (int j = 0; j < n; j++)//Rows
            {
                x = j;
                y = i;
                GameObject tmp = Instantiate(platform, platformStartingPoint + new Vector2(x, y) * (minValue * 1.05f), Quaternion.identity);
                tmp.name = "Platform (" + x + "," + y + ")";
                tmp.transform.SetParent(container.transform);
                Platform p = tmp.GetComponent<Platform>();
                p.point = new Vector2(x, y);
                p.minValue = minValue;
                p.controller = controller;
                p.free = true;
                platforms[x, y] = tmp.GetComponent<Platform>();
            }
        }

        //Select one random platform to be the place from where the Agent will start (the first row or between the first n/2 rows)
        x = Random.Range(1, n-1);

        if (controller.snake)
            y = m < (3) ? 0 : Random.Range(4, (m) / 2 - 1);
        else y = m < (3) ? 0 : Random.Range(1, (m) / 2 - 1);
        if (!controller.snake)
            platforms[x, y].SetStartingPoint();
        agentStartingPoint = platforms[x, y];

        //Select one random platform to be the place of the max reward (the last row or between the last n/2 rows)
        do
        {
            x = Random.Range(0, n);
            y = m < (3) ? 3 : Random.Range(m / 2 + 1, m);
        }
        while (checkPlatfromFree(new Vector2(x, y)));
        if (!controller.snake)
            platforms[x, y].SetMaxRewardPoint();

        //Select one random platform to be the place of the min reward
        //do
        //{
        //    x = Random.Range(0, n);
        //    y = Random.Range(0, m);
        //}
        //while (checkPlatfromFree(new Vector2(x, y)));
        //platforms[x, y].SetMinRewardPoint();
        //controller.minRewardPlatform = platforms[x, y];

        if (!controller.snake)
        {
            //Choose obstacles' position (10% of obstacles)
            for (int i = 0; i < (n * m / 10); i++)
            {
                do
                {
                    x = Random.Range(0, n);
                    y = Random.Range(0, m);
                }
                while (checkPlatfromFree(new Vector2(x, y)));

                platforms[x, y].SetObstaclePoint();
                obstaclesPoints.Add(new Vector2(x, y));
            }
        }
        
        controller.platforms = platforms;
        controller.agentStartingPlatform = agentStartingPoint;


        gameObject.transform.Translate(off);
    }

    bool checkPlatfromFree(Vector2 p)
    {
        if (maxRewardPoint && p == maxRewardPoint.point)
            return true;
        if (minRewardPoint && p == minRewardPoint.point)
            return true;
        if (agentStartingPoint && p == agentStartingPoint.point)
            return true;

        for (int j = 0; j < obstaclesPoints.Count; j++)
            if (p == obstaclesPoints[j])
                return true;
        return false;
    }
}
