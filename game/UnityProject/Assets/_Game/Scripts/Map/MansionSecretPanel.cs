using UnityEngine;
namespace DeductionGame
{
    // A real mapped secret opening, with no hovering cyan placeholder cube.
    public sealed class MansionSecretPanel : MonoBehaviour
    {
        bool initialized,lastOpen;
        void Update(){
            var gm=GameManager.Instance;if(gm==null)return;bool open=gm.SecretPassageActive;
            if(initialized&&open==lastOpen)return;initialized=true;lastOpen=open;
            var renderer=GetComponent<Renderer>();if(renderer!=null)renderer.enabled=!open;
            var collider=GetComponent<Collider>();if(collider!=null)collider.enabled=!open;
        }
    }
}
