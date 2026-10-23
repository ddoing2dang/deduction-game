using UnityEngine;
namespace DeductionGame {
 public static class MuseumLayout {
 public static readonly int[,] Links = { {1,2},{1,3},{1,4},{1,5},{2,3},{4,5},{4,6},{5,6},{4,7},{5,8},{6,9},{7,8},{8,9} };
 public static readonly MansionRoomLayout[] Rooms = {
 new(1,"중앙전시홀",new(0,0,0),new(12,12),false),
 new(2,"고대유물관",new(-14,0,-5),new(10,10),false),
 new(3,"미술관",new(14,0,-5),new(10,10),false),
 new(4,"보석전시실",new(-14,4,9),new(10,10),false),
 new(5,"특별전시관",new(14,4,9),new(10,10),false),
 new(6,"복원실",new(0,4,23),new(10,8),false),
 new(7,"수장고",new(-14,8,9),new(10,10),false),
 new(8,"보안실",new(14,8,9),new(10,10),false),
 new(9,"관장실",new(0,8,23),new(10,8),false) };
 }
}
