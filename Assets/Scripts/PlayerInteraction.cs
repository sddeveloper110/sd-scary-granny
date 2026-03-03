using UnityEngine;
using UnityEngine.UI;

public class PlayerInteraction : MonoBehaviour
{
    public Transform hand;
    public float throwForce = 6f;

    [Header("UI Buttons")]
    public Button pickBtn;
    public Button dropBtn;
    public Button interactBtn;

    [Header("Raycast Settings")]
    public float interactDistance = 4f;
    public LayerMask interactionLayer; // Assign in Inspector

    private PickableObject currentTarget;
    private PickableObject heldItem;
    private InteractableObject currentInteractable;

    void Start()
    {
        pickBtn.onClick.AddListener(Pick);
        dropBtn.onClick.AddListener(Drop);
        interactBtn.onClick.AddListener(Interact);

        UpdateButtons();
    }

    void Update()
    {
        DetectObject();
        HandleKeyboardInput();
        UpdateButtons();
    }

    void DetectObject()
    {
        currentTarget = null;
        currentInteractable = null;

        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactionLayer))
        {
            currentTarget = hit.collider.GetComponentInParent<PickableObject>();
            currentInteractable = hit.collider.GetComponentInParent<InteractableObject>();
        }
    }

    void UpdateButtons()
    {
        if (pickBtn == null || dropBtn == null || interactBtn == null) return;

        pickBtn.gameObject.SetActive(
            heldItem == null &&
            currentTarget != null
        );

        dropBtn.gameObject.SetActive(heldItem != null);

        interactBtn.gameObject.SetActive(
            currentInteractable != null
        );
    }

    public void Pick()
    {
        if (heldItem == null && currentTarget != null)
        {
            heldItem = currentTarget;
            heldItem.PickUp(hand);
        }
    }

    public void Drop()
    {
        if (heldItem != null)
        {
            heldItem.Throw(hand.forward * throwForce);
            heldItem = null;
        }
    }

    public void Interact()
    {
        if (currentInteractable != null)
        {
            currentInteractable.TryInteract(heldItem);
        }
    }

    void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (heldItem == null && currentTarget != null)
                Pick();
            else if (heldItem != null)
                Drop();
        }

        if (Input.GetKeyDown(KeyCode.F) && currentInteractable != null)
        {
            Interact();
        }
    }
}