using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class NewPlugin : MonoBehaviour
{
    AndroidJavaClass unityClass;
    AndroidJavaObject unityActivity;
    AndroidJavaClass customClass;
    [SerializeField]
    TextMeshProUGUI stepsText;
    [SerializeField]
    TextMeshProUGUI syncedStepsText;
    [SerializeField]
    TextMeshProUGUI syncedDateText;

    void Start()
    {
        //Replace with your full package name

        getCurrentSteps();

        //Now, start service
        //startService();
    }

    void sendActivityReference(string packageName)
    {
        unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        unityActivity = unityClass.GetStatic<AndroidJavaObject>("currentActivity");
        customClass = new AndroidJavaClass(packageName);
        customClass.CallStatic("receiveActivityInstance", unityActivity);
    }

    public void startService()
    {
        sendActivityReference("com.kdg.toast.plugin.Bridge");
        customClass.CallStatic("StartCheckerService");
        getCurrentSteps();
    }
    public void stopService()
    {
        customClass.CallStatic("StopCheckerService");
    }
    public void getCurrentSteps()
    {
        if (customClass!=null)
        {
            int? stepsCount = customClass.CallStatic<int>("GetCurrentSteps");
            if (stepsCount == null)
            {
                stepsText.text = "null :(";
            }
            else { stepsText.text = stepsCount.ToString(); }
        }
        else
        {
            sendActivityReference("com.kdg.toast.plugin.Bridge");
            int? stepsCount = customClass.CallStatic<int>("GetCurrentSteps");
            if (stepsCount == null)
            {
                stepsText.text = "null :(";
            }
            else { stepsText.text = stepsCount.ToString(); }

        }
    }
    public void SendDataToServer()
    {
        string data;
        if (customClass != null)
        {
            data = customClass.CallStatic<string>("SyncData");

        }
        else
        {
            sendActivityReference("com.kdg.toast.plugin.Bridge");
            data = customClass.CallStatic<string>("SyncData");

        }
        string[] parsedData = data.Split('#');
        syncedDateText.text = parsedData[0] + " - " + parsedData[1];
        int stepsToSend = Int32.Parse(parsedData[2]);
        syncedStepsText.text = stepsToSend.ToString();
        getCurrentSteps();
    }
}
