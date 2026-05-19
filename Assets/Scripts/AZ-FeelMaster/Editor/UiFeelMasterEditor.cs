using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(UiFeelMaster))]
public class UiFeelMasterEditor : Editor
{
    public Texture2D userLogo;

    public override void OnInspectorGUI()
    {
        UiFeelMaster script = (UiFeelMaster)target;
        Undo.RecordObject(script, "UI Feel Master Change");

        // --- Logo Section ---
        if (userLogo != null)
        {
            Rect logoRect = GUILayoutUtility.GetRect(Screen.width, 60);
            GUI.DrawTexture(logoRect, userLogo, ScaleMode.ScaleToFit);
        }

        EditorGUILayout.Space(5);

        // --- INTERACTION SECTION ---
        DrawSectionHeader("INTERACTION (Button Features)");
        script.playSoundOnClick = EditorGUILayout.ToggleLeft(" Play Sound On Click", script.playSoundOnClick, GetBoldToggle());
        script.popOnClick = EditorGUILayout.ToggleLeft(" Pop Animation On Click", script.popOnClick, GetBoldToggle());

        EditorGUILayout.Space(10);

        // --- CONTINUOUS EFFECTS ---
        DrawSectionHeader("CONTINUOUS EFFECTS");

        // Scale
        script.useScale = EditorGUILayout.BeginToggleGroup(" Pulse Scale", script.useScale);
        if (script.useScale)
        {
            script.scaleAmount = EditorGUILayout.Slider("   Amount", script.scaleAmount, 0.5f, 2f);
            script.scaleDuration = EditorGUILayout.FloatField("   Duration", script.scaleDuration);
            script.scaleWait = EditorGUILayout.FloatField("   Wait Time", script.scaleWait);
        }
        EditorGUILayout.EndToggleGroup();

        // Rotate
        script.useRotate = EditorGUILayout.BeginToggleGroup(" Impact Rotation", script.useRotate);
        if (script.useRotate)
        {
            script.rotateStrength = EditorGUILayout.FloatField("   Power", script.rotateStrength);
            script.rotateFriction = EditorGUILayout.Slider("   Friction", script.rotateFriction, 0.1f, 10f);
            script.rotateWait = EditorGUILayout.FloatField("   Wait Time", script.rotateWait);
        }
        EditorGUILayout.EndToggleGroup();

        // Shake
        script.useShake = EditorGUILayout.BeginToggleGroup(" Constant Shake", script.useShake);
        if (script.useShake)
        {
            script.shakeStrength = EditorGUILayout.Slider("   Strength", script.shakeStrength, 0.1f, 50f);
            script.shakeSpeed = EditorGUILayout.Slider("   Speed", script.shakeSpeed, 10f, 100f);
            script.shakeWait = EditorGUILayout.FloatField("   Wait Time", script.shakeWait);
        }
        EditorGUILayout.EndToggleGroup();

        // Shine
        script.useShine = EditorGUILayout.BeginToggleGroup(" Shine Overlay", script.useShine);
        if (script.useShine)
        {
            script.shineObject = (RectTransform)EditorGUILayout.ObjectField("   Shine Object", script.shineObject, typeof(RectTransform), true);
            script.shineSpeed = EditorGUILayout.FloatField("   Speed", script.shineSpeed);
            script.shineWait = EditorGUILayout.FloatField("   Wait", script.shineWait);
        }
        EditorGUILayout.EndToggleGroup();

        EditorGUILayout.Space(10);

        // --- ON ENABLE SECTION ---
        DrawSectionHeader("SPAWN EFFECTS (On Enable)");

        script.usePopEffect = EditorGUILayout.BeginToggleGroup(" Pop-In", script.usePopEffect);
        if (script.usePopEffect) script.popDuration = EditorGUILayout.Slider("   Duration", script.popDuration, 0.1f, 2f);
        EditorGUILayout.EndToggleGroup();

        script.useSlideEffect = EditorGUILayout.BeginToggleGroup(" Sliding", script.useSlideEffect);
        if (script.useSlideEffect)
        {
            script.appearFrom = (UiFeelMaster.Direction)EditorGUILayout.EnumPopup("   From", script.appearFrom);
            script.slideDuration = EditorGUILayout.FloatField("   Duration", script.slideDuration);
            script.offsetDistance = EditorGUILayout.FloatField("   Distance", script.offsetDistance);
        }
        EditorGUILayout.EndToggleGroup();

        script.useRandomImage = EditorGUILayout.BeginToggleGroup(" Random Sprites", script.useRandomImage);
        if (script.useRandomImage)
        {
            SerializedObject so = new SerializedObject(script);
            SerializedProperty sp = so.FindProperty("randomImages");
            EditorGUILayout.PropertyField(sp, true);
            so.ApplyModifiedProperties();
        }
        EditorGUILayout.EndToggleGroup();

        // --- TESTING ---
        EditorGUILayout.Space(15);
        if (Application.isPlaying)
        {
            DrawSectionHeader("RUNTIME TESTING");
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Pulse")) script.TestScale();
            if (GUILayout.Button("Rotate")) script.TestRotate();
            if (GUILayout.Button("Shake")) script.TestShake();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Pop-In")) script.TestPop();
            if (GUILayout.Button("Slide")) script.TestSlide();
            EditorGUILayout.EndHorizontal();
        }

        if (GUI.changed) EditorUtility.SetDirty(script);
    }

    private void DrawSectionHeader(string label)
    {
        EditorGUILayout.Space(5);
        GUIStyle s = new GUIStyle(EditorStyles.toolbarButton);
        s.fixedHeight = 20;
        s.alignment = TextAnchor.MiddleLeft;
        s.fontStyle = FontStyle.Bold;
        s.normal.textColor = new Color(0.1f, 0.8f, 1f);
        GUILayout.Label(label, s);
    }

    private GUIStyle GetBoldToggle()
    {
        GUIStyle s = new GUIStyle(EditorStyles.toggle);
        s.fontStyle = FontStyle.Bold;
        return s;
    }
}