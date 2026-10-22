using System;
using System.Collections.Generic;

namespace DeductionGame
{
    [Serializable]
    public sealed class PlayerState
    {
        public int Id;
        public string Name;
        public bool IsHuman;
        public bool IsCulprit;
        public int RoomId;
        public int MovementPoints;
        public bool SetupComplete;
        public bool Active = true;

        public string LocationCard;
        public string WeaponCard;
        public readonly List<string> PrivateNotes = new();

        public PlayerState(int id, string name, bool isHuman, int roomId)
        {
            Id = id;
            Name = name;
            IsHuman = isHuman;
            RoomId = roomId;
        }

        public bool Holds(string card) => LocationCard == card || WeaponCard == card;
    }
}
