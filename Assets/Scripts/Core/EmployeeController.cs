using UnityEngine;

public class EmployeeController : MonoBehaviour
{
    [Header("Data Source")]
    public EmployeeStateSO stateData;

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