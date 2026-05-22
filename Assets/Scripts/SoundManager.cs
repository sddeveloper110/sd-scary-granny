using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Audio Clip Arrays (Random Selection)")]
    [SerializeField] private AudioClip[] menuMusicTracks;
    [SerializeField] private AudioClip[] horrorMusicTracks;
    [SerializeField] private AudioClip[] suspenseMusicTracks;

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

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        PlayMenuMusic();
    }

    #region Music Control

    public void PlayMenuMusic()
    {
        PlayRandomFromList(menuMusicTracks, true);
    }

    public void PlayNothing()
    {
        if (musicSource != null)
        {
            Debug.Log($"[SoundManager] PlayNothing() called. Stopping musicSource (was playing: '{(musicSource.clip != null ? musicSource.clip.name : "null")}', isPlaying: {musicSource.isPlaying})");
            musicSource.Stop();
        }
        else
        {
            Debug.LogWarning("[SoundManager] PlayNothing() called but musicSource is null!");
        }
    }

    public void PlayGameGrannyMusic()
    {
        Debug.Log("[SoundManager] PlayGameGrannyMusic() called.");
        PlayRandomFromList(horrorMusicTracks, false);
    }

    public void PlaySuspenseMusic()
    {
        Debug.Log("[SoundManager] PlaySuspenseMusic() called.");
        PlayRandomFromList(suspenseMusicTracks, true);
    }

    /// <summary>
    /// Picks a random clip from the provided array and plays it.
    /// </summary>
    private void PlayRandomFromList(AudioClip[] clips, bool isLoop)
    {
        if (musicSource == null)
        {
            Debug.LogWarning("[SoundManager] musicSource is null! Cannot play music.");
            return;
        }
        if (clips == null || clips.Length == 0)
        {
            Debug.LogWarning("[SoundManager] clips array is null or empty! Cannot play music.");
            return;
        }

        // Pick a random index
        int randomIndex = Random.Range(0, clips.Length);
        AudioClip selectedClip = clips[randomIndex];

        // Only switch if it's a different clip (prevents restarting same song)
        if (musicSource.clip == selectedClip && musicSource.isPlaying && musicSource.loop == isLoop)
        {
            Debug.Log($"[SoundManager] Already playing '{selectedClip.name}' on '{musicSource.name}' (Loop: {isLoop}, Vol: {musicSource.volume}). Skipping update.");
            return;
        }

        Debug.Log($"[SoundManager] Selecting random track '{selectedClip.name}' (Loop: {isLoop}) from array of size {clips.Length}.");
        UpdateMusicSettings(selectedClip, isLoop);
    }

    private void UpdateMusicSettings(AudioClip clip, bool isLoop)
    {
        if (clip == null)
        {
            Debug.LogWarning("[SoundManager] Attempted to UpdateMusicSettings with a null clip!");
            return;
        }
        musicSource.clip = clip;
        musicSource.loop = isLoop;
        musicSource.volume = MusicVol;
        musicSource.Play();
        Debug.Log($"[SoundManager] Started playing clip '{clip.name}' on AudioSource '{musicSource.name}'. Loop={isLoop}, Vol={musicSource.volume}, IsPlaying={musicSource.isPlaying}");
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
        get => PlayerPrefs.GetFloat(nameof(MusicVol), .1f);
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
        get => PlayerPrefs.GetFloat(nameof(SoundVol), 1);
        set => PlayerPrefs.SetFloat(nameof(SoundVol), value);
    }
    #endregion
}