Shader "DeductionGame/Character Toon Atlas"
{
 Properties { _MainTex("Colour atlas",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) }
 SubShader {
  Tags {"RenderType"="Opaque" "Queue"="Geometry"}
  Pass {
   Tags {"LightMode"="ForwardBase"} Cull Off
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 2.0
   #include "UnityCG.cginc"
   #include "Lighting.cginc"
   sampler2D _MainTex; float4 _MainTex_ST; fixed4 _Color;
   struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;};
   struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;half3 normal:TEXCOORD1;};
   v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=TRANSFORM_TEX(v.uv,_MainTex);o.normal=UnityObjectToWorldNormal(v.normal);return o;}
   fixed4 frag(v2f i):SV_Target {
    half ndl=dot(normalize(i.normal),normalize(_WorldSpaceLightPos0.xyz));
    half shade=ndl>.45?1.0:ndl>-.1?.88:.73;
    fixed3 colour=tex2D(_MainTex,i.uv).rgb*_Color.rgb;
    return fixed4(colour*shade,1);
   }
   ENDCG
  }
  UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
 }
 Fallback "Mobile/Diffuse"
}
