using UnityEngine;
namespace DeductionGame
{
    // The existing service crossing stays physically open. Thick visual wall units
    // close the decorative entrance's sightlines, then cut away before an actor reaches them.
    [DefaultExecutionOrder(600)]
    public sealed class MansionEntranceScreen : MonoBehaviour
    {
        Renderer surface;
        CharacterActor[] actors;
        public Vector3 CrossingMin, CrossingMax;
        void Awake(){surface=GetComponent<Renderer>();actors=FindObjectsByType<CharacterActor>(FindObjectsSortMode.None);}
        void LateUpdate(){RefreshVisibility();}
        public void RefreshVisibility()
        {
            if(surface==null)surface=GetComponent<Renderer>();
            if(actors==null||actors.Length==0)actors=FindObjectsByType<CharacterActor>(FindObjectsSortMode.None);
            bool crossing=false;
            foreach(var a in actors)if(a!=null){var p=a.transform.position;if(p.x>CrossingMin.x&&p.x<CrossingMax.x&&p.z>CrossingMin.z&&p.z<CrossingMax.z&&p.y>CrossingMin.y&&p.y<CrossingMax.y){crossing=true;break;}}
            // Side/action presentation hides the entire exterior; never override that visibility.
            var cam=Camera.main;var director=cam!=null?cam.GetComponent<MansionGoldCinematicDirector>():null;
            surface.enabled=!crossing&&(director==null||!director.SideViewActive);
        }
    }
}
