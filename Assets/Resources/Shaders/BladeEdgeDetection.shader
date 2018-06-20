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
			#define FILL_IN_ARTIFACTS

			// Source texture information, screen before edge detection is applied
			sampler2D _MainTex;
			uniform float4 _MainTex_TexelSize;
			half4 _MainTex_ST;

			// Depth + Normal texture information, gathered from Unity's built-in global shader variables
			sampler2D _CameraDepthNormalsTexture;
			half4 _CameraDepthNormalsTexture_ST;

			// Constants
			#define NUM_SAMPLES 9
			#define DEPTH_THRESHOLD_BASELINE 0.0001
			#define DEPTH_THRESHOLD_CONSTANT 0.054
			#define NORMAL_THRESHOLD_CONSTANT 0.1
			#define OBLIQUENESS_FACTOR 100
			#define OBLIQUENESS_THRESHOLD 0.9

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

			float ObliquenessFromNormal(float3 sampleNormal, float2 scrPos) {
				// Credit to Keijiro Takahashi of Unity Japan (and this thread: https://forum.unity.com/threads/help-with-view-space-normals.454248/)
				// get the perspective projection
                float2 p11_22 = float2(unity_CameraProjection._11, unity_CameraProjection._22);
                // convert the uvs into view space by "undoing" projection
                float3 viewDir = -normalize(float3((scrPos * 2 - 1) / p11_22, -1));
 
                float fresnel = 1.0 - dot(viewDir.xyz, sampleNormal);
 
                return fresnel;
			}

			half DepthsAreSimilar(float a, float b, float obliqueness) {
				// Highly oblique angles have more tolerance for differences in depth values
				// "if (obliqueness > OBLIQUENESS_THRESHOLD) obliqueness = 0;"
				float obliquenessMultiplier = 1 + OBLIQUENESS_FACTOR * (step(OBLIQUENESS_THRESHOLD, obliqueness) * obliqueness);
				// Further values have more tolerance for difference in depth values
				float depthThreshold = DEPTH_THRESHOLD_BASELINE + obliquenessMultiplier * DEPTH_THRESHOLD_CONSTANT * min(a, b);
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

			// Returns 1 if all four depths are similar, 0 otherwise
			half FourDepthArtifactCheck(float a, float b, float c, float d, float obA, float obB, float obC, float obD) {
				half ab = DepthsAreSimilar(a, b, obA);
				half ac = DepthsAreSimilar(a, c, obA);
				half ad = DepthsAreSimilar(a, d, obA);
				half bc = DepthsAreSimilar(b, c, obB);
				half bd = DepthsAreSimilar(b, d, obB);
				half cd = DepthsAreSimilar(c, d, obC);
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

			fixed4 FinalColor(fixed4 original, half isEdge, half similarDepth, half similarNormals) {
				// Return values for normal render mode and debug mode
				fixed4 edgeDetectResult = ((1 - isEdge) * original) + (isEdge * _EdgeColor);
				fixed4 debugColors = (1-similarDepth) * fixed4(1,0,0,1) + (1-similarNormals) * fixed4(0,1,0,1);

				return (_DebugMode * debugColors) + (1-_DebugMode) * edgeDetectResult;
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
				float3 obliqueness[NUM_SAMPLES];
				for (int nd = 0; nd < NUM_SAMPLES; nd++) {
					float dummyDepth;
					float3 decodedNormal;
					DecodeDepthNormal(samples[nd], dummyDepth, decodedNormal);
					obliqueness[nd] = ObliquenessFromNormal(decodedNormal, uvPositions.UVs[nd]);
				}
				
				// Check depth similarity with surrounding samples
				half similarDepth = 1;
				half allDepthsAreDissimilar = 1;
				for (int x = 1; x < NUM_SAMPLES; x++) {
					float depthDiff = depthSamples[0] - depthSamples[x];
					half similar = DepthsAreSimilar(depthSamples[0], depthSamples[x], obliqueness[0]);
					////////////////////////
					// Depth Double-Check //
					////////////////////////
					/* If an edge is detected due to sufficient difference in depth,
					   double-check with the pixel on the opposite side.

					    -------------------
					    |  10 | 110 | 210 |  Not an edge, more likely an obliquely viewed surface
						-------------------
						
						-------------------
						|  10 | 110 |  10 |  Not an edge, more likely to be an artifact from floating point error near two adjacent faces
						-------------------

						-------------------
						|  10 | 110 | 120 |  More likely an edge than an obliquely viewed surface
						-------------------
					*/
					allDepthsAreDissimilar *= (1-similar);
					if (similar < 1) {
						// If the oppositeDepthDiff is similar to the depthDiff, don't consider this an edge
						float oppositeDepthDiff = depthSamples[NUM_SAMPLES-x] - depthSamples[0];
						similar = (abs(oppositeDepthDiff-depthDiff) * _DepthSensitivity < DEPTH_THRESHOLD_CONSTANT * depthSamples[0] * obliqueness[0]) ? 1 : 0;
					}

					similarDepth *= similar;
				}

#ifdef FILL_IN_ARTIFACTS
				// If this pixel seems to be an artifact due to all depths being dissimilar, color it in with an adjacent pixel (and exit edge detection)
				if (allDepthsAreDissimilar > 0) {
					return FinalColor(tex2D(_MainTex, uvPositions.UVs[2]), 0, 1, 1);
				}
#endif

				// If this pixel seems to be an edge when compared to every pixel around it, it is probably an artifact and should not be considered an edge
				similarDepth = max(similarDepth, allDepthsAreDissimilar);

				if (similarDepth < 1) {
					// If the depths of a +Cross or xCross are similar, don't consider this an edge
					half crossIsSimilar = FourDepthArtifactCheck(depthSamples[2], depthSamples[4], depthSamples[5], depthSamples[7],
																  obliqueness[2],  obliqueness[4],  obliqueness[5],  obliqueness[7]);
					half xCrossIsSimilar = FourDepthArtifactCheck(depthSamples[1], depthSamples[3], depthSamples[6], depthSamples[8],
					                                               obliqueness[1],  obliqueness[3],  obliqueness[6],  obliqueness[8]);

					similarDepth = max(crossIsSimilar, xCrossIsSimilar);
				}


				// Check normal similarity with surrounding samples
				half similarNormals = 1;
				for (int y = 1; y < NUM_SAMPLES; y++) {
					half similar = NormalsAreSimilar(normalSamples[0], normalSamples[y]);

					similarNormals *= similar;
				}

				//////////////////////////////////////////////////
				// Normal Double-Check A.K.A. The Chyr Maneuver //
				//////////////////////////////////////////////////
				/* If an edge is detected due to a difference in normals,
					double-check with the pixels on the opposite sides of a +Cross and xCross

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

				return FinalColor(original, isEdge, similarDepth, similarNormals);
			}
			ENDCG
		}
	}
}
