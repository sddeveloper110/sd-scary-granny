#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(UiFeelMaster))]
public class UIFeelMasterEditor : Editor
{
    private bool showScaleSettings = true;
    private bool showRotateSettings = true;
    private bool showShakeSettings = true;
    private bool showShineSettings = true;
    private bool showRandomImageSettings = true;
    private bool showPopSettings = true;
    private bool showSlideSettings = true;

    public override void OnInspectorGUI()
    {
        UiFeelMaster script = (UiFeelMaster)target;

        // Title
        EditorGUILayout.Space(10);
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
        titleStyle.fontSize = 16;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        EditorGUILayout.LabelField("UI Feel Master", titleStyle);
        EditorGUILayout.Space(5);

        EditorGUILayout.HelpBox("Enable effects that loop continuously during Update, or OnEnable effects that trigger when the object is enabled.", MessageType.Info);
        EditorGUILayout.Space(10);

        // Draw default toggles section
        DrawPropertiesExcluding(serializedObject,
            "m_Script",
            "scaleAmount", "scaleDuration", "scaleWait",
            "rotateStrength", "rotateFriction", "rotateWait",
            "shakeStrength", "shakeSpeed", "shakeDuration", "shakeWait",
            "shineObject", "shineSpeed", "shineWait",
            "randomImages",
            "popDuration",
            "appearFrom", "slideDuration", "offsetDistance"
        );

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // CONTINUOUS EFFECTS SECTION
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
        headerStyle.fontSize = 13;
        EditorGUILayout.LabelField("CONTINUOUS EFFECTS (Update Loop)", headerStyle);
        EditorGUILayout.Space(5);

        // ===== SCALE EFFECT =====
        if (script.useScale)
        {
            showScaleSettings = EditorGUILayout.Foldout(showScaleSettings, "Scale Settings", true);
            if (showScaleSettings)
            {
                EditorGUI.indentLevel++;
                script.scaleAmount = EditorGUILayout.Slider("Scale Amount", script.scaleAmount, 1.0f, 2.0f);
                script.scaleDuration = EditorGUILayout.Slider("Duration", script.scaleDuration, 0.1f, 2.0f);
                script.scaleWait = EditorGUILayout.Slider("Wait Time", script.scaleWait, 1.0f, 10.0f);

                if (GUILayout.Button("▶ Test Scale"))
                {
                    script.TestScale();
                }
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(5);
            }
        }

        // ===== ROTATE EFFECT =====
        if (script.useRotate)
        {
            showRotateSettings = EditorGUILayout.Foldout(showRotateSettings, "Rotate Settings", true);
            if (showRotateSettings)
            {
                EditorGUI.indentLevel++;
                script.rotateStrength = EditorGUILayout.Slider("Strength", script.rotateStrength, 100f, 2000f);
                script.rotateFriction = EditorGUILayout.Slider("Friction", script.rotateFriction, 1.0f, 10.0f);
                script.rotateWait = EditorGUILayout.Slider("Wait Time", script.rotateWait, 1.0f, 10.0f);

                if (GUILayout.Button("▶ Test Rotate"))
                {
                    script.TestRotate();
                }
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(5);
            }
        }

        // ===== SHAKE EFFECT =====
        if (script.useShake)
        {
            showShakeSettings = EditorGUILayout.Foldout(showShakeSettings, "Shake Settings", true);
            if (showShakeSettings)
            {
                EditorGUI.indentLevel++;
                script.shakeStrength = EditorGUILayout.Slider("Strength", script.shakeStrength, 1.0f, 20.0f);
                script.shakeSpeed = EditorGUILayout.Slider("Speed", script.shakeSpeed, 10.0f, 100.0f);
                script.shakeDuration = EditorGUILayout.Slider("Duration", script.shakeDuration, 0.1f, 2.0f);
                script.shakeWait = EditorGUILayout.Slider("Wait Time", script.shakeWait, 1.0f, 10.0f);

                if (GUILayout.Button("▶ Test Shake"))
                {
                    script.TestShake();
                }
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(5);
            }
        }

        // ===== SHINE EFFECT =====
        if (script.useShine)
        {
            showShineSettings = EditorGUILayout.Foldout(showShineSettings, "Shine Settings", true);
            if (showShineSettings)
            {
                EditorGUI.indentLevel++;
                script.shineObject = (RectTransform)EditorGUILayout.ObjectField("Shine Object", script.shineObject, typeof(RectTransform), true);
                script.shineSpeed = EditorGUILayout.Slider("Speed", script.shineSpeed, 100f, 1000f);
                script.shineWait = EditorGUILayout.Slider("Wait Time", script.shineWait, 1.0f, 10.0f);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(5);
            }
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // ONENABLE EFFECTS SECTION
        EditorGUILayout.LabelField("ONENABLE EFFECTS (When GameObject Enables)", headerStyle);
        EditorGUILayout.Space(5);

        // ===== RANDOM IMAGE =====
        if (script.useRandomImage)
        {
            showRandomImageSettings = EditorGUILayout.Foldout(showRandomImageSettings, "Random Image Settings", true);
            if (showRandomImageSettings)
            {
                EditorGUI.indentLevel++;

                SerializedProperty randomImagesProp = serializedObject.FindProperty("randomImages");
                EditorGUILayout.PropertyField(randomImagesProp, new GUIContent("Random Images"), true);

                EditorGUI.indentLevel--;
                EditorGUILayout.Space(5);
            }
        }

        // ===== POP EFFECT =====
        if (script.usePopEffect)
        {
            showPopSettings = EditorGUILayout.Foldout(showPopSettings, "Pop Effect Settings", true);
            if (showPopSettings)
            {
                EditorGUI.indentLevel++;
                script.popDuration = EditorGUILayout.Slider("Duration", script.popDuration, 0.1f, 1.0f);

                if (GUILayout.Button("▶ Test Pop"))
                {
                    script.TestPop();
                }
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(5);
            }
        }

        // ===== SLIDE EFFECT =====
        if (script.useSlideEffect)
        {
            showSlideSettings = EditorGUILayout.Foldout(showSlideSettings, "Slide Effect Settings", true);
            if (showSlideSettings)
            {
                EditorGUI.indentLevel++;
                script.appearFrom = (UiFeelMaster.Direction)EditorGUILayout.EnumPopup("Appear From", script.appearFrom);
                script.slideDuration = EditorGUILayout.Slider("Duration", script.slideDuration, 0.1f, 2.0f);
                script.offsetDistance = EditorGUILayout.Slider("Offset Distance", script.offsetDistance, 100f, 2000f);

                if (GUILayout.Button("▶ Test Slide"))
                {
                    script.TestSlide();
                }
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(5);
            }
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // Utility Buttons
        EditorGUILayout.Space(5);
        if (GUILayout.Button("Reset All Timers", GUILayout.Height(30)))
        {
            script.ResetTimers();
        }

        // Apply changes
        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(script);
        }
    }
}
#endif