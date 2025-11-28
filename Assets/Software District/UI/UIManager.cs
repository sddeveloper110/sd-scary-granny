using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For .FirstOrDefault()

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Panel References")]
    [Tooltip("Assign all your UIPanel GameObjects here in the Inspector.")]
    [SerializeField] public UIPanel[] allPanels; // Assign all your UIPanel GameObjects here in the Inspector

    private UIPanel _currentActiveMainPanel; // Tracks the main panel that is currently active
    private List<UIPanel> _activeOverlays = new List<UIPanel>(); // Tracks active overlay/sub-panels

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return; // Destroy duplicate and exit Awake
        }

        // Ensure all panels are initially hidden on Awake using their Hide method
      //  HideAllPanels();

        // Optional: Show a default panel on Awake, e.g., the MainPanel
    }
    private void Start()
    {
             ShowPanel("MainMenuPanel");

    }
    /// <summary>
    /// Shows a specific UIPanel by its GameObject name.
    /// This is the primary method for switching between main panels or showing overlays.
    /// </summary>
    /// <param name="panelName">The exact GameObject name of the panel to show.</param>
    /// <param name="isOverlay">If true, treats this panel as an overlay. If false, it acts as a main panel (hiding others).</param>
    public void ShowPanel(string panelName, bool isOverlay = false)
    {
        UIPanel panelToShow = allPanels.FirstOrDefault(p => p != null && p.gameObject.name == panelName);

        if (panelToShow == null)
        {
            Debug.LogWarning($"UIManager: Panel with name '{panelName}' not found in 'allPanels' array or is null. Make sure it's assigned and its name is correct.", this);
            return;
        }
          
        bool panelIsOverlay = isOverlay || panelToShow.IsSubPanel;

        if (panelIsOverlay)
        {
            if (!_activeOverlays.Contains(panelToShow))
            {
                _activeOverlays.Add(panelToShow);
            }
            panelToShow.Show();
        }
        else
        {
            // 1. Hide the current main panel (if any)
            if (_currentActiveMainPanel != null && _currentActiveMainPanel != panelToShow)
            {
                _currentActiveMainPanel.Hide();
            }

            // 2. Hide all currently active overlays
            foreach (var overlay in _activeOverlays.ToList())
            {
                if (overlay != null)
                {
                    overlay.Hide();
                  //  Debug.Log($"UIManager: Hidden overlay panel '{overlay.gameObject.name}'.");
                }
            }
            _activeOverlays.Clear();

            // 3. Activate the new main panel
            panelToShow.Show();
            _currentActiveMainPanel = panelToShow;
          //  Debug.Log($"UIManager: Main Panel '{panelToShow.gameObject.name}' requested to show.");
        }
    }

    /// <summary>
    /// Hides a specific UIPanel by its GameObject name.
    /// </summary>
    /// <param name="panelName">The exact GameObject name of the panel to hide.</param>
    public void HidePanel(string panelName)
    {
        UIPanel panelToHide = allPanels.FirstOrDefault(p => p != null && p.gameObject.name == panelName);

        if (panelToHide == null)
        {
            return;
        }

        panelToHide.Hide();

        // If it was the current main panel, clear the reference
        if (_currentActiveMainPanel == panelToHide)
        {
            _currentActiveMainPanel = null;
        }
        // If it was an overlay, remove it from the active overlays list
        if (_activeOverlays.Contains(panelToHide))
        {
            _activeOverlays.Remove(panelToHide);
        }
    }

    /// <summary>
    /// Hides all panels (useful for game start or scene transitions).
    /// </summary>
    public void HideAllPanels()
    {
        foreach (UIPanel panel in allPanels)
        {
            if (panel != null && panel.gameObject.activeSelf)
            {
                panel.Hide();
            }
        }
        _currentActiveMainPanel = null;
        _activeOverlays.Clear();
        Debug.Log("UIManager: All panels requested to hide.");
    }

    /// <summary>
    /// Retrieves the currently active main panel.
    /// </summary>
    public UIPanel GetCurrentActiveMainPanel()
    {
        return _currentActiveMainPanel;
    }
}