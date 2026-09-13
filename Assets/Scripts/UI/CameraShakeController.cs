using UnityEngine;

public class CameraShakeController : MonoBehaviour
{
    [Header("Shake Settings")]
    public float maxShakeIntensity = 0.15f; // 最大抖动幅度
    public float shakeSpeed = 25f;          // 抖动频率

    private Vector3 originalPosition;
    private float currentStress = 0f;       // 当前高压占比 (0 ~ 1)

    private void Start()
    {
        originalPosition = transform.localPosition;
    }

    private void OnEnable()
    {
        GameEventManager.OnEmployeeStatsChanged += HandleStatsChanged;
    }

    private void OnDisable()
    {
        GameEventManager.OnEmployeeStatsChanged -= HandleStatsChanged;
    }

    private void HandleStatsChanged(int characterID, float sanity, float heat)
    {
        // 仅响应玩家 (ID = 0) 的危机状态
        if (characterID == 0)
        {
            float sanityStress = sanity < 30f ? (30f - sanity) / 30f : 0f;
            float heatStress = heat > 70f ? (heat - 70f) / 30f : 0f;
            currentStress = Mathf.Max(sanityStress, heatStress);
        }
    }

    private void Update()
    {
        if (currentStress > 0.01f)
        {
            // 使用 PerlinNoise 实现平滑的生理震颤
            float offsetX = (Mathf.PerlinNoise(Time.time * shakeSpeed, 0f) - 0.5f) * 2f * maxShakeIntensity * currentStress;
            float offsetY = (Mathf.PerlinNoise(0f, Time.time * shakeSpeed) - 0.5f) * 2f * maxShakeIntensity * currentStress;

            transform.localPosition = originalPosition + new Vector3(offsetX, offsetY, 0f);
        }
        else
        {
            // 恢复原位
            transform.localPosition = Vector3.Lerp(transform.localPosition, originalPosition, Time.deltaTime * 5f);
        }
    }
}