using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
//  RoomNodeMarker  —  Place one of these GameObjects at the centre of each room.
//
//  It links a RoomNodeData ScriptableObject asset to a world-space position.
//  GrannyAI finds all markers in the scene automatically via
//  FindObjectsByType<RoomNodeMarker>() — no manual list needed.
//
//  SETUP:
//   1. Create an empty GameObject at the centre of the room.
//   2. Add this component.
//   3. Assign the matching RoomNodeData asset.
//   4. Name the GameObject the same as the room for clarity.
//
//  The Editor toolbar button "Auto-Find Room Markers" on GrannyAI will
//  collect all of these and populate the allRooms list automatically.
// ─────────────────────────────────────────────────────────────────────────────

[ExecuteAlways]
public class RoomNodeMarker : MonoBehaviour
{
    [Tooltip("The ScriptableObject asset that describes this room's connections and flags.")]
    public RoomNodeData data;

    [Header("Gizmo Display")]
    [Tooltip("Colour used to draw this room's gizmo disc in the Scene view.")]
    public Color gizmoColor = new Color(0.2f, 0.8f, 1f, 0.6f);
    [Tooltip("Radius of the floor disc drawn in Scene view (purely visual, does not affect gameplay).")]
    [Range(0.5f, 6f)]
    public float gizmoRadius = 2f;
    [Tooltip("Show the room label above the disc.")]
    public bool  showLabel   = true;

    // ── Runtime belief passthrough (read by Editor for live display) ──────────
    public float RuntimeBelief  => data != null ? data.beliefScore : 0f;
    public float RuntimeGrudge  => data != null ? data.grudgeHeat  : 0f;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (data == null) return;

        // Floor disc ─ colour shifts red with belief during play
        Color baseCol  = Application.isPlaying
            ? Color.Lerp(gizmoColor, new Color(1f, 0.1f, 0.1f, 0.7f), data.beliefScore)
            : gizmoColor;

        // Disc on ground
        UnityEditor.Handles.color = baseCol;
        UnityEditor.Handles.DrawSolidDisc(transform.position, Vector3.up, gizmoRadius);

        // Outline
        UnityEditor.Handles.color = new Color(baseCol.r, baseCol.g, baseCol.b, 1f);
        UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.up, gizmoRadius, 2f);

        // Floor-index tag (small disc at chest height)
        Color floorCol = data.floorIndex == 0
            ? new Color(0.3f, 1f, 0.4f, 0.5f)
            : new Color(1f, 0.85f, 0.2f, 0.5f);
        UnityEditor.Handles.color = floorCol;
        UnityEditor.Handles.DrawSolidDisc(transform.position + Vector3.up * 0.05f,
                                           Vector3.up, 0.25f);

        // Connection lines to neighbours
        if (data.connections != null)
        {
            foreach (var conn in data.connections)
            {
                if (conn?.target == null) continue;

                // Find the marker for the target in the scene
                var targets = Object.FindObjectsByType<RoomNodeMarker>(FindObjectsSortMode.None);
                foreach (var t in targets)
                {
                    if (t.data != conn.target) continue;

                    Color lineCol = conn.isStaircase
                        ? new Color(1f, 0.6f, 0.1f, 0.8f)   // Orange = staircase
                        : new Color(0.5f, 1f, 0.5f, 0.5f);  // Green = doorway

                    if (!conn.isDoorOpen)
                        lineCol = new Color(0.6f, 0.3f, 0.3f, 0.5f); // Dark red = closed

                    UnityEditor.Handles.color = lineCol;
                    UnityEditor.Handles.DrawDottedLine(
                        transform.position + Vector3.up * 0.1f,
                        t.transform.position + Vector3.up * 0.1f, 4f);

                    // Traversal cost label at midpoint
                    Vector3 mid = (transform.position + t.transform.position) * 0.5f + Vector3.up * 0.5f;
                    UnityEditor.Handles.Label(mid,
                        $"cost:{conn.traversalCost:F0}" + (conn.isStaircase ? " 🪜" : ""),
                        new GUIStyle { fontSize = 9, normal = { textColor = lineCol } });
                    break;
                }
            }
        }

        // Room label
        if (showLabel)
        {
            GUIStyle style = new GUIStyle
            {
                fontSize  = 11,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = Color.white },
                alignment = TextAnchor.MiddleCenter
            };

            string label = data.roomId + $"\n[F{data.floorIndex}]";
            if (data.isHidingSpotRoom) label += "\n🕵 Hiding";
            if (data.isExitRoom)       label += "\n🚪 Exit";

            if (Application.isPlaying)
                label += $"\nBelief: {data.beliefScore:P0}\nGrudge: {data.grudgeHeat:P0}";

            UnityEditor.Handles.Label(transform.position + Vector3.up * (gizmoRadius * 0.5f + 0.8f),
                                       label, style);
        }
    }
#endif
}
