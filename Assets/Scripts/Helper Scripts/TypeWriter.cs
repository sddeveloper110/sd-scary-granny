using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class TypeWriter : MonoBehaviour
{
    public TextMeshProUGUI uiText;
    public float typingSpeed = 0.04f;
    public AudioSource typingSound;

    Coroutine typingRoutine;

    string pendingText;
    bool shouldPlayWhenReady;

    public void ShowText(string fullText)
    {
        // Save text & intent
        pendingText = fullText;
        shouldPlayWhenReady = true;

        TryPlay();
    }

    void OnEnable()
    {
        TryPlay();
    }

    void TryPlay()
    {
        if (!shouldPlayWhenReady)
            return;

        if (!gameObject.activeInHierarchy || uiText == null || !uiText.gameObject.activeInHierarchy)
            return;

        if (typingRoutine != null)
            StopCoroutine(typingRoutine);

        shouldPlayWhenReady = false;
        typingRoutine = StartCoroutine(TypeText(pendingText));
    }

    IEnumerator TypeText(string fullText)
    {
        uiText.text = "";

        foreach (char c in fullText)
        {
            uiText.text += c;

            if (typingSound != null)
                typingSound.Play();

            yield return new WaitForSeconds(typingSpeed);
        }
    }
}
