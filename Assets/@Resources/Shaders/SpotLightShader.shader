Shader "Custom/SpotLightShader"
{
    Properties
    {
        _Center ("Center", Vector) = (0,0,0,0)
        _Radius ("Radius", Float) = 1.0
        _Softness ("Softness", Float) = 0.1
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            float4 _Center;
            float _Radius;
            float _Softness;
            float4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 toCenter = i.worldPos - _Center.xyz;
                float dist = length(toCenter);
                float falloff = smoothstep(_Radius - _Softness, _Radius, dist);
                return _Color * (1.0 - falloff);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
