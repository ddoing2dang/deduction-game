Shader "DeductionGame/Mansion Visual Gold" {
 Properties {
  _GroundCutaway("Ground cutaway",Float)=0 _CutawayFloor("View floor",Float)=0 _CutawayRoom("Room XZ center/half size",Vector)=(0,0,0,0) _CutawayDirection("Camera direction",Vector)=(0,-1,0,0)
  _Color("Tint",Color)=(1,1,1,1) _MainTex("CC0 albedo",2D)="white"{}
  _BumpMap("CC0 normal",2D)="bump"{} _BumpScale("Normal detail",Range(0,1))=.45
  _MetallicGlossMap("Metal / smoothness",2D)="white"{} _Metallic("Metal",Range(0,1))=0 _Glossiness("Smoothness",Range(0,1))=.35
  _EmissionColor("Fixture emission",Color)=(0,0,0,1) _LightFill("Readability fill",Range(0,.15))=.025 _Cutoff("Leaf cutout",Range(0,1))=.01
 }
 SubShader {
  Tags {"RenderType"="TransparentCutout" "Queue"="AlphaTest"} LOD 250 Cull Off
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows addshadow alphatest:_Cutoff
  #pragma target 3.0
  #pragma shader_feature_local _NORMALMAP
  #pragma shader_feature_local _METALLICGLOSSMAP
  sampler2D _MainTex,_BumpMap,_MetallicGlossMap;fixed4 _Color,_EmissionColor;half _Metallic,_Glossiness,_BumpScale,_LightFill;float _GroundCutaway,_CutawayFloor;float4 _CutawayRoom,_CutawayDirection;int _StairHoleCount;float4 _StairHoleMin[2],_StairHoleMax[2];
  struct Input {float2 uv_MainTex;float3 worldPos;};
  void surf(Input IN,inout SurfaceOutputStandard o){
   if(_GroundCutaway>.5){float3 projectedFloor=IN.worldPos+_CutawayDirection.xyz*((_CutawayFloor-IN.worldPos.y)/min(-.05,_CutawayDirection.y));if(abs(projectedFloor.x-_CutawayRoom.x)<_CutawayRoom.z&&abs(projectedFloor.z-_CutawayRoom.y)<_CutawayRoom.w)clip(-1);}
   float3 direction=_CutawayDirection.xyz;direction=float3(abs(direction.x)<.0001?.0001:direction.x,abs(direction.y)<.0001?-.0001:direction.y,abs(direction.z)<.0001?.0001:direction.z);
   for(int i=0;i<2;i++){if(i>=_StairHoleCount)break;float3 t0=(_StairHoleMin[i].xyz-IN.worldPos)/direction;float3 t1=(_StairHoleMax[i].xyz-IN.worldPos)/direction;float3 nearT=min(t0,t1),farT=max(t0,t1);if(min(farT.x,min(farT.y,farT.z))>=max(0,max(nearT.x,max(nearT.y,nearT.z))))clip(-1);}
   fixed4 c=tex2D(_MainTex,IN.uv_MainTex)*_Color;o.Albedo=c.rgb;o.Alpha=c.a;
   o.Metallic=_Metallic;o.Smoothness=_Glossiness;
   #ifdef _METALLICGLOSSMAP
   fixed4 mg=tex2D(_MetallicGlossMap,IN.uv_MainTex);o.Metallic=mg.r;o.Smoothness=mg.a;
   #endif
   #ifdef _NORMALMAP
   o.Normal=UnpackScaleNormal(tex2D(_BumpMap,IN.uv_MainTex),_BumpScale);
   #endif
   o.Emission=_EmissionColor.rgb+c.rgb*_LightFill;
  }
  ENDCG
 }
 Fallback "Diffuse"
}


