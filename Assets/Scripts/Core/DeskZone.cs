using UnityEngine;

public enum ZoneOwner
{
    PlayerA,
    PlayerB
}

public class DeskZone : MonoBehaviour
{
    public ZoneOwner zoneOwner;

    private void OnTriggerEnter(Collider other)
    {
        // 保持空逻辑或仅做碰撞检测，不干预文件甩锅/动画
    }

    private void OnTriggerExit(Collider other)
    {
        // 保持空逻辑
    }
}