Shader "Custom/StencilBuffer/StencilWriter" {
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
		return fixed4(0.3,.2,.5,1);
	}

	ENDCG
	SubShader {
		Tags { "RenderType"="Transparent" "Queue"="Geometry-1" }
		LOD 100
		
		Pass {
			// Write 1 into intersection area
			Stencil {
				Ref 1
				Comp Always
				ZFailBack IncrWrap
				ZFailFront DecrWrap
				WriteMask 7
			}

			Cull Off
			Blend Zero One
			ZWrite Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}
		Pass {
			// Handle overlap from multiple stencil writers
			Stencil {
				Ref 1
				Comp LEqual
				Pass Replace
				WriteMask 7
			}

			Cull Off
			Blend Zero One
			ZWrite Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}
	}
}
