using UnityEngine;

public class StateTester : MonoBehaviour
{
    [Header("Controllers")]
    public EmployeeController employeeA;
    public EmployeeController employeeB;
    public ExpressionController expressionB;

    [Header("Player A Controls (Space)")]
    public float holdThreshold = 0.3f;
    private float spacePressedTime;
    private bool isHoldingSpace;

    [Header("PvP Player B Controls (Enter/Shift)")]
    private float bHoldTime;
    private bool isHoldingB;

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.isTimePaused) return;

        HandlePlayerAInput();

        if (GameManager.Instance.currentMode == GameMode.PvP_Local)
        {
            HandlePlayerBInput();
        }
        else
        {
            HandleSimpleAI(); // PvE 模式下 AI 的随机或条件反馈
        }
    }

    // --- Player A (Space) ---
    private void HandlePlayerAInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            spacePressedTime = Time.time;
            isHoldingSpace = false;
        }

        if (Input.GetKey(KeyCode.Space) && !isHoldingSpace && (Time.time - spacePressedTime) >= holdThreshold)
        {
            isHoldingSpace = true;
            // A 进入观察/防御
            employeeA.ModifyStats(-2f, -5f);
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            if (!isHoldingSpace)
            {
                // A 单击施压 -> 触发 B 的紧张神态
                employeeA.ModifyStats(0f, 8f);
                employeeB.ModifyStats(-10f, 5f);
                if (expressionB != null) expressionB.TriggerExpression(ExpressionState.Nervous, 2f);
            }
            isHoldingSpace = false;
        }
    }

    // --- Player B (PvP Mode - Return Key) ---
    private void HandlePlayerBInput()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            bHoldTime = Time.time;
            isHoldingB = false;
        }

        if (Input.GetKey(KeyCode.Return) && !isHoldingB && (Time.time - bHoldTime) >= holdThreshold)
        {
            isHoldingB = true;
            employeeB.ModifyStats(-2f, -5f);
        }

        if (Input.GetKeyUp(KeyCode.Return))
        {
            if (!isHoldingB)
            {
                // Player B 施压
                employeeB.ModifyStats(0f, 8f);
                employeeA.ModifyStats(-10f, 5f);
            }
            isHoldingB = false;
        }
    }

    private void HandleSimpleAI()
    {
        // PvE 模式下 AI 简单逻辑（示例）
    }
}