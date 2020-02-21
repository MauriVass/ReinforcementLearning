using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManagerScenes : MonoBehaviour
{
    public static bool isSquare;
    public static bool isDeepQN;

    public static int x, y;
    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }
}
