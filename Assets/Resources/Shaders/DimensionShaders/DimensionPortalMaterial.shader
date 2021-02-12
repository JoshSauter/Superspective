Shader "Custom/DimensionShaders/DimensionPortalMaterial" {
    Properties {
		_MainTex("Main Texture", 2D) = "white" {}
		_DepthNormals("Depth Normals Texture", 2D) = "white" {}
		_Inverse("Inverted (true: 1, false: 0)", Int) = 0
    }
    SubShader {
		Tags  { 
			"RenderType" = "DimensionPortalMaterial" "Queue" = "Geometry" "PortalTag" = "Dimension"
		}

        Pass {
			ZWrite On

			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				#include "UnityCG.cginc"
				#include "./../DimensionShaders/DimensionShaderHelpers.cginc"

				struct appdata {
					float4 vertex : POSITION;
				};

				struct v2f {
					float4 vertex : SV_POSITION;
					float4 screenPos : TEXCOORD0;
				};

				v2f vert(appdata v) {
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.screenPos = ComputeScreenPos(o.vertex);
					return o;
				}

				uniform sampler2D _MainTex;
				uniform sampler2D _DepthNormals;
			
				uniform int _Channels[NUM_CHANNELS];
				uniform int _Inverse;

				fixed4 frag(v2f i) : SV_Target {
					ClipDimensionObject(i.vertex, _Channels, _Inverse);
					float2 uv = i.screenPos.xy / i.screenPos.w;

					fixed4 col = tex2D(_MainTex, uv);
					return col;
				}
			ENDCG
        }
    }
}
