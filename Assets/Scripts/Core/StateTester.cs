using UnityEngine;

public class StateTester : MonoBehaviour
{
    [Header("Controllers")]
    public EmployeeController employeeA;
    public EmployeeController employeeB;
    public ExpressionController expressionB;

    [Header("Document Link (甩鍋核心引用)")]
    public DocumentOwnership targetDocument;

    [Header("Control Settings")]
    public float holdThreshold = 0.3f;      
    public float repeatInterval = 2.0f;     
    public float attackCooldown = 0.5f;     

    [Header("Parry Mechanism Settings")]
    [Tooltip("按下防守後的完美格檔時間視窗 (秒)，超過此時間長按防守則無法觸發 Parry")]
    public float parryWindowDuration = 0.4f;

    [Header("AI Settings (PvE)")]
    [Tooltip("AI 決策間隔 (秒)，越小反應越快")]
    public float aiDecisionInterval = 0.8f; // 原 2.5f -> 縮短至 0.8f，大幅提升反應速度
    [Tooltip("當玩家持球時，AI 預判/防守的基礎機率 (0.0~1.0)")]
    public float aiDefenseProbability = 0.85f; // 原 0.8 -> 提高防守意願
    private float nextAiDecisionTime;

    [Header("=== 統一數值配置 ===")]
    public Vector2 defenseSelfStats = new Vector2(-3f, -10f); 
    public Vector2 attackSelfStats = new Vector2(-5f, 8f);
    public Vector2 attackTargetStats = new Vector2(-15f, 15f);

    public Vector2 parryAttackerPenalty = new Vector2(-15f, 25f); 
    public Vector2 parryDefenderReward = new Vector2(5f, -15f);   

    [Header("High Pressure Mechanics")]
    public float highHeatThreshold = 70f;
    public float heatPenaltyMultiplier = 2.0f;
    public float dimishingDuration = 2.0f;
    public float maxDiminishFactor = 0.3f;

    // Player A 狀態
    private float aHoldStartTime;
    private float aNextRepeatTime;
    public bool isHoldingSpace { get; private set; } = false;
    private float aDefensePressTime; 
    private float aNextAttackTime;
    private int aRecentAttackCount = 0; 
    private float aLastAttackTime;

    // Player B (AI / Player 2) 狀態
    private float bHoldStartTime;
    private float bNextRepeatTime;
    public bool isHoldingB { get; private set; } = false;
    private float bDefensePressTime;
    private float bNextAttackTime;
    private int bRecentAttackCount = 0;
    private float bLastAttackTime;

    private void Start()
    {
        nextAiDecisionTime = Time.time + 0.5f;

        if (targetDocument == null)
        {
            Debug.LogError("<color=red>[StateTester 警告]</color> targetDocument 未在 Inspector 中指定！");
        }

        if (GameManager.Instance != null && GameManager.Instance.isTimePaused)
        {
            GameManager.Instance.SetTimePause(false);
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameOver) return;

        if (GameManager.Instance != null && GameManager.Instance.isTimePaused)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                GameManager.Instance.SetTimePause(false);
            }
            return;
        }

        HandlePlayerAInput();

        bool isPvP = GameManager.Instance != null && GameManager.Instance.currentMode == GameMode.PvP_Local;
        if (isPvP)
        {
            HandlePlayerBInput();
        }
        else
        {
            HandleHighDefensiveAI(); // 優化後的 AI 邏輯
        }
        
        DecayAttackCounts();
    }

    private void DecayAttackCounts()
    {
        if (Time.time - aLastAttackTime > dimishingDuration) aRecentAttackCount = 0;
        if (Time.time - bLastAttackTime > dimishingDuration) bRecentAttackCount = 0;
    }

    private void ExecuteDefense(EmployeeController self)
    {
        int id = (self == employeeA) ? 0 : 1;
        GameManager.Instance?.RegisterAction(id);

        self.ModifyStats(defenseSelfStats.x, defenseSelfStats.y);
        GameEventManager.TriggerDefenseExecuted(id);
        Debug.Log($"<color=cyan>[防守成功]</color> {self.name} 降低 HEAT，消耗 SAN。");
    }

    private void ExecutePassDocumentAttack(EmployeeController self, EmployeeController target, ExpressionController targetExpression = null)
    {
        bool isPlayerA = (self == employeeA);
        int attackerID = isPlayerA ? 0 : 1;
        int defenderID = isPlayerA ? 1 : 0;

        // 1. 檢查甩鍋條件與文件狀態
        if (targetDocument != null)
        {
            DocumentOwner requiredOwner = isPlayerA ? DocumentOwner.PlayerA : DocumentOwner.PlayerB;
            if (targetDocument.currentOwner != requiredOwner) return;

            if (targetDocument.isPassingAnimating || targetDocument.IsInProtectionWindow)
            {
                Debug.LogWarning("[甩鍋攔截] 文件平移動畫中或處於保護期，無法發動甩鍋！");
                return;
            }

            if (targetDocument.isBeingHeld)
            {
                PhysicsDraggable draggable = targetDocument.GetComponent<PhysicsDraggable>();
                if (draggable != null && draggable.isDragging) draggable.StopDragging();
            }
        }

        GameManager.Instance?.RegisterAction(attackerID);

        // ==================== 【需求 2：優先結算主動甩鍋方的 SAN 值扣減】 ====================
        float finalSelfSanDmg = attackSelfStats.x;
        float finalSelfHeatGain = attackSelfStats.y;
        if (self.currentHeat > highHeatThreshold)
        {
            finalSelfSanDmg *= heatPenaltyMultiplier; // 高壓下消耗翻倍
        }

        // 立即扣除主動方的代價 (SAN & HEAT)
        self.ModifyStats(finalSelfSanDmg, finalSelfHeatGain);
        Debug.Log($"<color=orange>[甩鍋發動代價]</color> {self.name} 優先扣除代價 SAN: {finalSelfSanDmg}, HEAT: +{finalSelfHeatGain}");

        // 如果主動方因為這下甩鍋直接爆掉 (SAN<=0 或 HEAT>=100)，GameManager 會在 ModifyStats 內直接判輸，此處可安全繼續向下執行判定

        // 2. 判斷防守方是否成功 Parry
        bool targetIsDefending = isPlayerA ? isHoldingB : isHoldingSpace;
        float defenderPressTime = isPlayerA ? bDefensePressTime : aDefensePressTime;
        bool isParryWindowValid = targetIsDefending && ((Time.time - defenderPressTime) <= parryWindowDuration);

        if (isParryWindowValid)
        {
            // ==================== 【Parry 完美格檔成功】 ====================
            GameManager.Instance?.IncrementParryChain(); // 連擊數 +1

            float speedMult = GameManager.Instance != null ? GameManager.Instance.GetParrySpeedMultiplier() : 1.0f;
            float dmgMult = GameManager.Instance != null ? GameManager.Instance.GetParryDamageMultiplier() : 1.0f;

            Debug.Log($"<color=yellow>[Parry 完美格檔！]</color> 連擊：{GameManager.Instance?.currentParryChain} | 速度倍率：{speedMult:F2}x");

            // 額外套用 Parry 懲罰 (主動方再受罰，防守方得獎勵)
            self.ModifyStats(parryAttackerPenalty.x * dmgMult, parryAttackerPenalty.y * dmgMult);
            target.ModifyStats(parryDefenderReward.x, parryDefenderReward.y);

            // 觸發打擊反饋（頓幀 + 鏡頭震動）
            GameManager.Instance?.TriggerHitstopAndShake(0.08f, 0.2f, 0.18f);

            // 甩回給攻擊者 (將連擊的速度加成帶入動畫)
            if (targetDocument != null)
            {
                float baseSpeed = targetDocument.passAnimationSpeed;
                targetDocument.passAnimationSpeed = baseSpeed * speedMult;

                if (isPlayerA) targetDocument.PassDocumentToPlayerA();
                else targetDocument.PassDocumentToOpponent();

                targetDocument.passAnimationSpeed = baseSpeed; // 恢復基礎速度
            }

            GameEventManager.TriggerParry(defenderID, attackerID);
            GameEventManager.TriggerDocumentPassAttempt(attackerID, false);

            if (targetExpression != null)
            {
                targetExpression.TriggerExpression(ExpressionState.Default, 1.5f);
            }
        }
        else
        {
            // ==================== 【普通甩鍋命中】 ====================
            GameManager.Instance?.ResetParryChain(); // 重置連擊

            Debug.Log($"<color=green>[甩鍋成功！]</color> {self.name} 成功把文件甩給了 {target.name}！");

            if (targetDocument != null)
            {
                if (isPlayerA) targetDocument.PassDocumentToOpponent();
                else targetDocument.PassDocumentToPlayerA();
            }

            // 結算被甩鍋目標（防守方）的傷害
            float finalTargetSanDmg = attackTargetStats.x;
            if (targetIsDefending)
            {
                finalTargetSanDmg *= 0.7f; // 普通防守減傷 30%
                Debug.Log($"<color=cyan>[龜縮防守]</color> {target.name} 長按防守抵擋了部分甩鍋傷害！");
            }

            int recentAttacks = isPlayerA ? aRecentAttackCount : bRecentAttackCount;
            if (recentAttacks > 0)
            {
                float diminishRatio = Mathf.Clamp(1.0f - (recentAttacks * 0.25f), maxDiminishFactor, 1.0f);
                finalTargetSanDmg *= diminishRatio;
            }

            target.ModifyStats(finalTargetSanDmg, attackTargetStats.y);

            GameEventManager.TriggerDocumentPassAttempt(attackerID, true);

            if (targetExpression != null)
            {
                targetExpression.TriggerExpression(ExpressionState.Nervous, 2f);
            }
        }

        if (isPlayerA)
        {
            aLastAttackTime = Time.time;
            aRecentAttackCount++;
        }
        else
        {
            bLastAttackTime = Time.time;
            bRecentAttackCount++;
        }
    }

    private void HandlePlayerAInput()
    {
        bool isPlayerAHoldingDocument = (targetDocument != null && targetDocument.currentOwner == DocumentOwner.PlayerA);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (GameManager.Instance != null && GameManager.Instance.isTimePaused)
            {
                GameManager.Instance.SetTimePause(false);
            }

            aHoldStartTime = Time.time;
            aDefensePressTime = Time.time;
            isHoldingSpace = false;
        }

        if (Input.GetKey(KeyCode.Space))
        {
            if (isPlayerAHoldingDocument)
            {
                isHoldingSpace = false;
                return;
            }

            float holdDuration = Time.time - aHoldStartTime;

            if (!isHoldingSpace && holdDuration >= holdThreshold)
            {
                isHoldingSpace = true;
                ExecuteDefense(employeeA);
                aNextRepeatTime = Time.time + repeatInterval;
            }
            else if (isHoldingSpace && Time.time >= aNextRepeatTime)
            {
                ExecuteDefense(employeeA);
                aNextRepeatTime = Time.time + repeatInterval;
            }
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            if (!isHoldingSpace && isPlayerAHoldingDocument)
            {
                if (Time.time >= aNextAttackTime)
                {
                    ExecutePassDocumentAttack(employeeA, employeeB, expressionB);
                    float penaltyCD = employeeA.currentHeat > highHeatThreshold ? 0.3f : 0f;
                    aNextAttackTime = Time.time + attackCooldown + penaltyCD;
                }
            }
            isHoldingSpace = false;
        }
    }

    private void HandlePlayerBInput()
    {
        bool isPlayerBHoldingDocument = (targetDocument != null && targetDocument.currentOwner == DocumentOwner.PlayerB);

        if (Input.GetKeyDown(KeyCode.Return))
        {
            bHoldStartTime = Time.time;
            bDefensePressTime = Time.time;
            isHoldingB = false;
        }

        if (Input.GetKey(KeyCode.Return))
        {
            if (isPlayerBHoldingDocument)
            {
                isHoldingB = false;
                return;
            }

            float holdDuration = Time.time - bHoldStartTime;

            if (!isHoldingB && holdDuration >= holdThreshold)
            {
                isHoldingB = true;
                ExecuteDefense(employeeB);
                bNextRepeatTime = Time.time + repeatInterval;
            }
            else if (isHoldingB && Time.time >= bNextRepeatTime)
            {
                ExecuteDefense(employeeB);
                bNextRepeatTime = Time.time + repeatInterval;
            }
        }

        if (Input.GetKeyUp(KeyCode.Return))
        {
            if (!isHoldingB && isPlayerBHoldingDocument)
            {
                if (Time.time >= bNextAttackTime)
                {
                    ExecutePassDocumentAttack(employeeB, employeeA, null);
                    float penaltyCD = employeeB.currentHeat > highHeatThreshold ? 0.3f : 0f;
                    bNextAttackTime = Time.time + attackCooldown + penaltyCD;
                }
            }
            isHoldingB = false;
        }
    }

    // ==================== 【需求 1：大幅高 PVE 的 AI 防守率與靈敏度】 ====================
    private void HandleHighDefensiveAI()
    {
        if (Time.time < nextAiDecisionTime) return;
        nextAiDecisionTime = Time.time + aiDecisionInterval;

        bool playerHasDocument = (targetDocument != null && targetDocument.currentOwner == DocumentOwner.PlayerA);
        bool aiHasDocument = (targetDocument != null && targetDocument.currentOwner == DocumentOwner.PlayerB);

        // 1. 當玩家持有文件時：高機率進入防守態，隨機點按/長按觸發 Parry 視窗
        if (playerHasDocument)
        {
            if (Random.value < aiDefenseProbability)
            {
                isHoldingB = true;
                bDefensePressTime = Time.time; // 更新按壓時間，爭取觸發 Parry
                
                // 如果 AI Heat 過高，順便執行防守降溫
                if (employeeB.currentHeat > 30f)
                {
                    ExecuteDefense(employeeB);
                }
            }
            else
            {
                isHoldingB = false;
            }
            return;
        }

        // 2. 當 AI 持有文件時：迅速尋找時機甩鍋
        if (aiHasDocument && Time.time >= bNextAttackTime)
        {
            isHoldingB = false;
            if (Random.value < 0.85f) // 85% 機率果斷甩鍋
            {
                ExecutePassDocumentAttack(employeeB, employeeA, null);
                float penaltyCD = employeeB.currentHeat > highHeatThreshold ? 0.3f : 0f;
                bNextAttackTime = Time.time + attackCooldown + penaltyCD;
            }
        }
    }
}