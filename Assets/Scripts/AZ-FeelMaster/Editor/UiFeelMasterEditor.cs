using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(UiFeelMaster))]
public class UiFeelMasterEditor : Editor
{
    private int toolbarTab;
    private string[] toolbarLabels = { "Base Settings", "Continuous", "OnEnable", "Testing" };

    // Logo slot
    public Texture2D userLogo;

    public override void OnInspectorGUI()
    {
        UiFeelMaster script = (UiFeelMaster)target;

        // --- Logo Section ---
        EditorGUILayout.Space(10);
        userLogo = (Texture2D)EditorGUILayout.ObjectField("My Logo", userLogo, typeof(Texture2D), false, GUILayout.Height(20));

        if (userLogo != null)
        {
            Rect logoRect = GUILayoutUtility.GetRect(Screen.width, 100);
            GUI.DrawTexture(logoRect, userLogo, ScaleMode.ScaleToFit);
            
        }
        else
        {
            EditorGUILayout.HelpBox("Add your Logo texture above to brand this tool!", MessageType.Info);
        }

        EditorGUILayout.Space(10);

        // --- Professional Header ---
        //GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
        //headerStyle.fontSize = 18;
        //headerStyle.alignment = TextAnchor.MiddleCenter;
        //headerStyle.normal.textColor = new Color(0.1f, 0.7f, 1f);
        //EditorGUILayout.LabelField("UI FEEL MASTER", headerStyle);
        //EditorGUILayout.Space(5);

        // --- Tabs / Toolbar ---
        toolbarTab = GUILayout.Toolbar(toolbarTab, toolbarLabels);
        EditorGUILayout.Space(10);

        Undo.RecordObject(script, "UI Feel Master Change");

        switch (toolbarTab)
        {
            case 0: // Base Toggles
                DrawBaseSettings(script);
                break;
            case 1: // Continuous Settings
                DrawContinuousSettings(script);
                break;
            case 2: // OnEnable Settings
                DrawOnEnableSettings(script);
                break;
            case 3: // Test Buttons
                DrawTestingSection(script);
                break;
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(script);
        }
    }

    private void DrawBaseSettings(UiFeelMaster script)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Main Toggles", EditorStyles.boldLabel);
        script.useScale = EditorGUILayout.ToggleLeft(" Use Pulse Scale", script.useScale);
        script.useRotate = EditorGUILayout.ToggleLeft(" Use Impact Rotation", script.useRotate);
        script.useShake = EditorGUILayout.ToggleLeft(" Use Constant Shake", script.useShake);
        script.useShine = EditorGUILayout.ToggleLeft(" Use Shine Overlay", script.useShine);
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Spawn Toggles", EditorStyles.boldLabel);
        script.usePopEffect = EditorGUILayout.ToggleLeft(" Enable Pop-In", script.usePopEffect);
        script.useSlideEffect = EditorGUILayout.ToggleLeft(" Enable Sliding", script.useSlideEffect);
        script.useRandomImage = EditorGUILayout.ToggleLeft(" Enable Random Sprites", script.useRandomImage);
        EditorGUILayout.EndVertical();
    }

    private void DrawContinuousSettings(UiFeelMaster script)
    {
        if (script.useScale)
        {
            DrawHeader("Pulse Scale Configuration");
            script.scaleAmount = EditorGUILayout.Slider("Scale Amount", script.scaleAmount, 0.5f, 2f);
            script.scaleDuration = EditorGUILayout.FloatField("Duration", script.scaleDuration);
            script.scaleWait = EditorGUILayout.FloatField("Wait Time", script.scaleWait);
        }

        if (script.useRotate)
        {
            DrawHeader("Rotation Configuration");
            script.rotateStrength = EditorGUILayout.FloatField("Rotate Power", script.rotateStrength);
            script.rotateFriction = EditorGUILayout.Slider("Friction", script.rotateFriction, 0.1f, 10f);
            script.rotateWait = EditorGUILayout.FloatField("Wait Time", script.rotateWait);
        }

        if (script.useShake)
        {
            DrawHeader("Shake Configuration");
            script.shakeStrength = EditorGUILayout.Slider("Shake Strength", script.shakeStrength, 0.1f, 50f);
            script.shakeSpeed = EditorGUILayout.Slider("Shake Speed", script.shakeSpeed, 10f, 100f);
            script.shakeWait = EditorGUILayout.FloatField("Wait Time", script.shakeWait);
        }

        if (script.useShine)
        {
            DrawHeader("Shine Configuration");
            script.shineObject = (RectTransform)EditorGUILayout.ObjectField("Shine UI Object", script.shineObject, typeof(RectTransform), true);
            script.shineSpeed = EditorGUILayout.FloatField("Shine Speed", script.shineSpeed);
            script.shineWait = EditorGUILayout.FloatField("Shine Wait", script.shineWait);
        }
    }

    private void DrawOnEnableSettings(UiFeelMaster script)
    {
        if (script.usePopEffect)
        {
            DrawHeader("Pop-In Settings");
            script.popDuration = EditorGUILayout.Slider("Pop Duration", script.popDuration, 0.1f, 2f);
        }

        if (script.useSlideEffect)
        {
            DrawHeader("Slide Settings");
            script.appearFrom = (UiFeelMaster.Direction)EditorGUILayout.EnumPopup("Direction", script.appearFrom);
            script.slideDuration = EditorGUILayout.FloatField("Slide Duration", script.slideDuration);
            script.offsetDistance = EditorGUILayout.FloatField("Travel Distance", script.offsetDistance);
        }

        if (script.useRandomImage)
        {
            DrawHeader("Random Sprite List");
            SerializedObject so = new SerializedObject(script);
            SerializedProperty sp = so.FindProperty("randomImages");
            EditorGUILayout.PropertyField(sp, true);
            so.ApplyModifiedProperties();
        }
    }

    private void DrawTestingSection(UiFeelMaster script)
    {
        EditorGUILayout.HelpBox("Only works while Game is Running!", MessageType.Warning);
        if (GUILayout.Button("Test Pulse", GUILayout.Height(30))) script.TestScale();
        if (GUILayout.Button("Test Rotation", GUILayout.Height(30))) script.TestRotate();
        if (GUILayout.Button("Test Shake", GUILayout.Height(30))) script.TestShake();
        if (GUILayout.Button("Test Pop-In", GUILayout.Height(30))) script.TestPop();
        if (GUILayout.Button("Test Slide", GUILayout.Height(30))) script.TestSlide();
    }

    private void DrawHeader(string label)
    {
        EditorGUILayout.Space(5);
        GUIStyle s = new GUIStyle(EditorStyles.label);
        s.normal.textColor = Color.yellow;
        s.fontStyle = FontStyle.Bold;
        EditorGUILayout.LabelField(label, s);
    }
}