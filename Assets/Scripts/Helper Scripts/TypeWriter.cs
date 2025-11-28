using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TypeWriter : MonoBehaviour
{
    public Text uiText;
    public float typingSpeed = 0.04f;
    public AudioSource typingSound;          // Optional: leave null if no sound

    Coroutine typingRoutine;

    public void ShowText(string fullText)
    {
        if (typingRoutine != null)
            StopCoroutine(typingRoutine);

        typingRoutine = StartCoroutine(TypeText(fullText));
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
