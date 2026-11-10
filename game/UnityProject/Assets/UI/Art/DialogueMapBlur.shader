Shader "Mystery/DialogueMapBlur"
{
    Properties
    {
        _MainTex ("Map screenshot", 2D) = "white" {}
        _BlurStrength ("Blur strength", Range(0.5, 4)) = 2
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Cull Off ZWrite Off ZTest Always
        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _MainTex;
        float4 _MainTex_TexelSize;
        float _BlurStrength;

        fixed4 BlurFive(v2f_img i, float2 direction)
        {
            float2 delta = _MainTex_TexelSize.xy * direction * _BlurStrength;
            fixed4 color = tex2D(_MainTex, i.uv) * 0.227027;
            color += tex2D(_MainTex, i.uv + delta * 1.384615) * 0.316216;
            color += tex2D(_MainTex, i.uv - delta * 1.384615) * 0.316216;
            color += tex2D(_MainTex, i.uv + delta * 3.230769) * 0.070270;
            color += tex2D(_MainTex, i.uv - delta * 3.230769) * 0.070270;
            return color;
        }
        ENDCG

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment Horizontal
            fixed4 Horizontal(v2f_img i) : SV_Target
            {
                return BlurFive(i, float2(1, 0));
            }
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment Vertical
            fixed4 Vertical(v2f_img i) : SV_Target
            {
                return BlurFive(i, float2(0, 1));
            }
            ENDCG
        }
    }
    Fallback Off
}
