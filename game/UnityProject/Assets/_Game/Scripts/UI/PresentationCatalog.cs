using UnityEngine;

namespace DeductionGame
{
    // Presentation only: array order follows GameDefinitions.Names and MansionLayout.Rooms.
    // Never store roles, cards, clue traits or other match state in these assets.
    [CreateAssetMenu(menuName = "Deduction Game/Presentation Catalog")]
    public sealed class PresentationCatalog : ScriptableObject
    {
        public GameObject[] CharacterPrefabs = new GameObject[6];
        public Sprite[] CharacterPortraits = new Sprite[6];
        public GameObject[] MansionRoomPrefabs = new GameObject[9];
        public Sprite[] MansionRoomBackgrounds = new Sprite[9];

        public static PresentationCatalog Load() => Resources.Load<PresentationCatalog>("PresentationCatalog");

        public GameObject Character(int id) => Get(CharacterPrefabs, id);
        public GameObject Room(int id) => MapData.Theme == MapTheme.Mansion ? Get(MansionRoomPrefabs, id - 1) : null;

        private static GameObject Get(GameObject[] items, int index) =>
            items != null && index >= 0 && index < items.Length ? items[index] : null;
    }
}
