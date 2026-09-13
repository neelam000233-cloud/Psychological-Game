using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class StressPostProcessingController : MonoBehaviour
{
    [Header("Volume Reference")]
    public Volume globalVolume;

    [Header("Stress Thresholds")]
    [Tooltip("SAN 低于此值开始触发晕影与模糊")]
    public float sanityDangerThreshold = 40f;

    [Tooltip("HEAT 高于此值开始触发晕影与模糊")]
    public float heatDangerThreshold = 70f;

    [Header("Effect Intensities")]
    public float maxVignetteIntensity = 0.55f;
    public float maxMotionBlurIntensity = 0.75f;
    public float lerpSpeed = 4f;

    private Vignette vignette;
    private MotionBlur motionBlur;

    private float targetVignette = 0f;
    private float targetBlur = 0f;

    private void Awake()
    {
        if (globalVolume == null) globalVolume = GetComponent<Volume>();

        if (globalVolume != null && globalVolume.profile != null)
        {
            globalVolume.profile.TryGet(out vignette);
            globalVolume.profile.TryGet(out motionBlur);
        }
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
        // 仅根据玩家 (ID = 0) 的生理心理状态触发本地视觉惩罚
        if (characterID == 0)
        {
            CalculateStressEffects(sanity, heat);
        }
    }

    private void CalculateStressEffects(float sanity, float heat)
    {
        float sanityStress = 0f;
        float heatStress = 0f;

        // 计算 SAN 过低造成的压迫占比 (0 ~ 1)
        if (sanity < sanityDangerThreshold)
        {
            sanityStress = (sanityDangerThreshold - sanity) / sanityDangerThreshold;
        }

        // 计算 HEAT 过高造成的过热占比 (0 ~ 1)
        if (heat > heatDangerThreshold)
        {
            heatStress = (heat - heatDangerThreshold) / (100f - heatDangerThreshold);
        }

        // 取两者中的最高高压值作为当前视觉效果强度
        float combinedStress = Mathf.Max(sanityStress, heatStress);

        targetVignette = combinedStress * maxVignetteIntensity;
        targetBlur = combinedStress * maxMotionBlurIntensity;
    }

    private void Update()
{
    if (vignette != null)
    {
        vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, targetVignette, Time.deltaTime * lerpSpeed);
    }
    else
    {
        Debug.LogWarning("[PostProcessing] Vignette 组件未成功获取！");
    }

    if (motionBlur != null)
    {
        motionBlur.intensity.value = Mathf.Lerp(motionBlur.intensity.value, targetBlur, Time.deltaTime * lerpSpeed);
    }

    // 实时查看目标强度与实际强度
    if (targetVignette > 0f)
    {
        Debug.Log($"[PostProcessing] 触发高压后处理 | Target Vignette: {targetVignette} | Current: {vignette?.intensity.value}");
    }
}
}