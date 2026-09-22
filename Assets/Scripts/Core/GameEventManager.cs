using System;
using UnityEngine;

public static class GameEventManager
{
    // ==================== 1. 角色數值與狀態變更 ====================
    // 參數：(角色ID 0或1, 新Sanity值, 新Heat值)
    public static event Action<int, float, float> OnEmployeeStatsChanged;
    public static void TriggerStatsChanged(int employeeID, float sanity, float heat) 
        => OnEmployeeStatsChanged?.Invoke(employeeID, sanity, heat);

    // ==================== 2. 時間暫停/恢復事件 ====================
    public static event Action<bool> OnTimePauseStateChanged;
    public static void TriggerTimePauseStateChanged(bool isPaused) 
        => OnTimePauseStateChanged?.Invoke(isPaused);

    // ==================== 3. 神態與防衛報告事件 ====================
    public static event Action<int, ExpressionState> OnExpressionChanged;
    public static void TriggerExpressionChanged(int empID, ExpressionState newState) 
        => OnExpressionChanged?.Invoke(empID, newState);

    // 參數：(角色 ID: 0 或 1, 防守狀態: "Parry" / "Normal" / "End")
    public static event Action<int, string> OnDefenseStateReported;
    public static void TriggerDefenseStateReported(int empID, string state) 
        => OnDefenseStateReported?.Invoke(empID, state);

    // 假動作 (Bluff) 觸發事件：(角色 ID: 0 或 1)
    public static event Action<int> OnBluffTriggered;
    public static event Action<int> OnBluff; // ✅ 補上 OnBluff 宣告，解決 CS0117 錯誤

    public static void TriggerBluff(int empID) 
    {
        OnBluffTriggered?.Invoke(empID);
        OnBluff?.Invoke(empID); // ✅ 同時觸發 OnBluff 委派
    }

    // ==================== 4. 遊戲結局事件 ====================
    // (失敗者ID, 結局原因)
    public static event Action<int, string> OnGameOver;
    public static void TriggerGameOver(int loserID, string reason)
        => OnGameOver?.Invoke(loserID, reason);

    // ==================== 5. 對戰/甩鍋與 Parry 事件 ====================
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

    // 甩鍋動畫/Hitstop 觸發
    public static event Action<int> OnDocumentThrown;
    public static void TriggerDocumentThrown(int empID) 
        => OnDocumentThrown?.Invoke(empID);

    public static event Action<float, float> OnHitstopAndShake;
    public static void TriggerHitstopAndShake(float duration, float intensity) 
        => OnHitstopAndShake?.Invoke(duration, intensity);
}