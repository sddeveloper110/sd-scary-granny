using UnityEngine;
using UnityEngine.Events;

public class OnInteractionDoAction : MonoBehaviour
{
    public UnityEvent actionToPerform;
    private void OnEnable()
    {
        InteractableObject.OnObjectInteractionDone += PerformAction;
        PickableObject.OnObjectInteractionDone += PerformPickableAction;
    }
    private void OnDisable()
    {
        InteractableObject.OnObjectInteractionDone -= PerformAction;
        PickableObject.OnObjectInteractionDone -= PerformPickableAction;
    }
    void PerformAction(InteractableObject invokedFrom)
    {
        if (invokedFrom.gameObject == this.gameObject)
        {
            actionToPerform.Invoke();
        }
    }
    
    void PerformPickableAction(PickableObject invokedFrom)
    {
        if (invokedFrom.gameObject == this.gameObject)
        {
            actionToPerform.Invoke();
        }
    }
}
