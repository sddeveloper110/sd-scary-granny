using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;  
    [SerializeField] private AudioSource sfxSource;    

    [Header("Audio Clips")]
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip gameMusic;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (musicSource == null)
        {
            Debug.LogWarning("SoundManager: Music AudioSource is not assigned.");
        }

        if (sfxSource == null)
        {
            Debug.LogWarning("SoundManager: SFX AudioSource is not assigned.");
        }
    }

    private void Start()
    {
        // Start with menu music by default
        PlayMenuMusic();
    }

    #region Music Control

    public void PlayMenuMusic()
    {
        if (musicSource == null || menuMusic == null) return;

        musicSource.clip = menuMusic;
        musicSource.loop = true;
        musicSource.volume = 0.5f; // Adjust volume as needed
        musicSource.Play();
    }

    public void PlayGameMusic()
    {
        if (musicSource == null || gameMusic == null) return;

        musicSource.clip = gameMusic;
        musicSource.loop = true;
        musicSource.volume = 0.5f; // Adjust volume as needed
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource == null) return;
        musicSource.Stop();
    }

    public void SetMusicVolume(float volume)
    {
        if (musicSource != null) musicSource.volume = Mathf.Clamp01(volume);
    }

    #endregion

    #region SFX Control (Optional)

    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip, volume);
        }
    }

    #endregion
}
