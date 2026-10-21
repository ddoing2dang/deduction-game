const q=s=>document.querySelector(s);
const rooms=["중앙홀","서재","응접실","식당","주방","온실","보일러실","와인저장고","사용인실"];
const pos=[[42,28],[13,25],[70,25],[13,51],[42,52],[42,5],[13,76],[42,76],[71,76]];
const names=["나즈","찌로","마리히","마또","보키","문이레"];
let turn=0,round=1,points=3,room=0;
function draw(){
 q("#round").textContent="ROUND "+round+" / 12";
 q("#turnTitle").textContent="내 턴 · 나즈";
 q("#status").innerHTML="<b>위치</b> "+rooms[room]+"<br><b>이동 P</b> "+points+"<br><b>행동</b> 가능";
 q("#hand").innerHTML='<span class="pill">장소 카드: 식당</span><span class="pill">무기 카드: 단검</span><p>조사자 · 웹 테스트 모드</p>';
 q("#notes").innerHTML="웹 프로토타입 정상 실행 중";
 q("#actions").innerHTML='<button onclick="nextTurn()">턴 종료</button>';
 let m=q("#map");m.innerHTML="";
 rooms.forEach((n,i)=>{let d=document.createElement("div");d.className="room"+(i===room?" current":"");d.style.left=pos[i][0]+"%";d.style.top=pos[i][1]+"%";d.style.width="18%";d.style.height="12%";d.innerHTML="<b>"+(i+1)+". "+n+"</b><small>"+(i>5?"지하":"지상")+"</small>"+(i===room?'<div class="players"><span class="pawn me">나</span></div>':"");d.onclick=()=>{room=i;points=Math.max(0,points-1);draw()};m.appendChild(d)});
 q("#log").innerHTML="<div>게임 화면 정상 로드</div><div>방을 눌러 이동 테스트 가능</div>";
}
function nextTurn(){round=round>=12?1:round+1;points=round===1?3:2;draw()}
draw();