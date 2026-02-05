using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Screens")]
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject gameplay;
    [SerializeField] private GameObject hint;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        ShowMainMenu();
    }

    // ---------- SHOW ----------
    public void ShowMainMenu()
    {
        HideAll();
        mainMenu.SetActive(true);
    }

    public void ShowGameplay()
    {
        HideAll();
        gameplay.SetActive(true);
    }

    public void ShowHint()
    {
        HideAll();
        hint.SetActive(true);
    }

    // ---------- HIDE ----------
    public void HideMainMenu()
    {
        mainMenu.SetActive(false);
    }

    public void HideGameplay()
    {
        gameplay.SetActive(false);
    }

    public void HideHint()
    {
        hint.SetActive(false);
    }

    // ---------- COMMON ----------
    private void HideAll()
    {
        mainMenu.SetActive(false);
        gameplay.SetActive(false);
        hint.SetActive(false);
    }
}
