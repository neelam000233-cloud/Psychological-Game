using UnityEngine;
using TMPro;

public class GameTimerUI : MonoBehaviour
{
    public TMP_Text timerText;

    private void Awake()
    {
        if (timerText == null)
        {
            timerText = GetComponent<TMP_Text>();
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && timerText != null)
        {
            timerText.text = GameManager.Instance.GetFormattedRemainingTime();
        }
    }
}