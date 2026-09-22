using System.Collections;
using UnityEngine;

public enum DocumentOwner
{
    PlayerA,
    PlayerB
}

[RequireComponent(typeof(PhysicsDraggable))]
[RequireComponent(typeof(Rigidbody))]
public class DocumentOwnership : MonoBehaviour
{
    [Header("Current Status")]
    public DocumentOwner currentOwner = DocumentOwner.PlayerA;
    public bool isBeingHeld = false;

    [Header("Heat Build-up Settings")]
    [Tooltip("持球方每秒自然增加的 HEAT 值")]
    public float heatPerSecondWhileHolding = 2.5f;

    [Header("Desk Target References")]
    public Transform playerADeskPoint;
    public Transform playerBDeskPoint;
    public float passAnimationSpeed = 30f;

    [Header("Protection Settings")]
    [Tooltip("剛接到文件時的無敵/物理保護時間 (秒)")]
    public float passProtectionDuration = 0.15f;

    public bool isPassingAnimating { get; private set; } = false;
    private float passProtectionTimer = 0f;

    private PhysicsDraggable draggable;
    private Rigidbody rb;
    private bool hasGameStarted = false;

    public bool IsInProtectionWindow => passProtectionTimer > 0f;

    private void Awake()
    {
        draggable = GetComponent<PhysicsDraggable>();
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetTimePause(true);
        }
    }

    private void Update()
    {
        if (passProtectionTimer > 0f)
        {
            passProtectionTimer -= Time.deltaTime;
        }

        if (GameManager.Instance == null || GameManager.Instance.isGameOver) return;

        if (draggable != null && draggable.isDragging)
        {
            if (!hasGameStarted)
            {
                hasGameStarted = true;
                GameManager.Instance.SetTimePause(false);
            }
            isBeingHeld = true;
        }
        else
        {
            isBeingHeld = false;
        }

        // 持球方持續自然累積 HEAT
        if (hasGameStarted && !GameManager.Instance.isTimePaused && !isPassingAnimating)
        {
            AccumulateHoldingHeat();
        }
    }

    public void PassDocumentToOpponent()
    {
        currentOwner = DocumentOwner.PlayerB;
        StopDraggingIfActive();
        StartPassAnimation(playerBDeskPoint);
    }

    public void PassDocumentToPlayerA()
    {
        currentOwner = DocumentOwner.PlayerA;
        StopDraggingIfActive();
        StartPassAnimation(playerADeskPoint);
    }

    private void StartPassAnimation(Transform targetPoint)
    {
        if (targetPoint == null) return;
        passProtectionTimer = passProtectionDuration;
        StopAllCoroutines();
        StartCoroutine(AnimateMoveToPoint(targetPoint.position, targetPoint.rotation));
    }

    // ==================== 純角色座標計算假甩方向 (不調用桌子) ====================
    public void PlayBluffAnimation(Transform targetOpponent)
    {
        if (isPassingAnimating) return;
        StopAllCoroutines();
        StartCoroutine(AnimateBluffShake(targetOpponent));
    }

    private IEnumerator AnimateBluffShake(Transform targetOpponent)
    {
        Vector3 startPos = transform.position;
        Vector3 directionToOpponent = transform.forward;

        if (targetOpponent != null)
        {
            // 直接用 (對手角色位置 - 文件當前位置) 計算相對方向
            directionToOpponent = (targetOpponent.position - startPos);
            directionToOpponent.y = 0f; // 忽略高低差
            
            if (directionToOpponent.sqrMagnitude > 0.001f)
            {
                directionToOpponent.Normalize();
            }
        }

        Vector3 targetOffset = directionToOpponent * 0.5f; // 向對手角色方向假衝 0.5 單位

        float elapsed = 0f;
        float duration = 0.16f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float pingPong = Mathf.Sin((elapsed / duration) * Mathf.PI);
            transform.position = startPos + targetOffset * pingPong;
            yield return null;
        }

        transform.position = startPos;
    }

    private void StopDraggingIfActive()
    {
        if (draggable != null && draggable.isDragging)
        {
            bool originalReset = draggable.autoResetPosition;
            draggable.autoResetPosition = false;
            draggable.StopDragging();
            draggable.autoResetPosition = originalReset;
        }
    }

    private void AccumulateHoldingHeat()
    {
        int charID = (currentOwner == DocumentOwner.PlayerA) ? 0 : 1;
        GameManager.Instance.AddHeat(charID, heatPerSecondWhileHolding * Time.deltaTime);
    }

    private IEnumerator AnimateMoveToPoint(Vector3 targetPos, Quaternion targetRot)
    {
        isPassingAnimating = true;

        if (rb != null)
        {
            // 清空物理速度，消除 Unity 物理警告
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        float t = 0f;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        while (t < 1.0f)
        {
            t += Time.deltaTime * passAnimationSpeed;
            
            float smoothedT = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3);

            transform.position = Vector3.Lerp(startPos, targetPos, smoothedT);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, smoothedT);
            yield return null;
        }

        transform.position = targetPos;
        transform.rotation = targetRot;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.detectCollisions = true;
        }

        isPassingAnimating = false;
    }
}