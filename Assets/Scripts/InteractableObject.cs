using System;
using System.Collections.Generic;
using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    [Header("Interactable Settings")]
    public Animator anim;
    public string animationTrigger = "Activate";

    [Header("Interaction Count (For things like breaking chains)")]
    public int interactionRequired = 1;
    private int interactionDone = 0;

    public bool IsInteracted =false;
    public string INeedThisText;

    private HashSet<PickableObject> usedItems = new HashSet<PickableObject>();
    
    // GLOBAL EVENT fired when this interactable is fully activated
    public static Action<InteractableObject> OnObjectInteractionDone;


    public GameObject highlightVFX;
    // Highlight
    public void OnHighlight() => highlightVFX?.SetActive(true);
    public void OnUnhighlight() => highlightVFX?.SetActive(false);

    private void OnEnable()
    {
        CanvasManager.OnGameStart += () => { usedItems.Clear(); };
        OnUnhighlight();
    }

    public void TryInteract(PickableObject heldItem)
    {
        if (interactionRequired == 0)
        {
            interactionDone++;
            Activate();
            return; // 🔥 VERY IMPORTANT

        }

        if (heldItem == null && interactionRequired > 0)
        {
            CanvasManager.ShowPopup(INeedThisText);

            return;
        }

        if (heldItem.interactsWith == this)
        {
            if (interactionRequired > 1 && usedItems.Contains(heldItem))
            {
                CanvasManager.ShowPopup("You already used this item.");
                return;
            }
            usedItems.Add(heldItem);
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

        IsInteracted = true;

        // FIRE GLOBAL EVENT
        OnObjectInteractionDone?.Invoke(this);
    }
}
