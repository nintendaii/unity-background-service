
## What is Unity Background Service?
Unity Background Service is a project that shows how to create an Android service for Unity application working on background. 
Usually Android service (especially in Unity apps) shuts down when we kill the app. It is impossible to make the service working fully on background. The only solution is to let the
service work on foreground as a notification and then the users are able to hide that notification if they wants to.
Specifically, this project shows the creation of a step counting service that works on background. 
## How to use?
![doc_2021-03-02_12-38-25](https://user-images.githubusercontent.com/44233090/109638719-04edda00-7b57-11eb-9929-abd2e5665b42.gif)

## How it works?
---
### [Unity](https://github.com/nintendaii/unity-background-service/tree/master/Unity3DProject)
The main scene is located in Assets/Scenes. To see the example code go to [BackgroundService](https://github.com/nintendaii/unity-background-service/blob/master/Unity3DProject/Assets/Scripts/BackgroundService.cs) class. Explanation of the methods:
  1. On Awake the `SendActivityReference` method is called. It creates the Unity Android class and Unity activity. Then it sends the current activity to the plugin (the .aar file should be located at Assets/Plugins/Android)

```c#
void SendActivityReference(string packageName)
    {
        unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        unityActivity = unityClass.GetStatic<AndroidJavaObject>("currentActivity");
        customClass = new AndroidJavaClass(packageName);
        customClass.CallStatic("receiveActivityInstance", unityActivity);
    }
``` 
  2. `StartService` and `StopService` methods respectively starts and stops the background service as well as `GetCurrentSteps` method simply gets the walked steps from plugin.
  3. `SyncData` method returns a string data that holds 3 variables separated with # symbol:
   - date of `StartService` method invocation
   - date of `SyncData` method invocation
   - count of steps walked during this period
```c# 
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
  ```

 ---
 ### [Android](https://github.com/nintendaii/unity-background-service/tree/master/AndroidProject)
 The entry point of the Android application is the [`Bridge`](https://github.com/nintendaii/unity-background-service/blob/master/AndroidProject/app/src/main/java/com/kdg/toast/plugin/Bridge.java) class. Explanation of the methods:
 1. The `receiveActivityInstance` method is called when the `SendActivityReference` from Unity executes. It takes the Unity activity to know where to start the background service in the future. Also it checks if the permission for activity recognition is granted and asks for the permission if it is not (this logic is implemented for Android API 28 and above).
```java
public static void receiveActivityInstance(Activity tempActivity) {
        myActivity = tempActivity;
        String[] perms= new String[1];
        perms[0]=Manifest.permission.ACTIVITY_RECOGNITION;
        if (ContextCompat.checkSelfPermission(myActivity, Manifest.permission.ACTIVITY_RECOGNITION)
                != PackageManager.PERMISSION_GRANTED) {
            Log.i("PEDOMETER", "Permision isnt granted!");
            ActivityCompat.requestPermissions(Bridge.myActivity,
                    perms,
                    1);
        }
    }
```
2. `GetCurrentSteps` is called when the `GetCurrentSteps` is called from Unity.
