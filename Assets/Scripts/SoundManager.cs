using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Audio Clip Arrays (Random Selection)")]
    [SerializeField] private AudioClip[] menuMusicTracks;
    [SerializeField] private AudioClip[] gameMusicTracks;
    [SerializeField] private AudioClip[] horrorMusicTracks;

    [Header("Single SFX")]
    public AudioClip slapSound;
    public AudioClip tapSound;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
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

    #region Music Control

    public void PlayMenuMusic()
    {
        PlayRandomFromList(menuMusicTracks);
    }

    public void PlayGameDefaultMusic()
    {
        PlayRandomFromList(gameMusicTracks);
    }

    public void PlayGameGrannyMusic()
    {
        PlayRandomFromList(horrorMusicTracks);
    }

    /// <summary>
    /// Picks a random clip from the provided array and plays it.
    /// </summary>
    private void PlayRandomFromList(AudioClip[] clips)
    {
        if (musicSource == null || clips == null || clips.Length == 0) return;

        // Pick a random index
        int randomIndex = Random.Range(0, clips.Length);
        AudioClip selectedClip = clips[randomIndex];

        // Only switch if it's a different clip (prevents restarting same song)
        if (musicSource.clip == selectedClip && musicSource.isPlaying) return;

        UpdateMusicSettings(selectedClip);
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

    public static void SetMusicVolume(float volume)
    {
        if (Instance.musicSource != null) Instance.musicSource.volume = Mathf.Clamp01(volume);
        MusicVol = volume;
    }
    public static float MusicVol
    {
        get => PlayerPrefs.GetFloat(nameof(MusicVol), .2f);
        set => PlayerPrefs.SetFloat(nameof(MusicVol), value);
    }
    #endregion

    #region Sound (SFX) Control 
    public static void PlayThisAudio(AudioSource audioSource, AudioClip audioClip)
    {
        if (audioSource == null || audioClip == null) return;
        audioSource.volume = SoundVol;
        audioSource.PlayOneShot(audioClip);
    }

    public static void PlayThisAudio(AudioClip audioClip)
    {
        if (Instance.sfxSource == null || audioClip == null) return;
        Instance.sfxSource.PlayOneShot(audioClip, SoundVol);
    }

    public static void PlayTapAudio()
    {
        PlayThisAudio(Instance.tapSound);
    }

    public static void SetSoundVolume(float volume)
    {
        if (Instance.sfxSource != null) Instance.sfxSource.volume = volume;
        SoundVol = volume;
    }

    public static float SoundVol
    {
        get => PlayerPrefs.GetFloat(nameof(SoundVol), .5f);
        set => PlayerPrefs.SetFloat(nameof(SoundVol), value);
    }
    #endregion
}