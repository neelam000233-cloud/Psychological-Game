using System;
using UnityEngine;

public static class GameEventManager
{
    // 参数：(角色ID 0或1, 新Sanity值, 新Heat值)
    public static event Action<int, float, float> OnEmployeeStatsChanged;
    public static void TriggerStatsChanged(int employeeID, float sanity, float heat) 
        => OnEmployeeStatsChanged?.Invoke(employeeID, sanity, heat);

    // --- 时间暂停/恢复事件 ---
    public static event Action<bool> OnTimePauseStateChanged;
    public static void TriggerTimePauseStateChanged(bool isPaused) 
        => OnTimePauseStateChanged?.Invoke(isPaused);

    // --- 神态改变事件 ---
    public static event Action<int, ExpressionState> OnExpressionChanged;
    public static void TriggerExpressionChanged(int empID, ExpressionState newState) 
        => OnExpressionChanged?.Invoke(empID, newState);

    // === 【新增】游戏结局/结束事件 (失败者ID, 结局原因) ===
    public static event Action<int, string> OnGameOver;
    public static void TriggerGameOver(int loserID, string reason)
        => OnGameOver?.Invoke(loserID, reason);
}