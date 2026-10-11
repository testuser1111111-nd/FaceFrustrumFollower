Shader "FaceFrustrumFollower/AnaglyphCombine"
{
    Properties
    {
        _MainTex ("Red eye", 2D) = "black" {}
        _SecondTex ("Cyan eye", 2D) = "black" {}
    }
    SubShader
    {
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            sampler2D _SecondTex;
            fixed4 frag(v2f_img i) : SV_Target
            {
                return fixed4(tex2D(_MainTex, i.uv).r, tex2D(_SecondTex, i.uv).gb, 1);
            }
            ENDCG
        }
    }
}
