using System.Collections.Generic;

namespace DeductionGame
{
    public static class MapGraph
    {
        private static readonly Dictionary<int, int[]> Connections = new()
        {
            { 1, new[] { 2, 3, 5, 6 } },
            { 2, new[] { 1, 4 } },
            { 3, new[] { 1, 5 } },
            { 4, new[] { 2, 5, 7 } },
            { 5, new[] { 1, 3, 4, 8 } },
            { 6, new[] { 1 } },
            { 7, new[] { 4, 8 } },
            { 8, new[] { 5, 7, 9 } },
            { 9, new[] { 8 } }
        };

        public static bool IsConnected(int from, int to)
        {
            var list = GetConnections(from);
            for (int i = 0; i < list.Length; i++)
                if (list[i] == to) return true;
            return false;
        }

        public static int[] GetConnections(int roomId) {
            if (MapData.Theme == MapTheme.Mansion) return Connections.TryGetValue(roomId, out var list) ? list : System.Array.Empty<int>();
            var result = new List<int>(); var links = MapData.Links;
            for(int i=0;i<links.GetLength(0);i++) {
                if(links[i,0]==roomId) result.Add(links[i,1]);
                if(links[i,1]==roomId) result.Add(links[i,0]);
            }
            return result.ToArray();
        }
    }
}
