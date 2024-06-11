Shader "Custom/ConeSpotLightShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1) // 컬러 프로퍼티 추가
        _Alpha ("Alpha", Range(0,1)) = 1 // 알파 프로퍼티 추가
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
            float _Alpha; // 알파 변수 추가

            v2f vert(appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 색상과 알파값을 프로퍼티에서 받아와서 사용
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                col.a *= _Alpha;

                float alpha = 1.0 - saturate((i.uv.y - 0.2) * 5); // 위쪽 1/5까지 투명
                alpha *= saturate((0.4 - abs(i.uv.y - 0.4)) * 5); // 중간 부분에서 서서히 반투명
                alpha *= 1.0 - saturate((i.uv.y - 0.8) * 5); // 아래쪽 1/5까지 투명

                // 최종 색상에 알파값을 적용
                col *= alpha;

                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}