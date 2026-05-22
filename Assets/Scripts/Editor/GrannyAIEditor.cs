#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
//  GrannyAIEditor  —  Custom Inspector for GrannyAI
//
//  FILE LOCATION: Must be inside an "Editor" folder anywhere in your project.
//  e.g.  Assets/Scripts/GrannyAI/Editor/GrannyAIEditor.cs
//
//  Features:
//  • Tutorial panel (collapsible step-by-step guide)
//  • Setup Wizard buttons: Auto-find rooms, Create room marker, Validate graph
//  • Live runtime dashboard: current state, fear score, target room
//  • Section-collapsible inspector
//  • Colour-coded validation panel
// ─────────────────────────────────────────────────────────────────────────────

[CustomEditor(typeof(GrannyAI))]
public class GrannyAIEditor : Editor
{
    // ── Foldout states ────────────────────────────────────────────────────────
    private bool _showTutorial      = false;
    private bool _showSetupWizard   = true;
    private bool _showRoomGraph     = true;
    private bool _showPatrol        = false;
    private bool _showMovement      = false;
    private bool _showBelief        = false;
    private bool _showSound         = false;
    private bool _showVision        = false;
    private bool _showProximity     = false;
    private bool _showAttack        = false;
    private bool _showNearMiss      = false;
    private bool _showTension       = false;
    private bool _showFalseScares   = false;
    private bool _showAnimations    = false;
    private bool _showAmbientVoices = false;
    private bool _showValidation    = false;

    // ── Validation cache ─────────────────────────────────────────────────────
    private List<string> _validationIssues = new();
    private bool         _validationRan    = false;

    // ── Tutorial page ────────────────────────────────────────────────────────
    private int _tutorialPage = 0;
    private readonly string[] _tutorialTitles =
    {
        "Step 1 — Create Room Assets",
        "Step 2 — Place Room Markers",
        "Step 3 — Wire Connections",
        "Step 4 — Setup Granny",
        "Step 5 — Wire Sounds",
        "Step 6 — Test & Tune"
    };

    private readonly string[] _tutorialTexts =
    {
        // 0
        "For EACH room in your house (Kitchen, Bedroom, Hallway, etc.):\n\n" +
        "1. Right-click in the Project window.\n" +
        "2. Choose  Create → GrannyAI → Room Node.\n" +
        "3. Name it clearly: Room_Kitchen, Room_Bedroom1, Room_Hallway_Ground.\n" +
        "4. Set floorIndex (0 = ground floor, 1 = upstairs, etc.).\n" +
        "5. Tick isHidingSpotRoom if the room has wardrobes or under-bed spots.\n" +
        "6. Tick isExitRoom if it contains a main door or window escape.\n\n" +
        "You do NOT wire connections yet — do that after placing scene markers.",

        // 1
        "For EACH room asset you just created:\n\n" +
        "1. In the Scene view, navigate to the centre of that room.\n" +
        "2. Click 'Create Room Marker Here' (Setup Wizard below).\n" +
        "   — This creates an empty GameObject with a RoomNodeMarker component.\n" +
        "3. In the RoomNodeMarker Inspector, drag the matching RoomNodeData asset.\n" +
        "4. Name the GameObject the same as the room for sanity.\n\n" +
        "The marker's world position is where Granny will navigate TO when targeting\n" +
        "that room. Centre of the floor is ideal.",

        // 2
        "Open each RoomNodeData asset and fill in its Connections list:\n\n" +
        "• Add one entry per doorway or passage leading out of the room.\n" +
        "• Set 'target' to the adjacent RoomNodeData.\n" +
        "• traversalCost:  1 = open doorway   3 = staircase   5 = locked/window\n" +
        "• isDoorOpen: true by default. Set to false for locked doors at start.\n" +
        "• isStaircase: tick for any connection that crosses floor levels.\n\n" +
        "Connections do NOT need to be bidirectional — GrannyAI reads both directions\n" +
        "during BFS, but adding both keeps the graph explicit and easier to maintain.\n\n" +
        "Use the gizmo lines in the scene to verify everything is wired correctly.",

        // 3
        "On the Granny GameObject:\n\n" +
        "1. Click 'Auto-Find Room Markers' — this scans the scene and fills allRooms.\n" +
        "2. Assign startRoom (the room Granny spawns in).\n" +
        "3. Fill patrolRoute with 4–8 rooms in the order you want her to walk.\n" +
        "4. Drag the Animator component into the Animations section.\n" +
        "5. Assign your NavMeshAgent (should already be on the same object).\n" +
        "6. Set visionBlockMask to your Wall + Door layers.\n" +
        "7. Click 'Validate Room Graph' — fix any red issues before hitting Play.",

        // 4
        "Wherever the player makes noise, call:\n\n" +
        "    GrannyAI.Instance.HearSound(transform.position, isLoud);\n\n" +
        "• isLoud = true  → trap springs, object dropped, door slammed\n" +
        "• isLoud = false → normal footstep, crouch walk\n\n" +
        "When a door opens or closes at runtime:\n\n" +
        "    // Find the connection and flip isDoorOpen\n" +
        "    conn.isDoorOpen = true; // or false\n\n" +
        "Subscribe to events from other managers:\n\n" +
        "    GrannyAI.OnFearScoreChanged += score => postProcess.vignette = score;\n" +
        "    GrannyAI.OnAttackPlayer     += HandlePlayerDeath;\n" +
        "    GrannyAI.OnStateChanged     += state => Debug.Log(state);",

        // 5
        "Hit Play and select the Granny GameObject. The Inspector shows:\n\n" +
        "• LIVE RUNTIME DASHBOARD — current state, fear score, Granny room, target room.\n" +
        "• Belief scores visualised as green→red spheres above each room in Scene view.\n" +
        "• Cyan arc = vision cone. Red disc = attack range. Orange disc = near-miss range.\n" +
        "• Connection lines: green = open door, orange = staircase, dark red = closed door.\n\n" +
        "Tuning tips:\n" +
        "• If Granny finds the player too fast → lower soundBeliefLoud or increase beliefDecayRate.\n" +
        "• If near-misses never fire → widen the belief window or increase nearMissTriggerDist.\n" +
        "• If she climbs stairs too easily → raise crossFloorBeliefGate toward 0.9.\n" +
        "• If she feels robotic → increase falseScareProbability and patrolWaitMax."
    };

    // ── Styles (created lazily) ───────────────────────────────────────────────
    private GUIStyle _boxStyle;
    private GUIStyle _headerStyle;
    private GUIStyle _subHeaderStyle;
    private GUIStyle _tutorialStyle;
    private GUIStyle _dashboardLabelStyle;
    private GUIStyle _dashboardValueStyle;
    private GUIStyle _warningStyle;
    private GUIStyle _errorStyle;
    private GUIStyle _okStyle;
    private bool     _stylesInitialised = false;

    private void InitStyles()
    {
        if (_stylesInitialised) return;

        _boxStyle = new GUIStyle(EditorStyles.helpBox)
            { padding = new RectOffset(8, 8, 8, 8) };

        _headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize  = 13,
            alignment = TextAnchor.MiddleLeft,
            normal    = { textColor = new Color(0.9f, 0.75f, 0.3f) }
        };

        _subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 11,
            normal   = { textColor = new Color(0.75f, 0.9f, 1f) }
        };

        _tutorialStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
            { fontSize = 11, richText = true };

        _dashboardLabelStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            { normal = { textColor = new Color(0.7f, 0.7f, 0.7f) } };

        _dashboardValueStyle = new GUIStyle(EditorStyles.boldLabel)
            { normal = { textColor = Color.white }, fontSize = 12 };

        _warningStyle = new GUIStyle(EditorStyles.label)
            { normal = { textColor = new Color(1f, 0.85f, 0.2f) }, wordWrap = true };

        _errorStyle = new GUIStyle(EditorStyles.label)
            { normal = { textColor = new Color(1f, 0.35f, 0.35f) }, wordWrap = true };

        _okStyle = new GUIStyle(EditorStyles.label)
            { normal = { textColor = new Color(0.4f, 1f, 0.4f) } };

        _stylesInitialised = true;
    }

    // ─────────────────────────────────────────────────────────────────────────
    public override void OnInspectorGUI()
    {
        InitStyles();
        var granny = (GrannyAI)target;

        // ── Header banner ─────────────────────────────────────────────────────
        EditorGUILayout.Space(4);
        DrawBanner();
        EditorGUILayout.Space(4);

        // ── Runtime dashboard (Play mode only) ───────────────────────────────
        if (Application.isPlaying)
        {
            DrawRuntimeDashboard(granny);
            EditorGUILayout.Space(4);
            Repaint(); // Keep dashboard live
        }

        // ── Tutorial panel ────────────────────────────────────────────────────
        DrawTutorialPanel();
        EditorGUILayout.Space(4);

        // ── Setup Wizard ──────────────────────────────────────────────────────
        DrawSetupWizard(granny);
        EditorGUILayout.Space(4);

        // ── Validation ────────────────────────────────────────────────────────
        DrawValidationPanel(granny);
        EditorGUILayout.Space(6);

        // ── Collapsible property sections ────────────────────────────────────
        DrawSection(ref _showRoomGraph,   "🏠  Room Graph",        DrawRoomGraph);
        DrawSection(ref _showPatrol,      "🚶  Patrol",            DrawPatrol);
        DrawSection(ref _showMovement,    "💨  Movement Speeds",   DrawMovement);
        DrawSection(ref _showBelief,      "🧠  Belief System",     DrawBelief);
        DrawSection(ref _showSound,       "🔊  Sound Perception",  DrawSound);
        DrawSection(ref _showVision,      "👁   Vision",           DrawVision);
        DrawSection(ref _showProximity,   "⭕  Proximity Detection", DrawProximity);
        DrawSection(ref _showAttack,      "⚔️   Attack",           DrawAttack);
        DrawSection(ref _showNearMiss,    "😱  Near Miss Horror",  DrawNearMiss);
        DrawSection(ref _showTension,     "📈  Tension / Fear",    DrawTension);
        DrawSection(ref _showFalseScares, "👻  False Scares",      DrawFalseScares);
        DrawSection(ref _showAnimations,  "🎭  Animations",        DrawAnimations);
        DrawSection(ref _showAmbientVoices, "🎧  Ambient Voices",  DrawAmbientVoices);

        EditorGUILayout.Space(8);

        if (GUI.changed) EditorUtility.SetDirty(target);
    }

    // ─────────────────────────────────────────────────────────────────────────
    #region ── Banner ───────────────────────────────────────────────────────

    private void DrawBanner()
    {
        Rect rect = GUILayoutUtility.GetRect(0, 36, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rect, new Color(0.13f, 0.07f, 0.07f));
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 2, rect.width, 2),
                           new Color(0.85f, 0.3f, 0.2f));
        GUI.Label(rect, "  👵  GRANNY AI  —  Horror Controller",
                  new GUIStyle(EditorStyles.boldLabel)
                  {
                      fontSize  = 14,
                      alignment = TextAnchor.MiddleLeft,
                      normal    = { textColor = new Color(1f, 0.82f, 0.7f) }
                  });
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region ── Runtime Dashboard ─────────────────────────────────────────────

    private static readonly Color[] _stateColors =
    {
        new Color(0.4f, 0.4f, 0.4f),   // Idle
        new Color(0.3f, 0.7f, 0.3f),   // Patrol
        new Color(0.9f, 0.85f, 0.2f),  // Curious
        new Color(1f,   0.5f, 0.1f),   // Hunt
        new Color(1f,   0.2f, 0.2f),   // Chase
        new Color(0.8f, 0.1f, 0.8f),   // NearMiss
        new Color(0.9f, 0.1f, 0.1f),   // Attack
        new Color(1f,   0f,   0f),     // Survival
    };

    private void DrawRuntimeDashboard(GrannyAI granny)
    {
        EditorGUILayout.BeginVertical(new GUIStyle(EditorStyles.helpBox)
            { padding = new RectOffset(8, 8, 8, 8) });

        EditorGUILayout.LabelField("── LIVE RUNTIME ──", _subHeaderStyle);
        EditorGUILayout.Space(2);

        // State badge
        int stateIdx = (int)granny.EditorCurrentState;
        Color stateCol = stateIdx >= 0 && stateIdx < _stateColors.Length
            ? _stateColors[stateIdx] : Color.white;

        Rect stateRect = GUILayoutUtility.GetRect(0, 24, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(stateRect, new Color(stateCol.r * 0.3f, stateCol.g * 0.3f, stateCol.b * 0.3f));
        EditorGUI.DrawRect(new Rect(stateRect.x, stateRect.y, 4, stateRect.height), stateCol);
        GUI.Label(new Rect(stateRect.x + 10, stateRect.y, stateRect.width, stateRect.height),
                  granny.EditorCurrentState.ToString().ToUpper(),
                  new GUIStyle(EditorStyles.boldLabel)
                  {
                      fontSize  = 13,
                      alignment = TextAnchor.MiddleLeft,
                      normal    = { textColor = stateCol }
                  });

        EditorGUILayout.Space(4);

        // Fear bar
        EditorGUILayout.LabelField("Fear Score", _dashboardLabelStyle);
        Rect barRect = GUILayoutUtility.GetRect(0, 14, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(barRect, new Color(0.15f, 0.05f, 0.05f));
        float fear = granny.EditorFearScore;
        EditorGUI.DrawRect(new Rect(barRect.x, barRect.y, barRect.width * fear, barRect.height),
                           Color.Lerp(new Color(0.3f, 0.8f, 0.3f), new Color(1f, 0.1f, 0.1f), fear));
        GUI.Label(barRect, $"  {fear:P0}",
                  new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.white } });

        EditorGUILayout.Space(4);

        // Room info
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Granny in:", _dashboardLabelStyle, GUILayout.Width(70));
        EditorGUILayout.LabelField(granny.EditorGrannyRoom, _dashboardValueStyle);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Targeting:", _dashboardLabelStyle, GUILayout.Width(70));
        EditorGUILayout.LabelField(granny.EditorTargetRoom, _dashboardValueStyle);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region ── Tutorial Panel ────────────────────────────────────────────────

    private void DrawTutorialPanel()
    {
        _showTutorial = DrawFoldoutHeader(_showTutorial, "📖  Setup Tutorial", new Color(0.2f, 0.4f, 0.6f));
        if (!_showTutorial) return;

        EditorGUILayout.BeginVertical(_boxStyle);
        EditorGUILayout.LabelField(_tutorialTitles[_tutorialPage], _subHeaderStyle);
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField(_tutorialTexts[_tutorialPage], _tutorialStyle);
        EditorGUILayout.Space(6);

        EditorGUILayout.BeginHorizontal();
        GUI.enabled = _tutorialPage > 0;
        if (GUILayout.Button("◀  Previous", GUILayout.Width(100))) _tutorialPage--;
        GUI.enabled = true;

        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField($"{_tutorialPage + 1} / {_tutorialTitles.Length}",
                                    EditorStyles.centeredGreyMiniLabel, GUILayout.Width(60));
        GUILayout.FlexibleSpace();

        GUI.enabled = _tutorialPage < _tutorialTitles.Length - 1;
        if (GUILayout.Button("Next  ▶", GUILayout.Width(100))) _tutorialPage++;
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region ── Setup Wizard ──────────────────────────────────────────────────

    private void DrawSetupWizard(GrannyAI granny)
    {
        _showSetupWizard = DrawFoldoutHeader(_showSetupWizard, "🔧  Setup Wizard", new Color(0.25f, 0.45f, 0.25f));
        if (!_showSetupWizard) return;

        EditorGUILayout.BeginVertical(_boxStyle);

        // ── Auto-find rooms ──
        EditorGUILayout.LabelField("1.  Populate Room List", _subHeaderStyle);
        EditorGUILayout.LabelField(
            "Scans the scene for all RoomNodeMarker components and fills the 'allRooms' list automatically.",
            EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.Space(2);
        if (GUILayout.Button("🔍  Auto-Find Room Markers", GUILayout.Height(28)))
        {
            Undo.RecordObject(granny, "Auto-Find Room Markers");
            granny.Editor_AutoFindRooms();
            EditorUtility.SetDirty(granny);
            _validationRan = false;
        }

        EditorGUILayout.Space(8);

        // ── Create room marker ──
        EditorGUILayout.LabelField("2.  Create a New Room Marker in Scene", _subHeaderStyle);
        EditorGUILayout.LabelField(
            "Creates an empty GameObject with RoomNodeMarker at the Scene camera position. " +
            "Then assign the matching RoomNodeData asset on it.",
            EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.Space(2);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("➕  Create Room Marker Here", GUILayout.Height(28)))
        {
            Vector3 spawnPos = SceneView.lastActiveSceneView != null
                ? SceneView.lastActiveSceneView.camera.transform.position + SceneView.lastActiveSceneView.camera.transform.forward * 3f
                : Vector3.zero;

            GameObject marker = GrannyAI.Editor_CreateRoomMarker("NewRoom", spawnPos);
            Undo.RegisterCreatedObjectUndo(marker, "Create Room Marker");
            Selection.activeGameObject = marker;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);

        // ── Select all markers ──
        EditorGUILayout.LabelField("3.  Select All Room Markers", _subHeaderStyle);
        EditorGUILayout.LabelField(
            "Selects every RoomNodeMarker in the scene so you can inspect them all at once.",
            EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.Space(2);
        if (GUILayout.Button("📋  Select All Room Markers", GUILayout.Height(28)))
        {
            var markers = FindObjectsByType<RoomNodeMarker>(FindObjectsSortMode.None);
            var gos = new List<GameObject>();
            foreach (var m in markers) gos.Add(m.gameObject);
            Selection.objects = gos.ToArray();
        }

        EditorGUILayout.Space(8);

        // ── Create RoomNodeData asset ──
        EditorGUILayout.LabelField("4.  Create a Room Node Asset", _subHeaderStyle);
        EditorGUILayout.LabelField(
            "Opens the Project window focused on the GrannyAI folder ready to create a new Room Node asset. " +
            "You can also right-click → Create → GrannyAI → Room Node anywhere in Project.",
            EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.Space(2);
        if (GUILayout.Button("📄  Create Room Node Asset", GUILayout.Height(28)))
        {
            // Ping the GrannyAI folder in Project if it exists, otherwise Assets root
            string path = "Assets";
            var guids = AssetDatabase.FindAssets("t:RoomNodeData");
            if (guids.Length > 0)
                path = System.IO.Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(guids[0]));

            var asset = ScriptableObject.CreateInstance<RoomNodeData>();
            string assetPath = AssetDatabase.GenerateUniqueAssetPath(path + "/Room_New.asset");
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
        }

        EditorGUILayout.EndVertical();
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region ── Validation Panel ──────────────────────────────────────────────

    private void DrawValidationPanel(GrannyAI granny)
    {
        _showValidation = DrawFoldoutHeader(_showValidation, "✅  Room Graph Validation", new Color(0.35f, 0.25f, 0.45f));
        if (!_showValidation) return;

        EditorGUILayout.BeginVertical(_boxStyle);

        if (GUILayout.Button("▶  Run Validation", GUILayout.Height(26)))
        {
            _validationIssues = granny.Editor_ValidateRoomGraph();
            _validationRan    = true;
        }

        if (_validationRan)
        {
            EditorGUILayout.Space(4);
            if (_validationIssues.Count == 0)
            {
                EditorGUILayout.LabelField("✔  No issues found. Room graph looks good!", _okStyle);
            }
            else
            {
                EditorGUILayout.LabelField($"⚠  Found {_validationIssues.Count} issue(s):",
                                            EditorStyles.boldLabel);
                foreach (var issue in _validationIssues)
                {
                    bool isWarning = issue.StartsWith("Warning");
                    EditorGUILayout.LabelField((isWarning ? "⚠ " : "✖ ") + issue,
                                               isWarning ? _warningStyle : _errorStyle);
                }
            }
        }
        else
        {
            EditorGUILayout.LabelField("Click 'Run Validation' to check the room graph.",
                                        EditorStyles.wordWrappedMiniLabel);
        }

        EditorGUILayout.EndVertical();
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region ── Property Sections ─────────────────────────────────────────────

    private void DrawRoomGraph()
    {
        DrawProperty("allRooms",    "All Rooms",    "All RoomNodeData assets in the scene. Auto-populated by the Setup Wizard.");
        DrawProperty("startRoom",   "Start Room",   "The room Granny physically spawns in.");
    }

    private void DrawPatrol()
    {
        DrawProperty("patrolRoute",   "Patrol Route",   "Ordered list of rooms for idle patrol (4–8 recommended).");
        DrawProperty("patrolWaitMin", "Wait Min (s)",   "Minimum pause duration at each patrol stop.");
        DrawProperty("patrolWaitMax", "Wait Max (s)",   "Maximum pause duration at each patrol stop.");
    }

    private void DrawMovement()
    {
        EditorGUILayout.HelpBox(
            "Each state has a base speed. Fear score adds a bonus on top during Hunt/Chase.\n" +
            "startDelay is the time Granny stands idle at the start of the game.",
            MessageType.Info);
        DrawProperty("patrolSpeed",  "Patrol Speed");
        DrawProperty("curiousSpeed", "Curious Speed");
        DrawProperty("huntSpeed",    "Hunt Speed");
        DrawProperty("chaseSpeed",   "Chase Speed");
        DrawProperty("startDelay",   "Start Delay (s)");
    }

    private void DrawBelief()
    {
        EditorGUILayout.HelpBox(
            "Belief score drives state transitions — NOT distance.\n" +
            "Higher thresholds = Granny needs more evidence before escalating.\n" +
            "Higher decay = she forgets faster.",
            MessageType.Info);
        DrawProperty("beliefCuriousThreshold", "Curious Threshold",    "Belief needed to enter Curious.");
        DrawProperty("beliefHuntThreshold",    "Hunt Threshold",       "Belief needed to enter Hunt.");
        DrawProperty("crossFloorBeliefGate",   "Cross-Floor Gate",     "Belief in a cross-floor room needed before Granny uses stairs.");
        DrawProperty("beliefDecayRate",        "Belief Decay Rate",    "How fast belief fades per second.");
        DrawProperty("grudgeDecayRate",        "Grudge Decay Rate",    "How fast grudge heat fades (should be << beliefDecayRate).");
    }

    private void DrawSound()
    {
        EditorGUILayout.HelpBox(
            "Sound propagates room-by-room, not by 3D distance.\n" +
            "Closed doors and distance (hops) both attenuate the signal.\n" +
            "Call GrannyAI.HearSound(position, isLoud) from your game objects.",
            MessageType.Info);
        DrawProperty("soundPropagationHops", "Max Hops",           "How many rooms away sound travels.");
        DrawProperty("soundBeliefLoud",      "Loud Belief",        "Belief injected at source for a loud noise.");
        DrawProperty("soundBeliefQuiet",     "Quiet Belief",       "Belief injected at source for a quiet noise.");
        DrawProperty("soundHopAttenuation",  "Hop Attenuation",    "Multiplied per hop. Lower = quieter at distance.");
        DrawProperty("closedDoorAttenuation","Closed Door Factor", "Extra attenuation when door is closed on a connection.");
    }

    private void DrawVision()
    {
        EditorGUILayout.HelpBox(
            "Vision requires:\n" +
            " • Player in same room OR adjacent through an open doorway.\n" +
            " • Within the angle cone.\n" +
            " • Within range.\n" +
            " • No occluder on visionBlockMask.\n" +
            "Granny cannot see through floors — ever.",
            MessageType.Info);
        DrawProperty("visionAngle",     "Angle (°)",      "Total cone width.");
        DrawProperty("visionRange",     "Range (m)",      "Max vision distance.");
        DrawProperty("visionBlockMask", "Block Mask",     "Layers that occlude sight (walls, closed doors).");
    }

    private void DrawProximity()
    {
        EditorGUILayout.HelpBox(
            "Proximity Detection allows Granny to sense the player even if they are behind her.\n" +
            "It checks X and Z axes only (2D distance), using a max height difference to prevent floor-crossing triggers.",
            MessageType.Info);
        DrawProperty("proximityDetectionRadius", "Detection Radius (m)", "Radius of detection circle on X-Z plane.");
        DrawProperty("proximityMaxYDifference", "Max Height Diff (m)", "Maximum Y height difference allowed for detection.");
    }

    private void DrawAttack()
    {
        DrawProperty("attackDistance",    "Attack Distance (m)", "Granny snaps to player and attacks within this range.");
        DrawProperty("attackAnimDuration","Anim Duration (s)",   "Length of the attack animation before resuming.");
    }

    private void DrawNearMiss()
    {
        EditorGUILayout.HelpBox(
            "The near-miss beat fires when Granny enters a hiding-spot room with MEDIUM belief.\n" +
            "She turns toward the player, steps forward, then turns away.\n" +
            "Belief too high → she goes Chase instead. Belief too low → she doesn't react.\n" +
            "This is the scariest moment — tune the window carefully.",
            MessageType.Info);
        DrawProperty("nearMissBeliefMin",   "Belief Min",    "Floor of the belief window.");
        DrawProperty("nearMissBeliefMax",   "Belief Max",    "Ceiling of the belief window (above this = Chase).");
        DrawProperty("nearMissTriggerDist", "Trigger Dist",  "Player must be within this distance.");
        DrawProperty("nearMissCooldown",    "Cooldown (s)",  "Seconds before another near-miss can fire.");
    }

    private void DrawTension()
    {
        EditorGUILayout.HelpBox(
            "fearScore (0–1) rises during Hunt/Chase and falls during Patrol.\n" +
            "Subscribe to GrannyAI.OnFearScoreChanged to drive post-processing effects.",
            MessageType.Info);
        DrawProperty("fearRiseRate",      "Rise Rate",        "Fear increase per second during escalation.");
        DrawProperty("fearFallRate",      "Fall Rate",        "Fear decrease per second during calm.");
        DrawProperty("maxFearSpeedBonus", "Max Speed Bonus",  "Additive speed at maximum fear.");
    }

    private void DrawFalseScares()
    {
        EditorGUILayout.HelpBox(
            "At each patrol stop, there's a chance Granny reacts to 'nothing'.\n" +
            "She plays the react animation and checks an adjacent room.\n" +
            "Keep probability low (0.15–0.30) so it stays surprising.",
            MessageType.Info);
        DrawProperty("falseScareCooldown",    "Cooldown (s)",    "Minimum gap between false scares.");
        DrawProperty("falseScareProbability", "Probability",     "Chance per patrol stop. 0 = never, 1 = always.");
    }

    private void DrawAnimations()
    {
        DrawProperty("anim",           "Animator");
        DrawProperty("animParamSpeed", "Speed Param",  "Float parameter name in the Animator Controller.");
        DrawProperty("animIdleName",   "Idle State",   "Animator state name for idle.");
        DrawProperty("animWalkName",   "Walk State",   "Animator state name for walking.");
        DrawProperty("animAttackName", "Attack State", "Animator state name for attack.");
        DrawProperty("animReactName",  "React State",  "Animator state name for near-miss / false scare.");
        DrawProperty("animCrazyName",  "Crazy State",  "Animator state name for mental breakdown.");
        DrawProperty("lookAtWeight",   "Look-At Weight","IK weight for head tracking during Chase/Hunt.");
    }

    private void DrawAmbientVoices()
    {
        EditorGUILayout.HelpBox(
            "Plays random horror voices every 20-30 seconds during gameplay. Also loops footstep audio.",
            MessageType.Info);
        DrawProperty("grannyAudioSource",   "Audio Source",   "Granny's physical AudioSource.");
        DrawProperty("footstepClip",        "Footstep Clip",  "Footstep sound to play on loop.");
        DrawProperty("ambientHorrorVoices", "Voice Clips",    "List of horror sounds.");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Mental Breakdown Settings", EditorStyles.boldLabel);
        DrawProperty("grannyMentalBreakdown", "Breakdown Sound", "Audio clip played when mental breakdown starts.");
        DrawProperty("mentalBreakDownAlert",  "Breakdown UI Alert", "UI GameObject activated during mental breakdown.");
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region ── Drawing Helpers ───────────────────────────────────────────────

    private delegate void DrawContentDelegate();

    private void DrawSection(ref bool foldout, string label, DrawContentDelegate draw,
                              Color? accentColor = null)
    {
        Color accent = accentColor ?? new Color(0.55f, 0.35f, 0.2f);
        foldout = DrawFoldoutHeader(foldout, label, accent);
        if (!foldout) return;

        EditorGUILayout.BeginVertical(_boxStyle);
        draw();
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(2);
    }

    private bool DrawFoldoutHeader(bool current, string label, Color accent)
    {
        Rect rect = GUILayoutUtility.GetRect(0, 22, GUILayout.ExpandWidth(true));

        // Background
        EditorGUI.DrawRect(rect, new Color(accent.r * 0.3f, accent.g * 0.3f, accent.b * 0.3f));
        // Left accent bar
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, 3, rect.height), accent);

        // Foldout
        bool result = EditorGUI.Foldout(
            new Rect(rect.x + 8, rect.y, rect.width - 8, rect.height),
            current, "  " + label,
            new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold,
                fontSize  = 11,
                normal    = { textColor = new Color(0.9f, 0.9f, 0.9f) },
                onNormal  = { textColor = Color.white }
            });

        return result;
    }

    private void DrawProperty(string propName, string label = null, string tooltip = null)
    {
        var prop = serializedObject.FindProperty(propName);
        if (prop == null)
        {
            EditorGUILayout.HelpBox($"Property '{propName}' not found.", MessageType.Warning);
            return;
        }

        serializedObject.Update();
        string displayLabel   = label   ?? prop.displayName;
        string displayTooltip = tooltip ?? prop.tooltip;
        EditorGUILayout.PropertyField(prop, new GUIContent(displayLabel, displayTooltip), true);
        serializedObject.ApplyModifiedProperties();
    }

    #endregion
}
#endif
