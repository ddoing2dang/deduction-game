using UnityEngine;
namespace DeductionGame {
 public enum MapTheme { Mansion, Cruise, Museum }
 public sealed class MapThemeSettings : MonoBehaviour { public MapTheme Theme; }
 public static class MapData {
 public static MapTheme Theme => Object.FindFirstObjectByType<MapThemeSettings>()?.Theme ?? MapTheme.Mansion;
 public static MansionRoomLayout[] Rooms => Theme == MapTheme.Cruise ? CruiseLayout.Rooms : Theme == MapTheme.Museum ? MuseumLayout.Rooms : MansionLayout.Rooms;
 public static readonly int[,] MansionLinks = { {1,2},{1,3},{1,5},{1,6},{2,4},{3,5},{4,5},{4,7},{5,8},{7,8},{8,9} };
 public static int[,] Links => Theme == MapTheme.Cruise ? CruiseLayout.Links : Theme == MapTheme.Museum ? MuseumLayout.Links : MansionLinks;
 public static int SecretA => Theme == MapTheme.Cruise ? 8 : Theme == MapTheme.Museum ? 2 : 9;
 public static int SecretB => Theme == MapTheme.Cruise ? 2 : Theme == MapTheme.Museum ? 7 : 6;
 public static string RootName => Theme + "_Graybox";
 public static string ConnectionsName => Theme + "_Connections";
 public static string SecretName => $"DOOR_{SecretA:00}_{SecretB:00}_SECRET";
 public static MansionRoomLayout GetRoom(int id) => Rooms[Mathf.Clamp(id,1,9)-1];
 public static Vector3 GetStandPoint(int id,int slot) => MansionLayout.GetStandPoint(id,slot) - MansionLayout.GetRoom(id).Center + GetRoom(id).Center;
 public static bool IsSecretPair(int a,int b) => (a==SecretA && b==SecretB)||(b==SecretA && a==SecretB);
 }
}
