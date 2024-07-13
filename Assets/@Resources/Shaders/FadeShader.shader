Shader "Custom/FadeShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1) // 컬러 프로퍼티 추가
        _FadeWidth ("Fade Width", Range(0,1)) = 0.2 // 투명해지는 정도
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _Color; // 컬러 변수 추가
            float _FadeWidth; // 투명해지는 정도

            v2f vert(appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;

                // 중앙에서부터 좌우로 갈수록 투명해지는 계산
                float alpha = 1.0 - smoothstep(1.0 - _FadeWidth, 1.0, abs(i.uv.x));
                col.a *= alpha;

                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
