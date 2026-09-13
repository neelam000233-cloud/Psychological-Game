using UnityEngine;

public class StateTester : MonoBehaviour
{
    [Header("Controllers")]
    public EmployeeController employeeA;
    public EmployeeController employeeB;
    public ExpressionController expressionB;

    [Header("Control Settings")]
    public float holdThreshold = 0.3f;      // 判定为长按的门槛
    public float repeatInterval = 2.0f;     // 长按连续触发间隔 (2s)
    public float attackCooldown = 0.5f;     // 每次攻击后的基础硬直/冷却时间

    [Header("AI Settings (PvE)")]
    [Tooltip("AI 决策间隔时间（已延长）")]
    public float aiDecisionInterval = 3.0f; // 延长至 3 秒，防止频发攻击
    private float nextAiDecisionTime;
    private int playerConsecutiveDefenses = 0; // 追踪玩家连续防御次数

    [Header("=== 统一数值配置 ===")]
    [Tooltip("长按防御时【自身】：SAN 变化, HEAT 变化")]
    public Vector2 defenseSelfStats = new Vector2(2f, -5f);

    [Tooltip("长按防御时【对手】：SAN 变化 (+1), HEAT 变化 (-10)")]
    public Vector2 defenseTargetStats = new Vector2(1f, -10f);

    [Tooltip("短按施压时【自身】：SAN 变化, HEAT 变化")]
    public Vector2 attackSelfStats = new Vector2(-5f, 8f);

    [Tooltip("短按施压时【对手】：SAN 变化, HEAT 变化")]
    public Vector2 attackTargetStats = new Vector2(-10f, 10f);

    [Header("High Pressure Mechanics (高压机制)")]
    public float highHeatThreshold = 70f;     // 判定为高 HEAT 的阈值
    public float heatPenaltyMultiplier = 2.0f; // 高 HEAT 时，攻击反噬自身的倍率
    public float dimishingDuration = 2.0f;    // 伤害递减判定窗口期 (连续攻击时)
    public float maxDiminishFactor = 0.3f;    // 伤害最多衰减到原本的 30%

    // Player A 内部状态
    private float aHoldStartTime;
    private float aNextRepeatTime;
    private bool isHoldingSpace;
    private float aNextAttackTime;
    private int aRecentAttackCount = 0; 
    private float aLastAttackTime;

    // Player B 内部状态
    private float bHoldStartTime;
    private float bNextRepeatTime;
    private bool isHoldingB;
    private float bNextAttackTime;
    private int bRecentAttackCount = 0;
    private float bLastAttackTime;

    private void Start()
    {
        // 错开开局 AI 首次决策时间，给玩家思考空隙
        nextAiDecisionTime = Time.time + 1.5f;
    }

    private void Update()
    {
        // 游戏暂停或结束时拦截所有输入
        if (GameManager.Instance != null && (GameManager.Instance.isTimePaused || GameManager.Instance.isGameOver)) 
            return;

        HandlePlayerAInput();

        bool isPvP = GameManager.Instance != null && GameManager.Instance.currentMode == GameMode.PvP_Local;
        if (isPvP)
        {
            HandlePlayerBInput();
        }
        else
        {
            HandleSimpleAI();
        }
        
        DecayAttackCounts();
    }

    private void DecayAttackCounts()
    {
        if (Time.time - aLastAttackTime > dimishingDuration)
        {
            aRecentAttackCount = 0;
        }
        if (Time.time - bLastAttackTime > dimishingDuration)
        {
            bRecentAttackCount = 0;
        }
    }

    private void ExecuteDefense(EmployeeController self, EmployeeController target)
    {
        int id = (self == employeeA) ? 0 : 1;
        GameManager.Instance?.RegisterAction(id);

        self.ModifyStats(defenseSelfStats.x, defenseSelfStats.y);
        target.ModifyStats(defenseTargetStats.x, defenseTargetStats.y);

        if (self == employeeA)
        {
            playerConsecutiveDefenses++;
        }
    }

    private void ExecuteAttack(EmployeeController self, EmployeeController target, ExpressionController targetExpression = null)
    {
        int id = (self == employeeA) ? 0 : 1;
        GameManager.Instance?.RegisterAction(id);

        bool isPlayerA = (self == employeeA);
        
        // 1. 高 HEAT 攻击反噬
        float finalSelfSanDmg = attackSelfStats.x;
        float finalSelfHeatGain = attackSelfStats.y;
        
        if (self.currentHeat > highHeatThreshold)
        {
            finalSelfSanDmg *= heatPenaltyMultiplier;
            Debug.Log($"<color=orange>[情绪失控]</color> {self.name} 高HEAT下攻击，遭到严重反噬！");
        }

        // 2. 连续攻击 SAN 伤害递减
        float finalTargetSanDmg = attackTargetStats.x;
        int recentAttacks = isPlayerA ? aRecentAttackCount : bRecentAttackCount;
        
        if (recentAttacks > 0)
        {
            float diminishRatio = Mathf.Clamp(1.0f - (recentAttacks * 0.25f), maxDiminishFactor, 1.0f);
            finalTargetSanDmg *= diminishRatio;
        }

        self.ModifyStats(finalSelfSanDmg, finalSelfHeatGain);
        target.ModifyStats(finalTargetSanDmg, attackTargetStats.y);

        if (isPlayerA)
        {
            aLastAttackTime = Time.time;
            aRecentAttackCount++;
            playerConsecutiveDefenses = 0; 
        }
        else
        {
            bLastAttackTime = Time.time;
            bRecentAttackCount++;
        }

        if (targetExpression != null)
        {
            targetExpression.TriggerExpression(ExpressionState.Nervous, 2f);
        }
    }

    private void HandlePlayerAInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            aHoldStartTime = Time.time;
            isHoldingSpace = false;
        }

        if (Input.GetKey(KeyCode.Space))
        {
            float holdDuration = Time.time - aHoldStartTime;

            if (!isHoldingSpace && holdDuration >= holdThreshold)
            {
                isHoldingSpace = true;
                ExecuteDefense(employeeA, employeeB);
                aNextRepeatTime = Time.time + repeatInterval;
            }
            else if (isHoldingSpace && Time.time >= aNextRepeatTime)
            {
                ExecuteDefense(employeeA, employeeB);
                aNextRepeatTime = Time.time + repeatInterval;
            }
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            if (!isHoldingSpace)
            {
                if (Time.time >= aNextAttackTime)
                {
                    ExecuteAttack(employeeA, employeeB, expressionB);
                    float penaltyCD = employeeA.currentHeat > highHeatThreshold ? 0.3f : 0f;
                    aNextAttackTime = Time.time + attackCooldown + penaltyCD;
                }
            }
            isHoldingSpace = false;
        }
    }

    private void HandlePlayerBInput()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            bHoldStartTime = Time.time;
            isHoldingB = false;
        }

        if (Input.GetKey(KeyCode.Return))
        {
            float holdDuration = Time.time - bHoldStartTime;

            if (!isHoldingB && holdDuration >= holdThreshold)
            {
                isHoldingB = true;
                ExecuteDefense(employeeB, employeeA);
                bNextRepeatTime = Time.time + repeatInterval;
            }
            else if (isHoldingB && Time.time >= bNextRepeatTime)
            {
                ExecuteDefense(employeeB, employeeA);
                bNextRepeatTime = Time.time + repeatInterval;
            }
        }

        if (Input.GetKeyUp(KeyCode.Return))
        {
            if (!isHoldingB)
            {
                if (Time.time >= bNextAttackTime)
                {
                    ExecuteAttack(employeeB, employeeA, null);
                    float penaltyCD = employeeB.currentHeat > highHeatThreshold ? 0.3f : 0f;
                    bNextAttackTime = Time.time + attackCooldown + penaltyCD;
                }
            }
            isHoldingB = false;
        }
    }

    // --- 动态 AI 决策逻辑（更长的思考间隔） ---
    private void HandleSimpleAI()
    {
        if (Time.time < bNextAttackTime || Time.time < nextAiDecisionTime) return;

        // 设置下一次决策时间（3 秒间隔）
        nextAiDecisionTime = Time.time + aiDecisionInterval;

        // 条件 1: 自身过热 (HEAT > 60)，极大可能选择防守降温
        if (employeeB.currentHeat > 60f)
        {
            if (Random.value < 0.85f)
            {
                ExecuteDefense(employeeB, employeeA);
                Debug.Log("<color=cyan>[AI 决策]</color> 思考完毕：自身过热，选择防守降温");
                return;
            }
        }

        // 条件 2: 响应玩家善意
        if (playerConsecutiveDefenses >= 2)
        {
            if (Random.value < 0.75f)
            {
                ExecuteDefense(employeeB, employeeA);
                Debug.Log("<color=green>[AI 决策]</color> 思考完毕：感知到连续善意，跟随防守");
                return;
            }
        }

        // 条件 3: 常规决策 (60% 进攻 / 40% 防守，比之前稍微保守些)
        if (Random.value < 0.6f)
        {
            ExecuteAttack(employeeB, employeeA, null);
            float penaltyCD = employeeB.currentHeat > highHeatThreshold ? 0.3f : 0f;
            bNextAttackTime = Time.time + attackCooldown + penaltyCD;
            Debug.Log("<color=red>[AI 决策]</color> 思考完毕：选择进攻");
        }
        else
        {
            ExecuteDefense(employeeB, employeeA);
            Debug.Log("<color=cyan>[AI 决策]</color> 思考完毕：选择防守观察");
        }
    }
}