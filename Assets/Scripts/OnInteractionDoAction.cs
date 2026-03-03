using UnityEngine;
using UnityEngine.Events;

public class OnInteractionDoAction : MonoBehaviour
{
    public UnityEvent actionToPerform;
    private void OnEnable()
    {
        InteractableObject.OnObjectInteractionDone += PerformAction;
    }
    private void OnDisable()
    {
        InteractableObject.OnObjectInteractionDone -= PerformAction;
    }
    void PerformAction(InteractableObject invokedFrom)
    {
        if (invokedFrom.gameObject == this.gameObject)
        {
            actionToPerform.Invoke();
        }
    }
}
