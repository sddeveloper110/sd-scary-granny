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

    void UpdateButtons()
    {
        // Pick button
        pickBtn.gameObject.SetActive(heldItem == null && currentTarget != null);

        // Drop button
        dropBtn.gameObject.SetActive(heldItem != null);

        interactBtn.gameObject.SetActive(heldItem != null && currentInteractable != null);

    }

    // ===== Button Actions =====

    public void Pick()
    {
        if (heldItem == null && currentTarget != null)
        {
            heldItem = currentTarget;
            heldItem.PickUp(hand);
            UpdateButtons();
        }
    }

    public void Drop()
    {
        if (heldItem != null)
        {
            heldItem.Throw(hand.forward * throwForce);
            heldItem = null;
            UpdateButtons();
        }
    }
    public void Interact()
    {
            currentInteractable.TryInteract(heldItem);
    }


    private void Update()
    {
        HandleKeyboardInput();
        HandleClickInput();   // NEW FEATURE
    }

    private void HandleKeyboardInput()
    {
        // Pick up / Throw using keyboard (E)
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (heldItem == null && currentTarget != null)
            {
                heldItem = currentTarget;
                heldItem.PickUp(hand);
            }
            else if (heldItem != null)
            {
                heldItem.Throw(hand.forward * throwForce);
                heldItem = null;
            }
        }

        // Interact using keyboard (F)
        if (Input.GetKeyDown(KeyCode.F) && currentInteractable != null)
        {
            currentInteractable.TryInteract(heldItem);
        }
    }

    private void HandleClickInput()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, 5f))
        {
            return;
        }


        // Click pickup (ONLY if inside trigger)
        if (currentTarget != null && hit.collider.GetComponentInParent<PickableObject>() == currentTarget)
        {
            if (heldItem == null)
            {
                heldItem = currentTarget;
                heldItem.PickUp(hand);
            }
            else if (heldItem == currentTarget)
            {
                heldItem.Throw(hand.forward * throwForce);
                heldItem = null;
            }
            return;
        }

        // Click interact (ONLY if inside trigger)
        if (currentInteractable != null && hit.collider.GetComponentInParent<InteractableObject>() == currentInteractable)
        {
            currentInteractable.TryInteract(heldItem);
        }
    }
}
