using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class PanelWithGridCreator : Editor
{
    private const string MENU_PATH = "GameObject/Software District/Grid Panel";
    private const string PARENT_CANVAS_NAME = "Canvas";

    [MenuItem(MENU_PATH, false, 20)] // Order 20 to appear after other panel/button creators
    static void CreateGridPanel()
    {
        // 1. Ensure a Canvas exists in the scene, or create one
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject(PARENT_CANVAS_NAME, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // Common default for UI
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");
        }

        // 2. Create the main UIPanel GameObject
        GameObject mainPanelGO = new GameObject("NewGridPanel");
        RectTransform mainRectTransform = mainPanelGO.AddComponent<RectTransform>();
        UIPanel uiPanel = mainPanelGO.AddComponent<UIPanel>(); // Main object is UIPanel itself

        // Set parent to Canvas and stretch to fill
        mainPanelGO.transform.SetParent(canvas.transform, false);
        mainRectTransform.anchorMin = Vector2.zero;
        mainRectTransform.anchorMax = Vector2.one;
        mainRectTransform.pivot = new Vector2(0.5f, 0.5f);
        mainRectTransform.anchoredPosition = Vector2.zero;
        mainRectTransform.sizeDelta = Vector2.zero; // Stretch to fill parent
        mainRectTransform.localScale = Vector3.one;
        mainRectTransform.localRotation = Quaternion.identity;

        // Initially deactivate the main panel
        mainPanelGO.SetActive(false);

        // 3. Create the "ChildBackground" (black, low transparency, full stretch)
        GameObject childBackgroundGO = new GameObject("ChildBackground");
        RectTransform childBackgroundRect = childBackgroundGO.AddComponent<RectTransform>();
        Image childBackgroundImage = childBackgroundGO.AddComponent<Image>();

        childBackgroundGO.transform.SetParent(mainPanelGO.transform, false);
        childBackgroundRect.anchorMin = Vector2.zero;
        childBackgroundRect.anchorMax = Vector2.one;
        childBackgroundRect.pivot = new Vector2(0.5f, 0.5f);
        childBackgroundRect.anchoredPosition = Vector2.zero;
        childBackgroundRect.sizeDelta = Vector2.zero; // Stretch to fill parent
        childBackgroundRect.localScale = Vector3.one;
        childBackgroundRect.localRotation = Quaternion.identity;
        childBackgroundImage.color = new Color(0f, 0f, 0f, 0.1f); // Black with 10% alpha
        childBackgroundImage.raycastTarget = true;

        // 4. Create the "Heading" Text
        GameObject headingTextGO = new GameObject("HeadingText");
        RectTransform headingTextRect = headingTextGO.AddComponent<RectTransform>();
        Text headingUIText = headingTextGO.AddComponent<Text>();

        headingTextGO.transform.SetParent(mainPanelGO.transform, false);

        // Position top-center
        headingTextRect.anchorMin = new Vector2(0.5f, 1f);
        headingTextRect.anchorMax = new Vector2(0.5f, 1f);
        headingTextRect.pivot = new Vector2(0.5f, 1f);
        headingTextRect.anchoredPosition = new Vector2(0, -40); // 40 units below the top-center
        headingTextRect.sizeDelta = new Vector2(500, 80); // Width 500, Height 80
        headingTextRect.localScale = Vector3.one;
        headingTextRect.localRotation = Quaternion.identity;

        headingUIText.text = "Heading";
        headingUIText.alignment = TextAnchor.MiddleCenter;
        headingUIText.color = Color.white;
        headingUIText.fontSize = 60;
        headingUIText.resizeTextForBestFit = true;
        headingUIText.resizeTextMinSize = 20;
        headingUIText.resizeTextMaxSize = 80;

        // 5. Create the Back Button
        GameObject backButtonGO = new GameObject("BackButton");
        RectTransform backButtonRect = backButtonGO.AddComponent<RectTransform>();
        Image backButtonImage = backButtonGO.AddComponent<Image>();
        Button unityBackButton = backButtonGO.AddComponent<Button>();
        PanelToggleButton toggleBackButton = backButtonGO.AddComponent<PanelToggleButton>();

        backButtonGO.transform.SetParent(mainPanelGO.transform, false);

        // Position and size for top-left
        backButtonRect.anchorMin = new Vector2(0f, 1f); // Top-left anchor
        backButtonRect.anchorMax = new Vector2(0f, 1f); // Top-left anchor
        backButtonRect.pivot = new Vector2(0f, 1f); // Top-left pivot
        backButtonRect.anchoredPosition = new Vector2(150, -37.5f); // Half size offset for centering on pivot
        backButtonRect.sizeDelta = new Vector2(300, 75);
        backButtonRect.localScale = Vector3.one;
        backButtonRect.localRotation = Quaternion.identity;

        backButtonImage.color = new Color(0.8f, 0.2f, 0.2f, 1f); // Reddish color for Close/Back


        // Hook up the Button's OnClick to the PanelToggleButton's OnButtonClicked
        UnityEditor.Events.UnityEventTools.AddPersistentListener(unityBackButton.onClick, toggleBackButton.OnButtonClicked);

        // Create child Text for Back Button
        GameObject backButtonTextGO = new GameObject("Text");
        RectTransform backButtonTextRect = backButtonTextGO.AddComponent<RectTransform>();
        Text backButtonUIText = backButtonTextGO.AddComponent<Text>();

        backButtonTextGO.transform.SetParent(backButtonGO.transform, false);
        backButtonTextRect.anchorMin = Vector2.zero;
        backButtonTextRect.anchorMax = Vector2.one;
        backButtonTextRect.pivot = new Vector2(0.5f, 0.5f);
        backButtonTextRect.anchoredPosition = Vector2.zero;
        backButtonTextRect.sizeDelta = Vector2.zero; // Stretch to fill parent
        backButtonTextRect.localScale = Vector3.one;
        backButtonTextRect.localRotation = Quaternion.identity;

        backButtonUIText.text = "Close";
        backButtonUIText.alignment = TextAnchor.MiddleCenter;
        backButtonUIText.color = Color.white;
        backButtonUIText.fontSize = 50; // Set a default font size
        backButtonUIText.resizeTextForBestFit = true; // Enable Best Fit
        backButtonUIText.resizeTextMinSize = 10;
        backButtonUIText.resizeTextMaxSize = 60;
        // 6. Create the "Panel" (white, full alpha, with margins) - This will be the content area for ScrollRect
        GameObject panelContentGO = new GameObject("Panel");
        RectTransform panelContentRect = panelContentGO.AddComponent<RectTransform>();
        Image panelContentImage = panelContentGO.AddComponent<Image>();

        panelContentGO.transform.SetParent(mainPanelGO.transform, false);
        panelContentRect.anchorMin = Vector2.zero;
        panelContentRect.anchorMax = Vector2.one;
        panelContentRect.pivot = new Vector2(0.5f, 0.5f);
        panelContentRect.anchoredPosition = Vector2.zero;
        panelContentRect.offsetMin = new Vector2(100, 100); // Left and Bottom margins

        // ADJUSTED: Top margin to clear both BackButton and HeadingText
        // BackButton lowest point is at Y = -112.5 from top of mainPanel.
        // HeadingText lowest point is at Y = -40 (top) - 80 (height) = -120 from top.
        // Let's use the lower of the two, plus some padding (e.g., 5 units for safety).
        // Max(112.5, 120) + 5 = 125
        panelContentRect.offsetMax = new Vector2(-100, -125); // Right and Top margins (ADJUSTED)

        panelContentRect.localScale = Vector3.one;
        panelContentRect.localRotation = Quaternion.identity;
        panelContentImage.color = new Color(1f, 1f, 1f, 1f); // White with 100% alpha (full)
        panelContentImage.raycastTarget = true;


        // 7. Add ScrollRect and GridLayoutGroup to the "Panel"
        ScrollRect scrollRect = panelContentGO.AddComponent<ScrollRect>();

        // Create Viewport
        GameObject viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        RectTransform viewportRect = viewportGO.GetComponent<RectTransform>();
        Image viewportImage = viewportGO.GetComponent<Image>();

        viewportGO.transform.SetParent(panelContentGO.transform, false);
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.pivot = Vector2.zero;
        viewportRect.anchoredPosition = Vector2.zero;
        viewportRect.sizeDelta = Vector2.zero;
        viewportRect.localScale = Vector3.one;
        viewportRect.localRotation = Quaternion.identity;

        viewportImage.color = Color.clear; // Transparent image for masking
        viewportImage.raycastTarget = false; // Don't block raycasts

        // Create Content
        GameObject contentGO = new GameObject("Content", typeof(RectTransform));
        RectTransform contentRect = contentGO.GetComponent<RectTransform>();

        contentGO.transform.SetParent(viewportGO.transform, false); // Content is child of Viewport
        contentRect.anchorMin = new Vector2(0f, 1f); // Top-left aligned
        contentRect.anchorMax = new Vector2(1f, 1f); // Stretch width, top aligned
        contentRect.pivot = new Vector2(0.5f, 1f); // Center X, Top Y
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0, 0); // Height will be driven by content via ContentSizeFitter

        // Link ScrollRect to Viewport and Content
        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;

        // Configure ScrollRect
        scrollRect.horizontal = false; // Only vertical scrolling
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Elastic; // Smooth scrolling
        scrollRect.elasticity = 0.1f;
        scrollRect.scrollSensitivity = 20f;

        // Add GridLayoutGroup to the 'Content' GameObject
        GridLayoutGroup gridLayoutGroup = contentGO.AddComponent<GridLayoutGroup>();

        // Configure GridLayoutGroup
        gridLayoutGroup.cellSize = new Vector2(200, 200); // Example size for level buttons
        gridLayoutGroup.spacing = new Vector2(20, 20); // Spacing between buttons
        gridLayoutGroup.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayoutGroup.childAlignment = TextAnchor.UpperCenter; // Align grid within content
        gridLayoutGroup.constraint = GridLayoutGroup.Constraint.Flexible; // Flexible columns/rows
        gridLayoutGroup.constraintCount = 0; // Not fixed count, flexible
        gridLayoutGroup.startAxis = GridLayoutGroup.Axis.Horizontal; // Fill horizontally first

        // Set Content size fitter to expand height based on children
        contentGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;


        // 8. Add temporary child objects for visualization
        Color[] colors = { Color.red, Color.green, Color.blue, Color.yellow, Color.cyan, Color.magenta, Color.gray, Color.white, Color.black };
        for (int i = 0; i < 15; i++) // Create 15 temporary items
        {
            GameObject tempItemGO = new GameObject($"TempObject_{i + 1}");
            RectTransform tempItemRect = tempItemGO.AddComponent<RectTransform>();
            Image tempItemImage = tempItemGO.AddComponent<Image>();
            Text tempItemText = tempItemGO.AddComponent<Text>(); // Add Text for identification

            tempItemGO.transform.SetParent(contentGO.transform, false); // Parent to Content
            tempItemRect.localScale = Vector3.one;
            tempItemRect.localRotation = Quaternion.identity;

            tempItemImage.color = colors[i % colors.Length]; // Cycle through colors

            tempItemText.text = $"Item {i + 1}";
            tempItemText.alignment = TextAnchor.MiddleCenter;
            tempItemText.color = Color.black; // Make text visible on colored background
            tempItemText.fontSize = 30; // Set a default font size for temp text
            tempItemText.resizeTextForBestFit = true;
            tempItemText.resizeTextMinSize = 10;
            tempItemText.resizeTextMaxSize = 40;
            
            tempItemText.rectTransform.anchorMin = Vector2.zero;
            tempItemText.rectTransform.anchorMax = Vector2.one;
            tempItemText.rectTransform.sizeDelta = Vector2.zero;

            Undo.RegisterCreatedObjectUndo(tempItemGO, "Create Temp Object");
        }


        // 9. Register remaining created objects/components with Undo system
        Undo.RegisterCreatedObjectUndo(headingTextGO, "Create Grid Panel Heading Text"); // Register new heading
        Undo.RegisterCreatedObjectUndo(mainPanelGO, "Create Grid Panel");
        Undo.RegisterCreatedObjectUndo(childBackgroundGO, "Create Grid Panel Child Background");
        Undo.RegisterCreatedObjectUndo(panelContentGO, "Create Grid Panel Content");
        Undo.RegisterCreatedObjectUndo(backButtonGO, "Create Grid Panel Back Button");
        Undo.RegisterCreatedObjectUndo(backButtonTextGO, "Create Grid Panel Back Button Text");
        Undo.RegisterCreatedObjectUndo(viewportGO, "Create Grid Panel ScrollRect Viewport");
        Undo.RegisterCreatedObjectUndo(contentGO, "Create Grid Panel ScrollRect Content");

        // Select the newly created main object in the Hierarchy
        Selection.activeGameObject = mainPanelGO;

        Debug.Log("Grid Panel created: " + mainPanelGO.name + " with back button, heading, and scrollable grid.");
    }

    // Validation method for the menu item
    [MenuItem(MENU_PATH, true)]
    static bool ValidateCreateGridPanel()
    {
        return (FindObjectOfType<Canvas>() == null ||
                (Selection.activeGameObject != null && Selection.activeGameObject.GetComponentInParent<Canvas>() != null));
    }
}