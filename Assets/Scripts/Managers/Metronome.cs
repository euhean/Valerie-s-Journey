using UnityEngine;

/// <summary>
/// Listens to the TimeManager’s OnBeat event and plays a click (or logs the beat).
/// Attach this to any object in the scene and assign its TimeManager reference.
/// Optionally provide an AudioSource and AudioClip for the beat sound.
/// </summary>
public class Metronome : MonoBehaviour
{
    public TimeManager timeManager;
    public AudioSource audioSource;
    public AudioClip beatClip;

    private void OnEnable()
    {
        if (timeManager != null)
            timeManager.OnBeat += HandleBeat;
    }

    private void OnDisable()
    {
        if (timeManager != null)
            timeManager.OnBeat -= HandleBeat;
    }

    private void HandleBeat(int beatIndex)
    {
        if (audioSource != null && beatClip != null) audioSource.PlayOneShot(beatClip);
        else Debug.Log($"Beat {beatIndex}");
    }
}