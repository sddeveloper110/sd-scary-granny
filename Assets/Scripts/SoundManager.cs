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
    [SerializeField] private AudioClip horrosMusic;
    [SerializeField] public AudioClip slapSound; // Slap ke liye sound yahan assign karein

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: Scene change pe sound na ruke
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        PlayMenuMusic();
    }

    // --- SFX / Sound Playback ---



    #region Music Control

    public void PlayMenuMusic()
    {
        if (musicSource == null || menuMusic == null) return;
        UpdateMusicSettings(menuMusic);
    }

    public void PlayGameDefaultMusic()
    {
        if (musicSource == null || gameMusic == null) return;
        UpdateMusicSettings(gameMusic);
    }

    public void PlayGameGrannyMusic()
    {
        if (musicSource == null || horrosMusic == null) return;
        UpdateMusicSettings(horrosMusic);
    }

    private void UpdateMusicSettings(AudioClip clip)
    {
        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.volume = MusicVol;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null) musicSource.Stop();
    }

    // Properties for Music
    public void SetMusicVolume(float volume)
    {
        if (musicSource != null) musicSource.volume = Mathf.Clamp01(volume);
        MusicVol = volume;
    }

    public static float MusicVol
    {
        get => PlayerPrefs.GetFloat(nameof(MusicVol), .5f);
        set => PlayerPrefs.SetFloat(nameof(MusicVol), value);
    }
    #endregion

    #region Sound (SFX) Control 
    public static void PlayThisAudio(AudioSource audioSource, AudioClip audioClip)
    {
        if (audioSource == null || audioClip == null) return;
        audioSource.volume = SoundVol; // SoundVol property use ho rahi hai
        audioSource.PlayOneShot(audioClip);
    }

    public static void PlayThisAudio(AudioClip audioClip)
    {
        if (Instance.sfxSource == null || audioClip == null) return;
        Instance.sfxSource.PlayOneShot(audioClip, SoundVol);
    }
    public void SetSoundVolume(float volume)
    {
        if (sfxSource != null)
        {
            sfxSource.volume = volume;
        }
        SoundVol = volume;
    }

    public static float SoundVol
    {
        get => PlayerPrefs.GetFloat(nameof(SoundVol), .5f);
        set => PlayerPrefs.SetFloat(nameof(SoundVol), value);
    }

    #endregion
}