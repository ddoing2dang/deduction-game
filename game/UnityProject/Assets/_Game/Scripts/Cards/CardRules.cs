using System;
using System.Collections.Generic;

namespace DeductionGame
{
    public static class CardRules
    {
        public static readonly string[] Locations =
        {
            "중앙홀", "서재", "응접실", "식당", "주방",
            "온실", "보일러실", "와인저장고", "사용인실"
        };

        public static readonly string[] Weapons =
        {
            "촛대", "로프", "단검", "독약", "망치",
            "권총", "렌치", "가위", "장식검"
        };

        public static void Shuffle<T>(IList<T> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }

    [Serializable]
    public sealed class CaseAnswer
    {
        public int CulpritPlayerId;
        public string Location;
        public string Weapon;
    }
}
