using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    public Button[] buttonStructure;
    public Button[] buttonMode;
    public GameObject x;
    public GameObject y;
    // Start is called before the first frame update
    void Start()
    {
        SetIsSquare(true);
        SetIsDQN(false);
        SetX();
        //Don't choose a too large value. It may be difficult to evaluate the result
        SetY();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void SetIsSquare(bool value)
    {
        for (int i = 0; i < buttonStructure.Length; i++)
            buttonStructure[i].GetComponent<Image>().color = new Color32(0, 120, 160, 255);
        if (value)
            buttonStructure[0].GetComponent<Image>().color = new Color32(0, 183, 255, 255);
        else
            buttonStructure[1].GetComponent<Image>().color = new Color32(0, 183, 255, 255);

        ManagerScenes.isSquare = value;
    }

    public void SetIsDQN(bool value)
    {
        for (int i = 0; i < buttonMode.Length; i++)
            buttonMode[i].GetComponent<Image>().color = new Color32(0, 120, 160, 255);
        if (!value)
            buttonMode[0].GetComponent<Image>().color = new Color32(0, 183, 255, 255);
        else
            buttonMode[1].GetComponent<Image>().color = new Color32(0, 183, 255, 255);
        //Todo
        ManagerScenes.isDeepQN = !value;
    }

    public void SetX()
    {
        int value = (int)x.transform.GetChild(0).GetComponent<Slider>().value;
        ManagerScenes.x = value;
        x.GetComponent<Text>().text = "X: " + value;
    }
    public void SetY()
    {
        int value = (int)y.transform.GetChild(0).GetComponent<Slider>().value;
        ManagerScenes.y = value;
        y.GetComponent<Text>().text = "Y: " + value;
    }

    public void ChangeScene(string scene)
    {
        SceneManager.LoadScene(scene);
    }
}
