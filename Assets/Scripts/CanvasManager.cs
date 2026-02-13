using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using static GameManager;
using System;

public class CanvasManager : MonoBehaviour
{
    public static CanvasManager Instance;
    
    private void Awake()
    {
        Instance = this;
    }

    
    [System.Serializable]
    public class Panel
    {
        public PanelType type;
        public Button[] OpenButton;
        public Button[] CloseButton;
        public GameObject panelGO;
        public bool hideOthers = true;  // ✅ new toggle
    }
    

    [Header("Panels")]
    [SerializeField] List<Panel> panels = new List<Panel>();
    [SerializeField] Image fadeScreen;
    public GameObject privacyPolicy;

    [Header("Volume UI")]
    [SerializeField] Slider[] soundVol;
    [SerializeField] Slider[] musicVol;

    [Header("Text")]
    [SerializeField] TextMeshProUGUI loadingTxt;
    [SerializeField] Image loadingBar;
    [SerializeField] TextMeshProUGUI popupTxt;

    [Header("Button")]
    [SerializeField] Button[] exitGameplayBtn;
    [SerializeField] Button retryBtn;
    [SerializeField] Button nextLevelBtn;

  
    [HideInInspector] public PanelType lastActivePanel;

    private Coroutine popupCoroutine;
    public static event Action OnGameStart;
    public static event Action OnGameExit;
    public static event Action OnGameRetry;

    void Start()
    {
        Init();
    }

    #region UI Setup
    void Init()
    {
        //StartCoroutine(SplashSequence());

        //nextLevelBtn.onClick.AddListener(LoadNextLevel);
        //SwithControlBtn.onClick.AddListener(SwitchController);
        retryBtn.onClick.AddListener(Retry);
   

        for(int i = 0; i < exitGameplayBtn.Length; i++)
        {
            exitGameplayBtn[i].onClick.AddListener(LoadToMainMenu);
        }

       

        // ✅ Sliders auto-update
        for (int i = 0; i < soundVol.Length; i++)
        {
            //soundVol[i].value = AudioManager.SoundVol;
            //soundVol[i].onValueChanged.AddListener(AudioManager.SetSound);
        }

        for (int i = 0; i < musicVol.Length; i++)
        {
            //musicVol[i].value = AudioManager.MusicVol;
            //musicVol[i].onValueChanged.AddListener(AudioManager.SetMusic);
        }

        // ✅ Auto assign open/close buttons
        foreach (var p in panels)
        {
            foreach (var btn in p.OpenButton)
            {
                btn.onClick.AddListener(() =>
                {
                    //AudioManager.PlayTap();
                    EnablePanel(p.type);
                });
            }

            foreach (var btn in p.CloseButton)
            {
                btn.onClick.AddListener(() =>
                {
                    //AudioManager.PlayTap();
                    p.panelGO.SetActive(false);
                });
            }
        }
        LoadToMainMenu();
    }
    #endregion

    #region Panel Control
    public static void EnablePanel(PanelType type)
    {
        Panel openedPanel = Instance.panels.Find(p => p.type == type);

        if (openedPanel == null)
        {
            Debug.LogError("Panel not found: " + type);
            return;
        }

        if (openedPanel.panelGO.activeSelf)
            return;

        openedPanel.panelGO.SetActive(true);

        if (openedPanel.hideOthers)
        {
            foreach (var p in Instance.panels)
            {
                if (p != openedPanel && p.panelGO != null)
                    p.panelGO.SetActive(false);
            }
        }

    }

    public static void DisableAllPanel()
    {
        foreach (var p in Instance.panels)
        {
            if (p.panelGO.activeSelf)
            {
                Instance.lastActivePanel = p.type;
                break;
            }
        }

        foreach (var p in Instance.panels)
        {
                p.panelGO.SetActive(false);
        }
    }

    public static bool IsPanelActive(PanelType type)
    {
        Panel p = Instance.panels.Find(x => x.type == type);
        return p != null && p.panelGO.activeSelf;
    }



    #endregion

    #region Splash + Loading

    public static void LoadToGameplay()
    {
        Instance.StartCoroutine(Instance.Loading(4, () =>
        {
            EnablePanel(PanelType.Gameplay);
            if (PlayerPrefs.GetInt("FirstTimePlay", 0) == 0)
            {
                EnablePanel(PanelType.PrivacyPolicy);
                PlayerPrefs.SetInt("FirstTimePlay", 1);
            }
        }));
    }

    void Retry()
    {
        OnGameRetry?.Invoke();
        StartCoroutine(Loading(4, () => EnablePanel(PanelType.Gameplay)));
    }

    public static void LoadToMainMenu()
    {
        OnGameExit?.Invoke();
        Instance.StartCoroutine(Instance.Loading(3, () =>
        {
            EnablePanel(PanelType.MainMenu);
        }));
    }


    IEnumerator SplashSequence()
    {
        EnablePanel(PanelType.Splash);

        yield return new WaitForSeconds(6f);

        //LoadToMainMenu();
    }
    public IEnumerator Loading(float duration, UnityAction action)
    {
        EnablePanel(PanelType.Loading);

        float timer = 0f;
        float dotTimer = 0f;
        int dotCount = 0;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            dotTimer += Time.deltaTime;

            if (dotTimer >= 0.5f)
            {
                dotTimer = 0f;
                dotCount = (dotCount + 1) % 4;
                loadingTxt.text = $"Loading{new string('.', dotCount)}";
            }

            loadingBar.fillAmount = timer / duration;

            yield return null;
        }

        action?.Invoke();
    }

    #endregion

 


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
            timer += Time.deltaTime;
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
            timer += Time.deltaTime;
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
        yield return new WaitForSeconds(3f);

        // 🟦 3 — Do your action
        ua?.Invoke();

        // 🟦 4 — Now fade-out (1 → 0)
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
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
        Instance.popupTxt.text = msg;
        //Instance.popupTxt.transform.localScale = Vector3.zero;

        Instance.popupTxt.StartCoroutine(PopupRoutine());
    }

    private static IEnumerator PopupRoutine()
    {
        Transform popPanel = Instance.popupTxt.transform.parent;
        if (popPanel == null) yield break;

        popPanel.transform.localScale = Vector3.zero;

        // POP IN (0 → 1)
        float t = 0f;
        while (t < 1f)
        {
            if (popPanel == null) yield break;

            t += Time.deltaTime * 6f;
            float s = Mathf.Lerp(0f, 1f, t);
            popPanel.transform.localScale = Vector3.one * s;
            yield return null;
        }

        popPanel.transform.localScale = Vector3.one;

        // WAIT
        yield return new WaitForSeconds(4f);

        // POP OUT (1 → 0)
        t = 0f;
        while (t < 1f)
        {
            if (popPanel == null) yield break;

            t += Time.deltaTime * 6f;
            float s = Mathf.Lerp(1f, 0f, t);
            popPanel.transform.localScale = Vector3.one * s;
            yield return null;
        }

        popPanel.transform.localScale = Vector3.zero;
    }

}


public enum PanelType
{
    Splash,
    Loading,
    MainMenu,
    Settings,
    Gameplay,
    Pause,
    LevelComplete,
    Hint,
    PrivacyPolicy,
    RateUs
}