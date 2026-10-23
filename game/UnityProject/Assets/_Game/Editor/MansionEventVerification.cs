#if UNITY_EDITOR
using System;
using System.Reflection;
using DeductionGame;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

// Batch entry: -executeMethod MansionEventVerification.Run (without -quit).
[InitializeOnLoad]
public static class MansionEventVerification
{
    private static int frames;
    static MansionEventVerification()
    {
        EditorApplication.playModeStateChanged += OnPlayMode;
    }
    public static void Run()
    {
        SessionState.SetBool("MansionEventVerification.Running", true);
        string versionKey = (string)typeof(ProjectBootstrap).GetField("SceneVersionKey", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        EditorPrefs.SetInt(versionKey + ".Theme", 0); EditorPrefs.SetInt(versionKey, 3);
        typeof(ProjectBootstrap).GetMethod("EnsureProject", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayMode(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("MansionEventVerification.Running", false))
            EditorApplication.update += Check;
    }

    private static void Check()
    {
        if (++frames < 30) return;
        EditorApplication.update -= Check;
        SessionState.SetBool("MansionEventVerification.Running", false);
        try
        {
            var gm = GameManager.Instance;
            Require(gm != null && gm.Human != null, "Game initialized");
            var view = UnityEngine.Object.FindFirstObjectByType<MansionEventView>();
            Require(view != null, "Event view attached");
            var connections = GameObject.Find("Mansion_Connections").transform;
            Require(connections.childCount == 12, "11 locked graph links plus secret placeholder");
            Require(UnityEngine.Object.FindObjectsByType<RoomNodeView>(FindObjectsSortMode.None).Length == 9, "9 RoomNodes");
            Require(Camera.main.GetComponent<PcCameraController>() != null &&
                Camera.main.GetComponent<MansionCutawayView>() != null, "Existing camera zoom and cutaway attached");
            Require(UnityEngine.Object.FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None).Length == 6,
                "Six existing NavMesh player agents");
            var door = connections.Find("DOOR_01_02/EventMarkers");
            Require(door != null, "DoorID mapped");
            Set(gm, "<Round>k__BackingField", 6);
            Set(gm, "culpritLockEdge", "1-2"); Set(gm, "culpritLockUntilRound", 6);
            Set(gm, "routeBlockedEdge", "1-2"); Set(gm, "brokenDoorEdge", "1-2");
            Set(gm, "extraEventRound", 6);
            Set(gm, "blackoutRoom", 1); Set(gm, "blackoutUntilRound", 6);
            Set(gm, "smokeRoomA", 2); Set(gm, "smokeRoomB", 3); Set(gm, "smokeUntilRound", 6);
            Set(gm, "secretPassageA", 9); Set(gm, "secretPassageB", 6); Set(gm, "secretPassageUntilRound", 6);
            Refresh(view);
            Require(door.Find("Locked").gameObject.activeSelf && door.Find("RouteBlocked").gameObject.activeSelf &&
                door.Find("BrokenDoor").gameObject.activeSelf, "All door event markers activated");
            Require(connections.Find("DOOR_09_06_SECRET").gameObject.activeSelf, "Secret entrance visible");
            Require(view.transform.Find("Room_02_EventVisuals/Smoke").gameObject.activeSelf, "Smoke A visible");
            Require(view.transform.Find("Room_03_EventVisuals/Smoke").gameObject.activeSelf, "Smoke B visible");
            var floor = GameObject.Find("Mansion_Graybox/Room_01_중앙홀/Floor").GetComponent<Renderer>();
            var block = new MaterialPropertyBlock(); floor.GetPropertyBlock(block);
            Require(block.GetColor("_Color").r < 0.2f, "Blackout applied");
            var human = gm.Human;
            human.RoomId = 9; human.MovementPoints = 2;
            RuntimeBoardView.RefreshAll();
            Require(Move(gm, 6) && human.RoomId == 6 && human.MovementPoints == 1, "Secret movement keeps 1P rule");
            var token = GameObject.Find($"Token_0_{human.Name}");
            Require(Vector3.Distance(token.transform.position, MansionLayout.GetRoom(6).Center) < 4f,
                "Secret passage transitions agent to destination room");
            human.RoomId = 1; human.MovementPoints = 3;
            Require(!Move(gm, 2) && human.RoomId == 1 && human.MovementPoints == 3, "Route block denies once without cost");
            Refresh(view);
            Require(!door.Find("RouteBlocked").gameObject.activeSelf, "Consumed route block restored");
            Require(Move(gm, 2) && human.MovementPoints == 0, "Lock and broken door keep existing extra costs");
            Set(gm, "<Round>k__BackingField", 7);
            typeof(GameManager).GetMethod("ExpireRoundEffects", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(gm, null);
            Refresh(view);
            Require(!door.Find("Locked").gameObject.activeSelf && !door.Find("BrokenDoor").gameObject.activeSelf,
                "Expired door markers restored");
            Require(!connections.Find("DOOR_09_06_SECRET").gameObject.activeSelf, "Expired secret hidden");
            Require(!view.transform.Find("Room_02_EventVisuals/Smoke").gameObject.activeSelf &&
                !view.transform.Find("Room_03_EventVisuals/Smoke").gameObject.activeSelf, "Smoke restored");
            floor.GetPropertyBlock(block); Require(block.isEmpty, "Blackout property block restored");
            human.RoomId = 9; human.MovementPoints = 2;
            Require(!Move(gm, 6), "Expired secret rejects movement");
            Require(!MapGraph.IsConnected(9, 6), "Locked graph unchanged");
            Set(gm, "blackoutRoom", 1); Set(gm, "blackoutUntilRound", 12);
            Set(gm, "routeBlockedEdge", "1-2");
            gm.StartNewGame(); Refresh(view);
            Require(gm.ActiveBlackoutRoom == 0 && gm.ActiveRouteBlockedEdge == null && !gm.SecretPassageActive,
                "Restart clears public map effects");
            Debug.Log("MANSION_EVENT_VERIFICATION: OK");
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }

    private static void Set(GameManager gm, string name, object value) =>
        typeof(GameManager).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(gm, value);
    private static bool Move(GameManager gm, int room) => (bool)typeof(GameManager)
        .GetMethod("TryMove", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(gm, new object[] { gm.Human, room, false });
    private static void Refresh(MansionEventView view) => typeof(MansionEventView)
        .GetMethod("LateUpdate", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(view, null);
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception("Verification failed: " + message);
        Debug.Log("[Verified] " + message);
    }
}
#endif
