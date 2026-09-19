using System;
using UnityEngine;

public static class GameEventManager
{
    // 參數：(角色ID 0或1, 新Sanity值, 新Heat值)
    public static event Action<int, float, float> OnEmployeeStatsChanged;
    public static void TriggerStatsChanged(int employeeID, float sanity, float heat) 
        => OnEmployeeStatsChanged?.Invoke(employeeID, sanity, heat);

    // --- 時間暫停/恢復事件 ---
    public static event Action<bool> OnTimePauseStateChanged;
    public static void TriggerTimePauseStateChanged(bool isPaused) 
        => OnTimePauseStateChanged?.Invoke(isPaused);

    // --- 神態改變事件 ---
    public static event Action<int, ExpressionState> OnExpressionChanged;
    public static void TriggerExpressionChanged(int empID, ExpressionState newState) 
        => OnExpressionChanged?.Invoke(empID, newState);

    // === 遊戲結局/結束事件 (失敗者ID, 結局原因) ===
    public static event Action<int, string> OnGameOver;
    public static void TriggerGameOver(int loserID, string reason)
        => OnGameOver?.Invoke(loserID, reason);

    // === 【解耦廣播事件】 ===
    // 甩鍋觸發 (發起者ID, 是否成功命中)
    public static event Action<int, bool> OnDocumentPassAttempt;
    public static void TriggerDocumentPassAttempt(int attackerID, bool isHit) 
        => OnDocumentPassAttempt?.Invoke(attackerID, isHit);

    // Parry 完美格檔成功 (防守者ID, 甩鍋者ID)
    public static event Action<int, int> OnParryTriggered;
    public static void TriggerParry(int defenderID, int attackerID) 
        => OnParryTriggered?.Invoke(defenderID, attackerID);

    // 防守執行事件 (角色ID)
    public static event Action<int> OnDefenseExecuted;
    public static void TriggerDefenseExecuted(int empID) 
        => OnDefenseExecuted?.Invoke(empID);
}