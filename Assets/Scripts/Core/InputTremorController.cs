using UnityEngine;

public class InputTremorController : MonoBehaviour
{
    [Header("Target Data")]
    public EmployeeStateSO targetState;

    [Header("Tremor Settings")]
    public float noiseSpeed = 5f;      // 噪声变化频率
    public float maxOffset = 15f;      // 最大像素偏移量

    private float noiseSeedX;
    private float noiseSeedY;

    private void Start()
    {
        // 随机生成噪声种子，避免固定轨迹
        noiseSeedX = Random.Range(0f, 100f);
        noiseSeedY = Random.Range(100f, 200f);
    }

    // 获取当前受手抖影响后的鼠标偏移向量
    public Vector2 GetTremorOffset()
    {
        if (targetState == null) return Vector2.zero;

        // 从 ScriptableObject 获取基于 Sanity 计算的幅值 (0 到 1)
        float amplitude = targetState.GetCurrentTremorAmplitude();

        if (amplitude <= 0.01f) return Vector2.zero;

        // 计算 Perlin Noise (映射到 -1 到 1 的范围)
        float time = Time.time * noiseSpeed;
        float offsetX = (Mathf.PerlinNoise(noiseSeedX + time, 0f) - 0.5f) * 2f;
        float offsetY = (Mathf.PerlinNoise(0f, noiseSeedY + time) - 0.5f) * 2f;

        // 输出加权后的偏移像素值
        return new Vector2(offsetX, offsetY) * (amplitude * maxOffset);
    }
}