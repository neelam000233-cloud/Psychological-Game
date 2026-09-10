using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InspectionUIPanel : MonoBehaviour
{
    [Header("UI Component Links")]
    public GameObject panelRoot;            
    public TextMeshProUGUI titleText;       
    public TextMeshProUGUI descriptionText; 
    public Button optionAButton;            
    public Button optionBButton;            
    public TextMeshProUGUI optionAText;     
    public TextMeshProUGUI optionBText;     

    [Header("Data Links")]
    public EmployeeController playerA;
    public EmployeeController targetB;

    private InspectableDetail currentDetail;

    private void Awake()
    {
        // 刚开始确保 Panel 不可见
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void OnEnable()
    {
        // 1. 绑定时间暂停状态事件
        GameEventManager.OnTimePauseStateChanged += HandleTimePauseStateChanged;
    }

    private void OnDisable()
    {
        GameEventManager.OnTimePauseStateChanged -= HandleTimePauseStateChanged;
    }

    private void HandleTimePauseStateChanged(bool isPaused)
    {
        // 未进入时间暂停时，Panel 绝对不出现；进入暂停时由 ShowInspectionPanel 控制激活
        if (!isPaused && panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    // 由 DetailInspector 在触发细节看破时调用
    public void ShowInspectionPanel(InspectableDetail detail)
    {
        currentDetail = detail;

        if (titleText != null) titleText.text = detail.detailName;
        if (descriptionText != null) descriptionText.text = detail.detailDescription;

        // 隐藏具体的数值变化描述，仅保留博弈决策名称
        if (optionAText != null) optionAText.text = "Press Advantage";
        if (optionBText != null) optionBText.text = "Maintain Composure";

        // 绑定按钮点击事件
        if (optionAButton != null)
        {
            optionAButton.onClick.RemoveAllListeners();
            optionAButton.onClick.AddListener(OnSelectOptionA);
        }

        if (optionBButton != null)
        {
            optionBButton.onClick.RemoveAllListeners();
            optionBButton.onClick.AddListener(OnSelectOptionB);
        }

        // 只有进入时间暂停且数据准备就绪时，Panel 才出现
        if (panelRoot != null) panelRoot.SetActive(true);
    }

    private void OnSelectOptionA()
    {
        // 选项 A：增加对方 Heat
        if (targetB != null && currentDetail != null)
        {
            targetB.ModifyStats(0f, currentDetail.targetHeatPenalty);
            Debug.Log($"<color=red>[Decision A]</color> Applied +{currentDetail.targetHeatPenalty} Heat to Target B.");
        }
        ApplyDecisionAndResume();
    }

    private void OnSelectOptionB()
    {
        // 选项 B：恢复自身 Sanity
        if (playerA != null && currentDetail != null)
        {
            playerA.ModifyStats(currentDetail.selfSanityBonus, 0f);
            Debug.Log($"<color=green>[Decision B]</color> Recovered +{currentDetail.selfSanityBonus} Sanity to Player A.");
        }
        ApplyDecisionAndResume();
    }

    private void ApplyDecisionAndResume()
    {
        // 3. 做出选择后：隐藏 Panel，解除时间暂停
        if (panelRoot != null) panelRoot.SetActive(false);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetTimePause(false);
        }
    }
}