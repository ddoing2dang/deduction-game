using System;
using System.Linq;
using UnityEngine;

namespace DeductionGame
{
    // Blender-exported coordinates are the single source for these two local Entry revisions.
    [Serializable] public sealed class MansionStairArchitecture
    {
        [Serializable] public sealed class Entry { public int room; public int to; public Vector3 position; }
        [Serializable] public sealed class Route { public string door; public Vector3[] points; }
        public Entry[] entries;
        public Route[] routes;
        public static MansionStairArchitecture Load() => JsonUtility.FromJson<MansionStairArchitecture>(Resources.Load<TextAsset>("MansionStairArchitecture").text);
        public void ApplyEntries(Transform rooms)
        {
            foreach(var entry in entries) {
                int a=Math.Min(entry.room,entry.to),b=Math.Max(entry.room,entry.to);
                if(!((a==4&&b==7)||(a==5&&b==8)))
                    throw new InvalidOperationException("Unapproved stair Entry change");
                var room=rooms.Cast<Transform>().Single(t=>t.name.StartsWith($"Room_{entry.room:00}_"));
                var point=room.Find($"NavigationPoints/Entry_To_{entry.to:00}");
                point.position=entry.position;
                // Facing follows the first (level) approach to the actual stair landing.
                var route=routes.Single(r=>r.door==$"DOOR_{a:00}_{b:00}");
                Vector3 facing=entry.room==a?route.points[1]-route.points[0]:route.points[^2]-route.points[^1];
                point.rotation=Quaternion.LookRotation(facing,Vector3.up);
            }
        }
    }
}
