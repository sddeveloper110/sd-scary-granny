using UnityEngine;


public class PickableObject : MonoBehaviour
{
    [Header("Pickable Settings")]
    public InteractableObject interactsWith;
    public GameObject highlightVFX;

    protected bool isPicked;
    protected Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

    }

    // Highlight
    public void OnHighlight() => highlightVFX?.SetActive(true);
    public void OnUnhighlight() => highlightVFX?.SetActive(false);

    // Pick
    public void PickUp(Transform holderObject)
    {
        isPicked = true;
        if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }
        transform.SetParent(holderObject);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    // Throw
    public void Throw(Vector3 force)
    {
        isPicked = false;
        transform.SetParent(null);
        if (rb != null) { rb.isKinematic = false; rb.useGravity = true; rb.AddForce(force, ForceMode.Impulse); }
    }

    // Action when using item on object
    public virtual void Use(PickableObject self, InteractableObject target)
    {
        Debug.Log($"{name} used on {target.name}");
        // Optional: Add object-specific action here
    }
}
