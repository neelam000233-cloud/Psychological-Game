using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDStatusController : MonoBehaviour
{
    [Header("Player A (Player) HUD UI Elements")]
    [SerializeField] private Slider playerSanitySlider;
    [SerializeField] private Slider playerHeatSlider;
    [SerializeField] private TextMeshProUGUI playerSanityText;
    [SerializeField] private TextMeshProUGUI playerHeatText;

    [Header("Player B (Target/AI) HUD UI Elements")]
    [SerializeField] private Slider targetSanitySlider;
    [SerializeField] private Slider targetHeatSlider;
    [SerializeField] private TextMeshProUGUI targetSanityText;
    [SerializeField] private TextMeshProUGUI targetHeatText;

    [Header("Settings")]
    [SerializeField] private float lerpSpeed = 5f; // Slider 平滑过渡速度

    private float targetPlayerSanity;
    private float targetPlayerHeat;
    private float targetTargetSanity;
    private float targetTargetHeat;

    private void OnEnable()
    {
        // 订阅数值变更事件
        GameEventManager.OnEmployeeStatsChanged += HandleStatsChanged;
    }

    private void OnDisable()
    {
        // 解绑事件
        GameEventManager.OnEmployeeStatsChanged -= HandleStatsChanged;
    }

    private void Update()
    {
        // 平滑更新 Slider 动画
        if (playerSanitySlider != null)
            playerSanitySlider.value = Mathf.Lerp(playerSanitySlider.value, targetPlayerSanity, Time.unscaledDeltaTime * lerpSpeed);

        if (playerHeatSlider != null)
            playerHeatSlider.value = Mathf.Lerp(playerHeatSlider.value, targetPlayerHeat, Time.unscaledDeltaTime * lerpSpeed);

        if (targetSanitySlider != null)
            targetSanitySlider.value = Mathf.Lerp(targetSanitySlider.value, targetTargetSanity, Time.unscaledDeltaTime * lerpSpeed);

        if (targetHeatSlider != null)
            targetHeatSlider.value = Mathf.Lerp(targetHeatSlider.value, targetTargetHeat, Time.unscaledDeltaTime * lerpSpeed);
    }

    private void HandleStatsChanged(int characterID, float currentSanity, float currentHeat)
{
    // 根据 characterID 判断是玩家（例如 0）还是目标对手（例如 1）
    if (characterID == 0) // 玩家
    {
        targetPlayerSanity = currentSanity;
        targetPlayerHeat = currentHeat;

        if (playerSanityText != null) playerSanityText.text = $"SAN: {Mathf.RoundToInt(currentSanity)}";
        if (playerHeatText != null) playerHeatText.text = $"HEAT: {Mathf.RoundToInt(currentHeat)}";
    }
    else // 目标对手
    {
        targetTargetSanity = currentSanity;
        targetTargetHeat = currentHeat;

        if (targetSanityText != null) targetSanityText.text = $"SAN: {Mathf.RoundToInt(currentSanity)}";
        if (targetHeatText != null) targetHeatText.text = $"HEAT: {Mathf.RoundToInt(currentHeat)}";
    }
}
}