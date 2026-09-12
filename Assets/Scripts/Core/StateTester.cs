using UnityEngine;

public class StateTester : MonoBehaviour
{
    [Header("Controllers")]
    public EmployeeController employeeA;
    public EmployeeController employeeB;
    public ExpressionController expressionB;

    [Header("Control Settings")]
    public float holdThreshold = 0.3f;      // 判定为长按的首次延迟门槛
    public float repeatInterval = 2.0f;     // 长按状态下连续触发的间隔时间 (2s)

    [Header("=== 统一数值配置 ===")]
    [Tooltip("长按防御时【自身】：SAN 变化, HEAT 变化")]
    public Vector2 defenseSelfStats = new Vector2(-2f, -5f);

    [Tooltip("长按防御时【对手】：SAN 变化 (+1), HEAT 变化 (-5)")]
    public Vector2 defenseTargetStats = new Vector2(1f, -5f);

    [Tooltip("短按施压时【自身】：SAN 变化, HEAT 变化")]
    public Vector2 attackSelfStats = new Vector2(-2f, 8f);

    [Tooltip("短按施压时【对手】：SAN 变化, HEAT 变化")]
    public Vector2 attackTargetStats = new Vector2(-20f, 5f);

    // Player A 内部计时状态
    private float aHoldStartTime;
    private float aNextRepeatTime;
    private bool isHoldingSpace;

    // Player B 内部计时状态
    private float bHoldStartTime;
    private float bNextRepeatTime;
    private bool isHoldingB;

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.isTimePaused) return;

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
    }

    // --- 通用动作逻辑 ---
    private void ExecuteDefense(EmployeeController self, EmployeeController target)
    {
        self.ModifyStats(defenseSelfStats.x, defenseSelfStats.y);
        target.ModifyStats(defenseTargetStats.x, defenseTargetStats.y);
    }

    private void ExecuteAttack(EmployeeController self, EmployeeController target, ExpressionController targetExpression = null)
    {
        self.ModifyStats(attackSelfStats.x, attackSelfStats.y);
        target.ModifyStats(attackTargetStats.x, attackTargetStats.y);

        if (targetExpression != null)
        {
            targetExpression.TriggerExpression(ExpressionState.Nervous, 2f);
        }
    }

    // --- Player A (Space) 连续长按逻辑 ---
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

            // 1. 首次达到长按门槛：立即触发第一次长按效果，并设定下一次重复时间
            if (!isHoldingSpace && holdDuration >= holdThreshold)
            {
                isHoldingSpace = true;
                ExecuteDefense(employeeA, employeeB);
                aNextRepeatTime = Time.time + repeatInterval;
            }
            // 2. 持续按住：每满 2 秒 (repeatInterval) 再次触发一次
            else if (isHoldingSpace && Time.time >= aNextRepeatTime)
            {
                ExecuteDefense(employeeA, employeeB);
                aNextRepeatTime = Time.time + repeatInterval;
            }
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            // 单击短按触发施压
            if (!isHoldingSpace)
            {
                ExecuteAttack(employeeA, employeeB, expressionB);
            }
            isHoldingSpace = false;
        }
    }

    // --- Player B (Enter) 连续长按逻辑 ---
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
                ExecuteAttack(employeeB, employeeA, null);
            }
            isHoldingB = false;
        }
    }

    private void HandleSimpleAI() { }
}