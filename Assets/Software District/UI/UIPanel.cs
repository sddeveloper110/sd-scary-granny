using UnityEngine;
using DG.Tweening; // Import DOTween namespace
using UnityEngine.UI; // Required for Image component
using Sirenix.OdinInspector; // Required for Odin Inspector attributes

public class UIPanel : MonoBehaviour
{
    // Use BoxGroup for logical grouping in the Inspector
    [BoxGroup("Panel Configuration")]
    [Tooltip("If true, showing this panel will NOT hide the currently active main panel. Useful for popups, overlays, or nested sub-panels.")]
    public bool IsSubPanel = false;

    // Use BoxGroup for Animation Settings
    [BoxGroup("Animation Settings")]
    [Tooltip("Duration of the scale and fade animations.")]
    [SerializeField, Range(0.01f, 2.0f)] // Add Range for a slider in Inspector
    private float animationDuration = 0.3f;

    [BoxGroup("Animation Settings")]
    [Tooltip("Target alpha for the ChildBackground when shown (e.g., 0.5 for 50%).")]
    [SerializeField, Range(0f, 1f)] // Add Range for a slider
    private float backgroundTargetAlpha = 0.9f;

    [BoxGroup("Animation Settings")]
    [Tooltip("The target scale for the 'Panel' child when fully visible.")]
    [SerializeField]
    private Vector3 subPanelVisibleScale = new Vector3(1f, 1f, 1f); // Usually Vector3.one

    [BoxGroup("Animation Settings")]
    [Tooltip("The scale for the 'Panel' child when hidden (usually Vector3.zero or a very small value).")]
    [SerializeField]
    private Vector3 subPanelHiddenScale = new Vector3(0.01f, 0.01f, 0.01f); // Use small value to prevent division by zero

    // Internal references to the child components
    private Image _childBackgroundImage;
    private RectTransform _subPanelRectTransform;

    void Awake()
    {
        // Ensure the main UIPanel object itself is always at full scale
        transform.localScale = Vector3.one;

        // Get references to child components
        Transform backgroundTransform = transform.Find("ChildBackground");
        if (backgroundTransform != null)
        {
            _childBackgroundImage = backgroundTransform.GetComponent<Image>();
            if (_childBackgroundImage == null)
            {
                Debug.LogWarning($"UIPanel '{gameObject.name}': 'ChildBackground' found but no Image component attached.", this);
            }
        }
        else
        {
            Debug.LogError($"UIPanel '{gameObject.name}': 'ChildBackground' GameObject not found as a child! Please ensure it's named exactly 'ChildBackground'.", this);
        }

        Transform subPanelTransform = transform.Find("Main Panel");
        if (subPanelTransform != null)
        {
            _subPanelRectTransform = subPanelTransform.GetComponent<RectTransform>();
            if (_subPanelRectTransform == null)
            {
                Debug.LogWarning($"UIPanel '{gameObject.name}': 'Main Panel' found but no RectTransform component attached.", this);
            }
        }
        else
        {
            Debug.LogError($"UIPanel '{gameObject.name}': 'Main Panel' GameObject not found as a child! Please ensure it's named exactly 'Panel'.", this);
        }

        // Set initial hidden state for children
        if (_childBackgroundImage != null)
        {
            Color initialBgColor = _childBackgroundImage.color;
            initialBgColor.a = 0f; // Start completely transparent
            _childBackgroundImage.color = initialBgColor;
        }
        if (_subPanelRectTransform != null)
        {
            _subPanelRectTransform.localScale = subPanelHiddenScale; // Start at hidden scale
        }


    }

    /// <summary>
    /// Shows the panel with a smooth scale-up for 'Panel' and fade-in for 'ChildBackground'.
    /// </summary>
    [Button("Show Panel"), ButtonGroup("Panel Actions")] // Group buttons horizontally
    public void Show()
    {
        // If already active and seemingly visible, don't re-animate
        if (gameObject.activeSelf && _subPanelRectTransform != null && _subPanelRectTransform.localScale == subPanelVisibleScale)
        {
            Debug.Log($"Panel '{gameObject.name}' is already active and fully visible.");
            return;
        }

        // Activate the main GameObject immediately for children animations to run
        gameObject.SetActive(true);

        // Create a DOTween sequence to run animations concurrently
        Sequence showSequence = DOTween.Sequence();

        if (_childBackgroundImage != null)
        {
            // Ensure background is fully transparent at start of animation
            Color currentBgColor = _childBackgroundImage.color;
            currentBgColor.a = 0f;
            _childBackgroundImage.color = currentBgColor;

            showSequence.Join(_childBackgroundImage.DOFade(backgroundTargetAlpha, animationDuration));
        }

        if (_subPanelRectTransform != null)
        {
            // Ensure sub-panel is at hidden scale at start of animation
            _subPanelRectTransform.localScale = subPanelHiddenScale;

            showSequence.Join(_subPanelRectTransform.DOScale(subPanelVisibleScale, animationDuration).SetEase(Ease.OutBack));
        }

        // Set completion callback for the sequence
        showSequence.OnComplete(() =>
        {
          //  Debug.Log($"Panel '{gameObject.name}' show animation completed.");
        });

       // Debug.Log($"Panel '{gameObject.name}' starting show animation.");
    }

    /// <summary>
    /// Hides the panel with a smooth scale-down for 'Panel' and fade-out for 'ChildBackground'.
    /// </summary>
    [Button("Hide Panel"), ButtonGroup("Panel Actions")] // Group buttons horizontally
    public void Hide()
    {
        // If already inactive and seemingly hidden, don't re-animate
        if (!gameObject.activeSelf && _subPanelRectTransform != null && _subPanelRectTransform.localScale == subPanelHiddenScale)
        {
            return;
        }

        // Create a DOTween sequence to run animations concurrently
        Sequence hideSequence = DOTween.Sequence();

        if (_childBackgroundImage != null)
        {
            // Ensure background is at target alpha at start of animation
            Color currentBgColor = _childBackgroundImage.color;
            currentBgColor.a = backgroundTargetAlpha;
            _childBackgroundImage.color = currentBgColor;

            hideSequence.Join(_childBackgroundImage.DOFade(0f, animationDuration));
        }

        if (_subPanelRectTransform != null)
        {
            // Ensure sub-panel is at visible scale at start of animation
            _subPanelRectTransform.localScale = subPanelVisibleScale;

            hideSequence.Join(_subPanelRectTransform.DOScale(subPanelHiddenScale, animationDuration).SetEase(Ease.InBack));
        }

        // Set completion callback for the sequence
        hideSequence.OnComplete(() =>
        {
                gameObject.SetActive(false);
        });

    }
}