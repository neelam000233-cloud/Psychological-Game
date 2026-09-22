using UnityEngine;
using System.Collections;

public enum ExpressionState
{
    Default,    // 預設 / 待機
    Defending,  // 防守姿態 (Parry 視窗內，警惕/護頭)
    Exhausted,  // 長按防守過久 (Parry 視窗過期，吃力/露破綻)
    Nervous,    // 高壓 / 緊張流汗 / 被甩鍋受擊
    Aggressive, // 挑釁 / 主動甩鍋 / 發動假動作
    Confident,  // 得意 / Parry 成功 / 甩鍋成功 / 假動作騙人成功
    CalmFake    // 偽裝鎮定 (PvP 欺騙)
}

public class ExpressionController : MonoBehaviour
{
    [Header("Data Link")]
    public EmployeeStateSO stateSO;

    [Header("Current Expression")]
    public ExpressionState currentExpression = ExpressionState.Default;

    [Header("Visual Textures (貼圖資源)")]
    public Texture defaultTexture;
    public Texture defendingTexture;
    public Texture exhaustedTexture;
    public Texture nervousTexture;
    public Texture aggressiveTexture;
    public Texture calmFakeTexture;

    [Header("Target Renderer & Animator")]
    public Renderer characterRenderer; // 用於替換 3D/2D 角色材質貼圖
    public Animator characterAnimator; // 用於觸發 3D/2D 骨骼動作 (選填)
    public Transform characterBody;     // 角色 3D Mesh (方案一程序化位移 Anchor)

    private Material targetMaterial;
    private Coroutine resetCoroutine;
    private Coroutine motionCoroutine;

    private Vector3 originalPosition;
    private Quaternion originalRotation;

    private void Awake()
    {
        if (characterRenderer != null)
        {
            targetMaterial = characterRenderer.material;
        }

        if (characterBody != null)
        {
            originalPosition = characterBody.localPosition;
            originalRotation = characterBody.localRotation;
        }
    }

    private void OnEnable()
    {
        GameEventManager.OnDefenseStateReported += OnDefenseReport;
        GameEventManager.OnBluffTriggered += OnBluffReport;
    }

    private void OnDisable()
    {
        GameEventManager.OnDefenseStateReported -= OnDefenseReport;
        GameEventManager.OnBluffTriggered -= OnBluffReport;
    }

    /// <summary>
    /// 設定表情狀態。若 duration > 0，將在指定秒數（Realtime）後自動恢復 Default。
    /// </summary>
    public void TriggerExpression(ExpressionState newState, float duration = -1f)
    {
        if (resetCoroutine != null)
        {
            StopCoroutine(resetCoroutine);
            resetCoroutine = null;
        }

        currentExpression = newState;
        UpdateVisuals();

        if (duration > 0)
        {
            resetCoroutine = StartCoroutine(ResetToDefaultAfterDelay(duration));
        }
    }

    /// <summary>
    /// 即時無協程切換表情
    /// </summary>
    public void SetExpression(ExpressionState newState)
    {
        if (currentExpression == newState) return;

        if (resetCoroutine != null)
        {
            StopCoroutine(resetCoroutine);
            resetCoroutine = null;
        }

        currentExpression = newState;
        UpdateVisuals();
    }

    private IEnumerator ResetToDefaultAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        currentExpression = ExpressionState.Default;
        UpdateVisuals();
        resetCoroutine = null;
    }

    private void OnDefenseReport(int empID, string state)
    {
        if (stateSO == null || stateSO.employeeID != empID) return;

        switch (state)
        {
            case "Parry":
                SetExpression(ExpressionState.Defending);
                break;
            case "Normal":
                SetExpression(ExpressionState.Exhausted);
                break;
            case "End":
                SetExpression(ExpressionState.Default);
                break;
        }
    }

    private void OnBluffReport(int empID)
    {
        if (stateSO == null || stateSO.employeeID != empID) return;

        TriggerExpression(ExpressionState.Aggressive, 0.4f);
        PlayBluffMotion();
    }

    private void UpdateVisuals()
    {
        Texture selectedTex = defaultTexture;
        switch (currentExpression)
        {
            case ExpressionState.Default: 
                selectedTex = defaultTexture; 
                break;
            case ExpressionState.Defending: 
                selectedTex = defendingTexture != null ? defendingTexture : defaultTexture; 
                break;
            case ExpressionState.Exhausted: 
                selectedTex = exhaustedTexture != null ? exhaustedTexture : defendingTexture; 
                break;
            case ExpressionState.Nervous: 
                selectedTex = nervousTexture != null ? nervousTexture : defaultTexture; 
                break;
            case ExpressionState.Confident:
            case ExpressionState.Aggressive: 
                selectedTex = aggressiveTexture != null ? aggressiveTexture : defaultTexture; 
                break;
            case ExpressionState.CalmFake: 
                selectedTex = calmFakeTexture != null ? calmFakeTexture : defaultTexture; 
                break;
        }

        if (targetMaterial != null && selectedTex != null)
        {
            targetMaterial.mainTexture = selectedTex;
        }

        if (characterAnimator != null)
        {
            characterAnimator.SetInteger("ExpressionState", (int)currentExpression);
            characterAnimator.SetBool("IsDefending", currentExpression == ExpressionState.Defending || currentExpression == ExpressionState.Exhausted);
        }
    }

    // ==================== 方案一：程序化體感位移 (Procedural Motion) ====================

    public void PlayBluffMotion()
    {
        if (characterBody == null) return;
        if (motionCoroutine != null) StopCoroutine(motionCoroutine);
        motionCoroutine = StartCoroutine(BluffRoutine());
    }

    private IEnumerator BluffRoutine()
    {
        float t = 0f;
        Vector3 targetPos = originalPosition + new Vector3(0, 0, 0.35f);
        Quaternion targetRot = originalRotation * Quaternion.Euler(12f, 0, 0);

        // 1. 向前假突進
        while (t < 0.1f)
        {
            t += Time.deltaTime;
            characterBody.localPosition = Vector3.Lerp(originalPosition, targetPos, t / 0.1f);
            characterBody.localRotation = Quaternion.Slerp(originalRotation, targetRot, t / 0.1f);
            yield return null;
        }

        // 2. 短暫停頓誘騙對手
        yield return new WaitForSeconds(0.15f);

        // 3. 彈回原位
        t = 0f;
        while (t < 0.1f)
        {
            t += Time.deltaTime;
            characterBody.localPosition = Vector3.Lerp(targetPos, originalPosition, t / 0.1f);
            characterBody.localRotation = Quaternion.Slerp(targetRot, originalRotation, t / 0.1f);
            yield return null;
        }

        characterBody.localPosition = originalPosition;
        characterBody.localRotation = originalRotation;

        TriggerExpression(ExpressionState.Confident, 1.0f);
    }
}