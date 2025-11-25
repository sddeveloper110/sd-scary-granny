using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

public class PanelToggleButton : MonoBehaviour
{
    public enum PanelActionType
    {
        Toggle,  // Default: toggles on/off
        Open,    // Always show
        Close    // Always hide
    }

    [BoxGroup("Panel Control")]
    [Tooltip("The UIPanel this button will control (show/hide).")]
    [SerializeField] private UIPanel targetPanel;



    [BoxGroup("Panel Control")]
    [Tooltip("Select what action this button should perform when clicked.")]
    [SerializeField, EnumToggleButtons]
    private PanelActionType actionType = PanelActionType.Toggle;

    private Button _unityUIButton;

    void Awake()
    {
        _unityUIButton = GetComponent<Button>();
        if (_unityUIButton == null)
        {
            _unityUIButton = gameObject.AddComponent<Button>();
            Debug.LogWarning($"PanelToggleButton on '{gameObject.name}': No Button component found, added one automatically.", this);
        }

        _unityUIButton.onClick.AddListener(OnButtonClicked);
    }

    void OnDestroy()
    {
        if (_unityUIButton != null)
        {
            _unityUIButton.onClick.RemoveListener(OnButtonClicked);
        }
    }


    [Button("Simulate Click")]
    public void OnButtonClicked()
    {
        if (targetPanel == null)
        {
            Debug.LogWarning($"PanelToggleButton on '{gameObject.name}': No Target Panel assigned!", this);
            return;
        }

        if (UIManager.Instance == null)
        {
            Debug.LogError($"PanelToggleButton on '{gameObject.name}': UIManager.Instance is null.", this);
            return;
        }

        switch (actionType)
        {
            case PanelActionType.Open:
                UIManager.Instance.ShowPanel(targetPanel.gameObject.name);
                break;

            case PanelActionType.Close:
                UIManager.Instance.HidePanel(targetPanel.gameObject.name);
                break;

            case PanelActionType.Toggle:
            default:
                if (targetPanel.gameObject.activeSelf)
                    UIManager.Instance.HidePanel(targetPanel.gameObject.name);
                else
                    UIManager.Instance.ShowPanel(targetPanel.gameObject.name);
                break;
        }
    }
}
