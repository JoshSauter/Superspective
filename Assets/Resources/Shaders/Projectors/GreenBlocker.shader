Shader "Custom/Projector/GreenBlocker" {
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
		Tags { "RenderType"="Transparent" "Queue"="Geometry+2" }
		LOD 100

		// Incrementer
		Pass {
			Stencil {
				Ref 64
				Comp Always
				ZFail Replace
				WriteMask 64
			}

			Cull Front
			Blend Zero One
			ZWrite Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}
		// Decrementer
		Pass {
			Stencil {
				Ref 0
				Comp Always
				ZFail Replace
				WriteMask 64
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
