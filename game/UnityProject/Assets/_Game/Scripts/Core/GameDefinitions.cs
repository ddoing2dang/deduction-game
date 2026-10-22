using System.Collections.Generic;

namespace DeductionGame
{
    public static class GameDefinitions
    {
        public static readonly string[] Names =
        {
            "나즈", "찌로", "마리히", "마또", "보키", "문이레"
        };

        public static readonly CharacterProfile[] Profiles =
        {
            new("긴 머리", "리본/모자", "따뜻한 계열", "단정형"),
            new("트윈테일", "꽃/나비", "선명한 계열", "활동형"),
            new("트윈테일", "장식 적음", "밝은 계열", "화려형"),
            new("긴 머리", "장식 적음", "선명한 계열", "활동형"),
            new("짧은 머리", "리본/모자", "밝은 계열", "화려형"),
            new("짧은 머리", "꽃/나비", "따뜻한 계열", "단정형")
        };

        public static readonly string[] AbilityNames =
        {
            "", "문지기", "질주", "도약", "추적", "심문",
            "정보분석", "재의심", "회귀", "비축", "우회"
        };

        public static readonly Dictionary<InvestigatorAbility, int> AbilityBaseUses = new()
        {
            { InvestigatorAbility.Gatekeeper, 3 },
            { InvestigatorAbility.Sprint, 2 },
            { InvestigatorAbility.Leap, 1 },
            { InvestigatorAbility.Tracking, 2 },
            { InvestigatorAbility.Interrogation, 1 },
            { InvestigatorAbility.Analysis, 1 },
            { InvestigatorAbility.ReSuspicion, 1 },
            { InvestigatorAbility.Return, 3 },
            { InvestigatorAbility.Reserve, 3 },
            { InvestigatorAbility.Detour, 2 }
        };
    }
}
