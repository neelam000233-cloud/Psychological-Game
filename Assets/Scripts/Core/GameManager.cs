using UnityEngine;

public enum GameMode
{
    PvE_AI,
    PvP_Local
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Mode Setup")]
    public GameMode currentMode = GameMode.PvE_AI;

    [Header("Time Control")]
    public bool isTimePaused { get; private set; } = false;

    // 屏蔽锁：解除暂停的瞬间屏蔽一次点击输入，防止误触拖拽
    public bool ignoreClickThisFrame { get; private set; } = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        // 恢复正常时间后的下一帧重置屏蔽锁
        if (ignoreClickThisFrame && Input.GetMouseButtonUp(0))
        {
            ignoreClickThisFrame = false;
        }
    }

    public void SetTimePause(bool pause)
    {
        isTimePaused = pause;
        
        if (pause)
        {
            Time.timeScale = 0f; // 彻底挂起物理与时间更新
            GameEventManager.TriggerTimePauseStateChanged(true);
            Debug.Log("<color=yellow>[GameManager] Inspection Mode: Time Freezed. Awaiting UI Selection.</color>");
        }
        else
        {
            ignoreClickThisFrame = true; // 开启解冻屏蔽锁，防止解冻当帧触发拖拽
            Time.timeScale = 1f;        // 恢复时间流逝
            GameEventManager.TriggerTimePauseStateChanged(false);
            Debug.Log("<color=green>[GameManager] Decision Made: Time Resumed.</color>");
        }
    }

    public void PauseGameTime(bool pause) => SetTimePause(pause);
}