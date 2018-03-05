Shader "Hidden/BladeEdgeDetection" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
		_EdgeColor ("Color of Edges", Color) = (1, 0, 0, 1)
		[Toggle]
		_DebugMode ("Debug mode on", Float) = 0
		_SampleDistance ("Sampling distance", Int) = 1
	}

	SubShader {
		Tags { "RenderType" = "Transparent" }
		Pass {

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			
			#include "UnityCG.cginc"
			float _DebugMode;

			// Tunable parameters
			float _DepthSensitivity;
			float _NormalSensitivity;
			int _SampleDistance;
			fixed4 _EdgeColor;

			// Source texture information, screen before edge detection is applied
			sampler2D _MainTex;
			uniform float4 _MainTex_TexelSize;
			half4 _MainTex_ST;

			// Depth + Normal texture information, gathered from Unity's built-in global shader variables
			sampler2D _CameraDepthNormalsTexture;
			half4 _CameraDepthNormalsTexture_ST;

			// Constants
			#define NUM_SAMPLES 9
			#define DEPTH_THRESHOLD_CONSTANT .054
			#define NORMAL_THRESHOLD_CONSTANT 0.1

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
				float4 vertex : SV_POSITION;
				float2 UVs[NUM_SAMPLES] : TEXCOORD0;
			};


			// TODO: Add a check for dot(normal, camera.forward) here to remove same surface depth dissimilarities
			// That way more oblique angles require much higher depth threshold to be considered similar
			half DepthsAreSimilar(float a, float b) {
				// Further values have more tolerance for difference in depth values
				float depthThreshold = DEPTH_THRESHOLD_CONSTANT * min(a, b);
				float depthDiff = abs(a - b);

				int isSameDepth = depthDiff * _DepthSensitivity < depthThreshold;
				return isSameDepth ? 1.0 : 0.0;
			}

			half NormalsAreSimilar(half2 a, half2 b) {
				half2 normalDiff = distance(a, b);

				return normalDiff * _NormalSensitivity < NORMAL_THRESHOLD_CONSTANT;
			}

			// Returns 1 if all four normals are similar, 0 otherwise
			half FourNormalArtifactCheck(half2 a, half2 b, half2 c, half2 d) {
				half ab = NormalsAreSimilar(a, b);
				half ac = NormalsAreSimilar(a, c);
				half ad = NormalsAreSimilar(a, d);
				half bc = NormalsAreSimilar(b, c);
				half bd = NormalsAreSimilar(b, d);
				half cd = NormalsAreSimilar(c, d);
				return ab * ac * ad * bc * bd * cd;
			}

			UVPositions Vert (appdata_img v) {
				// Constant offsets:
				const half2 uvDisplacements[NUM_SAMPLES] = {
					half2(0,0),
					half2(-1,-1),
					half2(0,-1),
					half2(1,-1),
					half2(-1,0),
					half2(1,0),
					half2(-1,1),
					half2(0,1),
					half2(1,1),
				};
				float2 center = MultiplyUV(UNITY_MATRIX_TEXTURE0, v.texcoord);

				// Set up the UV samples
				UVPositions uvPositions;
				uvPositions.vertex = UnityObjectToClipPos(v.vertex);
				for (int i = 0; i < NUM_SAMPLES; i++) {
					uvPositions.UVs[i] = UnityStereoScreenSpaceUVAdjust(center + _MainTex_TexelSize.xy * _SampleDistance * uvDisplacements[i], _MainTex_ST);
				}

				return uvPositions;
			}
			
			fixed4 Frag (UVPositions uvPositions) : SV_Target {
				fixed4 original = tex2D(_MainTex, uvPositions.UVs[0]);

				// Initialize the pixel samples
				half4 samples[NUM_SAMPLES];
				for (int i = 0; i < NUM_SAMPLES; i++) {
					samples[i] = tex2D(_CameraDepthNormalsTexture, uvPositions.UVs[i]);
				}
				float depthSamples[NUM_SAMPLES];
				for (int d = 0; d < NUM_SAMPLES; d++) {
					depthSamples[d] = DecodeFloatRG(samples[d].zw);
				}
				half2 normalSamples[NUM_SAMPLES];
				for (int n = 0; n < NUM_SAMPLES; n++) {
					normalSamples[n] = samples[n].xy;
				}
				
				// Check depth similarity with surrounding samples
				half similarDepth = 1;
				for (int x = 1; x < NUM_SAMPLES; x++) {
					float depthDiff = depthSamples[0] - depthSamples[x];
					half similar = DepthsAreSimilar(depthSamples[0], depthSamples[x]);
					////////////////////////
					// Depth Double-Check //
					////////////////////////
					/* If an edge is detected due to sufficient difference in depth,
					   double-check with the pixel on the opposite side.

					    -------------------
					    |  10 | 110 | 210 |  Not an edge, more likely an obliquely viewed surface
						-------------------

						-------------------
						|  10 | 110 | 120 |  More likely an edge than an obliquely viewed surface
						-------------------

					*/
					if (similar < 1) {
						// If the oppositeDepthDiff is similar to the depthDiff, don't consider this an edge
						float oppositeDepthDiff = depthSamples[NUM_SAMPLES-x] - depthSamples[0];
						similar = (abs(oppositeDepthDiff-depthDiff) * _DepthSensitivity < DEPTH_THRESHOLD_CONSTANT * depthSamples[0]) ? 1 : 0;
					}

					similarDepth *= similar;
				}

				// Check normal similarity with surrounding samples
				half similarNormals = 1;
				for (int y = 1; y < NUM_SAMPLES; y++) {
					half similar = NormalsAreSimilar(normalSamples[0], normalSamples[y]);

					similarNormals *= similar;
				}

				/////////////////////////
				// Normal Double-Check //
				/////////////////////////
				/* If an edge is detected due to a difference in normals,
					double-check with the pixel on the opposite side.

					-------------------------------
					| (1,0,0) | (0,1,0) | (1,0,0) |  Not an edge, more likely to be an artifact from floating point error near two adjacent faces
					-------------------------------

					-------------------------------
					| (1,0,0) | (0,1,0) | (0,1,0) |  More likely an edge than an artifact from close faces/Z-fighting
					-------------------------------

				*/
				if (similarNormals < 1) {
					half crossIsSimilar = FourNormalArtifactCheck(normalSamples[2], normalSamples[4], normalSamples[5], normalSamples[7]);
					half xCrossIsSimilar = FourNormalArtifactCheck(normalSamples[1], normalSamples[3], normalSamples[6], normalSamples[8]);

					similarNormals = max(crossIsSimilar, xCrossIsSimilar);
				}


				int isEdge = 1 - (similarDepth * similarNormals);

				// Return values for normal render mode and debug mode
				fixed4 edgeDetectResult = ((1 - isEdge) * original) + (isEdge * _EdgeColor);
				fixed4 debugColors = (1-similarDepth) * fixed4(1,0,0,1) + (1-similarNormals) * fixed4(0,1,0,1);

				return (_DebugMode * debugColors) + (1-_DebugMode) * edgeDetectResult;
			}
			ENDCG
		}
	}
}
