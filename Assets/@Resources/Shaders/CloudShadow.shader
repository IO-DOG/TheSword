Shader "Custom/MovingCloudShadow_SpriteRenderer_Color"
{
    Properties
    {
        _MainTex ("Cloud Texture", 2D) = "white" {} // 텍스처 프로퍼티
        _Color ("Cloud Color", Color) = (1, 1, 1, 1) // 구름 색상
        _Speed ("Speed", Float) = 0.1 // 이동 속도
        _Direction ("Direction", Vector) = (1, 0, 0, 0) // 이동 방향
        _Opacity ("Opacity", Float) = 0.5 // 투명도
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }
        LOD 100

        Pass
        {
            Name "SpritePass"
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex; // 텍스처 샘플러
            float4 _Color;      // 색상 속성
            float _Speed;
            float4 _Direction;
            float _Opacity;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float time = _Time.y;
                float2 offset = float2(_Direction.x, _Direction.y) * _Speed * time;

                // 텍스처 샘플링
                float4 cloudColor = tex2D(_MainTex, i.uv + offset);

                // 색상 조절 및 투명도 적용
                cloudColor.rgb *= _Color.rgb; // 텍스처 색상에 선택한 색상을 곱함
                cloudColor.a *= _Opacity;    // 투명도 적용

                return cloudColor;
            }
            ENDHLSL
        }
    }
    FallBack "Sprites/Default"
}
