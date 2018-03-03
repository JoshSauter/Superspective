Shader "Hidden/CustomEdgeDetection" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
		_EdgeColor ("Colors of Edges", Color) = (0, 0, 0, 1)
	}

	SubShader {

		Pass {
			CGPROGRAM
// Upgrade NOTE: excluded shader from DX11 because it uses wrong array syntax (type[size] name)
#pragma exclude_renderers d3d11
			#pragma vertex Vert
			#pragma fragment Frag
			
			#include "UnityCG.cginc"
			sampler2D _MainTex;

			const half2 uvDisplacements = half2[9](
				half2(0,0),
				half2(-1,-1),
				half2(0,-1),
				half2(1,-1),
				half2(-1,0),
				half2(1,0),
				half2(-1,1),
				half2(0,1),
				half2(1,1),
			);

			struct UVPositions {
				/* UVs are laid out as below
				     -1   0   1
				
				    –––––––––––––
			   -1   | 1 | 2 | 3 |
				    |–––––––––––|
				0   | 4 | 0 | 5 |
				    |–––––––––––|
				1   | 6 | 7 | 8 |
				    –––––––––––––

				Or, as displacement from center:
				i:		( x,  y)
				–––––––––––––––––
				0:		( 0,  0)
				1:		(-1, -1)
				2:		( 0, -1)
				3:		( 1, -1)
				4:		(-1,  0)
				5:		( 1,  0)
				6:		(-1,  1)
				7:		( 0,  1)
				8:		( 1,  1)
				*/
				float2 UVs[9];
			};

			UVPositions Vert (appdata_img v) {
				float2 uv = MultiplyUV(UNITY_MATRIX_TEXTURE0, v.texcoord);
				UVPositions uvPositions;
				for (int i = 0; i < 9; i++) {
					uvPositions[i] = UnityStereoScreenSpaceUVAdjust(uv + _MainTex_TexelSize.xy * uvDisplacements[i] * _SampleDistance, _MainTex_ST);
				}
				//o.uv[0] = UnityStereoScreenSpaceUVAdjust(uv, _MainTex_ST);
				//o.uv[1] = UnityStereoScreenSpaceUVAdjust(uv + _MainTex_TexelSize.xy * half2(-1,-1) * _SampleDistance, _MainTex_ST);
			}
			

			fixed4 Frag (v2f i) : SV_Target {
				return fixed4(0,0,0,0);
			}
			ENDCG
		}
	}
}
