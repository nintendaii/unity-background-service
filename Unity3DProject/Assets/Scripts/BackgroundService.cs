using TMPro;
using UnityEngine;

public class BackgroundService : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI stepsText;
    [SerializeField] private TextMeshProUGUI totalStepsText;
    [SerializeField] private TextMeshProUGUI syncedDateText;

    private AndroidJavaClass unityClass;
    private AndroidJavaObject unityActivity;
    private AndroidJavaClass customClass;
    private const string PlayerPrefsTotalSteps = "totalSteps";
    private const string PackageName = "com.kdg.toast.plugin.Bridge";
    private const string UnityDefaultJavaClassName = "com.unity3d.player.UnityPlayer";
    private const string CustomClassReceiveActivityInstanceMethod = "ReceiveActivityInstance";
    private const string CustomClassStartServiceMethod = "StartService";
    private const string CustomClassStopServiceMethod = "StopService";
    private const string CustomClassGetCurrentStepsMethod = "GetCurrentSteps";
    private const string CustomClassSyncDataMethod = "SyncData";


    private void Awake()
    {
        SendActivityReference(PackageName);
        GetCurrentSteps();
    }


    private void SendActivityReference(string packageName)
    {
        unityClass = new AndroidJavaClass(UnityDefaultJavaClassName);
        unityActivity = unityClass.GetStatic<AndroidJavaObject>("currentActivity");
        customClass = new AndroidJavaClass(packageName);
        customClass.CallStatic(CustomClassReceiveActivityInstanceMethod, unityActivity);
    }

    public void StartService()
    {
        customClass.CallStatic(CustomClassStartServiceMethod);
        GetCurrentSteps();
    }

    public void StopService()
    {
        customClass.CallStatic(CustomClassStopServiceMethod);
    }

    public void GetCurrentSteps()
    {
        int? stepsCount = customClass.CallStatic<int>(CustomClassGetCurrentStepsMethod);
        stepsText.text = stepsCount.ToString();
    }

    public void SyncData()
    {
        var data = customClass.CallStatic<string>(CustomClassSyncDataMethod);

        var parsedData = data.Split('#');
        var dateOfSync = parsedData[0] + " - " + parsedData[1];
        syncedDateText.text = dateOfSync;
        var receivedSteps = int.Parse(parsedData[2]);
        var prefsSteps = PlayerPrefs.GetInt(PlayerPrefsTotalSteps, 0);
        var prefsStepsToSave = prefsSteps + receivedSteps;
        PlayerPrefs.SetInt(PlayerPrefsTotalSteps, prefsStepsToSave);
        totalStepsText.text = prefsStepsToSave.ToString();

        GetCurrentSteps();
    }
}