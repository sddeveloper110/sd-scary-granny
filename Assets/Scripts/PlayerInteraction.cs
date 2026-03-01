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

    private void OnTriggerEnter(Collider other)
    {
        PickableObject pickable = other.GetComponent<PickableObject>();
        if (pickable != null && !pickable.isPicked)
        {
            currentTarget = pickable;
            currentTarget.OnHighlight();
        }

        InteractableObject interactable = other.GetComponent<InteractableObject>();
        if (interactable != null)
        {
            currentInteractable = interactable;
        }

        UpdateButtons();
    }

    private void OnTriggerExit(Collider other)
    {
        if (currentTarget != null && other.GetComponent<PickableObject>() == currentTarget)
        {
            currentTarget.OnUnhighlight();
            currentTarget = null;
        }

        if (currentInteractable != null && other.GetComponent<InteractableObject>() == currentInteractable)
        {
            currentInteractable = null;
        }

        UpdateButtons();
    }

    // Logic updated per your requirement:
    void UpdateButtons()
    {
        if (pickBtn == null || dropBtn == null || interactBtn == null) return;

        // Pick: Only show if hand is empty AND there is a target to pick
        pickBtn.gameObject.SetActive(heldItem == null && currentTarget != null);

        // Drop: Only show if we are holding something
        dropBtn.gameObject.SetActive(heldItem != null);

        // Interact: Show if an interactable is in range (regardless of holding item or not)
        interactBtn.gameObject.SetActive(currentInteractable != null);
    }

    // ===== Action Wrappers (To ensure UI updates) =====

    public void Pick()
    {
        if (heldItem == null && currentTarget != null)
        {
            heldItem = currentTarget;
            heldItem.PickUp(hand);
            UpdateButtons(); // Critical call
        }
    }

    public void Drop()
    {
        if (heldItem != null)
        {
            heldItem.Throw(hand.forward * throwForce);
            heldItem = null;
            UpdateButtons(); // Critical call
        }
    }

    public void Interact()
    {
        if (currentInteractable != null)
        {
            currentInteractable.TryInteract(heldItem);
            UpdateButtons();
        }
    }

    private void Update()
    {
        HandleKeyboardInput();
        HandleClickInput();
    }

    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (heldItem == null && currentTarget != null) Pick();
            else if (heldItem != null) Drop();
        }

        if (Input.GetKeyDown(KeyCode.F) && currentInteractable != null)
        {
            Interact();
        }
    }

    private void HandleClickInput()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 5f)) return;

        // Click pickup
        if (currentTarget != null && hit.collider.GetComponentInParent<PickableObject>() == currentTarget)
        {
            if (heldItem == null) Pick();
            else if (heldItem == currentTarget) Drop();
        }

        // Click interact
        if (currentInteractable != null && hit.collider.GetComponentInParent<InteractableObject>() == currentInteractable)
        {
            Interact();
        }
    }
}