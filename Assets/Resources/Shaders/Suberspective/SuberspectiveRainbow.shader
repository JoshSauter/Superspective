Shader "Suberspective/SuberspectiveRainbow" {
	Properties {
		_Distortion("Distortion", Range(0.0, 1.0)) = 0.5
		
		_SpecColor("Color", Color) = (1.0,1.0,1.0)
		_Shininess("Shininess", Range(0.001, 100.0)) = 1
		_DiffuseMagnitude("Diffuse Magnitude", Range(0.0, 1.0)) = 0.25
		_SpecularMagnitude("Specular Magnitude", Range(0.0, 1.0)) = 1
		_AmbientMagnitude("Ambient Magnitude", Range(0.0, 1.0)) = 1
		
    	// These are my internal cached values for the inspector enum
    	[HideInInspector] __SuberspectiveBlendMode("InternalBlendModeCache", Int) = 0
        [HideInInspector] __CullMode("__cull_mode", Float) = 2.0
        [HideInInspector] __BlendOp("__blend_op", Float) = 0.0
        [HideInInspector] __SrcBlend("__src", Float) = 1.0
		[HideInInspector] __DstBlend("__dst", Float) = 0.0
		[HideInInspector] __ZWrite("__zw", Float) = 1.0
    	
    	_MainTex("Main Texture", 2D) = "white" {}
		_Color("Main Color", Color) = (1.0, 1.0, 1.0, 1.0)
    	_EmissionEnabled("Emission enabled", Float) = 1.0
    	_EmissionMap("Emission Map", 2D) = "white" {}
		[HDR]
		_EmissionColor("Emissive Color", Color) = (0, 0, 0, 0)
    	// DimensionObject
		_Inverse("Inverted (true: 1, false: 0)", Int) = 0
    	// Hardcoded 8 here because no way to read NUM_CHANNELS from DimensionShaderHelpers.cginc
    	_Channel("Channel", Range(0, 8)) = 0
    	// DissolveObject
        _DissolveTex("Dissolve Texture", 2D) = "white" {}
        _DissolveAmount("Dissolve Amount", Range(0.0, 1.0)) = 0
        _DissolveBurnSize("Burn Size", Range(0.0, 1.0)) = 0.15
        _DissolveBurnRamp("Burn Ramp (RGB)", 2D) = "white" {}
        _DissolveBurnColor("Burn Color", Color) = (.3,.6,1,1)
    	_DissolveEmissionAmount("Dissolve Emission Amount", float) = 2.0
    	// PowerTrail
    	_CapsuleRadius("SDF Capsule Radius", float) = .25
    	_ReverseVisibility("Invert PowerTrail", Int) = 0
    	// AffectedByShutter
        _ShutterNoise ("Noise Texture", 2D) = "black" {}
        _ShutterInverse ("Inverse", Int) = 0
        // PortalCopy
		_PortalPos("Portal Position", Vector) = (0, 0, 0)
		_PortalNormal("Portal Normal", Vector) = (0, 0, 1)
		_FudgeDistance("Fudge Position", Float) = 0.0
    	// RenderInZone
    	_MinRenderZone("Min Render Zone", Vector) = (0,0,0)
    	_MaxRenderZone("Max Render Zone", Vector) = (0,0,0)
		// Distance Fade
		_DistanceFadeEffectMagnitude("Distance Fade Effect Magnitude", Range(0, 1)) = 0.0
		_DistanceFadeEffectStartDistance("Distance Fade Effect Start Distance", Range(0, 1)) = 0.0
	}
	
	CGINCLUDE
		#include "SuberspectiveLitHelpers.cginc"
		#include "../Raymarching/RaymarchingUtils.cginc"
		
		float worldSDF(in float3 pos) {
		    // Time variables for dynamic transformations
		    float sinTime = (_SinTime.y + 1) * 0.5;
		    float cosTime = (_CosTime.y + 1) * 0.5;

		    // Twisting space using time
		    float twist = sinTime * 3.1415;
		    //pos.xz = mul(rot2D(twist), pos.xz);
		    //pos.xy = mul(rot2D(cosTime * 3.1415), pos.xy);

		    // Infinite repetition
		    float baseSize = 8.0;
		    float3 repeatPos = repeatRegular(pos, baseSize);

		    // Layer 1: Central cube with dynamic scaling
		    float cubeSize = 3.0 + sinTime;
		    float cube = boxSDF(repeatPos, cubeSize);

		    // Layer 2: Hollow boxes for intersecting frames
		    float frameSize = 4.0 + cosTime;
		    float frameThickness = 0.5;
		    float hollowBox = emptyCubeSDF(repeat(repeatPos, float3(baseSize / 2.0, baseSize / 2.0, baseSize)), frameSize, frameThickness);

		    // Layer 4: Empty boxes carving out patterns
		    float carveSize = cubeSize - 0.5;
		    float carveThickness = 0.8;
		    float carvedBox = emptyBoxSDF(repeatRegular(repeatPos, baseSize), float3(carveSize, carveSize, carveSize), carveThickness);

		    // Combining layers
		    float combined = smoothIntersectionSDF(cube, hollowBox, 0.5);
		    combined = diffSDF(combined, carvedBox);

		    // Layer 5: Subtractive dynamic tunnels
		    float tunnel = boxSDF(repeat(pos, float3(baseSize, baseSize / 2.0, baseSize)), float3(2.0, 2.0, 10.0));
		    float result = diffSDF(combined, tunnel);

		    return result;
		}



	ENDCG
	
	SubShader {
        Tags { "LightMode" = "ForwardBase" "Queue"="Geometry" "RenderType"="Suberspective" }
        LOD 100
        
    	Cull[__CullMode]
    	BlendOp[__BlendOp]
    	Blend[__SrcBlend][__DstBlend]
    	ZWrite[__ZWrite]

		Pass {
			CGPROGRAM

			#pragma vertex SuberspectiveVert
			#pragma fragment frag

			#define SDF(p) worldSDF(p)
			#include "../Raymarching/RaymarchingMacros.cginc"

			// user defined variables
			uniform float4 _SpecColor;
			uniform float _Shininess;

			uniform float _Distortion;

			uniform float _DiffuseMagnitude;
			uniform float _SpecularMagnitude;
			uniform float _AmbientMagnitude;

			uniform float _DistanceFadeEffectMagnitude;
			uniform float _DistanceFadeEffectStartDistance;
			uniform float3 _PlayerCamPos;

			// unity defined variables
			uniform float4 _LightColor0;

            #define MAX_STEPS 50

            fixed4 frag (SuberspectiveV2F i) : SV_Target {
				// vectors
				float3 normalDirection = i.normalDir;
				float atten = 1.0;

				// lighting
				float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
				float3 diffuseReflection = atten * _LightColor0.xyz * max(0.0, dot(normalDirection, lightDirection));

				// specular direction
				float3 lightReflectDirection = reflect(-lightDirection, normalDirection);
                float3 viewDirection = normalize(i.worldPos - _WorldSpaceCameraPos);
            	viewDirection.x += 0.5 * sin(_Time.y * 0.123);
            	viewDirection.y += 0.25 * sin(_Time.x + 0.5);
            	viewDirection.z += 0.25 * _CosTime.x;
            	viewDirection = normalize(viewDirection);
				
				float4 rainbowColor = abs(float4(viewDirection.x, viewDirection.y, viewDirection.z, _Color.a));

				SuberspectiveClipOnly(i);

            	float4 raymarchResult = Raymarch(MAX_STEPS, i.worldPos, 100, 400) * rainbowColor;

            	return rainbowColor;
                return rainbowColor * raymarchResult.r;
            }

            float rand(float3 co) {
				float r1 = frac(sin(dot(co, float3(127.1, 311.7, 74.7))) * 43758.5453);
				float r2 = frac(sin(dot(co, float3(269.5, 183.3, 54.2))) * 43758.5453);
				return frac(r1 + r2);
			}

			float rand(float2 co) {
				return frac(sin(dot(co, float3(127.1, 311.7, 74.7))) * 43758.5453);
			}

			float4 addStars(SuberspectiveV2F i) {
				float3 randSeed = i.worldPos;
				float starThreshold = 0.995; // Controls how sparse the stars are
				float starBrightness = noised(randSeed * 100.0); // Adjust scale for desired distribution
				if (starBrightness < starThreshold) {
				    starBrightness = 0.0; // No star here
				}

				float starTwinkle = sin(_Time.y * 5.0 + rand(randSeed * 100.0) * 20.0) * 0.5 + 0.75;
				starBrightness *= starTwinkle;

				return 10000 * float4(starBrightness, starBrightness, starBrightness, 1.0);
			}
			ENDCG
		}
	}
	CustomEditor "SuberspectiveLitGUI"
}
