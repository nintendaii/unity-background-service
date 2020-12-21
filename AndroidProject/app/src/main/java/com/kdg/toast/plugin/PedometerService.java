package com.kdg.toast.plugin;

import android.app.Notification;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.app.Service;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.content.res.Configuration;
import android.hardware.Sensor;
import android.hardware.SensorEvent;
import android.hardware.SensorEventListener;
import android.hardware.SensorManager;
import android.os.Build;
import android.os.Handler;
import android.os.IBinder;
import android.os.Looper;
import android.preference.PreferenceManager;
import android.util.Log;
import android.widget.Toast;

import java.util.Calendar;
import java.util.Date;

import androidx.annotation.LongDef;
import androidx.annotation.Nullable;
import androidx.core.app.NotificationCompat;
import androidx.core.app.NotificationManagerCompat;

public class PedometerService extends Service implements SensorEventListener {

    public SharedPreferences sharedPreferences;
    String TAG = "PEDOMETER";
    SensorManager sensorManager;
    boolean running;
    Date currentDate;
    Date initialDate;
    @Nullable
    @Override
    public IBinder onBind(Intent intent) {
        return null;
    }

    private void createNotificationChannel() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            final NotificationChannel notificationChannel = new NotificationChannel(
                    "PedometerLib",
                    "Service Channel",
                    NotificationManager.IMPORTANCE_DEFAULT
            );
            final NotificationManagerCompat notificationManager = NotificationManagerCompat.from(this);
            notificationManager.createNotificationChannel(notificationChannel);
        }
    }

    private void startNotification(){
        String input = "Counting your steps...";
        Intent notificationIntent = new Intent(this, Bridge.myActivity.getClass());
        PendingIntent pendingIntent = PendingIntent.getActivity(this,
                0, notificationIntent, 0);
        Notification notification = new NotificationCompat.Builder(this, "PedometerLib")
                .setContentTitle("BitPet Walking Service")
                .setContentText(input)
                .setSmallIcon(R.mipmap.ic_launcher)
                .setContentIntent(pendingIntent)
                .build();
        startForeground(112, notification);
    }


    @Override
    public void onCreate() {
        Log.i(TAG, "onCreate: CREATED"+Bridge.steps);
        sharedPreferences = PreferenceManager.getDefaultSharedPreferences(this);
        loadData();
        saveSummarySteps(Bridge.summarySteps+Bridge.steps);
    }

    @Override
    public void onTaskRemoved(Intent rootIntent) {
        super.onTaskRemoved(rootIntent);
        Log.i(TAG, "onTaskRemoved: REMOVED"+Bridge.steps);
        initSensorManager();

    }

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
        Log.i(TAG, "onStartCommand: STARTED");
        createNotificationChannel();
        startNotification();
        super.onCreate();
        Bridge.initialSteps=0;
        initSensorManager();
        SharedPreferences.Editor editor = sharedPreferences.edit();
        initialDate = Calendar.getInstance().getTime();
        editor.putString(Bridge.INIT_DATE, currentDate.toString());
        editor.apply();
        return START_NOT_STICKY;
    }

    @Override
    public void onDestroy() {
        super.onDestroy();
        Log.i(TAG, "onDestroy: DESTROYED");
        disposeSensorManager();
        loadData();
        saveSummarySteps(Bridge.summarySteps+Bridge.steps);
    }

    @Override
    public void onSensorChanged(SensorEvent sensorEvent) {
        Log.i(TAG, "onSensorChanged!!!!!!: "+sensorEvent.values[0]);
        if (Bridge.initialSteps==0){
            Log.i(TAG, "onSensorChanged: AWAKE");
            Bridge.initialSteps=(int) sensorEvent.values[0];
        }
        if (running){
            Bridge.steps=(int)sensorEvent.values[0]-Bridge.initialSteps;
            Log.i(TAG, "onSensorChanged: current steps: "+Bridge.steps);
            saveData(Bridge.steps);
        }
    }

    @Override
    public void onAccuracyChanged(Sensor sensor, int i) {    }

    public void initSensorManager(){
        sensorManager = (SensorManager) getSystemService(Context.SENSOR_SERVICE);
        running = true;
        Sensor countSensor = sensorManager.getDefaultSensor(Sensor.TYPE_STEP_COUNTER);
        if (countSensor!=null){
            sensorManager.registerListener(this,countSensor,SensorManager.SENSOR_DELAY_UI);
        }
        else {
            Toast.makeText(Bridge.myActivity,"Sensor Not Found (", Toast.LENGTH_LONG).show();
        }
    }
    public void disposeSensorManager(){
        running=false;
        sensorManager.unregisterListener(this);
    }

    public void saveData(int currentSteps) {

        SharedPreferences.Editor editor = sharedPreferences.edit();
        currentDate = Calendar.getInstance().getTime();
        editor.putString(Bridge.DATE, currentDate.toString());
        Log.i(TAG, "saveData: saved! "+currentSteps);
        editor.putInt(Bridge.STEPS, currentSteps);
        editor.apply();
    }
    public void saveSummarySteps(int stepsToSave) {
        SharedPreferences.Editor editor = sharedPreferences.edit();
        currentDate = Calendar.getInstance().getTime();
        editor.putString(Bridge.DATE, currentDate.toString());
        Log.i(TAG, "saveSummarySteps: saved! "+stepsToSave);
        editor.putInt("summarySteps", stepsToSave);
        editor.apply();
    }
    public void loadData() {
        Bridge.steps = sharedPreferences.getInt(Bridge.STEPS, 0);
        Bridge.summarySteps = sharedPreferences.getInt("summarySteps",0);
        Log.i(TAG, "loadData: steps"+Bridge.steps);
        Log.i(TAG, "loadData: summarySteps "+Bridge.summarySteps);
    }
}