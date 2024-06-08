Shader "Custom/CombinedShader" {
    Properties{
        _MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
        _Color1("Color1", Color) = (0.1, 1, 1, 1)
        _Color2("Color2", Color) = (0, 0.46, 1, 0)
        _Speed("Speed", Range(0, 5.0)) = 0.5
        _Size("Size", Range(1.0, 30.0)) = 15.0
        _Skew("Skew", Range(-1.0, 1.0)) = 0.5
        _Shear("Shear", Range(0.0, 5.0)) = 1.0
        _Fade("Fade", Range(0.0, 1.0)) = 1.0
        _Contrast("Contrast", Range(0.0, 50.0)) = 1.0
    }

        SubShader{
            Tags {
                "Queue" = "Transparent"
                "IgnoreProjector" = "True"
                "RenderType" = "Transparent"
                "PreviewType" = "Plane"
            }
            Cull Off
            Lighting Off
            ZWrite Off
            Fog { Mode Off }
            Blend DstColor SrcColor
            LOD 110

            Pass {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"

                sampler2D _MainTex;
                float4 _MainTex_ST;
                fixed4 _Color1;
                fixed4 _Color2;
                float _Speed;
                float _Size;
                float _Skew;
                float _Shear;
                float _Fade;
                float _Contrast;

                struct appdata_t {
                    float4 vertex : POSITION;
                    float2 texcoord : TEXCOORD0;
                    float4 color : COLOR;
                };

                struct v2f {
                    float4 vertex : POSITION;
                    half2 texcoord : TEXCOORD0;
                    fixed4 color : COLOR;
                };

                v2f vert(appdata_t v) {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                    o.color = v.color;
                    return o;
                }

                float4 permute(float4 x) {
                    return fmod(34.0 * x * x + x, 289.0);
                }

                float2 fade(float2 t) {
                    return t * t * t * (t * (t * 6.0 - 15.0) + 10.0);
                }

                float4 taylorInvSqrt(float4 r) {
                    return 1.79284291400159 - 0.85373472095314 * r;
                }

                float mod289(float x) {
                    return x - floor(x * 0.00346020761245674740484429065744) * 289.0;
                }

                float PerlinNoise2D(float2 P) {
                    float4 Pi = floor(P.xyxy) + float4(0.0, 0.0, 1.0, 1.0);
                    float4 Pf = frac(P.xyxy) - float4(0.0, 0.0, 1.0, 1.0);
                    float4 ix = Pi.xzxz;
                    float4 iy = Pi.yyww;
                    float4 fx = Pf.xzxz;
                    float4 fy = Pf.yyww;
                    float4 i = permute(permute(ix) + iy);
                    float4 gx = frac(i / 41.0) * 2.0 - 1.0;
                    float4 gy = abs(gx) - 0.5;
                    float4 tx = floor(gx + 0.5);
                    gx = gx - tx;
                    float2 g00 = float2(gx.x, gy.x);
                    float2 g10 = float2(gx.y, gy.y);
                    float2 g01 = float2(gx.z, gy.z);
                    float2 g11 = float2(gx.w, gy.w);
                    float4 norm = taylorInvSqrt(float4(dot(g00, g00), dot(g01, g01), dot(g10, g10), dot(g11, g11)));
                    g00 *= norm.x;
                    g01 *= norm.y;
                    g10 *= norm.z;
                    g11 *= norm.w;
                    float n00 = dot(g00, float2(fx.x, fy.x));
                    float n10 = dot(g10, float2(fx.y, fy.y));
                    float n01 = dot(g01, float2(fx.z, fy.z));
                    float n11 = dot(g11, float2(fx.w, fy.w));
                    float2 fade_xy = fade(Pf.xy);
                    float2 n_x = lerp(float2(n00, n01), float2(n10, n11), fade_xy.x);
                    float n_xy = lerp(n_x.x, n_x.y, fade_xy.y);
                    return 2.3 * n_xy;
                }

                float4 frag(v2f i) : COLOR {
                    float4 tex = tex2D(_MainTex, i.texcoord);
                    fixed4 color = lerp(_Color1, _Color2, 1 - i.texcoord.y);
                    float noisePos = i.texcoord.x;
                    noisePos += -0.5;
                    noisePos *= _Size;
                    noisePos += (1 - i.texcoord.y) * (_Size * _Skew);
                    noisePos *= 1 / lerp(1, _Shear, 1 - i.texcoord.y);
                    float val = PerlinNoise2D(float2(noisePos, _Time.y * _Speed)) / 2 + 0.5f;
                    val = _Contrast * (val - 0.5) + 0.5;
                    color.a *= lerp(val, val * i.texcoord.y, _Fade);
                    color.a = clamp(color.a, 0.0, 1.0);
                    float4 final;
                    final.rgb = color.rgb * tex.rgb * 2;
                    final.a = color.a * tex.a;
                    return lerp(float4(0.5f, 0.5f, 0.5f, 0.5f), final, final.a);
                }
                ENDCG
            }
        }
}