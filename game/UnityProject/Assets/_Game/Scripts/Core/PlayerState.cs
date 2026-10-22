using System;

namespace DeductionGame
{
    [Serializable]
    public sealed class PlayerState
    {
        public int Id;
        public string Name;
        public bool IsHuman;
        public int RoomId;
        public int MovementPoints;
        public bool SetupComplete;
        public bool Active = true;

        public PlayerState(int id, string name, bool isHuman, int roomId)
        {
            Id = id;
            Name = name;
            IsHuman = isHuman;
            RoomId = roomId;
        }
    }
}
