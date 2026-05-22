using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static GameManager;

public class CanvasManager : MonoBehaviour
{
    public static CanvasManager Instance;
    
    private void Awake()
    {
        Instance = this;
        if (popupTxt != null)
        {
            popupTxt.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            // Clear static events when the manager is destroyed (scene transition)
            OnGameStart = null;
            OnGameExit = null;
            OnGameRetry = null;
            OnGameRevive = null;
            OnSensetivityChange = null;
        }
    }

    
    

    [Header("UI")]
    [SerializeField] Image fadeScreen;

    [Header("Volume UI")]
    [SerializeField] Slider[] soundVol;
    [SerializeField] Slider[] musicVol;
    [SerializeField] Slider sensetivitySlider;

    [SerializeField] TextMeshProUGUI popupTxt;
    
    [Header("Button")]
    [SerializeField] Button[] exitGameplayBtn;
    [SerializeField] Button[] retryBtn;
    [SerializeField] Button[] reviveBtn;
    [SerializeField] Button startNewGameBtn;
    [SerializeField] Button pauseBtn;
    [SerializeField] Button resumeBtn;

  
    [Header("Avatar & Flag Integration")]
    [SerializeField] private TextMeshProUGUI playerNameDisplay;
    [SerializeField] private TextMeshProUGUI playerAgeDisplay;

    [Header("UI Containers")]
    [SerializeField] private Image avatarContainer;
    [SerializeField] private Image flagContainer;
    [SerializeField] private List<Sprite> avatarSprites;
    [SerializeField] private List<Sprite> flagSprites;

    [HideInInspector] public PanelType lastActivePanel;

    private Coroutine popupCoroutine;
    public static event Action OnGameStart;
    public static event Action OnGameExit;
    public static event Action OnGameRetry;
    public static event Action OnGameRevive;
    public static event Action<float> OnSensetivityChange;

    void Start()
    {
        Init();
    }



    public void PauseGame()
    {
        Time.timeScale = 0f;
        UIPanelEnabler.OpenPanel(PanelType.Pause);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        UIPanelEnabler.ClosePanel(PanelType.Pause);

        // Never lock cursor on mobile — locked cursor freezes EventData.position
        // causing the joystick knob to snap to first touch and never update during drag.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;

        // The Resume button click leaves a stale pointer in the EventSystem.
        // Clear it one frame later so the joystick receives clean drag events.
        StartCoroutine(ClearEventSystemSelection());
    }

    private IEnumerator ClearEventSystemSelection()
    {
        yield return null; // wait one frame for button click to fully process
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        if (GameManager.Instance != null)
            GameManager.Instance.NewGame();
    }

    #region UI Setup
    void Init()
    {
        currentActivePopupOnButton = null;
        //StartCoroutine(SplashSequence());

        //nextLevelBtn.onClick.AddListener(LoadNextLevel);
        //SwithControlBtn.onClick.AddListener(SwitchController);
        for(int i = 0; i < retryBtn.Length;i++)
            retryBtn[i].onClick.AddListener(Retry);
   
        for(int i = 0; i < reviveBtn.Length;i++)
            reviveBtn[i].onClick.AddListener(Revive);

        for(int i = 0; i < exitGameplayBtn.Length; i++)
        {
            exitGameplayBtn[i].onClick.AddListener(() => { SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); });
        }

        if (startNewGameBtn != null) startNewGameBtn.onClick.AddListener(OnStartButtonPress);
        if (pauseBtn != null) pauseBtn.onClick.AddListener(PauseGame);
        if (resumeBtn != null) resumeBtn.onClick.AddListener(ResumeGame);

        sensetivitySlider.value = PlayerPrefs.GetFloat("Sensitivity", 1f);

        OnSensetivityChange?.Invoke(PlayerPrefs.GetFloat("Sensitivity",3f));
        sensetivitySlider.onValueChanged.AddListener((value) =>
        {
            PlayerPrefs.SetFloat("Sensitivity", value);
            OnSensetivityChange?.Invoke(value);
        });

        // ✅ Sliders auto-update
        for (int i = 0; i < soundVol.Length; i++)
        {
            soundVol[i].value = SoundManager.SoundVol;
            soundVol[i].onValueChanged.AddListener(SoundManager.SetSoundVolume);
        }

        for (int i = 0; i < musicVol.Length; i++)
        {
            musicVol[i].value = SoundManager.MusicVol;
            musicVol[i].onValueChanged.AddListener(SoundManager.SetMusicVolume);
        }

        UpdateAvatarAndFlagIntegration();
        LoadToMainMenu();
    }

    private void UpdateAvatarAndFlagIntegration()
    {
        int selectedAvatar = PlayerPrefs.GetInt("SelectedAvatar", 0);
        int selectedFlag = PlayerPrefs.GetInt("SelectedFlag", 0);

        // Update player name display
        if (playerNameDisplay != null)
            playerNameDisplay.text = PlayerPrefs.GetString("PlayerName", "Player");

        // Update player age display
        if (playerAgeDisplay != null)
            playerAgeDisplay.text = PlayerPrefs.GetString("PlayerAge", "18");

        // Update UI containers with selected sprites
        if (avatarContainer != null && avatarSprites.Count > selectedAvatar)
            avatarContainer.sprite = avatarSprites[selectedAvatar];

        if (flagContainer != null && flagSprites.Count > selectedFlag)
            flagContainer.sprite = flagSprites[selectedFlag];
    }

    #endregion


    #region Splash + Loading

    public static void LoadToGameplay()
    {
            UIPanelEnabler.OpenPanel(PanelType.Gameplay);
            if (PlayerPrefs.GetInt("FirstTimePlay", 0) == 0)
            {
                UIPanelEnabler.OpenPanel(PanelType.PrivacyPolicy);
                PlayerPrefs.SetInt("FirstTimePlay", 1);
            }
    }

    void Retry()
    {
        OnGameRetry?.Invoke();
        StartCoroutine(UIPanelEnabler.Instance.Loading(4, () => UIPanelEnabler.OpenPanel(PanelType.Gameplay)));
    }

    void Revive()
    {
        OnGameRevive?.Invoke();
        StartCoroutine(UIPanelEnabler.Instance.Loading(4, () => UIPanelEnabler.OpenPanel(PanelType.Gameplay)));
    }

    public static void LoadToMainMenu()
    {
        OnGameExit?.Invoke();
            UIPanelEnabler.OpenPanel(PanelType.MainMenu);
    }


    IEnumerator SplashSequence()
    {
        UIPanelEnabler.OpenPanel(PanelType.Splash);

        yield return new WaitForSeconds(6f);

        //LoadToMainMenu();
    }

    #endregion


    public void OnStartButtonPress()
    {
        Debug.Log("[CanvasManager] OnStartButtonPress clicked! Calling GameManager.NewGame()");
        GameManager.Instance.NewGame();
    }

    public void OpenUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            Debug.LogError("URL is empty");
            return;
        }

        Debug.Log("Opening URL: " + url);
        Application.OpenURL(url);
    }

    public static void FadeIn(float duration, UnityAction ua)
    {
        Instance.StartCoroutine(Instance.FadeSequence(duration, ua));
    }

    private IEnumerator FadeSequence(float duration, UnityAction ua)
    {
        // Fade in (transparency to 1)
        fadeScreen.gameObject.SetActive(true);
        fadeScreen.color = new Color(0, 0, 0, 0);

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(0, 1, timer / duration);
            fadeScreen.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        fadeScreen.color = new Color(0, 0, 0, 1);

        // Perform action
        ua?.Invoke();

        // Fade out quickly (1.2 seconds)
        timer = 0f;
        float fadeOutDuration = 2f;

        while (timer < fadeOutDuration)
        {
            timer += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(1, 0, timer / fadeOutDuration);
            fadeScreen.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        fadeScreen.color = new Color(0, 0, 0, 0);
        fadeScreen.gameObject.SetActive(false);
    }


    public static void FadeReverse(float duration, UnityAction ua)
    {
        Instance.StartCoroutine(Instance.FadeReverseSequence(duration, ua));
    }

    private IEnumerator FadeReverseSequence(float duration, UnityAction ua)
    {
        // 🟦 1 — Instantly black
        fadeScreen.gameObject.SetActive(true);
        fadeScreen.color = new Color(0, 0, 0, 1);

        // 🟦 2 — Wait 2 seconds
        yield return new WaitForSecondsRealtime(3f);

        // 🟦 3 — Do your action
        ua?.Invoke();

        // 🟦 4 — Now fade-out (1 → 0)
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(1, 0, timer / duration);
            fadeScreen.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        // 🟦 RESET
        fadeScreen.color = new Color(0, 0, 0, 0);
        fadeScreen.gameObject.SetActive(false);
    }
    public static void ShowPopup(string msg)
    {
        if (Instance.popupTxt == null) return;

        GameObject clone = Instantiate(Instance.popupTxt.gameObject, Instance.popupTxt.transform.parent);
        clone.SetActive(true);

        TextMeshProUGUI cloneText = clone.GetComponent<TextMeshProUGUI>();
        cloneText.text = msg;

        Instance.StartCoroutine(PopupRoutine(clone));
    }

    private static IEnumerator PopupRoutine(GameObject clone)
    {
        if (clone == null) yield break;
        Transform popPanel = clone.transform;

        popPanel.transform.localScale = Vector3.zero;

        // POP IN (0 → 1)
        float t = 0f;
        while (t < 1f)
        {
            if (popPanel == null) yield break;

            t += Time.unscaledDeltaTime * 6f;
            float s = Mathf.Lerp(0f, 1f, t);
            popPanel.transform.localScale = Vector3.one * s;
            yield return null;
        }

        popPanel.transform.localScale = Vector3.one;

        // WAIT 3 seconds
        yield return new WaitForSecondsRealtime(3f);

        // POP OUT (1 → 0) and slide down slightly over 2 seconds
        Vector3 startPos = popPanel.localPosition;
        Vector3 targetPos = startPos + Vector3.down * 50f; // Shift down by 50 units
        float popOutDuration = 2f;
        float elapsed = 0f;
        while (elapsed < popOutDuration)
        {
            if (popPanel == null) yield break;

            elapsed += Time.unscaledDeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / popOutDuration);
            float s = Mathf.Lerp(1f, 0f, normalizedTime);
            popPanel.transform.localScale = Vector3.one * s;
            popPanel.localPosition = Vector3.Lerp(startPos, targetPos, normalizedTime);
            yield return null;
        }

        Destroy(clone);
    }

    public void ShowPopupOnButton(Button button)
    {
        if (button == null) return;
        ShowPopupOnButton(button.transform, "Coming Soon!");
    }

    private static GameObject currentActivePopupOnButton;

    public static void ShowPopupOnButton(Transform buttonTransform, string msg)
    {
        if (Instance == null || Instance.popupTxt == null || buttonTransform == null) return;

        if (currentActivePopupOnButton != null)
        {
            Destroy(currentActivePopupOnButton);
        }

        GameObject clone = Instantiate(Instance.popupTxt.gameObject, Instance.popupTxt.transform.parent);
        clone.SetActive(true);
        currentActivePopupOnButton = clone;

        // Position the clone at the button's position
        clone.transform.position = buttonTransform.position;

        TextMeshProUGUI cloneText = clone.GetComponent<TextMeshProUGUI>();
        if (cloneText != null)
        {
            cloneText.text = msg;
        }

        Instance.StartCoroutine(PopupOnButtonRoutine(clone));
    }

    private static IEnumerator PopupOnButtonRoutine(GameObject clone)
    {
        if (clone == null) yield break;
        Transform popPanel = clone.transform;

        // Set to full scale instantly
        popPanel.localScale = Vector3.one;

        Vector3 startPos = popPanel.localPosition;
        // Go up by 80 units
        Vector3 targetPos = startPos + Vector3.up * 80f;

        float duration = 2f;
        float elapsed = 0f;

        TextMeshProUGUI cloneText = clone.GetComponent<TextMeshProUGUI>();
        Color startColor = cloneText != null ? cloneText.color : Color.white;

        while (elapsed < duration)
        {
            if (popPanel == null) yield break;

            elapsed += Time.unscaledDeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / duration);

            // Move up
            popPanel.localPosition = Vector3.Lerp(startPos, targetPos, normalizedTime);

            // In the last 0.5s (normalizedTime > 0.75f), vanish (scale down and fade text out)
            if (normalizedTime > 0.75f)
            {
                float fadeT = (normalizedTime - 0.75f) / 0.25f;
                float s = Mathf.Lerp(1f, 0f, fadeT);
                popPanel.localScale = Vector3.one * s;

                if (cloneText != null)
                {
                    cloneText.color = new Color(startColor.r, startColor.g, startColor.b, 1f - fadeT);
                }
            }
            else
            {
                popPanel.localScale = Vector3.one;
                if (cloneText != null)
                {
                    cloneText.color = startColor;
                }
            }

            yield return null;
        }

        if (currentActivePopupOnButton == clone)
        {
            currentActivePopupOnButton = null;
        }
        Destroy(clone);
    }
}

