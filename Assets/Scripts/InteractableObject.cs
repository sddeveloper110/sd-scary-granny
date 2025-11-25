using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    [Header("Interactable Settings")]
    public InteractionType type = InteractionType.None;
    public Animator anim;
    public string animationTrigger = "Activate";

    [Header("Interaction Count (For things like breaking chains)")]
    public int interactionRequired = 1;      // how many times player must hit/interact
    private int interactionDone = 0;         // internal counter

    [Header("Task Integration")]
    public bool completesTask = false;       // check this if this object completes a task
    public string taskNameToComplete;        // leave empty to complete current task

    public void TryInteract(PickableObject heldItem)
    {
        if (heldItem == null)
        {
            Debug.Log("You need an item to interact.");
            return;
        }

        if (heldItem.interactsWith == type)
        {
            interactionDone++;
            Debug.Log($"Interaction success {interactionDone}/{interactionRequired}: {heldItem.name} -> {name}");

            heldItem.Use(heldItem, this);

            if (interactionDone >= interactionRequired)
                Activate();
        }
        else
        {
            Debug.Log("Wrong item type: " + heldItem.interactsWith);
        }
    }

    public void Activate()
    {
        if (anim != null)
            anim.SetTrigger(animationTrigger);

        Debug.Log($"{name} activated!");

        // ---------------------------
        // TASK COMPLETION SECTION
        // ---------------------------
        if (completesTask)
        {
            if (!string.IsNullOrEmpty(taskNameToComplete))
            {
                GameManager.Instance.CompleteTaskByName(taskNameToComplete);
            }
            else
            {
                GameManager.Instance.CompleteCurrentTask();
            }
        }
    }
}
