using UnityEngine;
using UnityEngine.UI;

public class HintSystem : MonoBehaviour
{
    public static HintSystem Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Text hintText; // Assign Text component inside HintPanel

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Shows the hint on the HintPanel and animates the panel.
    /// </summary>
    /// <param name="hint">Hint string to display</param>
    public void ShowHint(string hint)
    {
        if (hintText != null)
            hintText.text = hint;
    }

    /// <summary>
    /// Hides the hint panel
    /// </summary>
    public void HideHint()
    {
        UIManager.Instance.HidePanel("HintPanel");
    }
}
