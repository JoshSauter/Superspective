Shader "Custom/DebugStencilBuffer"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" "Queue"="Geometry+4" }
		LOD 100

		CGINCLUDE
		// make fog work
		#pragma multi_compile_fog
			
		#include "UnityCG.cginc"
		struct appdata {
			float4 vertex : POSITION;
			float2 uv : TEXCOORD0;
		};

		struct v2f {
			float2 uv : TEXCOORD0;
			UNITY_FOG_COORDS(1)
			float4 vertex : SV_POSITION;
		};

		sampler2D _MainTex;
		float4 _MainTex_ST;
			
		v2f vert (appdata v) {
			v2f o;
			o.vertex = UnityObjectToClipPos(v.vertex);
			o.uv = TRANSFORM_TEX(v.uv, _MainTex);
			UNITY_TRANSFER_FOG(o,o.vertex);
			return o;
		}
			
		fixed4 fragWhite (v2f i) : SV_Target {
			return fixed4(1,1,1,1);
		}
		fixed4 fragTeal (v2f i) : SV_Target {
			return fixed4(0,1,1,1);
		}
		fixed4 fragPurple (v2f i) : SV_Target {
			return fixed4(1,0,1,1);
		}
		fixed4 fragBlue (v2f i) : SV_Target {
			return fixed4(0,0,1,1);
		}
		fixed4 fragYellow (v2f i) : SV_Target {
			return fixed4(1,1,0,1);
		}
		fixed4 fragGreen (v2f i) : SV_Target {
			return fixed4(0,1,0,1);
		}
		fixed4 fragRed (v2f i) : SV_Target {
			return fixed4(1,0,0,1);
		}
		fixed4 fragBlack (v2f i) : SV_Target {
			return fixed4(0,0,0,1);
		}
		ENDCG

		// Final Colors
		Pass {
			Stencil {
				Ref 0
				Comp Equal
			}
			ZWrite Off
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragBlack
			ENDCG
		}
		Pass {
			Stencil {
				Ref 7
				Comp Equal
				Pass Zero
			}
			ZWrite Off
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragWhite
			ENDCG
		}
		Pass {
			Stencil {
				Ref 6
				Comp Equal
				Pass Zero
			}
			ZWrite Off
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragTeal
			ENDCG
		}
		Pass {
			Stencil {
				Ref 5
				Comp Equal
				Pass Zero
			}
			ZWrite Off
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragPurple
			ENDCG
		}
		Pass {
			Stencil {
				Ref 4
				Comp Equal
				Pass Zero
			}
			ZWrite Off
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragBlue
			ENDCG
		}
		Pass {
			Stencil {
				Ref 3
				Comp Equal
				Pass Zero
			}
			ZWrite Off
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragYellow
			ENDCG
		}
		Pass {
			Stencil {
				Ref 2
				Comp Equal
				Pass Zero
			}
			ZWrite Off
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragGreen
			ENDCG
		}
		Pass {
			Stencil {
				Ref 1
				Comp Equal
				Pass Zero
			}
			ZWrite Off
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragRed
			ENDCG
		}
	}
}
