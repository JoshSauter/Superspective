Shader "Custom/DimensionShaders/InverseDimensionObject"
{
	Properties
	{
		_Color ("Main Color", Color) = (1.0, 1.0, 1.0, 1.0)
		[HDR]
		_EmissionColor("Emissive Color", Color) = (0, 0, 0, 0)
		_Dimension("Dimension", Int) = 0
	}
	SubShader
	{
		Tags { "Queue"="Geometry" "RenderType"="InverseDimensionObject" }
		LOD 100

		Pass
		{
			Stencil {
				Ref 0
				Comp Equal
			}
			ZWrite On
			ZTest LEqual

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"
			#include "DimensionShaderHelpers.cginc"

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
			float _ResolutionX;
			float _ResolutionY;
			
			sampler2D _DimensionMask;
			int _Dimension;

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
				float2 viewportVertex = float2(i.vertex.x / _ResolutionX, i.vertex.y / _ResolutionY);
				fixed4 dimensionTest = tex2D(_DimensionMask, viewportVertex);
				int dimensionValue = ColorToDimensionValue(dimensionTest);
				clip(-(dimensionValue != -1));
				return col;
			}
			ENDCG
		}
	}

	Fallback "VertexLit"
}
