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
            if (!Connections.TryGetValue(from, out var list)) return false;
            for (int i = 0; i < list.Length; i++)
                if (list[i] == to) return true;
            return false;
        }

        public static int[] GetConnections(int roomId) =>
            Connections.TryGetValue(roomId, out var list) ? list : System.Array.Empty<int>();
    }
}
