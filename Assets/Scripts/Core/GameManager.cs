using UnityEngine;
using System;

public enum GameMode
{
    PvE,
    PvP_Local
}

public enum EndingType
{
    None,
    PlayerWin,      // Player 胜 (对方 SAN 归零 / 超时优势)
    OpponentWin,    // 对手胜 (Player SAN 归零 / 超时劣势)
    MutualLoss,     // 双输 (双方 SAN 同时归零 / 高压内耗)
    MutualWin,      // 双赢 (双方高 SAN + 低 HEAT)
    TimeoutDraw     // 3分钟超时平局
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    public GameMode currentMode = GameMode.PvE;
    public bool isTimePaused = false;
    public bool isGameOver = false;

    [Header("Input Handling")]
    public bool ignoreClickThisFrame = false; // 拖拽/UI误触拦截标记

    [Header("Match Settings")]
    public float minGameDuration = 10f;  // 允许判定双赢/双输的最少进行时间 (秒)
    public float maxMatchTime = 180f;    // 单局最大时长 (3 分钟)

    // 数值与计数追踪
    private float playerASanity = 100f, playerAHeat = 0f;
    private float playerBSanity = 100f, playerBHeat = 0f;
    
    private int playerAActionCount = 0;
    private int playerBActionCount = 0;
    public float gameTimer = 0f;

    // 结局结算事件 (传递 结局类型与文字说明)
    public static event Action<EndingType, string> OnGameOverWithEnding;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        GameEventManager.OnEmployeeStatsChanged += HandleStatsChanged;
    }

    private void OnDisable()
    {
        GameEventManager.OnEmployeeStatsChanged -= HandleStatsChanged;
    }

    private void Update()
    {
        if (isGameOver || isTimePaused) return;

        gameTimer += Time.deltaTime;

        // 持续检测双赢与双输极值
        CheckContinuousEndings();

        // 3 分钟 (180s) 超时保底逻辑
        if (gameTimer >= maxMatchTime)
        {
            EvaluateTimeoutEnding();
        }
    }

    private void LateUpdate()
    {
        if (ignoreClickThisFrame)
        {
            ignoreClickThisFrame = false;
        }
    }

    /// <summary>
    /// 设置时间暂停状态（供 UI 面板/检视界面调用）
    /// </summary>
    public void SetTimePause(bool pause)
    {
        isTimePaused = pause;
        Time.timeScale = pause ? 0f : 1f;
    }

    // 记录双方的有效操作数
    public void RegisterAction(int characterID)
    {
        if (characterID == 0) playerAActionCount++;
        else if (characterID == 1) playerBActionCount++;
    }

    private void HandleStatsChanged(int characterID, float sanity, float heat)
    {
        if (isGameOver) return;

        if (characterID == 0)
        {
            playerASanity = sanity;
            playerAHeat = heat;
        }
        else if (characterID == 1)
        {
            playerBSanity = sanity;
            playerBHeat = heat;
        }

        // SAN 归零即时判定 (硬性规则)
        CheckSanityZeroEndings();
    }

    // 1. SAN 归零即时判定 (只要一方 SAN=0，另一方立刻判 WIN)
    private void CheckSanityZeroEndings()
    {
        bool aSanityZero = playerASanity <= 0f;
        bool bSanityZero = playerBSanity <= 0f;

        // 情况 A: 双方 SAN 同时归零 -> 双输
        if (aSanityZero && bSanityZero)
        {
            TriggerEnding(EndingType.MutualLoss, "All devastated!");
            return;
        }

        // 情况 B: 对手 SAN 归零 -> Player WIN
        if (bSanityZero && !aSanityZero)
        {
            TriggerEnding(EndingType.PlayerWin, "You WIN!");
            return;
        }

        // 情况 C: 玩家 SAN 归零 -> 对手 WIN
        if (aSanityZero && !bSanityZero)
        {
            TriggerEnding(EndingType.OpponentWin, "You LOST.");
            return;
        }
    }

    // 2. 双赢与极端内耗双输判定 (结合时间与操作数)
    private void CheckContinuousEndings()
    {
        bool meetsTimeAndOps = gameTimer >= minGameDuration && playerAActionCount >= 1 && playerBActionCount >= 1;
        if (!meetsTimeAndOps) return;

        // 结局：双赢 (双方 SAN > 70 且 HEAT < 30)
        bool isMutualWin = (playerASanity > 70f && playerBSanity > 70f) && (playerAHeat < 30f && playerBHeat < 30f);
        if (isMutualWin)
        {
            TriggerEnding(EndingType.MutualWin, "WIN-WIN, congrats!");
            return;
        }

        // 结局：极度内耗双输 (双方 SAN < 30 且 HEAT > 70)
        bool isMutualLoss = (playerASanity < 30f && playerBSanity < 30f) && (playerAHeat > 70f && playerBHeat > 70f);
        if (isMutualLoss)
        {
            TriggerEnding(EndingType.MutualLoss, "See you mate, in the hell.");
            return;
        }
    }

    // 3. 3 分钟超时保底结算
    private void EvaluateTimeoutEnding()
    {
        if (Mathf.Approximately(playerASanity, playerBSanity))
        {
            TriggerEnding(EndingType.TimeoutDraw, "Time's up.");
        }
        else if (playerASanity > playerBSanity)
        {
            TriggerEnding(EndingType.PlayerWin, "Time's up and you WIN");
        }
        else
        {
            TriggerEnding(EndingType.OpponentWin, "Time's up and you LOST.");
        }
    }

    private void TriggerEnding(EndingType ending, string reason)
    {
        Debug.Log($"<color=red>[调试] TriggerEnding 成功被调用了！结局: {ending}</color>");

        isGameOver = true;
        SetTimePause(true);

        Debug.Log($"<color=gold>[GAME OVER - {ending}]</color> {reason}");
        OnGameOverWithEnding?.Invoke(ending, reason);
    }

    public void RestartGame()
    {
        isGameOver = false;
        SetTimePause(false);
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }
}