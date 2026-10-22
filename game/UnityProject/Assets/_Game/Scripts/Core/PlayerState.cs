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
        public bool Active = true;
        public bool Spectating;

        public int RoomId;
        public int PreviousTurnStartRoomId;
        public int MovementPoints;
        public int ReservedPoint;
        public bool SetupComplete;
        public bool InfoActionUsed;
        public bool ForensicsChecked;
        public bool HasKey;
        public bool FakeDeductionUsed;
        public int DeductionAttempts;
        public int LastDeductionRound;
        public int DisruptionPenaltyNextTurn;

        public string LocationCard;
        public string WeaponCard;

        public InvestigatorAbility Ability = InvestigatorAbility.None;
        public int AbilityUsesLeft;

        public readonly List<string> PrivateNotes = new();
        public readonly List<string> KnownNotLocations = new();
        public readonly List<string> KnownNotWeapons = new();

        public PlayerState(int id, string name, bool isHuman, int roomId)
        {
            Id = id;
            Name = name;
            IsHuman = isHuman;
            RoomId = roomId;
            PreviousTurnStartRoomId = roomId;
        }

        public bool Holds(string card) => LocationCard == card || WeaponCard == card;
    }
}
