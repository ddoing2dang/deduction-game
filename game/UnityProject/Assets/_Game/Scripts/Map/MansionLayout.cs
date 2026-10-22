using UnityEngine;

namespace DeductionGame
{
    public readonly struct MansionRoomLayout
    {
        public readonly int Id;
        public readonly string Name;
        public readonly Vector3 Center;
        public readonly Vector2 Size;
        public readonly bool IsBasement;

        public MansionRoomLayout(int id, string name, Vector3 center, Vector2 size, bool isBasement)
        {
            Id = id; Name = name; Center = center; Size = size; IsBasement = isBasement;
        }
    }

    public static class MansionLayout
    {
        // ART_GUIDE.md LOCK coordinates converted from Blender (X,Y,Z-up)
        // to Unity (X,Y-up,Z). These are production-scale meters, not board-game P distance.
        public static readonly MansionRoomLayout[] Rooms =
        {
            new(1, "중앙홀",     new Vector3(  0f,  0f,  2f), new Vector2(12f, 11f), false),
            new(2, "서재",       new Vector3(-12f,  0f,  4f), new Vector2( 9f,  8f), false),
            new(3, "응접실",     new Vector3( 12f,  0f,  4f), new Vector2( 9f,  8f), false),
            new(4, "식당",       new Vector3(-11f,  0f, -7f), new Vector2(10f,  8f), false),
            new(5, "주방",       new Vector3(  2f,  0f, -8f), new Vector2( 9f,  8f), false),
            new(6, "온실",       new Vector3(  0f,  0f, 13f), new Vector2(10f,  8f), false),
            new(7, "보일러실",   new Vector3(-10f, -4f, -6f), new Vector2( 8f,  7f), true),
            new(8, "와인저장고", new Vector3(  1f, -4f, -7f), new Vector2( 9f,  8f), true),
            new(9, "사용인실",   new Vector3( 10f, -4f, -5f), new Vector2( 8f,  7f), true)
        };

        public static MansionRoomLayout GetRoom(int roomId)
        {
            int index = Mathf.Clamp(roomId, 1, 9) - 1;
            return Rooms[index];
        }

        public static Vector3 GetStandPoint(int roomId, int slot)
        {
            var room = GetRoom(roomId);
            // Six natural cluster slots, kept away from room edges/door zones.
            Vector3[] offsets =
            {
                new(-1.5f, 0.55f,  1.1f),
                new( 0.0f, 0.55f,  1.4f),
                new( 1.5f, 0.55f,  1.1f),
                new(-1.5f, 0.55f, -1.1f),
                new( 0.0f, 0.55f, -1.4f),
                new( 1.5f, 0.55f, -1.1f)
            };
            return room.Center + offsets[Mathf.Clamp(slot, 0, 5)];
        }
    }
}
