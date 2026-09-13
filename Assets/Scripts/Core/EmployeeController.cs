using UnityEngine;

public class EmployeeController : MonoBehaviour
{
    [Header("Data Source")]
    public EmployeeStateSO stateData;

    // 快捷属性暴露，供 StateTester 或 UI 快速访问
    public float currentHeat => stateData != null ? stateData.currentHeat : 0f;
    public float currentSanity => stateData != null ? stateData.currentSanity : 0f;

    private void Start()
    {
        if (stateData != null)
        {
            stateData.ResetState();
            NotifyStatsChanged();
        }
    }

    public void ModifyStats(float sanityDelta, float heatDelta)
    {
        if (stateData == null) return;

        stateData.currentSanity = Mathf.Clamp(stateData.currentSanity + sanityDelta, 0, stateData.maxSanity);
        stateData.currentHeat = Mathf.Clamp(stateData.currentHeat + heatDelta, 0, 100f);

        NotifyStatsChanged();
    }

    private void NotifyStatsChanged()
    {
        GameEventManager.TriggerStatsChanged(stateData.employeeID, stateData.currentSanity, stateData.currentHeat);
    }
}