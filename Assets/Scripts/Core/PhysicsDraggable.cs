using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class PhysicsDraggable : MonoBehaviour
{
    [Header("Data Link")]
    public EmployeeStateSO targetState;

    [Header("Physics Drag Settings")]
    public float maxMoveSpeed = 25f;
    public float minMoveSpeed = 0.8f;

    [Header("Camera Control Settings")]
    public float cameraLookSensitivity = 0.5f;
    public float cameraSmoothSpeed = 5f;

    [Header("Reset Settings")]
    public bool autoResetPosition = true;

    private Rigidbody rb;
    private Camera mainCamera;
    public bool isDragging { get; private set; } = false;
    private float mZCoord;

    private Vector3 originalCamPos;
    private Vector3 initialMousePos;

    private Vector3 initialTransformPos;
    private Quaternion initialTransformRot;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            originalCamPos = mainCamera.transform.position;
        }

        initialTransformPos = transform.position;
        initialTransformRot = transform.rotation;
    }

    private void Update()
    {
        // 时间暂停时，镜头微调跟随冻结在当前位置
        if (GameManager.Instance != null && GameManager.Instance.isTimePaused) return;

        if (isDragging && mainCamera != null)
        {
            HandleCameraLook();
        }
    }

    private void OnMouseDown()
{
    // 游戏结束时禁止继续拖拽
    if (GameManager.Instance != null && GameManager.Instance.isGameOver) return;

    // 拦截 UI / 误触
    if (GameManager.Instance != null && GameManager.Instance.ignoreClickThisFrame)
    {
        return;
    }

    if (!isDragging) 
    {
        StartDragging();
    }
    else 
    {
        StopDragging();
    }
}

    private void StartDragging()
{
    isDragging = true;
    rb.useGravity = false;
    rb.linearVelocity = Vector3.zero;
    rb.angularVelocity = Vector3.zero;
    mZCoord = mainCamera.WorldToScreenPoint(transform.position).z;
    initialMousePos = Input.mousePosition;

    // 拿起物件：恢复倒计时流动
    if (GameManager.Instance != null)
    {
        GameManager.Instance.SetTimePause(false);
    }
}

public void StopDragging()
{
    
    isDragging = false;
    rb.useGravity = true;
    rb.linearDamping = 0f;

    if (autoResetPosition)
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.position = initialTransformPos;
        transform.rotation = initialTransformRot;
    }

    if (mainCamera != null)
    {
        mainCamera.transform.position = originalCamPos;
    }

    // 放下物件：暂停倒计时流动
    if (GameManager.Instance != null)
    {
        GameManager.Instance.SetTimePause(true);
    }
}

    private void FixedUpdate()
    {
        // 时间暂停时，固定物理速度不更新
        if (GameManager.Instance != null && GameManager.Instance.isTimePaused) return;

        if (isDragging && targetState != null)
        {
            Vector3 targetPos = GetMouseWorldPos();
            float resistance = targetState.GetCurrentResistance();
            
            float resistanceFactor = Mathf.Pow(1f - resistance, 2f); 
            float effectiveSpeed = Mathf.Lerp(minMoveSpeed, maxMoveSpeed, resistanceFactor);
            
            rb.linearDamping = Mathf.Lerp(0f, 15f, resistance);

            Vector3 moveDirection = (targetPos - transform.position);
            rb.linearVelocity = moveDirection * effectiveSpeed;
        }
    }

    private void HandleCameraLook()
    {
        Vector3 mouseDelta = Input.mousePosition - initialMousePos;
        float offsetX = (mouseDelta.x / Screen.width) * cameraLookSensitivity;
        float offsetY = (mouseDelta.y / Screen.height) * cameraLookSensitivity;

        Vector3 targetCamPos = originalCamPos + new Vector3(offsetX, offsetY, 0f);
        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetCamPos, Time.deltaTime * cameraSmoothSpeed);
    }

    private Vector3 GetMouseWorldPos()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = mZCoord;
        return mainCamera.ScreenToWorldPoint(mousePoint);
    }
}