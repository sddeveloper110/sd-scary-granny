using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class SplashSequence : MonoBehaviour
{

    [Header("Settings")]
    public float splashDuration = 5f;

    [Header("Avatar & Flag Selection")]
    public GameObject[] avatarFills;
    public GameObject[] flagFills;

    [Header("Player Info")]
    public TMP_InputField nameInputField;
    public TMP_InputField ageInputField;

    void Start()
    {
        // Ensure initial state
        if (nameInputField != null)
            nameInputField.text = PlayerPrefs.GetString("PlayerName", "Player");
        if (ageInputField != null)
            ageInputField.text = PlayerPrefs.GetString("PlayerAge", "18");

        UpdateFills();
        StartCoroutine(ShowPrivacyPolicyAfterDelay());
    }

    public void SavePlayerData()
    {
        if (nameInputField != null && !string.IsNullOrEmpty(nameInputField.text))
        {
            PlayerPrefs.SetString("PlayerName", nameInputField.text);
        }

        if (ageInputField != null && !string.IsNullOrEmpty(ageInputField.text))
        {
            PlayerPrefs.SetString("PlayerAge", ageInputField.text);
        }

        Debug.Log("Saved Player Data: " + PlayerPrefs.GetString("PlayerName") + " | Age: " + PlayerPrefs.GetString("PlayerAge"));
    }

    public void SelectAvatar(int index)
    {
        PlayerPrefs.SetInt("SelectedAvatar", index);
        UpdateFills();
    }

    public void SelectFlag(int index)
    {
        PlayerPrefs.SetInt("SelectedFlag", index);
        UpdateFills();
    }

    private void UpdateFills()
    {
        int selectedAvatar = PlayerPrefs.GetInt("SelectedAvatar", 0);
        int selectedFlag = PlayerPrefs.GetInt("SelectedFlag", 0);

        for (int i = 0; i < avatarFills.Length; i++)
        {
            if (avatarFills[i] != null)
                avatarFills[i].SetActive(i == selectedAvatar);
        }

        for (int i = 0; i < flagFills.Length; i++)
        {
            if (flagFills[i] != null)
                flagFills[i].SetActive(i == selectedFlag);
        }
    }

    private IEnumerator ShowPrivacyPolicyAfterDelay()
    {
        UIPanelEnabler.OpenPanel(PanelType.Splash);
        yield return new WaitForSeconds(splashDuration);

        // Check if privacy policy has already been accepted
        if (PlayerPrefs.GetInt("PrivacyAccepted", 0) == 0)
        {
            UIPanelEnabler.OpenPanel(PanelType.PrivacyPolicy);
        }
        else
        {
            // Skip privacy policy and load next scene directly
            LoadNextScene();
        }
    }

    /// <summary>
    /// Call this from the "Accept" button on the Privacy Policy panel.
    /// </summary>
    public void AcceptPrivacyAndLoadNext()
    {
        PlayerPrefs.SetInt("PrivacyAccepted", 1);
        LoadNextScene();
    }

    public void LoadNextScene()
    {
        // Hide privacy policy panel before starting loading
        PlayerPrefs.SetInt("PrivacyAccepted", 1);
        PlayerPrefs.Save();


        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            if (UIPanelEnabler.Instance != null)
            {
                // Start the loading sequence via UIPanelEnabler
                UIPanelEnabler.Instance.StartCoroutine(UIPanelEnabler.Instance.Loading(2.5f, () => 
                {
                    SceneManager.LoadScene(nextSceneIndex);
                }));
            }
            else
            {
                // Fallback if UIPanelEnabler is not present
                SceneManager.LoadScene(nextSceneIndex);
            }
        }
        else
        {
            Debug.LogWarning("No next scene in Build Settings!");
        }
    }
}
