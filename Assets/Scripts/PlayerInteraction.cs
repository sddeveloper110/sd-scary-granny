using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public Transform hand;
    public float throwForce = 6f;

    private PickableObject currentTarget;
    private PickableObject heldItem;
    private InteractableObject currentInteractable;

    private void OnTriggerEnter(Collider other)
    {
        PickableObject pickable = other.GetComponent<PickableObject>();
        if (pickable != null)
        {
            currentTarget = pickable;
            currentTarget.OnHighlight();
            //Debug.Log("Entered Pickable Range: " + currentTarget.name);
        }

        InteractableObject interactable = other.GetComponent<InteractableObject>();
        if (interactable != null)
        {
            currentInteractable = interactable;
            //Debug.Log("Entered Interactable Range: " + currentInteractable.name);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (currentTarget != null && other.GetComponent<PickableObject>() == currentTarget)
        {
            currentTarget.OnUnhighlight();
            //Debug.Log("Left Pickable Range: " + currentTarget.name);
            currentTarget = null;
        }

        if (currentInteractable != null && other.GetComponent<InteractableObject>() == currentInteractable)
        {
            //Debug.Log("Left Interactable Range: " + currentInteractable.name);
            currentInteractable = null;
        }
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

        Debug.Log("Clicked on: " + hit.collider.name);

        // Click pickup (ONLY if inside trigger)
        if (currentTarget != null && hit.collider.GetComponentInParent<PickableObject>() == currentTarget)
        {
            if (heldItem == null)
            {
                heldItem = currentTarget;
                heldItem.PickUp(hand);
                //Debug.Log("Picked up (CLICK): " + heldItem.name);
            }
            else if (heldItem == currentTarget)
            {
                heldItem.Throw(hand.forward * throwForce);
                //Debug.Log("Thrown (CLICK): " + heldItem.name);
                heldItem = null;
            }
            return;
        }

        // Click interact (ONLY if inside trigger)
        if (currentInteractable != null && hit.collider.GetComponentInParent<InteractableObject>() == currentInteractable)
        {
            Debug.Log("Interacted (CLICK) with: " + currentInteractable.name);
            currentInteractable.TryInteract(heldItem);
        }
    }
}
