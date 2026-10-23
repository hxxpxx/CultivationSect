// ===== 游戏时间追踪 =====
using UnityEngine;

public class GameTimeTracker : MonoBehaviour
{
    public static GameTimeTracker Instance { get; private set; }

    float sessionStartTime;
    float accumulatedSeconds;
    bool isTracking;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        sessionStartTime = Time.time;
        isTracking = true;
    }

    void Update()
    {
        if (!isTracking) return;
        accumulatedSeconds = Time.time - sessionStartTime;
    }

    public float GetTotalPlaySeconds()
    {
        return accumulatedSeconds;
    }

    public void Pause() { isTracking = false; }
    public void Resume() { sessionStartTime = Time.time - accumulatedSeconds; isTracking = true; }

    public void Reset()
    {
        sessionStartTime = Time.time;
        accumulatedSeconds = 0f;
    }
}