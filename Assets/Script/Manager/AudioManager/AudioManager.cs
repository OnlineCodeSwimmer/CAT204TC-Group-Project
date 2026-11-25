using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    [HideInInspector] public static AudioManager instance;

    public void Awake()
    {
        instance = this;
    }

    public void PlaySFX(AudioSource source, AudioClip clip)
    {
        source.PlayOneShot(clip);
    }

    public void PlayLoopSound(AudioSource source)
    {
        source.Play();
    }

    public void StopLoopSound(AudioSource source)
    {
        source.Stop();
    }

}
