// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/PhotoshopOverlayShader"
{
	Properties
	{
		_MainTex("Sprite Texture", 2D) = "white" {}
	}
		SubShader
	{
		Tags 
		{ 
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Overlay"
			"PreviewType" = "Plane"
			"CanUseSpriteAtlas" = "True"
		}
		//LOD 100

		Cull Off
		Lighting On
		ZWrite Off
		ZTest Always
		Blend One OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest

			#include "UnityCG.cginc"

			struct appdata_custom
			{
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				fixed2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				fixed2 uv : TEXCOORD0;
			};

			sampler2D _MainTex;
			fixed4 _MainTex_ST;


			v2f vert(appdata_custom v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.color = v.color;
				o.uv = TRANSFORM_TEX(v.uv,_MainTex);
				return o;
			}

			fixed4 frag(v2f i) : COLOR
			{
				fixed4 diffuse = tex2D(_MainTex, i.uv);
				fixed oldAlpha = diffuse.a;

				diffuse = lerp(1 - 2 * (1 - diffuse) * (1 - i.color), 2 * diffuse * i.color, step(diffuse, 0.5));
				diffuse.a = oldAlpha * i.color.a;
				return diffuse;
			}
			ENDCG
		}

	}
}
