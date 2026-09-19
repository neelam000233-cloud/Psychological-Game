using System;
using System.Collections;
using UnityEngine;

public enum GameMode
{
    PvE,
    PvP_Local
}

public enum EndingType
{
    None,
    PlayerWin,      // Player 勝 (對手 SAN 歸零 / 對手 HEAT 滿 100 / 超時優勢)
    OpponentWin,    // 對手勝 (Player SAN 歸零 / Player HEAT 滿 100 / 超時劣勢)
    MutualLoss,     // 雙輸 (雙方 SAN 同時歸零 / 雙方 HEAT 同時爆表 / 高壓內耗)
    MutualWin,      // 雙贏 (雙方高 SAN + 低 HEAT)
    TimeoutDraw     // 3分鐘超時平局
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Mode & State")]
    public GameMode currentMode = GameMode.PvE;
    public bool isTimePaused = false;
    public bool isGameOver = false;

    [Header("Input Handling")]
    public bool ignoreClickThisFrame = false; // 拖拽/UI誤觸攔截標記

    [Header("Match Settings")]
    public float minGameDuration = 10f;  // 允許判定雙贏/雙輸的最少進行時間 (秒)
    public float maxMatchTime = 180f;    // 單局最大時長 (3 分鐘)

    [Header("Parry Chain System")]
    [Tooltip("每次連續 Parry 增加的飛行速度百分比 (例如 0.2 代表 +20%)")]
    public float parrySpeedMultiplierPerChain = 0.2f;
    [Tooltip("連續 Parry 的最大速度倍率上限")]
    public float maxParrySpeedMultiplier = 2.5f;
    [Tooltip("連續 Parry 的傷害加成倍率 (每次 +25%)")]
    public float parryDamageMultiplierPerChain = 0.25f;

    public int currentParryChain { get; private set; } = 0;

    [Header("Juiciness (Hitstop & Shake)")]
    public Camera mainCamera;
    private Vector3 originalCamPos;
    private Coroutine shakeCoroutine;

    // 數值與計數追蹤
    private float playerASanity = 100f, playerAHeat = 0f;
    private float playerBSanity = 100f, playerBHeat = 0f;
    
    private int playerAActionCount = 0;
    private int playerBActionCount = 0;
    public float gameTimer = 0f;

    // 結局結算事件 (傳遞 結局類型與文字說明)
    public static event Action<EndingType, string> OnGameOverWithEnding;

    private void Awake()
    {
        if (Instance == null) 
        {
            Instance = this;
            // 如果需要跨場景保留可開啟 DontDestroyOnLoad(gameObject);
        }
        else 
        {
            Destroy(gameObject);
            return;
        }

        gameTimer = 0f;
        isTimePaused = true; // 確保每次進 Scene/重開，未拿起文件前時間均保持暫停
    }

    private void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        if (mainCamera != null)
        {
            originalCamPos = mainCamera.transform.localPosition;
        }
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

        // 持續檢測雙贏與雙輸極值
        CheckContinuousEndings();

        // 3 分鐘 (180s) 超時保底邏輯
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
    /// 供文件/書本互動腳本調用：控制倒計時流逝與暫停
    /// </summary>
    public void SetDocumentHoldingState(bool isHolding)
    {
        isTimePaused = !isHolding;
        Debug.Log($"<color=yellow>[GameManager] 文件狀態變更: {(isHolding ? "拿起 (時間開始流逝)" : "放下 (時間暫停)")}</color>");
    }

    /// <summary>
    /// 控制時間暫停狀態
    /// </summary>
    public void SetTimePause(bool pause)
    {
        if (isGameOver) return;

        isTimePaused = pause;
        Time.timeScale = 1.0f; 

        Debug.Log($"<color=yellow>[GameManager] 倒計時狀態變更: {(pause ? "暫停中" : "流動中")}</color>");
        GameEventManager.TriggerTimePauseStateChanged(isTimePaused);
    }

    /// <summary>
    /// 獲取當前剩餘時間的格式化字串 (如 "02:45")
    /// </summary>
    public string GetFormattedRemainingTime()
    {
        float remainingTime = Mathf.Max(0f, maxMatchTime - gameTimer);
        int minutes = Mathf.FloorToInt(remainingTime / 60f);
        int seconds = Mathf.FloorToInt(remainingTime % 60f);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    // 記錄雙方的有效操作數
    public void RegisterAction(int characterID)
    {
        if (characterID == 0) playerAActionCount++;
        else if (characterID == 1) playerBActionCount++;
    }

    /// <summary>
    /// 增加指定角色的 HEAT 值，並觸發事件與勝負檢測
    /// </summary>
    public void AddHeat(int characterID, float amount)
    {
        if (isGameOver) return;

        if (characterID == 0)
        {
            playerAHeat = Mathf.Clamp(playerAHeat + amount, 0f, 100f);
            GameEventManager.TriggerStatsChanged(0, playerASanity, playerAHeat);
        }
        else if (characterID == 1)
        {
            playerBHeat = Mathf.Clamp(playerBHeat + amount, 0f, 100f);
            GameEventManager.TriggerStatsChanged(1, playerBSanity, playerBHeat);
        }

        // 每次增加 HEAT 後主動進行一次勝負檢查，防止拿著文件 Heat 滿 100 無法即時結算
        CheckInstantLimitEndings();
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

        // 實時檢測硬性規則 (SAN <= 0 或 HEAT >= 100)
        CheckInstantLimitEndings();
    }

    /// <summary>
    /// 硬性規則即時判定：SAN <= 0 或 HEAT >= 100 瞬間結算勝負
    /// </summary>
    private void CheckInstantLimitEndings()
    {
        bool aDefeated = playerASanity <= 0f || playerAHeat >= 100f;
        bool bDefeated = playerBSanity <= 0f || playerBHeat >= 100f;

        // 情況 A: 雙方同時達到失敗條件 -> 雙輸
        if (aDefeated && bDefeated)
        {
            TriggerEnding(EndingType.MutualLoss, "Both Burned Out / Devastated!");
            return;
        }

        // 情況 B: 對手達到失敗條件 (SAN<=0 或 HEAT>=100) -> Player WIN
        if (bDefeated && !aDefeated)
        {
            string reason = playerBSanity <= 0f ? "Opponent SAN Depleted!" : "Opponent Overheated!";
            TriggerEnding(EndingType.PlayerWin, $"You WIN! ({reason})");
            return;
        }

        // 情況 C: 玩家達到失敗條件 (SAN<=0 或 HEAT>=100) -> 對手 WIN
        if (aDefeated && !bDefeated)
        {
            string reason = playerASanity <= 0f ? "Your SAN Depleted!" : "You Overheated!";
            TriggerEnding(EndingType.OpponentWin, $"You LOST. ({reason})");
            return;
        }
    }

    // 雙贏與極端內耗雙輸判定 (結合時間與操作數)
    private void CheckContinuousEndings()
    {
        bool meetsTimeAndOps = gameTimer >= minGameDuration && playerAActionCount >= 1 && playerBActionCount >= 1;
        if (!meetsTimeAndOps) return;

        // 結局：雙贏 (雙方 SAN > 70 且 HEAT < 30)
        bool isMutualWin = (playerASanity > 70f && playerBSanity > 70f) && (playerAHeat < 30f && playerBHeat < 30f);
        if (isMutualWin)
        {
            TriggerEnding(EndingType.MutualWin, "WIN-WIN, congrats!");
            return;
        }

        // 結局：極度內耗雙輸 (雙方 SAN < 30 且 HEAT > 70)
        bool isMutualLoss = (playerASanity < 30f && playerBSanity < 30f) && (playerAHeat > 70f && playerBHeat > 70f);
        if (isMutualLoss)
        {
            TriggerEnding(EndingType.MutualLoss, "See you mate, in the hell.");
            return;
        }
    }

    // 3 分鐘超時保底結算
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
        if (isGameOver) return; // 避免重複觸發

        Debug.Log($"<color=red>[調試] TriggerEnding 成功被調用了！結局: {ending}</color>");

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

    // ==================== 【Parry 連擊系統】 ====================
    public void IncrementParryChain()
    {
        currentParryChain++;
        Debug.Log($"<color=orange>[Parry Chain!]</color> 當前連擊數：{currentParryChain} | 速度倍率：{GetParrySpeedMultiplier():F2}x");
    }

    public void ResetParryChain()
    {
        if (currentParryChain > 0)
        {
            Debug.Log("<color=grey>[Parry Chain Reset]</color> 連擊重置。");
        }
        currentParryChain = 0;
    }

    public float GetParrySpeedMultiplier()
    {
        float mult = 1.0f + (currentParryChain * parrySpeedMultiplierPerChain);
        return Mathf.Min(mult, maxParrySpeedMultiplier);
    }

    public float GetParryDamageMultiplier()
    {
        return 1.0f + (currentParryChain * parryDamageMultiplierPerChain);
    }

    // ==================== 【視聽反饋：頓幀 & 震動】 ====================
    /// <summary>
    /// 觸發打擊反饋（頓幀 + 鏡頭震動）
    /// </summary>
    public void TriggerHitstopAndShake(float hitstopDuration = 0.08f, float shakeDuration = 0.2f, float shakeIntensity = 0.15f)
    {
        StartCoroutine(DoHitstop(hitstopDuration));
        
        if (mainCamera != null)
        {
            if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
            shakeCoroutine = StartCoroutine(DoCameraShake(shakeDuration, shakeIntensity));
        }
    }

    private IEnumerator DoHitstop(float duration)
    {
        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0.05f; // 微幅凍結
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = originalTimeScale;
    }

    private IEnumerator DoCameraShake(float duration, float intensity)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float x = UnityEngine.Random.Range(-1f, 1f) * intensity;
            float y = UnityEngine.Random.Range(-1f, 1f) * intensity;

            mainCamera.transform.localPosition = originalCamPos + new Vector3(x, y, 0);

            elapsed += Time.unscaledDeltaTime; // 避免受到 Hitstop 影響
            yield return null;
        }
        mainCamera.transform.localPosition = originalCamPos;
    }
}