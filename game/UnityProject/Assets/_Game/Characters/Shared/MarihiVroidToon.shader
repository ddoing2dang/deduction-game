Shader "DeductionGame/Marihi Candidate Toon"
{
 Properties {
  _MainTex("Character albedo",2D)="white"{} _Color("Base",Color)=(1,1,1,1)
  _ShadeColor("Shade",Color)=(.61,.61,.61,1) _Cutoff("Cutout",Range(0,1))=0
  _BumpMap("Normal",2D)="bump"{} _NormalStrength("Normal strength",Range(0,1))=1
  _EmissionTex("Emission",2D)="black"{} _EmissionColor("Emission colour",Color)=(0,0,0,1)
  _MatcapTex("Matcap",2D)="black"{} _MatcapColor("Matcap colour",Color)=(0,0,0,1)
  _Metallic("Metal",Range(0,1))=0
  _ShadeFloor("Studio fill",Range(0,1))=0 _MatcapStrength("Matcap strength",Range(0,1))=.12
  _FaceAtlas("Face atlas backface regions",Float)=0 _VertexColor("Authored vertex tint",Float)=0
 }
 SubShader {
  Tags {"RenderType"="TransparentCutout" "Queue"="AlphaTest"}
  Pass {
   Tags {"LightMode"="ForwardBase"} Cull Off ZWrite On
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.0
   #include "UnityCG.cginc"
   #include "Lighting.cginc"
   sampler2D _MainTex,_BumpMap,_EmissionTex,_MatcapTex;
   fixed4 _Color,_ShadeColor,_EmissionColor,_MatcapColor;
   half _Cutoff,_Metallic,_NormalStrength,_ShadeFloor,_MatcapStrength,_FaceAtlas,_VertexColor;
   struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;float4 tangent:TANGENT;float2 uv:TEXCOORD0;float4 color:COLOR;};
   struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float3 normal:TEXCOORD1;float3 tangent:TEXCOORD2;float3 bitangent:TEXCOORD3;float3 world:TEXCOORD4;float4 color:COLOR;};
   v2f vert(appdata v) {
    v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;o.world=mul(unity_ObjectToWorld,v.vertex).xyz;
    o.normal=UnityObjectToWorldNormal(v.normal);o.tangent=UnityObjectToWorldDir(v.tangent.xyz);
    o.bitangent=cross(o.normal,o.tangent)*v.tangent.w*unity_WorldTransformParams.w;return o;
   }
   fixed4 frag(v2f i,fixed facing:VFACE):SV_Target {
    // The source VRM culls mouth, iris and sclera backs but keeps skin/ink/highlights double-sided.
    if(_FaceAtlas>.5&&facing<0) {
     bool mouth=i.uv.x>=.5&&i.uv.x<=.75&&i.uv.y<=.25;
     bool irisOrSclera=i.uv.x<=.5&&i.uv.y>=.5;
     if(mouth||irisOrSclera)discard;
    }
    fixed4 albedo=tex2D(_MainTex,i.uv)*_Color;albedo.rgb*=lerp(half3(1,1,1),i.color.rgb,_VertexColor);clip(albedo.a-_Cutoff);
    half3 bump=UnpackNormal(tex2D(_BumpMap,i.uv));bump.xy*=_NormalStrength;
    half3 n=normalize(normalize(i.tangent)*bump.x+normalize(i.bitangent)*bump.y+normalize(i.normal)*bump.z);
    half3 light=normalize(_WorldSpaceLightPos0.xyz);half ndl=dot(n,light);
    half ramp=lerp(_ShadeFloor,1,smoothstep(-.2,.65,ndl));half3 colour=albedo.rgb*lerp(_ShadeColor.rgb,half3(1,1,1),ramp);
    half3 view=normalize(_WorldSpaceCameraPos-i.world);half spec=pow(saturate(dot(n,normalize(light+view))),36)*_Metallic;
    colour+=lerp(half3(.04,.04,.04),albedo.rgb,_Metallic)*spec*.8;
    half3 vn=mul((half3x3)UNITY_MATRIX_V,n);half2 mc=vn.xy*.5+.5;
    colour+=tex2D(_MatcapTex,mc).rgb*_MatcapColor.rgb*_MatcapStrength;
    colour+=tex2D(_EmissionTex,i.uv).rgb*_EmissionColor.rgb;
    return fixed4(colour,1);
   }
   ENDCG
  }
  Pass {
   Tags {"LightMode"="ShadowCaster"} Cull Off
   CGPROGRAM
   #pragma vertex vertShadow
   #pragma fragment fragShadow
   #pragma multi_compile_shadowcaster
   #include "UnityCG.cginc"
   sampler2D _MainTex;fixed4 _Color;half _Cutoff;
   struct v2f {V2F_SHADOW_CASTER;float2 uv:TEXCOORD1;};
   v2f vertShadow(appdata_base v){v2f o;o.uv=v.texcoord;TRANSFER_SHADOW_CASTER_NORMALOFFSET(o);return o;}
   float4 fragShadow(v2f i):SV_Target {clip(tex2D(_MainTex,i.uv).a*_Color.a-_Cutoff);SHADOW_CASTER_FRAGMENT(i)}
   ENDCG
  }
 }
 Fallback "Mobile/Diffuse"
}
