using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject movementController;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private GameObject gameplayPanel;

    private void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStarted += SetupForGameplay;
        
    }


    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStarted -= SetupForGameplay;
    }

    private void Start()
    {
        ToggleControls(GameManager.Instance.isGameStarted);
        HideGameplayUI();
    }

    public void ToggleControls(bool isActive)
    {
        if (movementController != null) movementController.SetActive(isActive);
        if (playerCamera != null) playerCamera.gameObject.SetActive(isActive);
        if(gameplayPanel !=null) gameplayPanel.gameObject.SetActive(isActive);   
    }

    private void ShowGameplayUI()
    {
        //if (gameplayPanel != null)
        //    UIManager.Instance.ShowPanel(gameplayPanel.name, isOverlay: false);

        UIManager.Instance.ShowGameplay();
    }

    private void HideGameplayUI()
    {
        //if (gameplayPanel != null)
        //    UIManager.Instance.HidePanel(gameplayPanel.name);
        UIManager.Instance.HideGameplay();

    }

    public void SetupForGameplay()
    {
        ResetPlayer(GameManager.Instance.GetSpawnPosition,Quaternion.identity);
        ToggleControls(true);
        ShowGameplayUI();
    }

    public void ResetPlayer(Vector3 position, Quaternion rotation)
    {
        ToggleControls(false);
        HideGameplayUI();
        transform.localPosition = position;
        transform.rotation = rotation;
    }
}
