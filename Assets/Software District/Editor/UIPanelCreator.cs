using UnityEditor;
using UnityEngine;
using UnityEngine.UI; // Required for Canvas, RectTransform, Image

public class UIPanelCreator : Editor
{
    private const string MENU_PATH = "GameObject/Software District/UIPanel";
    private const string PARENT_CANVAS_NAME = "Canvas"; // Default parent name for UI elements

    [MenuItem(MENU_PATH, false, 0)] // false means it's not a validation method. 0 sets the order.
    static void CreateUIPanel()
    {
        // 1. Ensure a Canvas exists in the scene, or create one
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject(PARENT_CANVAS_NAME, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // Common default for UI
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");
        }

        // 2. Create the main UI Panel GameObject (the one with UIPanel script)
        GameObject mainPanelGO = new GameObject("NewUIPanel");

        // Ensure it has a RectTransform, which all UI elements need
        RectTransform mainRectTransform = mainPanelGO.AddComponent<RectTransform>();

        // Attach the UIPanel script
        UIPanel uiPanel = mainPanelGO.AddComponent<UIPanel>(); // This object only has UIPanel

        // Set the parent to the Canvas
        mainPanelGO.transform.SetParent(canvas.transform, false); // false to maintain local scale/rotation

        // Set Main Panel RectTransform to stretch to fill parent (common for main panels)
        mainRectTransform.anchorMin = Vector2.zero;
        mainRectTransform.anchorMax = Vector2.one;
        mainRectTransform.pivot = new Vector2(0.5f, 0.5f);
        mainRectTransform.anchoredPosition = Vector2.zero;
        mainRectTransform.sizeDelta = Vector2.zero; // Stretch to fill parent
        mainRectTransform.localScale = Vector3.one;
        mainRectTransform.localRotation = Quaternion.identity;

        // 3. Create the first child: "ChildBackground" with Image (low transparency & black color)
        GameObject childBackgroundGO = new GameObject("ChildBackground");
        RectTransform childBackgroundRect = childBackgroundGO.AddComponent<RectTransform>();
        Image childBackgroundImage = childBackgroundGO.AddComponent<Image>();

        // Set parent to the main panel
        childBackgroundGO.transform.SetParent(mainPanelGO.transform, false);

        // Set RectTransform to stretch to fill its parent (NewUIPanel)
        childBackgroundRect.anchorMin = Vector2.zero;
        childBackgroundRect.anchorMax = Vector2.one;
        childBackgroundRect.pivot = new Vector2(0.5f, 0.5f);
        childBackgroundRect.anchoredPosition = Vector2.zero;
        childBackgroundRect.sizeDelta = Vector2.zero; // Stretch to fill parent
        childBackgroundRect.localScale = Vector3.one;
        childBackgroundRect.localRotation = Quaternion.identity;

        // Set specific color and transparency for the background image
        childBackgroundImage.color = new Color(0f, 0f, 0f, 0.5f); // Black with 10% alpha
        childBackgroundImage.raycastTarget = true; // Typically, backgrounds block raycasts

        // 4. Create the second child: "Panel" with Image (white, full alpha, stretched with margins)
        GameObject panelContentGO = new GameObject("Main Panel");
        RectTransform panelContentRect = panelContentGO.AddComponent<RectTransform>();
        Image panelContentImage = panelContentGO.AddComponent<Image>();

        // Set parent to the main panel
        panelContentGO.transform.SetParent(mainPanelGO.transform, false);

        // Set RectTransform for the content panel: center aligned, stretched 100 from each side
        panelContentRect.anchorMin = Vector2.zero;
        panelContentRect.anchorMax = Vector2.one;
        panelContentRect.pivot = new Vector2(0.5f, 0.5f);
        panelContentRect.anchoredPosition = Vector2.zero;
        panelContentRect.offsetMin = new Vector2(100, 100); // Left and Bottom margins
        panelContentRect.offsetMax = new Vector2(-100, -100); // Right and Top margins
        panelContentRect.localScale = Vector3.one;
        panelContentRect.localRotation = Quaternion.identity;

        // Set specific color and transparency for the content panel
        panelContentImage.color = new Color(1f, 1f, 1f, 1f); // White with 100% alpha (full)
        panelContentImage.raycastTarget = true; // Typically, content panels block raycasts

        // Register all created objects/components with Undo system
        Undo.RegisterCreatedObjectUndo(mainPanelGO, "Create UI Panel with Children");
        Undo.RegisterCreatedObjectUndo(childBackgroundGO, "Create UI Panel Child Background");
        Undo.RegisterCreatedObjectUndo(panelContentGO, "Create UI Panel Child Content");

        // Select the newly created main object in the Hierarchy
        Selection.activeGameObject = mainPanelGO;

        Debug.Log("UIPanel created: " + mainPanelGO.name + " with two image children (specific colors/offsets).");
    }

    //// Optional: Validate the menu item (e.g., enable/disable based on context)
    //[MenuItem(MENU_PATH, true)] // true means it's a validation method
    //static bool ValidateCreateUIPanel()
    //{
    //    // Only enable if we are in a UI context (e.g., right-clicking on Canvas)
    //    // or if there's no Canvas yet (so we can create one)
    //    if (Selection.activeGameObject != null)
    //    {
    //        // If right-clicking on a Canvas or an object within a Canvas, enable
    //        if (Selection.activeGameObject.GetComponentInParent<Canvas>() != null)
    //        {
    //            return true;
    //        }
    //    }
    //    // Always allow if no Canvas exists, so the user can create the first one.
    //    return FindObjectOfType<Canvas>() == null;
    //}
}