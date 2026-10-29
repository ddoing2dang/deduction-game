using UnityEngine;

namespace DeductionGame
{
    public sealed class RoomNodeView : MonoBehaviour
    {
        [SerializeField] private int roomId;
        [SerializeField] private string roomName;
        public int RoomId => roomId;

        private readonly ScreenLabel label = new();
        private LineRenderer hint;
        private void LateUpdate(){
            var gm=GameManager.Instance; if(gm==null || gm.Human==null)return;
            if(hint==null){
                var go=new GameObject("MovementHint");go.transform.SetParent(transform.parent,false);hint=go.AddComponent<LineRenderer>();
                hint.sharedMaterial=new Material(Shader.Find("Sprites/Default"));hint.useWorldSpace=true;hint.loop=true;hint.positionCount=4;hint.widthMultiplier=0.08f;
                var layout=MapData.GetRoom(roomId);var c=layout.Center+Vector3.up*0.08f;var half=layout.Size*0.46f;
                hint.SetPositions(new[]{c+new Vector3(-half.x,0,-half.y),c+new Vector3(half.x,0,-half.y),c+new Vector3(half.x,0,half.y),c+new Vector3(-half.x,0,half.y)});
            }
            var ui=FindFirstObjectByType<RuntimeGameUI>();
            Color color=gm.Human.RoomId==roomId?Color.cyan:ui!=null && ui.SelectedRoom==roomId?Color.yellow:gm.MoveReason(roomId)==""?Color.green:new Color(0.4f,0.4f,0.4f,0.3f);
            if(MapData.Theme==MapTheme.Mansion&&GameObject.Find("MansionVisualGold")!=null){
                hint.widthMultiplier=.035f;
                color=gm.Human.RoomId==roomId?new Color(.32f,.65f,.62f,.72f):ui!=null&&ui.SelectedRoom==roomId?new Color(.86f,.66f,.24f,.82f):gm.MoveReason(roomId)==""?new Color(.39f,.56f,.40f,.38f):new Color(.4f,.4f,.4f,.16f);
            }
            hint.startColor=hint.endColor=color;var floor=GetComponent<Renderer>();hint.enabled=floor==null || floor.enabled;
            UpdateLabel();
        }
        private void OnDestroy(){label.Dispose();if(hint!=null){Destroy(hint.sharedMaterial);Destroy(hint.gameObject);}}

        private void Start()
        {
            RoomPresentation.Install(gameObject, roomId, PresentationCatalog.Load());
            RuntimeBoardView.RefreshAll();
        }

        public void Configure(int id, string displayName)
        {
            roomId = id;
            roomName = displayName;
            if (transform.parent == null)
                gameObject.name = $"Room_{id}_{displayName}";
        }

        private void OnMouseDown()
        {
            if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
            foreach (var touch in Input.touches)
                if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touch.fingerId)) return;
            if (GameManager.Instance != null)
                { var ui=FindFirstObjectByType<RuntimeGameUI>();if(ui!=null)ui.SelectRoom(roomId); GameManager.Instance.TryMoveHuman(roomId); }
        }

        private void UpdateLabel()
        {
            label.Hide();
            var cam = Camera.main;
            if (cam == null || GameManager.Instance != null && GameManager.Instance.Phase == GamePhase.GameOver) return;
            var renderer=GetComponent<Renderer>();if(renderer!=null && !renderer.enabled)return;
            var screen = cam.WorldToScreenPoint(transform.position + Vector3.up * 0.8f);
            if (screen.z <= 0 || screen.x-60 < Screen.width*0.27f || screen.x+150 > Screen.width*0.76f || screen.y < Screen.height*0.23f || screen.y > Screen.height*0.81f) return;

            var rect = new Rect(screen.x - 60, Screen.height - screen.y + 25, 120, 24);
            var gm=GameManager.Instance;
            string state=gm!=null && gm.Human!=null ? gm.Human.RoomId==roomId?" [현재]":gm.MoveReason(roomId)==""?$" [{gm.PreviewMovementCost(roomId)}P 이동가능]":"" : "";
            label.Draw(screen,$"{roomId}. {roomName}"+(MapData.Theme==MapTheme.Mansion?"":" / "+CardRules.Locations[roomId-1])+state,300,new Vector2(45,-37));
        }
    }
}
