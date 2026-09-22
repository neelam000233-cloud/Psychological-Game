using UnityEngine; // 加上這一行即可解決所有 CS0246 報錯！

public class VFXAndAudioManager : MonoBehaviour
{
    [Header("Audio Clips")]
    public AudioSource sfxSource;
    public AudioClip parrySFX;
    public AudioClip passSFX;
    public AudioClip deskSlamSFX;

    [Header("VFX Prefabs")]
    public GameObject parryVFXPrefab;
    public GameObject deskImpactVFXPrefab;

    private void OnEnable()
    {
        GameEventManager.OnParryTriggered += PlayParryFeedback;
        GameEventManager.OnDocumentPassAttempt += PlayPassFeedback;
    }

    private void OnDisable()
    {
        GameEventManager.OnParryTriggered -= PlayParryFeedback;
        GameEventManager.OnDocumentPassAttempt -= PlayPassFeedback;
    }

    private void PlayParryFeedback(int defenderID, int attackerID)
    {
        if (sfxSource != null && parrySFX != null)
        {
            sfxSource.PlayOneShot(parrySFX);
        }
    }

    private void PlayPassFeedback(int attackerID, bool isSuccess)
    {
        if (sfxSource == null) return;

        if (isSuccess && deskSlamSFX != null) 
            sfxSource.PlayOneShot(deskSlamSFX);
        else if (!isSuccess && passSFX != null) 
            sfxSource.PlayOneShot(passSFX);
    }
}