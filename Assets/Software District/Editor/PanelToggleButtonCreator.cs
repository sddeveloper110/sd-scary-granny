using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class PanelToggleButtonCreator
{
    private const string MENU_PATH = "GameObject/Software District/Panel Button";
    private const string PARENT_CANVAS_NAME = "Canvas";

    [MenuItem(MENU_PATH, false, 10)]
    private static void CreatePanelHandlingButton()
    {
        // 1. Ensure Canvas exists
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject(PARENT_CANVAS_NAME, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");
        }

        // 2. Create Button
        GameObject buttonGO = new GameObject("PanelButton");
        buttonGO.transform.SetParent(canvas.transform, false);

        RectTransform rect = buttonGO.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(300, 100);
        rect.anchoredPosition = Vector2.zero;

        Image img = buttonGO.AddComponent<Image>();
        img.color = new Color(0.2f, 0.5f, 0.6f, 1f);

        Button btn = buttonGO.AddComponent<Button>();

        // ✅ Add your toggle script if it exists
        PanelToggleButton toggleScript = buttonGO.AddComponent<PanelToggleButton>();

        // 3. Add Text
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        Text uiText = textGO.AddComponent<Text>();
        uiText.text = "Button";
        uiText.alignment = TextAnchor.MiddleCenter;

        // ✅ Optional: Automatically assign UI text reference if your PanelToggleButton expects it
        // Example (uncomment if applicable):
        // toggleScript.buttonText = uiText;

        Undo.RegisterCreatedObjectUndo(buttonGO, "Create Panel Button");
        Selection.activeGameObject = buttonGO;

        Debug.Log("Created Panel Button with PanelToggleButton script attached.");
    }
}
