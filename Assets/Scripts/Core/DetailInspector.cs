using UnityEngine;

public class DetailInspector : MonoBehaviour
{
    [Header("Inspection Setup")]
    public Camera mainCamera;
    public float maxInspectDistance = 10f;
    public float requiredFocusTime = 1.5f;

    [Header("Data Links")]
    public EmployeeController playerAController;
    public EmployeeController targetBController;

    [Header("UI Link")]
    public InspectionUIPanel uiPanel; // 关联 Day 5 新建的 UI 控制器

    private float currentFocusTimer = 0f;
    private InspectableDetail currentTargetDetail = null;
    private bool isObserving = false;

    private void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
    }

    private void Update()
    {
        // 时间暂停模式下，无视所有 Space 键操作和射线检测
        if (GameManager.Instance != null && GameManager.Instance.isTimePaused) return;

        isObserving = Input.GetKey(KeyCode.Space);

        if (isObserving)
        {
            PerformRaycastInspection();
        }
        else
        {
            ResetInspection();
        }
    }

    
    private void PerformRaycastInspection()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, maxInspectDistance);

        InspectableDetail detailFound = null;

        foreach (var hit in hits)
        {
            InspectableDetail detail = hit.collider.GetComponent<InspectableDetail>();
            if (detail != null)
            {
                detailFound = detail;
                break;
            }
        }

        if (detailFound != null)
        {
            if (currentTargetDetail == detailFound)
            {
                currentFocusTimer += Time.deltaTime;
                Debug.Log($"<color=yellow>[Focusing]</color> inspecting: {detailFound.detailName} ({currentFocusTimer:F1}s / {requiredFocusTime}s)");

                if (currentFocusTimer >= requiredFocusTime)
                {
                    TriggerDetailFreeze(detailFound);
                }
            }
            else
            {
                currentTargetDetail = detailFound;
                currentFocusTimer = 0f;
            }
        }
        else
        {
            ResetInspection();
        }
    }

    private void ResetInspection()
    {
        currentTargetDetail = null;
        currentFocusTimer = 0f;
    }

    
    private void TriggerDetailFreeze(InspectableDetail detail)
    {
        ResetInspection();

        // 1. 先激活全局时间暂停
        GameManager.Instance.SetTimePause(true);

        // 2. 传递细节数据并呼出 UI Panel
        if (uiPanel != null)
        {
            uiPanel.ShowInspectionPanel(detail);
        }
        else
        {
            // 防错降级处理
            if (playerAController != null) playerAController.ModifyStats(detail.selfSanityBonus, 0f);
            if (targetBController != null) targetBController.ModifyStats(0f, detail.targetHeatPenalty);
            GameManager.Instance.SetTimePause(false);
        }
    }
}