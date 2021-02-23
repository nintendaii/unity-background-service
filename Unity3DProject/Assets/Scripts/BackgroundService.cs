using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BackgroundService : MonoBehaviour
{
    AndroidJavaClass unityClass;
    AndroidJavaObject unityActivity;
    AndroidJavaClass customClass;
    [SerializeField]
    TextMeshProUGUI stepsText;
    [SerializeField]
    TextMeshProUGUI totalStepsText;
    [SerializeField]
    TextMeshProUGUI syncedDateText;

    private string _prlayerPrefsTotalSteps="totalSteps";
    

    private void Awake()
    {
        SendActivityReference("com.kdg.toast.plugin.Bridge");
        GetCurrentSteps();
    }
    

    void SendActivityReference(string packageName)
    {
        unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        unityActivity = unityClass.GetStatic<AndroidJavaObject>("currentActivity");
        customClass = new AndroidJavaClass(packageName);
        customClass.CallStatic("receiveActivityInstance", unityActivity);
    }

    public void StartService()
    {
        customClass.CallStatic("StartCheckerService");
        GetCurrentSteps();
    }
    public void StopService()
    {
        customClass.CallStatic("StopCheckerService");
    }
    public void GetCurrentSteps()
    {
        int? stepsCount = customClass.CallStatic<int>("GetCurrentSteps");
        stepsText.text = stepsCount.ToString();
    }
    public void SyncData()
    {
        string data;
        data = customClass.CallStatic<string>("SyncData");
        
        string[] parsedData = data.Split('#');
        string dateOfSync=parsedData[0] + " - " + parsedData[1];
        syncedDateText.text = dateOfSync;
        int receivedSteps = Int32.Parse(parsedData[2]);
        int prefsSteps = PlayerPrefs.GetInt(_prlayerPrefsTotalSteps,0);
        int prefsStepsToSave = prefsSteps + receivedSteps;
        PlayerPrefs.SetInt(_prlayerPrefsTotalSteps,prefsStepsToSave);
        totalStepsText.text = prefsStepsToSave.ToString();
        
        GetCurrentSteps();
    }
}