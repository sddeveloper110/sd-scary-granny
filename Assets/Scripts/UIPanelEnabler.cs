using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Collections;

public class UIPanelEnabler : MonoBehaviour
{
    public static UIPanelEnabler Instance;

    [System.Serializable]
    public class PanelData
    {
        public PanelType type;
        public GameObject panelGO;
        public Button[] OpenButton;
        public Button[] CloseButton;
        public bool hideOthers = true;
        
        [Header("Options")]
        public bool showLoading = false;
        public float loadingDuration = 2f;
        public bool showBanner = false;
    }

    [Header("Panels Configuration")]
    [SerializeField] private List<PanelData> panels = new List<PanelData>();

    [Header("Loading UI")]
    [SerializeField] private TextMeshProUGUI loadingTxt;
    [SerializeField] private Image loadingBar;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Optional: keep across scenes
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        Init();
    }

    private void Init()
    {
        foreach (var p in panels)
        {
            if (p.panelGO == null) continue;

            foreach (var btn in p.OpenButton)
            {
                if (btn == null) continue;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OpenPanel(p.type));
            }

            foreach (var btn in p.CloseButton)
            {
                if (btn == null) continue;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => ClosePanel(p.type));
            }
        }
    }

    /// <summary>
    /// Opens a panel by type, handling loading and banners if configured.
    /// </summary>
    public static void OpenPanel(PanelType type)
    {
        if (Instance == null) return;

        PanelData data = Instance.panels.Find(x => x.type == type);
        if (data == null)
        {
            Debug.LogWarning($"[UIPanelEnabler] Panel {type} not found!");
            return;
        }

        if (data.showLoading)
        {
            Instance.StartCoroutine(Instance.Loading(data.loadingDuration, () => Instance.EnablePanelLogic(data)));
        }
        else
        {
            Instance.EnablePanelLogic(data);
        }
    }

    private void EnablePanelLogic(PanelData data)
    {
        if(SoundManager.Instance != null)
        SoundManager.PlayTapAudio();
        
        if (data.hideOthers)
        {
            foreach (var p in panels)
            {
                if (p != data && p.panelGO != null)
                    p.panelGO.SetActive(false);
            }
        }

        if (data.panelGO != null)
            data.panelGO.SetActive(true);

        if (data.showBanner)
        {
            // Placeholder for Ads system integration
            // Ads_Manager.Instance?.ShowMediumBanner();
            Debug.Log($"[UIPanelEnabler] Showing Medium Banner for {data.type}");
        }

        // Context-specific music (moved from CanvasManager)
        if (data.type == PanelType.MainMenu) SoundManager.Instance.PlayMenuMusic();
        else if (data.type == PanelType.Gameplay) SoundManager.Instance.PlayGameDefaultMusic();
    }

    /// <summary>
    /// Closes a panel by type and hides banner if configured.
    /// </summary>
    public static void ClosePanel(PanelType type)
    {
        if (Instance == null) return;

        PanelData data = Instance.panels.Find(x => x.type == type);
        if (data == null) return;

        SoundManager.PlayTapAudio();
        
        if (data.panelGO != null)
            data.panelGO.SetActive(false);

        if (data.showBanner)
        {
            // Placeholder for Ads system integration
            // Ads_Manager.Instance?.HideMediumBanner();
            Debug.Log($"[UIPanelEnabler] Hiding Medium Banner for {data.type}");
        }
    }
    
    /// <summary>
    /// Hides all panels in the configuration.
    /// </summary>
    public static void DisableAllPanels()
    {
        if (Instance == null) return;

        foreach (var p in Instance.panels)
        {
            if (p.panelGO != null)
                p.panelGO.SetActive(false);
        }
    }
    
    /// <summary>
    /// Checks if a specific panel is currently active.
    /// </summary>
    public static bool IsPanelActive(PanelType type)
    {
        if (Instance == null) return false;

        PanelData p = Instance.panels.Find(x => x.type == type);
        return p != null && p.panelGO != null && p.panelGO.activeSelf;
    }

    /// <summary>
    /// Global loading coroutine that shows the loading panel and fills the bar.
    /// </summary>
    public IEnumerator Loading(float duration, UnityAction action)
    {
        OpenPanel(PanelType.Loading);

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
                if (loadingTxt != null)
                    loadingTxt.text = $"Loading{new string('.', dotCount)}";
            }

            if (loadingBar != null)
                loadingBar.fillAmount = timer / duration;

            yield return null;
        }

        action?.Invoke();
    }
}
