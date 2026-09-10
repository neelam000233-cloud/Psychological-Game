using System;
using UnityEngine;

public static class GameEventManager
{
    // 参数：(角色ID 0或1, 新Sanity值, 新Heat值)
    public static event Action<int, float, float> OnEmployeeStatsChanged;
    public static void TriggerStatsChanged(int employeeID, float sanity, float heat) 
        => OnEmployeeStatsChanged?.Invoke(employeeID, sanity, heat);

    // --- 新增：时间暂停/恢复事件 (Day 4 细节阅读模式核心) ---
    public static event Action<bool> OnTimePauseStateChanged;
    public static void TriggerTimePauseStateChanged(bool isPaused) 
        => OnTimePauseStateChanged?.Invoke(isPaused);

    // --- 新增：神态改变事件 (Day 3 表情博弈) ---
    public static event Action<int, ExpressionState> OnExpressionChanged;
    public static void TriggerExpressionChanged(int empID, ExpressionState newState) 
        => OnExpressionChanged?.Invoke(empID, newState);
}