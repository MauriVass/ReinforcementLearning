using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Client
{
    //Change according to the Server ip address
    private string url = "http://127.0.0.1:8080/";

    //State of the agent: [0,0,0,1,...]
    public void fit(Experience exp)
    {
        byte[] dataToPut = System.Text.Encoding.UTF8.GetBytes(exp.ToString());
        UnityWebRequest uwr = new UnityWebRequest();

        uwr.url = url;
        uwr.method = "FIT";
        uwr.uploadHandler = new UploadHandlerRaw(dataToPut);

        uwr.useHttpContinue = false;
        uwr.redirectLimit = 0;  // disable redirects
        uwr.timeout = 60;       // don't make this small, web requests do take some time

        uwr.downloadHandler = new DownloadHandlerBuffer();

        uwr.SendWebRequest();
        //yield return uwr.SendWebRequest();

        //Not best solution but with a local server work wery well
        //also there a no other options: you have to wait the deep neural network output before going on
        //this allows also to return something (float[], ...)
        //while (!uwr.isDone) ;

        if (uwr.isNetworkError )
        {
            Debug.Log("Error While Sending: " + uwr.error);
        }
    }

    public float[] predict(float[] state, bool isPolicyNet)
    {
        //Convert the state to string using . as separator
        string data = string.Join(".", state);
        byte[] dataToPut = System.Text.Encoding.UTF8.GetBytes(data);
        UnityWebRequest uwr = new UnityWebRequest();

        uwr.url = url;
        if(isPolicyNet)
            uwr.method = "PREDICT_PN";
        else
            uwr.method = "PREDICT_TN";
        uwr.uploadHandler = new UploadHandlerRaw(dataToPut);

        uwr.useHttpContinue = false;
        uwr.redirectLimit = 0;  // disable redirects
        uwr.timeout = 60;       // don't make this small, web requests do take some time

        uwr.downloadHandler = new DownloadHandlerBuffer();

        uwr.SendWebRequest();
        //yield return uwr.SendWebRequest();

        //Not best solution but with a local server work wery well
        //also there a no other options: you have to wait the deep neural network output before going on
        //this allows also to return something (float[], ...)
        while (!uwr.isDone) ;

        if (uwr.isNetworkError)
        {
            Debug.Log("Error While Sending: " + uwr.error);
            return null;
        }
        else
        {
            print(uwr.downloadHandler.text);
            var output = Array.ConvertAll(uwr.downloadHandler.text.Split('.'), s => float.Parse(s));
            return output;
            //Debug.Log("Received: " + uwr.downloadHandler.text);
        }
    }

    public void copyNN()
    {
        //Convert the state to string using . as separator
        //string data = string.Join(".", state);
        //byte[] dataToPut = System.Text.Encoding.UTF8.GetBytes(data);
        UnityWebRequest uwr = new UnityWebRequest();

        uwr.url = url;
        uwr.method = "COPY_NN";
        uwr.uploadHandler = new UploadHandlerRaw(null);

        uwr.useHttpContinue = false;
        uwr.redirectLimit = 0;  // disable redirects
        uwr.timeout = 60;       // don't make this small, web requests do take some time

        //uwr.downloadHandler = new DownloadHandlerBuffer();

        uwr.SendWebRequest();
        //yield return uwr.SendWebRequest();

        //Not best solution but with a local server work wery well
        //also there a no other options: you have to wait the deep neural network output before going on
        //this allows also to return something (float[], ...)
        //while (!uwr.isDone) ;

        if (uwr.isNetworkError)
        {
            Debug.Log("Error While Sending: " + uwr.error);
        }
    }

    //Non-MonoBehaviour scripts does not have print functions
    //Then you create your own print function with with blackjack and hookers! semicit
    void print(string txt)
    {
        Debug.Log(txt);
    }
}
