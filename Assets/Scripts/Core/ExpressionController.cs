using UnityEngine;
using System.Collections;

public enum ExpressionState
{
    Default,    // 默认/平静
    Nervous,    // 紧张/流汗
    Aggressive, // 挑衅/施压
    CalmFake    // 伪装镇定 (PvP 欺骗)
}

public class ExpressionController : MonoBehaviour
{
    public EmployeeStateSO stateSO;

    [Header("Current Expression")]
    public ExpressionState currentExpression = ExpressionState.Default;

    [Header("Visual Feedback (Placeholder Material/Sprites)")]
    public Renderer characterRenderer; // 用于测试替换材质或纹理

    private Coroutine resetCoroutine;

    // 触发 2 秒临时神态改变 (需求 4)
    public void TriggerExpression(ExpressionState newState, float duration = 2.0f)
    {
        if (resetCoroutine != null) StopCoroutine(resetCoroutine);

        currentExpression = newState;
        UpdateVisuals();
        Debug.Log($"<color=orange>[Expression]</color> Employee {stateSO.employeeID} 展现神态: {newState}");

        resetCoroutine = StartCoroutine(ResetToDefaultAfterDelay(duration));
    }

    private IEnumerator ResetToDefaultAfterDelay(float delay)
    {
        // 需使用 Realtime，防止细节阅读界面 PauseTime 影响协程计时
        yield return new WaitForSecondsRealtime(delay);

        currentExpression = ExpressionState.Default;
        UpdateVisuals();
        Debug.Log($"<color=gray>[Expression]</color> Employee {stateSO.employeeID} 神态恢复默认");
    }

    private void UpdateVisuals()
    {
        // 此处后续可对接 BlendShapes、Spine 动画或 2D 贴图切换
    }
}