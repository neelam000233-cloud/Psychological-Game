using UnityEngine;
using UnityEngine.UI;

public class HUDStatusController : MonoBehaviour
{
    [System.Serializable]
    public class HUDGroup
    {
        public Slider sanitySlider;
        public Image sanityFillImage;
        public Image sanityBackgroundImage;

        public Slider heatSlider;
        public Image heatFillImage;
        public Image heatBackgroundImage;

        [HideInInspector] public float targetSanity = 100f;
        [HideInInspector] public float targetHeat = 0f;
    }

    [Header("UI Groups")]
    public HUDGroup playerHUD;
    public HUDGroup targetHUD;

    [Header("Settings")]
    public float lerpSpeed = 5f;

    [Header("Color Thresholds")]
    public Color colorDanger = Color.red;                         // 危险色 (SAN<30 或 HEAT>70)
    public Color colorWarning = new Color(1f, 0.8f, 0.2f);        // 预警黄色 (30 - 70)
    public Color colorSafe = new Color(0.2f, 0.8f, 0.2f);          // 安全绿色 (SAN>70 或 HEAT<30)
    public Color colorRightBackground = new Color(0.3f, 0.3f, 0.3f, 0.5f); // 右侧底色灰色

    private void OnEnable()
    {
        GameEventManager.OnEmployeeStatsChanged += HandleStatsChanged;
    }

    private void OnDisable()
    {
        GameEventManager.OnEmployeeStatsChanged -= HandleStatsChanged;
    }

    private void Start()
    {
        SetBackgroundColor(playerHUD);
        SetBackgroundColor(targetHUD);
    }

    private void Update()
    {
        UpdateHUDGroup(playerHUD);
        UpdateHUDGroup(targetHUD);
    }

    private void HandleStatsChanged(int characterID, float sanity, float heat)
    {
        if (characterID == 0)
        {
            playerHUD.targetSanity = sanity;
            playerHUD.targetHeat = heat;
        }
        else if (characterID == 1)
        {
            targetHUD.targetSanity = sanity;
            targetHUD.targetHeat = heat;
        }

        Debug.Log($"[Stats Status] 玩家 SAN: {playerHUD.targetSanity} | HEAT: {playerHUD.targetHeat} <===> 对手 SAN: {targetHUD.targetSanity} | HEAT: {targetHUD.targetHeat}");
    }

    private void UpdateHUDGroup(HUDGroup group)
    {
        // 1. 更新 SAN Slider (SAN 低于 30 变红)
        if (group.sanitySlider != null)
        {
            group.sanitySlider.value = Mathf.Lerp(group.sanitySlider.value, group.targetSanity, Time.deltaTime * lerpSpeed);
            UpdateFillColor(group.sanityFillImage, group.sanitySlider.value, isHeat: false);
        }

        // 2. 更新 HEAT Slider (HEAT 高于 70 变红)
        if (group.heatSlider != null)
        {
            group.heatSlider.value = Mathf.Lerp(group.heatSlider.value, group.targetHeat, Time.deltaTime * lerpSpeed);
            UpdateFillColor(group.heatFillImage, group.heatSlider.value, isHeat: true);
        }
    }

    private void UpdateFillColor(Image fillImage, float currentVal, bool isHeat)
    {
        if (fillImage == null) return;

        if (!isHeat)
        {
            // SAN 逻辑：低数值危险 (<30 变红, 30-70 变黄, >70 变绿)
            if (currentVal < 30f) fillImage.color = colorDanger;
            else if (currentVal <= 70f) fillImage.color = colorWarning;
            else fillImage.color = colorSafe;
        }
        else
        {
            // HEAT 逻辑：高数值危险 (<30 变绿, 30-70 变黄, >70 变红)
            if (currentVal < 30f) fillImage.color = colorSafe;
            else if (currentVal <= 70f) fillImage.color = colorWarning;
            else fillImage.color = colorDanger;
        }
    }

    private void SetBackgroundColor(HUDGroup group)
    {
        if (group == null) return;

        if (group.sanityBackgroundImage != null)
            group.sanityBackgroundImage.color = colorRightBackground;

        if (group.heatBackgroundImage != null)
            group.heatBackgroundImage.color = colorRightBackground;
    }
}