using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro; // 如果使用的是 Legacy Text，请改为 using UnityEngine.UI; 并将 TMP_Text 改为 Text

public class GameOverUI : MonoBehaviour
{
    [Header("UI 控件槽位")]
    public GameObject overlayPanel; 
    public TMP_Text resultText;     // 若用 Legacy Text，请改为 public Text resultText;
    public Button restartButton;     

    private void Awake()
    {
        // 游戏启动时强制隐藏面板
        if (overlayPanel != null)
        {
            overlayPanel.SetActive(false);
        }
    }

    private void Start()
    {
        // 绑定按钮事件
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartGame);
        }

        // 双重保险：在 Start 中确保订阅静态事件
        GameManager.OnGameOverWithEnding -= ShowResult; // 先解绑防止重复订阅
        GameManager.OnGameOverWithEnding += ShowResult;
    }

    private void OnDestroy()
    {
        // 销毁时解绑
        GameManager.OnGameOverWithEnding -= ShowResult;
    }

    public void ShowResult(EndingType ending, string reason)
    {
        Debug.Log($"<color=green>[GameOverUI] 收到结局通知，正在展示 UI！结局类型: {ending}</color>");

        if (overlayPanel == null)
        {
            Debug.LogError("[GameOverUI] overlayPanel 槽位未赋值！请在 Inspector 中拖入 GameOverOverlay 物体。");
            return;
        }

        // 显现结算面板
        overlayPanel.SetActive(true);

        // 确保面板被置于 UI 最顶层，防止被其他 Canvas/Panel 遮挡
        overlayPanel.transform.SetAsLastSibling();

        if (resultText != null)
        {
            resultText.text = reason;
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1.0f; // 恢复时间流速

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}