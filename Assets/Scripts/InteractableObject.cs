using UnityEngine;
using System;

public class InteractableObject : MonoBehaviour
{
    [Header("Interactable Settings")]
    public Animator anim;
    public string animationTrigger = "Activate";

    [Header("Interaction Count (For things like breaking chains)")]
    public int interactionRequired = 1;
    private int interactionDone = 0;

    // GLOBAL EVENT fired when this interactable is fully activated
    public static Action<InteractableObject> OnObjectInteractionDone;

    public void TryInteract(PickableObject heldItem)
    {
        if (heldItem == null)
        {
            return;
        }

        if (heldItem.interactsWith == this)
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


        // FIRE GLOBAL EVENT
        OnObjectInteractionDone?.Invoke(this);
    }
}
