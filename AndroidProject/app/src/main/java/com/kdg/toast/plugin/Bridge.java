package com.kdg.toast.plugin;

import android.Manifest;
import android.app.Activity;
import android.app.Application;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.content.pm.PackageManager;
import android.net.Uri;
import android.os.Build;
import android.os.PowerManager;
import android.preference.PreferenceManager;
import android.provider.Settings;
import android.util.Log;

import java.util.Calendar;
import java.util.Date;
import java.util.Arrays;

import androidx.core.app.ActivityCompat;
import androidx.core.content.ContextCompat;

import static androidx.core.app.ActivityCompat.requestPermissions;

public final class Bridge extends Application {
    static int summarySteps;
    static int steps;
    static int initialSteps;
    static Activity myActivity;
    static Context appContext;
    Date currentDate;
    static final String STEPS="steps";
    static final String SUMMARY_STEPS="summarySteps";
    static final String DATE="currentDate";
    static final String INIT_DATE="initialDate";

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

    public static void StartCheckerService() {
        checkOptimization();
        myActivity.startService(new Intent(myActivity, PedometerService.class));
    }

    public static void StopCheckerService(){
        Intent serviceIntent = new Intent(myActivity, PedometerService.class);
        myActivity.stopService(serviceIntent);

    }
    public static int GetCurrentSteps(){
        SharedPreferences sharedPreferences = PreferenceManager.getDefaultSharedPreferences(appContext);
        SharedPreferences.Editor editor = sharedPreferences.edit();
        Date currentDate = Calendar.getInstance().getTime();
        editor.putString(DATE, currentDate.toString());
        int walkedSteps = sharedPreferences.getInt(STEPS, 0);
        int allSteps = sharedPreferences.getInt(SUMMARY_STEPS,0);
        summarySteps=walkedSteps+allSteps;
        Log.i("PEDOMETER", "FROM BRIDGE CLASS - GetCurrentSteps:"+summarySteps);
        return summarySteps;
    }
    public static String SyncData(){
        SharedPreferences sharedPreferences = PreferenceManager.getDefaultSharedPreferences(appContext);
        int stepsToSend=GetCurrentSteps();
        String firstDate = sharedPreferences.getString(INIT_DATE,"");
        String lastDate = sharedPreferences.getString(DATE,"");
        String data = firstDate+'#'+lastDate+'#'+stepsToSend;
        SharedPreferences.Editor editor = sharedPreferences.edit();
        editor.putInt(STEPS,0);
        editor.putInt(SUMMARY_STEPS,0);
        steps=0;
        summarySteps=0;
        initialSteps=0;
        Date currentDate = Calendar.getInstance().getTime();
        editor.putString(INIT_DATE,currentDate.toString());
        editor.putString(DATE,currentDate.toString());
        editor.apply();
        Log.i("PEDOMETER", "SyncData: "+steps+' '+summarySteps+data);
        return data;
    }

    @Override
    public void onCreate() {
        super.onCreate();
        Bridge.appContext=getApplicationContext();

    }
    public static void checkOptimization(){
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
            String packageName = myActivity.getPackageName();
            Log.i("PEDOMETER", "checkOptimization: PACKAGE NAME: "+packageName);
            PowerManager pm = (PowerManager) myActivity.getSystemService(Context.POWER_SERVICE);
            if (pm != null) {
                if (!pm.isIgnoringBatteryOptimizations(packageName)) {
                    try {
                        Intent intent = new Intent();
                        intent.setAction(Settings.ACTION_REQUEST_IGNORE_BATTERY_OPTIMIZATIONS);
                        intent.setData(Uri.parse("package:" + packageName));
                        myActivity.startActivity(intent);
                    }
                    catch (Exception e){
                    }

                }
            }
        }
    }
}
