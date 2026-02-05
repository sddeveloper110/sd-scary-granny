using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using System.Collections;

public class Loading : MonoBehaviour
{
    public static Loading instance;
    private void Awake()
    {
        if (!instance)
        {
            instance = this;
            gameObject.SetActive(false);
        }
    }


    [Header("UI")]
    public Image loadingBar;
    public TMP_Text loadingText;

    [Header("Settings")]
    public float loadingTime = 3f;

    public static void StartLoading(UnityAction onComplete)
    {
        instance.gameObject.SetActive(true);
        instance.StartCoroutine(instance.LoadingRoutine(onComplete));
    }

    IEnumerator LoadingRoutine(UnityAction onComplete)
    {
        float timer = 0f;

        while (timer < loadingTime)
        {
            timer += Time.deltaTime;

            // Fill bar
            loadingBar.fillAmount = timer / loadingTime;

            // Loading text animation

            yield return null;
        }

        // Final call
        onComplete?.Invoke();

        gameObject.SetActive(false);
    }
}
