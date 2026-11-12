using UnityEngine;

namespace Case464.NPC
{
    [DisallowMultipleComponent]
    public sealed class HotelNpcClickProxy : MonoBehaviour
    {
        [SerializeField] private HotelNpcActor actor;
        public void Configure(HotelNpcActor value) { actor = value; }

        private void OnMouseUpAsButton()
        {
            if (actor != null) actor.Interact();
        }
    }
}
