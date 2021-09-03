using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlatformGenerator : MonoBehaviour
{
    //These script create all the platforms

    //Length and width of the playground
    [HideInInspector]
    public int n, m;

    //Platform's prefab
    public GameObject[] platformPrefabs;
    int index;

    //The container of all the platforms
    GameObject container;

    Vector2 platformStartingPoint;

    //Single platform side size
    [HideInInspector]
    public float minValue;

    Platform[,] platforms;

    //Most important platforms
    Platform agentStartingPoint, maxRewardPoint, minRewardPoint;
    List<Vector2> obstaclesPoints = new List<Vector2>();

    public Controller controller;

    public Vector2 off;
    void Start() {
        index = ManagerScenes.isSquare ? 0 : 1;
        n = ManagerScenes.x;
        m = ManagerScenes.y;

        print($"/// Starting PlatformGenerator with n: {n}, m: {m} ///");

        controller.n = n;
        controller.m = m;
        platforms = new Platform[n, m];
        container = gameObject;
        minValue = 1.5f * 1.01f;

        int x;
        int y;

        platformStartingPoint = new Vector2(Camera.main.ScreenToWorldPoint(Vector3.left).x, Camera.main.ScreenToWorldPoint(Vector3.down).y) + Vector2.one * minValue / 2;
        //Generate platform of size nXm and populate the tha platforms array
        for (y = 0; y < m; y++)//Columns
        {
            for (x = 0; x < n; x++)//Rows
            {
                GameObject tmp;
                if (index == 0)
                {
                    platformPrefabs[index].transform.localScale = Vector3.one * minValue;
                    tmp = Instantiate(platformPrefabs[index], platformStartingPoint + new Vector2(x, y) * (minValue * 1.05f), Quaternion.identity);
                }
                else
                {
                    //Set the right position for the hexagones
                    float a = y % 2 == 0 ? 2 * x : 2 * x + 1;

                    //A value to make some space between the platforms
                    float v = 1.1f;
                    float w = platformPrefabs[index].transform.localScale.x * 3 / 4.0f * v;
                    float h = platformPrefabs[index].transform.localScale.x * Mathf.Sqrt(3) / 4.0f * v;
                    tmp = Instantiate(platformPrefabs[index], platformStartingPoint + new Vector2(a * w, y * h), Quaternion.Euler(0, 0, 90));
                }
                tmp.name = "Platform (" + x + "," + y + ")";
                tmp.transform.SetParent(container.transform);
                Platform p = tmp.GetComponent<Platform>();
                p.point = new Vector2(x, y);
                p.minValue = minValue;
                p.controller = controller;
                platforms[x, y] = tmp.GetComponent<Platform>();
            }
        }

        //Select one random platform to be the place from where the Agent will start (the first row or between the first n/2 rows)
        x = Random.Range(1, n - 1);
        y = m < (3) ? 0 : Random.Range(1, (m) / 2 - 1);
        platforms[x, y].SetStartingPoint();
        agentStartingPoint = platforms[x, y];

        //Select one random platform to be the place of the max reward (the last row or between the last n/2 rows)
        do
        {
            x = Random.Range(0, n);
            y = m < (3) ? 3 : Random.Range(m / 2 + 1, m);
        }
        while (checkPlatfromFree(new Vector2(x, y)));
        platforms[x, y].SetMaxRewardPoint();
        maxRewardPoint = platforms[x, y];

        //Select one random platform to be the place of the min reward
        //do
        //{
        //    x = Random.Range(0, n);
        //    y = Random.Range(0, m);
        //}
        //while (checkPlatfromFree(new Vector2(x, y)));
        //platforms[x, y].SetMinRewardPoint();
        //controller.minRewardPlatform = platforms[x, y];


        //Choose obstacles' position (25% of obstacles)
        for (int i = 0; i < (n * m * 0.25f); i++)
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

        controller.platforms = platforms;
        controller.agentStartingPlatform = agentStartingPoint;
        controller.maxRewardPlatform = maxRewardPoint;


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
