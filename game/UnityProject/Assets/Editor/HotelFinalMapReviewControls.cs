using System.Linq;
using Case464.Hotel;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Case464.EditorTools
{
    // Standalone controls for the approved map: no intermediate scene generators required.
    internal sealed class HotelFinalMapReviewControls : EditorWindow
    {
        private const string FinalScene="Assets/Case464/Scenes/Hotel/Hotel_RoseBlack_Final_Map.unity";
        [MenuItem("Case464/Hotel Final/Review Controls")]
        private static void Open()=>GetWindow<HotelFinalMapReviewControls>("Hotel Final");
        [MenuItem("Case464/Hotel Final/Open Approved Map")]
        private static void OpenMap()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)return;
            if(Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Any(s=>s.isDirty))
            {EditorUtility.DisplayDialog("Hotel Final","Save current scene changes first. Nothing was discarded.","OK");return;}
            EditorSceneManager.OpenScene(FinalScene,OpenSceneMode.Single);Open();
        }
        private void OnGUI()
        {
            var scene=SceneManager.GetActiveScene();
            var controller=scene.path==FinalScene?scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<HotelMapCameraController>()).SingleOrDefault():null;
            EditorGUILayout.HelpBox("Approved hotel map. 1F: H00, 101–103 / 2F: 201–204. Edit Mode camera review only.",MessageType.Info);
            using(new EditorGUI.DisabledScope(controller==null || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling))
            {
                foreach(var mode in new[]{HotelMapView.PhysicalBuilding,HotelMapView.FirstFloor,HotelMapView.SecondFloor})
                    if(GUILayout.Button(mode==HotelMapView.PhysicalBuilding?"Full Map":mode.ToString())){controller.Show(mode);controller.FinishTransition();}
                if(controller!=null)
                    foreach(var room in controller.GetComponentsInChildren<HotelOverviewRoomArea>(true).OrderBy(r=>r.Floor).ThenBy(r=>r.DisplayNumber))
                        if(GUILayout.Button(string.IsNullOrEmpty(room.DisplayNumber)?"H00 Lobby":room.DisplayNumber)){controller.FocusRoom(room.RoomId);controller.FinishTransition();}
                if(GUILayout.Button("Align Scene View to Review Camera"))
                {
                    var camera=controller.ReviewCamera;var view=SceneView.lastActiveSceneView;
                    if(view!=null)view.LookAt(camera.transform.position+camera.transform.forward*20,camera.transform.rotation,camera.orthographicSize,true,true);
                }
            }
        }
    }
}
