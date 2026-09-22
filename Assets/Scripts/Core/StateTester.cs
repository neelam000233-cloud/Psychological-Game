using System.Collections;
using UnityEngine;

public class StateTester : MonoBehaviour
{
    [Header("Controllers")]
    public EmployeeController employeeA;
    public EmployeeController employeeB;
    public ExpressionController expressionA;
    public ExpressionController expressionB;

    [Header("Document Link (甩鍋核心引用)")]
    public DocumentOwnership targetDocument;

    [Header("Control Settings")]
    public float holdThreshold = 0.3f;      
    public float repeatInterval = 2.0f;     
    public float attackCooldown = 0.5f;     

    [Header("Parry Mechanism Settings")]
    [Tooltip("按下防守後的完美格檔時間視窗 (秒)，超過此時間長按防守則無法觸發 Parry")]
    public float parryWindowDuration = 1f;

    [Header("Bluff Mechanism Settings")]
    [Tooltip("假動作冷卻時間 (秒)")]
    public float bluffCooldown = 1.2f;
    [Tooltip("身體假動作前傾距離")]
    public float bluffBodyMoveDistance = 0.6f;
    private float lastBluffTimeA = -999f;
    private float lastBluffTimeB = -999f;

    [Header("AI Settings (PvE)")]
    [Tooltip("AI 決策間隔 (秒)，越小反應越快")]
    public float aiDecisionInterval = 0.8f; 
    [Tooltip("當玩家持球時，AI 預判/防守的基礎機率 (0.0~1.0)")]
    public float aiDefenseProbability = 0.85f; 
    [Tooltip("當 AI 持球時，發動假動作誘騙玩家的機率 (0.0~1.0)")]
    public float aiBluffProbability = 0.25f;
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

    private void OnEnable()
    {
        GameEventManager.OnBluff += OnBluffTriggered;
    }

    private void OnDisable()
    {
        GameEventManager.OnBluff -= OnBluffTriggered;
    }

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
            HandleHighDefensiveAI();
        }
        
        UpdateDefenseExpressions();
        DecayAttackCounts();
    }

    private void OnBluffTriggered(int attackerID)
    {
        bool isPvP = GameManager.Instance != null && GameManager.Instance.currentMode == GameMode.PvP_Local;

        // 若在 PvE 模式下，甲方 (0) 發動假動作，讓乙方 AI (1) 有 70% 機率被騙而做出誤防姿態
        if (!isPvP && attackerID == 0)
        {
            if (Random.value < 0.70f)
            {
                isHoldingB = true;
                bDefensePressTime = Time.time;
                ExecuteDefense(employeeB);

                if (expressionB != null)
                {
                    expressionB.TriggerExpression(ExpressionState.Nervous, 1.2f);
                }

                Debug.Log("<color=red>[乙方 AI 誤防]</color> 被甲方的假動作騙到了，交出了防禦！");
            }
        }
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
    }

    private void ExecutePassDocumentAttack(EmployeeController self, EmployeeController target, ExpressionController targetExpression = null)
    {
        bool isPlayerA = (self == employeeA);
        int attackerID = isPlayerA ? 0 : 1;
        int defenderID = isPlayerA ? 1 : 0;
        ExpressionController attackerExpression = isPlayerA ? expressionA : expressionB;

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

        // ✅ 新增：成功發動真實甩鍋時，發起方角色身體也做朝向對手的前傾動作
        if (self != null)
        {
            PlayCharacterBluffBodyAnimation(self.transform, isPlayerA);
        }

        GameManager.Instance?.RegisterAction(attackerID);

        float finalSelfSanDmg = attackSelfStats.x;
        float finalSelfHeatGain = attackSelfStats.y;
        if (self.currentHeat > highHeatThreshold)
        {
            finalSelfSanDmg *= heatPenaltyMultiplier;
        }

        self.ModifyStats(finalSelfSanDmg, finalSelfHeatGain);

        bool targetIsDefending = isPlayerA ? isHoldingB : isHoldingSpace;
        float defenderPressTime = isPlayerA ? bDefensePressTime : aDefensePressTime;
        bool isParryWindowValid = targetIsDefending && ((Time.time - defenderPressTime) <= parryWindowDuration);

        if (isParryWindowValid)
        {
            // Parry 完美格檔
            GameManager.Instance?.IncrementParryChain();

            float speedMult = GameManager.Instance != null ? GameManager.Instance.GetParrySpeedMultiplier() : 1.0f;
            float dmgMult = GameManager.Instance != null ? GameManager.Instance.GetParryDamageMultiplier() : 1.0f;

            self.ModifyStats(parryAttackerPenalty.x * dmgMult, parryAttackerPenalty.y * dmgMult);
            target.ModifyStats(parryDefenderReward.x, parryDefenderReward.y);

            GameManager.Instance?.TriggerHitstopAndShake(0.08f, 0.2f, 0.18f);

            if (targetDocument != null)
            {
                float baseSpeed = targetDocument.passAnimationSpeed;
                targetDocument.passAnimationSpeed = baseSpeed * speedMult;

                if (isPlayerA) targetDocument.PassDocumentToPlayerA();
                else targetDocument.PassDocumentToOpponent();

                targetDocument.passAnimationSpeed = baseSpeed;
            }

            GameEventManager.TriggerParry(defenderID, attackerID);
            GameEventManager.TriggerDocumentPassAttempt(attackerID, false);

            if (targetExpression != null) targetExpression.TriggerExpression(ExpressionState.Aggressive, 1.5f);
            if (attackerExpression != null) attackerExpression.TriggerExpression(ExpressionState.Nervous, 1.5f);
        }
        else
        {
            // 普通甩鍋命中
            GameManager.Instance?.ResetParryChain();

            if (targetDocument != null)
            {
                if (isPlayerA) targetDocument.PassDocumentToOpponent();
                else targetDocument.PassDocumentToPlayerA();
            }

            float finalTargetSanDmg = attackTargetStats.x;
            if (targetIsDefending)
            {
                finalTargetSanDmg *= 0.7f;
            }

            int recentAttacks = isPlayerA ? aRecentAttackCount : bRecentAttackCount;
            if (recentAttacks > 0)
            {
                float diminishRatio = Mathf.Clamp(1.0f - (recentAttacks * 0.25f), maxDiminishFactor, 1.0f);
                finalTargetSanDmg *= diminishRatio;
            }

            target.ModifyStats(finalTargetSanDmg, attackTargetStats.y);

            GameEventManager.TriggerDocumentPassAttempt(attackerID, true);

            if (attackerExpression != null) attackerExpression.TriggerExpression(ExpressionState.Confident, 1.2f);
            if (targetExpression != null) targetExpression.TriggerExpression(ExpressionState.Nervous, 2.0f);
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

    private void UpdateDefenseExpressions()
    {
        bool isPlayerAHoldingDocument = (targetDocument != null && targetDocument.currentOwner == DocumentOwner.PlayerA);

        if (!isPlayerAHoldingDocument && Input.GetKey(KeyCode.Space))
        {
            float pressDuration = Time.time - aDefensePressTime;

            if (pressDuration <= parryWindowDuration)
            {
                GameEventManager.TriggerDefenseStateReported(0, "Parry");
            }
            else
            {
                GameEventManager.TriggerDefenseStateReported(0, "Normal");
            }
        }
        else if (Input.GetKeyUp(KeyCode.Space))
        {
            GameEventManager.TriggerDefenseStateReported(0, "End");
        }

        bool isPlayerBHoldingDocument = (targetDocument != null && targetDocument.currentOwner == DocumentOwner.PlayerB);

        if (!isPlayerBHoldingDocument && isHoldingB)
        {
            float pressDuration = Time.time - bDefensePressTime;

            if (pressDuration <= parryWindowDuration)
            {
                GameEventManager.TriggerDefenseStateReported(1, "Parry");
            }
            else
            {
                GameEventManager.TriggerDefenseStateReported(1, "Normal");
            }
        }
    }

    private void HandlePlayerAInput()
    {
        bool isPlayerAHoldingDocument = (targetDocument != null && targetDocument.currentOwner == DocumentOwner.PlayerA);

        // ===== Player A (甲方) F 鍵假動作 =====
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (Time.time - lastBluffTimeA >= bluffCooldown)
            {
                lastBluffTimeA = Time.time;
                
                GameEventManager.TriggerBluff(0);

                if (expressionA != null)
                {
                    expressionA.TriggerExpression(ExpressionState.Aggressive, 0.8f);
                }

                if (employeeA != null)
                {
                    PlayCharacterBluffBodyAnimation(employeeA.transform, true);
                }

                if (targetDocument != null && targetDocument.currentOwner == DocumentOwner.PlayerA)
                {
                    targetDocument.PlayBluffAnimation(employeeB != null ? employeeB.transform : null);
                }
            }
        }

        // ===== 1. 按下 Space 鍵 =====
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (GameManager.Instance != null && GameManager.Instance.isTimePaused)
            {
                GameManager.Instance.SetTimePause(false);
            }

            aHoldStartTime = Time.time;
            aDefensePressTime = Time.time;

            if (!isPlayerAHoldingDocument)
            {
                isHoldingSpace = true;
                ExecuteDefense(employeeA);
                aNextRepeatTime = Time.time + repeatInterval;
            }
            else
            {
                isHoldingSpace = false;
            }
        }

        // ===== 2. 持續按住 Space 鍵 =====
        if (Input.GetKey(KeyCode.Space))
        {
            isPlayerAHoldingDocument = (targetDocument != null && targetDocument.currentOwner == DocumentOwner.PlayerA);

            if (!isPlayerAHoldingDocument)
            {
                if (!isHoldingSpace)
                {
                    isHoldingSpace = true;
                    aDefensePressTime = Time.time;
                    ExecuteDefense(employeeA);
                    aNextRepeatTime = Time.time + repeatInterval;
                }
                else if (Time.time >= aNextRepeatTime)
                {
                    ExecuteDefense(employeeA);
                    aNextRepeatTime = Time.time + repeatInterval;
                }
            }
        }

        // ===== 3. 松開 Space 鍵 =====
        if (Input.GetKeyUp(KeyCode.Space))
        {
            if (isPlayerAHoldingDocument && !isHoldingSpace)
            {
                if (Time.time >= aNextAttackTime)
                {
                    ExecutePassDocumentAttack(employeeA, employeeB, expressionB);
                    float penaltyCD = employeeA.currentHeat > highHeatThreshold ? 0.3f : 0f;
                    aNextAttackTime = Time.time + attackCooldown + penaltyCD;
                }
            }

            isHoldingSpace = false;
            GameEventManager.TriggerDefenseStateReported(0, "End");
        }
    }

    private void HandlePlayerBInput()
    {
        bool isPlayerBHoldingDocument = (targetDocument != null && targetDocument.currentOwner == DocumentOwner.PlayerB);

        // ===== Player B (乙方) Keypad Enter 假動作 =====
        if (Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (Time.time - lastBluffTimeB >= bluffCooldown)
            {
                lastBluffTimeB = Time.time;

                GameEventManager.TriggerBluff(1);

                if (expressionB != null)
                {
                    expressionB.TriggerExpression(ExpressionState.Aggressive, 0.8f);
                }

                if (employeeB != null)
                {
                    PlayCharacterBluffBodyAnimation(employeeB.transform, false);
                }

                if (targetDocument != null && targetDocument.currentOwner == DocumentOwner.PlayerB)
                {
                    targetDocument.PlayBluffAnimation(employeeA != null ? employeeA.transform : null);
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            bHoldStartTime = Time.time;
            bDefensePressTime = Time.time;

            if (!isPlayerBHoldingDocument)
            {
                isHoldingB = true;
                ExecuteDefense(employeeB);
                bNextRepeatTime = Time.time + repeatInterval;
            }
            else
            {
                isHoldingB = false;
            }
        }

        if (Input.GetKey(KeyCode.Return))
        {
            isPlayerBHoldingDocument = (targetDocument != null && targetDocument.currentOwner == DocumentOwner.PlayerB);

            if (!isPlayerBHoldingDocument)
            {
                if (!isHoldingB)
                {
                    isHoldingB = true;
                    bDefensePressTime = Time.time;
                    ExecuteDefense(employeeB);
                    bNextRepeatTime = Time.time + repeatInterval;
                }
                else if (Time.time >= bNextRepeatTime)
                {
                    ExecuteDefense(employeeB);
                    bNextRepeatTime = Time.time + repeatInterval;
                }
            }
        }

        if (Input.GetKeyUp(KeyCode.Return))
        {
            if (isPlayerBHoldingDocument && !isHoldingB)
            {
                if (Time.time >= bNextAttackTime)
                {
                    ExecutePassDocumentAttack(employeeB, employeeA, expressionA);
                    float penaltyCD = employeeB.currentHeat > highHeatThreshold ? 0.3f : 0f;
                    bNextAttackTime = Time.time + attackCooldown + penaltyCD;
                }
            }

            isHoldingB = false;
            GameEventManager.TriggerDefenseStateReported(1, "End");
        }
    }

    private void HandleHighDefensiveAI()
    {
        if (Time.time < nextAiDecisionTime) return;
        nextAiDecisionTime = Time.time + aiDecisionInterval;

        bool playerHasDocument = (targetDocument != null && targetDocument.currentOwner == DocumentOwner.PlayerA);
        bool aiHasDocument = (targetDocument != null && targetDocument.currentOwner == DocumentOwner.PlayerB);

        if (playerHasDocument)
        {
            if (Random.value < aiDefenseProbability)
            {
                isHoldingB = true;
                bDefensePressTime = Time.time;
                
                if (employeeB.currentHeat > 30f)
                {
                    ExecuteDefense(employeeB);
                }
            }
            else
            {
                isHoldingB = false;
                GameEventManager.TriggerDefenseStateReported(1, "End");
            }
            return;
        }

        // ===== AI 持球時的行動邏輯 =====
        if (aiHasDocument)
        {
            // ✅ AI 發動假動作機制：一定機率發動假動作誘騙玩家
            if (Time.time - lastBluffTimeB >= bluffCooldown && Random.value < aiBluffProbability)
            {
                lastBluffTimeB = Time.time;

                GameEventManager.TriggerBluff(1);

                if (expressionB != null)
                {
                    expressionB.TriggerExpression(ExpressionState.Aggressive, 0.8f);
                }

                if (employeeB != null)
                {
                    PlayCharacterBluffBodyAnimation(employeeB.transform, false);
                }

                if (targetDocument != null)
                {
                    targetDocument.PlayBluffAnimation(employeeA != null ? employeeA.transform : null);
                }

                Debug.Log("<color=yellow>[AI 發動假動作]</color> AI 做了甩鍋假動作誘騙玩家防守！");
                return;
            }

            // AI 真實甩鍋攻擊
            if (Time.time >= bNextAttackTime)
            {
                isHoldingB = false;
                if (Random.value < 0.85f)
                {
                    ExecutePassDocumentAttack(employeeB, employeeA, expressionA);
                    float penaltyCD = employeeB.currentHeat > highHeatThreshold ? 0.3f : 0f;
                    bNextAttackTime = Time.time + attackCooldown + penaltyCD;
                }
            }
        }
    }

    // ==================== 純角色座標相對向量前傾 ====================
    private void PlayCharacterBluffBodyAnimation(Transform characterTransform, bool isPlayerA)
    {
        if (characterTransform == null) return;

        Vector3 targetOpponentPos = Vector3.zero;

        if (isPlayerA && employeeB != null)
        {
            targetOpponentPos = employeeB.transform.position;
        }
        else if (!isPlayerA && employeeA != null)
        {
            targetOpponentPos = employeeA.transform.position;
        }

        if (targetOpponentPos != Vector3.zero)
        {
            StartCoroutine(AnimateCharacterBodyShakeTowards(characterTransform, targetOpponentPos));
        }
    }

    private IEnumerator AnimateCharacterBodyShakeTowards(Transform charTransform, Vector3 opponentWorldPos)
    {
        Vector3 originalPos = charTransform.position;
        Quaternion originalRot = charTransform.rotation;
        Vector3 originalScale = charTransform.localScale;

        // 1. 計算方向向量 (Pos_Opponent - Pos_Self)
        Vector3 directionToOpponent = (opponentWorldPos - originalPos);
        directionToOpponent.y = 0f; // 忽略高低差
        
        if (directionToOpponent.sqrMagnitude > 0.001f)
        {
            directionToOpponent.Normalize();
        }
        else
        {
            directionToOpponent = charTransform.forward;
        }

        // 2. 幅度設定
        float moveDistance = bluffBodyMoveDistance > 0f ? bluffBodyMoveDistance : 0.6f; 
        Vector3 targetPos = originalPos + directionToOpponent * moveDistance;

        // 3. 俯身旋轉設定
        Vector3 pitchAxis = Vector3.Cross(Vector3.up, directionToOpponent); 
        Quaternion targetRot = Quaternion.AngleAxis(15f, pitchAxis) * originalRot; 

        // 4. 視覺縮放擠壓
        Vector3 targetScale = new Vector3(originalScale.x, originalScale.y * 0.92f, originalScale.z * 1.08f);

        float duration = 0.18f; 
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float pingPong = Mathf.Sin((elapsed / duration) * Mathf.PI);

            charTransform.position = Vector3.Lerp(originalPos, targetPos, pingPong);
            charTransform.rotation = Quaternion.Slerp(originalRot, targetRot, pingPong);
            charTransform.localScale = Vector3.Lerp(originalScale, targetScale, pingPong);

            yield return null;
        }

        // 復原初始狀態
        charTransform.position = originalPos;
        charTransform.rotation = originalRot;
        charTransform.localScale = originalScale;
    }
}