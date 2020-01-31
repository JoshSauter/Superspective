Shader "Custom/StencilBuffer/SimpleStencilReaderUnlit"
{
	Properties
	{
		_Color ("Main Color", Color) = (1.0, 1.0, 1.0, 1.0)
		[HDR]
		_EmissionColor("Emissive Color", Color) = (0, 0, 0, 0)
		_StencilValue("Stencil Value", Int) = 1
	}
	SubShader
	{
		Tags { "RenderType"="SimpleStencilReader" }
		LOD 100

		Stencil {
			Ref [_StencilValue]
			Comp Equal
		}

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			float4 _Color;
			float4 _EmissionColor;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = _Color;
				// apply fog
				col += _EmissionColor;
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}

	Fallback "VertexLit"
}
