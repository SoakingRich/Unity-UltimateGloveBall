using UnityEngine;

public class GlobalTimeManager : MonoBehaviour
{
    public static GlobalTimeManager Instance { get; private set; }
    public static float GameStartTime { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep it alive across scenes

            if (GameStartTime == 0)
                GameStartTime = Time.realtimeSinceStartup;
        }
        else
        {
            Destroy(gameObject); // Ensure only one instance exists
        }
    }

    public float GetGlobalTime()
    {
        return Time.realtimeSinceStartup - GameStartTime;
    }
}