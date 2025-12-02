using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif


public class RemoveChildColliders : MonoBehaviour
{
#if UNITY_EDITOR
    [ContextMenu("Remove Colliders")]
    void RemoveCollidersContext()
    {
        RemoveColliders();
    }
    [ContextMenu("Remove Scripts")]
    void RemoveScriptsContext()
    {
        RemoveScripts();
    }
#endif

    public void RemoveColliders()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();

        foreach (Collider collider in colliders)
        {
            DestroyImmediate(collider);
        }
    }

    public void RemoveScripts()
    {
        int count = 0;

        // Go through this object and all children (include inactive)
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            count += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
        }
    }

}



[CustomEditor(typeof(RemoveChildColliders))]
public class RemoveChildCollidersEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        RemoveChildColliders removeChildColliders = (RemoveChildColliders)target;

        if (GUILayout.Button("Remove Colliders"))
        {
            removeChildColliders.RemoveColliders();
        }
        if (GUILayout.Button("Remove Scripts"))
        {
            removeChildColliders.RemoveScripts();
        }
    }
}