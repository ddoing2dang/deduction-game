using UnityEngine;
namespace DeductionGame {
 public static class CruiseLayout {
 public static readonly int[,] Links = { {1,2},{2,3},{2,4},{3,4},{3,5},{4,6},{5,6},{5,7},{6,8},{7,9},{8,9} };
 public static readonly MansionRoomLayout[] Rooms = {
 new(1,"조타실",new(0,0,28),new(8,8),false),
 new(2,"전망 라운지",new(0,0,16),new(12,10),false),
 new(3,"카지노",new(-9,0,3),new(10,10),false),
 new(4,"아트리움",new(9,0,3),new(10,10),false),
 new(5,"레스토랑",new(-9,0,-11),new(10,10),false),
 new(6,"풀 데크",new(9,0,-11),new(12,10),false),
 new(7,"기관제어실",new(-9,-4,-22),new(10,8),true),
 new(8,"승무원구역",new(9,-4,-22),new(10,8),true),
 new(9,"화물실",new(0,-4,-34),new(12,8),true) };
 }
}
