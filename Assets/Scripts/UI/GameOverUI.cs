using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    [Header("UI 控件槽位")]
    public GameObject overlayPanel; 
    public TMP_Text resultText;     
    public Button restartButton;    

    [Header("结局视觉控件 (拖入对应组件以更改颜色)")]
    public TMP_Text titleText;               // 结局标题文本 (若为空，代码会自动尝试从子物体抓取)
    public Image backgroundImage;            // 结局背景图片 (若为空，代码会自动尝试从 overlayPanel 抓取)

    [Header("结局颜色自定义 (可在 Inspector 中直接调整)")]
    public Color winColor = new Color(0.2f, 0.8f, 0.4f, 0.9f);        // PlayerWin (绿)
    public Color lossColor = new Color(0.8f, 0.2f, 0.2f, 0.9f);       // OpponentWin (红)
    public Color mutualLossColor = new Color(0.4f, 0.1f, 0.1f, 0.95f); // MutualLoss (暗红)
    public Color mutualWinColor = new Color(0.2f, 0.6f, 0.9f, 0.9f);   // MutualWin (蓝)
    public Color drawColor = new Color(0.8f, 0.8f, 0.2f, 0.9f);        // TimeoutDraw (黄)

    [Header("动画配置")]
    public float fadeDuration = 0.6f;
    public Vector3 startScale = new Vector3(0.85f, 0.85f, 1f);

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (overlayPanel == null)
        {
            overlayPanel = gameObject;
        }

        // 自动补充组件引用，防止 Inspector 忘记拖拽
        if (backgroundImage == null)
        {
            backgroundImage = overlayPanel.GetComponent<Image>();
        }

        if (titleText == null)
        {
            titleText = overlayPanel.GetComponentInChildren<TMP_Text>();
        }

        canvasGroup = overlayPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = overlayPanel.AddComponent<CanvasGroup>();
        }

        // 初始化隐形
        overlayPanel.SetActive(true);
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        overlayPanel.transform.localScale = startScale;
    }

    private void Start()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartGame);
        }

        GameManager.OnGameOverWithEnding -= ShowResult;
        GameManager.OnGameOverWithEnding += ShowResult;
    }

    private void OnDestroy()
    {
        GameManager.OnGameOverWithEnding -= ShowResult;
    }

    public void ShowResult(EndingType ending, string reason)
    {
        if (overlayPanel == null) return;

        overlayPanel.transform.SetAsLastSibling();

        SetupEndingTheme(ending, reason);

        StopAllCoroutines();
        StartCoroutine(Co_AnimateFadeIn());
    }

    private void SetupEndingTheme(EndingType ending, string reason)
    {
        if (resultText != null)
        {
            resultText.text = reason;
        }

        Color themeColor = Color.white;
        string titleStr = "GAME OVER";

        switch (ending)
        {
            case EndingType.PlayerWin:
                titleStr = "VICTORY";
                themeColor = winColor;
                break;

            case EndingType.OpponentWin:
                titleStr = "DEFEAT";
                themeColor = lossColor;
                break;

            case EndingType.MutualLoss:
                titleStr = "MUTUAL COLLAPSE";
                themeColor = mutualLossColor;
                break;

            case EndingType.MutualWin:
                titleStr = "PERFECT HARMONY";
                themeColor = mutualWinColor;
                break;

            case EndingType.TimeoutDraw:
                titleStr = "TIME OUT - DRAW";
                themeColor = drawColor;
                break;

            case EndingType.None:
            default:
                titleStr = "GAME OVER";
                themeColor = Color.gray;
                break;
        }

        // 强行更新标题文字与颜色
        if (titleText != null)
        {
            titleText.text = titleStr;
            titleText.color = themeColor;
        }

        // 强行更新背景颜色
        if (backgroundImage != null)
        {
            backgroundImage.color = themeColor;
        }
    }

    private IEnumerator Co_AnimateFadeIn()
    {
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(timer / fadeDuration);

            canvasGroup.alpha = Mathf.Lerp(0f, 1f, progress);

            float smoothProgress = Mathf.Sin(progress * Mathf.PI * 0.5f);
            overlayPanel.transform.localScale = Vector3.Lerp(startScale, Vector3.one, smoothProgress);

            yield return null;
        }

        canvasGroup.alpha = 1f;
        overlayPanel.transform.localScale = Vector3.one;
    }

    public void RestartGame()
    {
        Time.timeScale = 1.0f;

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