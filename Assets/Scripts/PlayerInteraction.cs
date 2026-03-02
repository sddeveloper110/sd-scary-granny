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

    [Header("View Settings")]
    public float interactDistance = 4f;
    [Range(0.5f, 1f)]
    public float viewDotThreshold = 0.75f;

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

        pickBtn.gameObject.SetActive(
            heldItem == null &&
            currentTarget != null &&
            IsFacingTarget(currentTarget.transform)
        );

        dropBtn.gameObject.SetActive(heldItem != null);

        interactBtn.gameObject.SetActive(
            currentInteractable != null &&
            IsFacingTarget(currentInteractable.transform)
        );
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
        if (currentTarget != null || currentInteractable != null)
        {
            UpdateButtons();
        }
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

    bool IsFacingTarget(Transform target)
    {
        Vector3 dirToTarget = (target.position - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, target.position);

        if (distance > interactDistance)
            return false;

        float dynamicThreshold = Mathf.Lerp(0.3f, viewDotThreshold, distance / interactDistance);

        float dot = Vector3.Dot(transform.forward, dirToTarget);

        return dot >= dynamicThreshold;
    }
    private void OnDrawGizmosSelected()
    {
        // Draw interaction distance sphere
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactDistance);

        // Convert dot threshold to angle
        float angle = Mathf.Acos(viewDotThreshold) * Mathf.Rad2Deg;

        // Draw left boundary
        Vector3 leftDir = Quaternion.AngleAxis(-angle, Vector3.up) * transform.forward;
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, leftDir * interactDistance);

        // Draw right boundary
        Vector3 rightDir = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;
        Gizmos.DrawRay(transform.position, rightDir * interactDistance);

        // Draw forward line
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.forward * interactDistance);
    }
}