using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Logs : MonoBehaviour
{
    uint qsize = 15;  // number of messages to keep
    Queue<LogMessage> myLogQueue = new Queue<LogMessage>();
    float messageDuration = 5f; // Duration for each message to be displayed

    void Start() {
        Debug.Log("Started up logging.");
    }

    void OnEnable() {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable() {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Error)
        {
            LogMessage logMessage = new LogMessage
            {
                message = "[" + type + "] : " + logString,
                timestamp = Time.time
            };

            if (type == LogType.Exception)
                logMessage.stackTrace = stackTrace;

            myLogQueue.Enqueue(logMessage);

            while (myLogQueue.Count > qsize)
                myLogQueue.Dequeue();
        }
    }

    void Update() {
        // Remove messages that have been shown for more than 'messageDuration' seconds
        if (myLogQueue.Count > 0 && Time.time - myLogQueue.Peek().timestamp > messageDuration)
        {
            myLogQueue.Dequeue();
        }
    }

    void OnGUI() {
        GUILayout.BeginArea(new Rect(Screen.width - 400, 0, 400, Screen.height));
        foreach (var logMessage in myLogQueue)
        {
            GUILayout.Label(logMessage.message);
        }
        GUILayout.EndArea();
    }

    // Struct to store log message and its timestamp
    struct LogMessage
    {
        public string message;
        public string stackTrace;
        public float timestamp;
    }
}