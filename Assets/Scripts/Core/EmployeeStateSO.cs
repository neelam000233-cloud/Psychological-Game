using UnityEngine;

[CreateAssetMenu(fileName = "EmployeeState", menuName = "Deadlock/Employee State")]
public class EmployeeStateSO : ScriptableObject
{
    [Header("Base Attributes")]
    public int employeeID; // 0 代表角色A, 1 代表角色B
    public float maxSanity = 100f;
    public float currentSanity = 100f;
    public float currentHeat = 0f; // 甩锅热度/责任沉重度

    [Header("Dynamic Curves")]
    public AnimationCurve tremorAmplitudeCurve; // 手抖映射曲线
    public AnimationCurve resistanceCurve;      // 阻力映射曲线

    public void ResetState()
    {
        currentSanity = maxSanity;
        currentHeat = 0f;
    }

    public float GetCurrentTremorAmplitude()
    {
        float ratio = Mathf.Clamp01(currentSanity / maxSanity);
        return tremorAmplitudeCurve.Evaluate(1f - ratio);
    }

    public float GetCurrentResistance()
    {
        float ratio = Mathf.Clamp01(currentHeat / 100f);
        return resistanceCurve.Evaluate(ratio);
    }
}