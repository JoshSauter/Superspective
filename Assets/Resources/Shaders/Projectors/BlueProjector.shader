Shader "Custom/Projector/BlueProjector" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
	}

	CGINCLUDE
	#include "UnityCG.cginc"

	struct appdata {
		float4 vertex : POSITION;
	};

	struct v2f {
		float4 vertex : SV_POSITION;
	};

	sampler2D _MainTex;
	float4 _MainTex_ST;
			
	v2f vert (appdata v) {
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		return o;
	}
			
	fixed4 frag (v2f i) : SV_Target {
		return 0;
	}

	ENDCG
	SubShader {
		Tags { "RenderType"="Transparent" "Queue"="Geometry+3" }
		LOD 100

		// Incrementers
		Pass {
			Stencil {
				Ref 4
				Comp Always
				ZFail Replace
				WriteMask 4
			}

			Cull Front
			Blend Zero One
			ZWrite Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}
		// Decrementers
		Pass {
			Stencil {
				Ref 0
				Comp Always
				ZFail Replace
				WriteMask 4
			}

			Cull Back
			Blend Zero One
			ZWrite Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}
	}
}
