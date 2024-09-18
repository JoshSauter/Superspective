Shader "Custom/DimensionShaders/DimensionWall" {
	Properties {
		_WallChannel("Channel", Int) = 0
		_StencilValue("Channel+1", Int) = 1
	}
    SubShader {
        Tags { "Queue"="Background-1000" "RenderType"="VisibilityMask" }
        LOD 100

		Pass {
			Blend One One
			ZWrite Off
			
			Stencil {
				Ref [_StencilValue]
				Comp Greater
				Pass Replace
			}

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "../DimensionShaderHelpers.cginc"

			int _WallChannel;
            int _StencilValue;

			struct appdata {
				float4 vertex : POSITION;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				fixed depth : TEXCOORD0;
			};
			

			v2f vert (appdata v) {
				v2f o;
				o.depth = COMPUTE_DEPTH_01;
				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target {
				float4 col = ColorFromChannel(_WallChannel);
				return fixed4(col.r, col.g, col.b, i.depth-0.01);
			}
            ENDCG
        }
    }
}
