using System;

namespace DeductionGame
{
    public enum InvestigatorAbility
    {
        None, Gatekeeper, Sprint, Leap, Tracking, Interrogation,
        Analysis, ReSuspicion, Return, Reserve, Detour
    }

    public enum ExtraEventType
    {
        None, Blackout, BrokenDoor, SecretPassage
    }

    [Serializable]
    public sealed class CulpritPowerUses
    {
        public int Lock = 2;
        public int Disrupt = 2;
        public int RouteBlock = 1;
        public int Smoke = 1;
    }

    [Serializable]
    public sealed class CharacterProfile
    {
        public string Hair;
        public string Ornament;
        public string Tone;
        public string Style;

        public CharacterProfile(string hair, string ornament, string tone, string style)
        {
            Hair = hair; Ornament = ornament; Tone = tone; Style = style;
        }
    }
}
