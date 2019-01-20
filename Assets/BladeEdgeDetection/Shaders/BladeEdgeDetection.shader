Shader "Hidden/BladeEdgeDetection" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
		_EdgeColor ("Color of Edges", Color) = (0, 0, 0, 1)
		[Toggle]
		_DebugMode ("Debug mode on", Float) = 0
		_SampleDistance ("Sampling distance", Int) = 1
		_DepthSensitivity ("Depth sensitivity", Float) = 1
		_NormalSensitivity ("Normal sensitivity", Float) = 1
	}

	SubShader {
		Tags { "RenderType" = "Transparent" }
		Pass {

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#include "UnityCG.cginc"
			float _DebugMode;

			#pragma multi_compile __ DOUBLE_SIDED_EDGES

			// Constants
			#define NUM_SAMPLES 9
#ifdef DOUBLE_SIDED_EDGES
			#define SAMPLE_RANGE_START 5
#else
			#define SAMPLE_RANGE_START 7
#endif
			#define DEPTH_THRESHOLD_BASELINE 0.001
			#define DEPTH_THRESHOLD_CONSTANT 0.16
			#define NORMAL_THRESHOLD_CONSTANT 0.1
			#define OBLIQUENESS_THRESHOLD 0.95
			#define GRADIENT_RESOLUTION 10	// 10 == MaxNumberOfKeysSetInGradient + 1 (for keyTime of 0) + 1 (for keyTime of 1)

			// Artifact checks -- turn off at your own risk to edge quality
			#define FILL_IN_ARTIFACTS		// Fill in pixels which appear to be an artifact with a neighboring pixel
			#define DEPTH_DOUBLE_CHECKS		// Double-checks on depth-detected edges to remove artifacts and de-duplicate nearby edges
			#define DEPTH_ARTIFACT_CHECK	// Checks in a cross and plus pattern around the pixel to check for artifacts from depth-detected edges
			#define NORMAL_ARTIFACT_CHECK	// Checks in a cross and plus pattern around the pixel to check for artifacts from normal-detected edges
			#define SMOOTH_SURFACE_CHECK	// Makes it so that rounded edges appear less often as normal-detected edges by checking the gradient of normal changes

			// Tunable parameters
			float _DepthSensitivity;
			float _NormalSensitivity;
			int _SampleDistance;

			// Edge Colors
			int _ColorMode;				// 0 == Simple color, 1 == Gradient from inspector, 2 == Color ramp (gradient) texture
			fixed4 _EdgeColor;
			// Edge gradient
			int _GradientMode;			// 0 == Blend, 1 == Fixed
			float _GradientAlphaKeyTimes[GRADIENT_RESOLUTION];
			float _AlphaGradient[GRADIENT_RESOLUTION];
			float _GradientKeyTimes[GRADIENT_RESOLUTION];
			fixed4 _EdgeColorGradient[GRADIENT_RESOLUTION];
			// Edge gradient from texture
			sampler2D _GradientTexture;

			// Source texture information, screen before edge detection is applied
			sampler2D _MainTex;
			uniform float4 _MainTex_TexelSize;
			half4 _MainTex_ST;

			// Depth + Normal texture information, gathered from Unity's built-in global shader variables
			sampler2D _CameraDepthNormalsTexture;
			half4 _CameraDepthNormalsTexture_ST;

			struct UVPositions {
				/* UVs are laid out as below (0 is center, 1-4 are corner neighbors, 5-8 are cardinal neighbors)
					 -1   0   1

					–––––––––––––
			    1   | 4 | 7 | 3 |
					|–––––––––––|
				0   | 8 | 0 | 6 |
					|–––––––––––|
			   -1   | 1 | 5 | 2 |
					–––––––––––––

				Or, as displacement from center:
				i:		(col, row)
				–––––––––––––––––
				0:		( 0,  0)    >     center

				1:		(-1, -1)    v
				2:		( 1, -1)    |    corner neighbors
				3:		( 1,  1)    |
				4:		(-1,  1)    ^

				5:		( 0, -1)    v
				6:		( 1,  0)    |    cardinal neighbors
				7:		( 0,  1)    |
				8:		(-1,  0)    ^

				*/
				float4 vertex : SV_POSITION;
				float2 UVs[NUM_SAMPLES] : TEXCOORD0;
			};
			
			// Returns the coordinate on the opposite side of the center
			// (1 -> 3), (2 -> 4), (3 -> 1), (4 -> 2), (5 -> 7), (6 -> 8), (7 -> 5), (8 -> 6)
			uint GetOppositeIndex(uint i) {
				return 4 * step(5, i) + ((i + 1) % 4 + 1);
			}


			float ObliquenessFromNormal(float3 sampleNormal, float2 scrPos) {
				// Credit to Keijiro Takahashi of Unity Japan (and this thread: https://forum.unity.com/threads/help-with-view-space-normals.454248/)
				// get the perspective projection
				float2 p11_22 = float2(unity_CameraProjection._11, unity_CameraProjection._22);
				// convert the uvs into view space by "undoing" projection
				float3 viewDir = -normalize(float3((scrPos * 2 - 1) / p11_22, -1));

				float fresnel = 1.0 - dot(viewDir.xyz, sampleNormal);

				return fresnel;
			}

			/* Depth edges are detected based on differences in the ratio of depth values between
				the sample pixel x with the center pixel, and the opposite pixel x' with the center pixel.
				For example, consider the following made-up depth values below:
				-------------------
				|  x  | mid |  x' |  depthRatio = x/mid, oppositeDepthRatio = x'/mid
				-------------------

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
			half DepthRatiosAreSimilar(float center, float a, float b, float obliqueness) {
				float depthRatio = max(a, center) / min(a, center);
				float oppositeDepthRatio = max(b, center) / min(b, center);
				
				// Highly oblique angles have more tolerance for differences in depth ratios (due to perspective)
				float obliquenessModifier = (1-obliqueness);

				// Multiplied by magic number 100 just to keep things from getting too close to underflow levels (DEPTH_THRESHOLD_CONSTANT is 100x higher to compensate)
				// Lower depth values == higher depth ratio diffs to be considered an edge (2:1 ratio means very different things at low depth values and high depth values)
				int isEdge = 100 * (depthRatio - oppositeDepthRatio) * _DepthSensitivity * center * obliquenessModifier > DEPTH_THRESHOLD_CONSTANT;

				return !isEdge;
			}

			half DepthValuesAreSimilar(float a, float b, float obliqueness) {
				// Further values have more tolerance for difference in depth values
				float depthThreshold = DEPTH_THRESHOLD_BASELINE + DEPTH_THRESHOLD_CONSTANT * min(a, b);
				float depthDiff = abs(a - b);
				
				// Multiplied by magic number 100 just to keep things from getting too close to underflow levels (DEPTH_THRESHOLD_CONSTANT is 100x higher to compensate)
				int isSameDepth = 100 * depthDiff * _DepthSensitivity * (1-obliqueness) < depthThreshold;
				return isSameDepth ? 1.0 : 0.0;
			}

			half NormalsAreSimilar(half2 a, half2 b) {
				half normalDiff = distance(a, b);
				half isEdge = normalDiff * _NormalSensitivity > NORMAL_THRESHOLD_CONSTANT;

				return !isEdge;
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
				half ab = DepthValuesAreSimilar(a, b, obA);
				half ac = DepthValuesAreSimilar(a, c, obA);
				half ad = DepthValuesAreSimilar(a, d, obA);
				half bc = DepthValuesAreSimilar(b, c, obB);
				half bd = DepthValuesAreSimilar(b, d, obB);
				half cd = DepthValuesAreSimilar(c, d, obC);
				return ab * ac * ad * bc * bd * cd;
			}

			// Gradient here refers to the 2D derivative, not the color gradient
			// Returns 1 if no discontinuities in normals are detected, 0 otherwise
			half AllGradientsAreSimilar(half2 original, half2 topLeft, half2 topRight, half2 botRight, half2 botLeft, half2 topCenter, half2 midRight, half2 botCenter, half2 midLeft) {
				half backslash = NormalsAreSimilar(topLeft - original, original - botRight);
				half pipe = NormalsAreSimilar(topCenter - original, original - botCenter);
				half slash = NormalsAreSimilar(topRight - original, original - botLeft);
				half dash = NormalsAreSimilar(midLeft - original, original - midRight);

				return backslash * pipe * slash * dash;
			}

			UVPositions Vert(appdata_img v) {
				// Constant offsets:
				const half2 uvDisplacements[NUM_SAMPLES] = {
					// center
					half2(0,  0),
					// corner neighbors
					half2(-1, -1),
					half2(1, -1),
					half2(1,  1),
					half2(-1,  1),
					// cardinal neighbors
					half2(0, -1),
					half2(1,  0),
					half2(0,  1),
					half2(-1,  0)
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

			float4 GradientColor(float depth) {
				float4 colorChosen = float4(0,0,0,0);
				float alphaValue = 0;
				depth = saturate(depth);
				for (int i = 1; i < GRADIENT_RESOLUTION; i++) {
					// inRange is equivalent to "if (depth > _GradientKeyTimes[i-1] && depth <= _GradientKeyTimes[i])"
					int colorInRange = (1-step(depth, _GradientKeyTimes[i-1])) * step(depth, _GradientKeyTimes[i]);
					int alphaInRange = (1-step(depth, _GradientAlphaKeyTimes[i-1])) * step(depth, _GradientAlphaKeyTimes[i]);
					float colorLerpValue = saturate((depth - _GradientKeyTimes[i-1]) / (_GradientKeyTimes[i] - _GradientKeyTimes[i-1]) + _GradientMode);
					float alphaLerpValue = saturate((depth - _GradientAlphaKeyTimes[i-1]) / (_GradientAlphaKeyTimes[i] - _GradientAlphaKeyTimes[i-1]) + _GradientMode);

					colorChosen += colorInRange * lerp(_EdgeColorGradient[i-1], _EdgeColorGradient[i], colorLerpValue);
					alphaValue += alphaInRange * lerp(_AlphaGradient[i-1], _AlphaGradient[i], alphaLerpValue);
				}
				colorChosen.a = alphaValue;

				return colorChosen;
			}

			float4 GradientFromTexture(float depth) {
				return tex2D(_GradientTexture, depth);
			}

			fixed4 FinalColor(fixed4 original, half isEdge, half similarDepth, half similarNormals, float depth) {
				fixed4 edgeColor = fixed4(0,0,0,0);
				if (_ColorMode == 0) edgeColor = _EdgeColor;
				if (_ColorMode == 1) edgeColor = GradientColor(depth);
				if (_ColorMode == 2) edgeColor = GradientFromTexture(depth);
				fixed4 gradientColor = GradientColor(depth);
				// Return values for normal render mode and debug mode
				fixed4 edgeDetectResult = ((1 - isEdge) * original) + (isEdge * lerp(original, edgeColor, edgeColor.a));
				// Debug colors are: Red if this is a depth-difference edge, Green if this is a normal-difference edge, yellow if both
				fixed4 debugColors = (1-similarDepth) * fixed4(1,0,0,1) + (1-similarNormals) * fixed4(0,1,0,1);

				return (_DebugMode * debugColors) + (1-_DebugMode) * edgeDetectResult;
			}

			fixed4 Frag(UVPositions uvPositions) : SV_Target {
				fixed4 original = tex2D(_MainTex, uvPositions.UVs[0]);

				// Initialize the pixel samples
				half4 samples[NUM_SAMPLES];
				for (int i = 0; i < NUM_SAMPLES; i++) {
					samples[i] = tex2D(_CameraDepthNormalsTexture, uvPositions.UVs[i]);
				}
				float minDepthValue = 1;
				float depthSamples[NUM_SAMPLES];
				for (int d = 0; d < NUM_SAMPLES; d++) {
					float depthValue = DecodeFloatRG(samples[d].zw);
					depthSamples[d] = depthValue;
					minDepthValue = min(minDepthValue, depthValue);
				}

				half2 normalSamples[NUM_SAMPLES];
				for (int n = 0; n < NUM_SAMPLES; n++) {
					normalSamples[n] = samples[n].xy;
				}
				float avgObliqueness = 0;
				float obliqueness[NUM_SAMPLES];
				for (int nd = 0; nd < NUM_SAMPLES; nd++) {
					float dummyDepth;
					float3 decodedNormal;
					DecodeDepthNormal(samples[nd], dummyDepth, decodedNormal);
					obliqueness[nd] = ObliquenessFromNormal(decodedNormal, uvPositions.UVs[nd]);
					avgObliqueness += obliqueness[nd];
				}
				avgObliqueness /= NUM_SAMPLES;

				// Check depth and normal similarity with surrounding samples
				half similarDepth = 1;
				half similarNormals = 1;
				half allDepthsAreDissimilar = 1;
				for (int x = 1; x < NUM_SAMPLES; x++) {
					uint oppositeIndex = GetOppositeIndex(x);
					half thisDepthIsSimilar = DepthRatiosAreSimilar(depthSamples[0], depthSamples[x], depthSamples[oppositeIndex], avgObliqueness);
					half thisNormalIsSimilar = NormalsAreSimilar(normalSamples[0], normalSamples[x]);

					// Keep track of whether all depths appear different before any double-checks
					allDepthsAreDissimilar *= (1 - thisDepthIsSimilar);
					/////////////////////////
					// Depth Double-Checks //
					/////////////////////////
					/*
						We perform the following double-checks on depth-detected edges to remove artifacts and de-duplicate nearby edges:
						1) isBorder check -- If we cannot compare depth ratios due to being on the border of the screen, don't consider this an edge
						2) similarDiffs -- De-dupes edges detected by ratios on concave corners by comparing depth values rather than ratios
						3) nextToEdge -- De-dupes edges in single-sided edge mode that appear on the opposite side of normal-detected edges (thus falsely making it double-sided)
					*/
#ifdef DEPTH_DOUBLE_CHECKS
					if (thisDepthIsSimilar < 1) {
						// If the oppositeDepthDiff is similar to the depthDiff, don't consider this an edge
						float depthDiff = depthSamples[0] - depthSamples[x];
						float oppositeDepthDiff = depthSamples[oppositeIndex] - depthSamples[0];
						
						half isBorder = 1 - (step(0, uvPositions.UVs[2].y) * step(uvPositions.UVs[7].y, 1) * step(0, uvPositions.UVs[8].x) * step(uvPositions.UVs[6], 1));
						
						float depthRatio = max(depthSamples[0], depthSamples[x]) / min(depthSamples[0], depthSamples[x]);
						float oppositeDepthRatio = max(depthSamples[0], depthSamples[oppositeIndex]) / min(depthSamples[0], depthSamples[oppositeIndex]);
						
						int obliqueModifer = step(OBLIQUENESS_THRESHOLD, obliqueness[0]);
						// Check if this depth sample is next to an edge by comparing the depth ratios (possibly with obliqueness modifiers)
						half nextToEdge = depthRatio * 1-(obliqueModifer * obliqueness[0]) < oppositeDepthRatio * 1-(obliqueModifer * (1-obliqueness[0])) ? 1 : 0;
						
						half similarDiffs = (abs(oppositeDepthDiff - depthDiff) * _DepthSensitivity * (1-avgObliqueness) * abs(depthDiff) < (DEPTH_THRESHOLD_BASELINE/1000) + abs(oppositeDepthDiff) * DEPTH_THRESHOLD_CONSTANT * depthSamples[0]) ? 1 : 0;
						thisDepthIsSimilar = max(isBorder, max(similarDiffs, nextToEdge));
					}
#endif

					// equivalent to "if (x >= SAMPLE_RANGE_START) similarDepth *= thisDepthIsSimilar;"
					similarDepth *= max(thisDepthIsSimilar, (1 - step(SAMPLE_RANGE_START, x)));
					
					// equivalent to "if (x >= SAMPLE_RANGE_START) similarNormals *= thisNormalIsSimilar;"
					similarNormals *= max(thisNormalIsSimilar, (1 - step(SAMPLE_RANGE_START, x)));
				}

#ifdef FILL_IN_ARTIFACTS
				// If this pixel seems to be an artifact due to all depths being dissimilar, color it in with an adjacent pixel (and exit edge detection)
				if (allDepthsAreDissimilar > 0) {
					return FinalColor(tex2D(_MainTex, uvPositions.UVs[2]), 0, 1, 1, minDepthValue);
				}
#endif

				// If this pixel seems to be an edge when compared to every pixel around it, it is probably an artifact and should not be considered an edge
				similarDepth = max(similarDepth, allDepthsAreDissimilar);

#ifdef DEPTH_ARTIFACT_CHECK
				if (similarDepth < 1) {
					// If the depths of a +Cross or xCross are similar, don't consider this an edge
					half crossIsSimilar = FourDepthArtifactCheck(depthSamples[5], depthSamples[6], depthSamples[7], depthSamples[8],
																  obliqueness[5],  obliqueness[6],  obliqueness[7],  obliqueness[8]);
					half xCrossIsSimilar = FourDepthArtifactCheck(depthSamples[1], depthSamples[2], depthSamples[3], depthSamples[4],
																   obliqueness[1],  obliqueness[2],  obliqueness[3],  obliqueness[4]);

					similarDepth = max(crossIsSimilar, xCrossIsSimilar);
				}
#endif

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
					half crossIsSimilar = 0;
					half xCrossIsSimilar = 0;
					half gradientIsSimilar = 0;
#ifdef NORMAL_ARTIFACT_CHECK
					crossIsSimilar = FourNormalArtifactCheck(normalSamples[5], normalSamples[6], normalSamples[7], normalSamples[8]);
					xCrossIsSimilar = FourNormalArtifactCheck(normalSamples[1], normalSamples[2], normalSamples[3], normalSamples[4]);
#endif
					/*  Additionally, if there is a smooth and continuous difference in normals across the samples,
						it is more likely that this is a smoothly curved object rather than a true edge

						---------------------------------
						| (1,0,-1) | (0,0,0) | (-1,0,1) |  Not an edge, more likely to be a smoothly curving surface
						---------------------------------
					*/
#ifdef SMOOTH_SURFACE_CHECK
					// Gradient here refers to the 2D derivative, not the color gradient
					gradientIsSimilar = AllGradientsAreSimilar(
						normalSamples[0],
						normalSamples[1],
						normalSamples[2],
						normalSamples[3],
						normalSamples[4],
						normalSamples[5],
						normalSamples[6],
						normalSamples[7],
						normalSamples[8]
					);
#endif

					similarNormals = max(gradientIsSimilar, max(crossIsSimilar, xCrossIsSimilar));
				}

				int isEdge = 1 - (similarDepth * similarNormals);
				return FinalColor(original, isEdge, similarDepth, similarNormals, minDepthValue);
			}
			ENDCG
		}
	}
}
