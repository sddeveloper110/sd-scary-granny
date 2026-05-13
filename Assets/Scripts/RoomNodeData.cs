using System.Collections.Generic;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
//  RoomNodeData  —  ScriptableObject asset for one room in the house.
//
//  HOW TO CREATE:
//    Right-click in Project window → Create → GrannyAI → Room Node
//
//  One asset per room (Kitchen, Bedroom_1, Hallway, etc.)
//  Wire them together via the Connections list.
// ─────────────────────────────────────────────────────────────────────────────

[CreateAssetMenu(menuName = "GrannyAI/Room Node", fileName = "Room_New")]
public class RoomNodeData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Unique human-readable ID. Use the room's real name: Kitchen, Bedroom1, Hallway_Ground, etc.")]
    public string roomId = "RoomName";

    [Tooltip("0 = ground floor, 1 = first floor, 2 = second floor, etc.")]
    public int floorIndex = 0;

    [Header("Room Flags")]
    [Tooltip("TRUE if this room has wardrobes, under-bed spots, or closets. " +
             "Granny will perform the near-miss scripted beat here.")]
    public bool isHidingSpotRoom = false;

    [Tooltip("TRUE if this room is adjacent to a main exit (front door, window escape). " +
             "Granny will try to cut here during chase to block escape.")]
    public bool isExitRoom = false;

    [Tooltip("TRUE if this room is a staircase / landing connecting two floors.")]
    public bool isStaircaseRoom = false;

    [Header("Connections")]
    [Tooltip("All rooms directly reachable from this room. Add one entry per doorway or passage. " +
             "Staircase connections should have traversalCost = 3.")]
    public List<RoomConnectionData> connections = new();

    // ── Runtime belief (NOT serialised — lives only during Play) ──────────────
    [System.NonSerialized] public float beliefScore;
    [System.NonSerialized] public float grudgeHeat;
    [System.NonSerialized] public float lastHeardTime = -9999f;

    /// <summary>Resets runtime fields. Called by GrannyAI on game start/retry.</summary>
    public void RuntimeReset()
    {
        beliefScore   = 0f;
        grudgeHeat    = 0f;
        lastHeardTime = -9999f;
    }
}

[System.Serializable]
public class RoomConnectionData
{
    [Tooltip("The room this connection leads to.")]
    public RoomNodeData target;

    [Tooltip("How expensive it is to traverse.\n" +
             "1  = open doorway (cheap)\n" +
             "2  = narrow passage\n" +
             "3  = staircase (Granny needs strong belief to cross floors)\n" +
             "5  = locked door / window (very expensive, rarely used)")]
    [Range(1f, 5f)]
    public float traversalCost = 1f;

    [Tooltip("Is this connection a staircase between floors?")]
    public bool isStaircase = false;

    [Tooltip("Is the door on this connection currently open? " +
             "Closed doors block sound propagation. Update this at runtime when doors open/close.")]
    public bool isDoorOpen = true;
}
