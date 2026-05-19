using System;
using UnityEngine;


public class PickableObject : MonoBehaviour
{
    [Header("Pickable Settings")]
    public InteractableObject interactsWith;
    public Animator animator;
    public string animationTrigger = "Activate";

    public AudioClip useAudio;
    public AudioClip pickAudio;
    public AudioClip dropAudio;
    public bool isPicked;
    protected Rigidbody rb;
    
    public static event Action<PickableObject> OnObjectInteractionDone;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        OnUnhighlight();

    }

    public GameObject highlightVFX;
    // Highlight
    public void OnHighlight()
    {
        if (!highlightVFX) return;
        highlightVFX.SetActive(true);
    }

    public void OnUnhighlight()
    {
        if (!highlightVFX) return;
        highlightVFX.SetActive(false);
    }

    // Pick
    public void PickUp(Transform holderObject)
    {

        isPicked = true;
        if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }
        transform.SetParent(holderObject);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        if (pickAudio != null)
            SoundManager.PlayThisAudio(pickAudio);

        OnUnhighlight();

    }

    // Throw
    public void Throw(Vector3 force)
    {
        isPicked = false;
        transform.SetParent(null);
        if (rb != null) { rb.isKinematic = false; rb.useGravity = true; rb.AddForce(force, ForceMode.Impulse); }

        if (dropAudio != null)
            SoundManager.PlayThisAudio(dropAudio);
    }

    // Action when using item on object
    public virtual void Use(PickableObject self, InteractableObject target)
    {
        Debug.Log($"{name} used on {target.name}");
        // Optional: Add object-specific action here
        if (animator != null)
            animator.SetTrigger(animationTrigger);
        if (useAudio != null)
        {
            SoundManager.PlayThisAudio(useAudio);
        }

        OnObjectInteractionDone?.Invoke(this);
    }

    // Call this via UnityEvents when you want the object to vanish after being used
    public void Consume()
    {
        isPicked = false;
        transform.SetParent(null);
        gameObject.SetActive(false);

        PlayerInteraction playerInteraction = FindFirstObjectByType<PlayerInteraction>();
        if (playerInteraction != null)
        {
            playerInteraction.ForceClearHeldItem(this);
        }
    }
}
