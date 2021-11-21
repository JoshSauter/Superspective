Shader "Custom/WhiteRoom/UnlitAngleFade" {
	Properties {
		_AngleFadeColor ("Main Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_AngleFadeColor2 ("Main Color 2", Color) = (0.0, 0.0, 0.0, 1.0)
		_InvertAngleFade("Invert Angle Fade", Int) = 0
		[HDR]
		_EmissionColor("Emissive Color", Color) = (0, 0, 0, 0)
		[HDR]
		_EmissionColor2("Emissive Color 2", Color) = (0, 0, 0, 0)
		
	    // These are my internal cached values for the inspector enum
    	[HideInInspector] __SuberspectiveBlendMode("InternalBlendModeCache", Int) = 0
        [HideInInspector] __CullMode("__cull_mode", Float) = 2.0
        [HideInInspector] __BlendOp("__blend_op", Float) = 0.0
        [HideInInspector] __SrcBlend("__src", Float) = 1.0
		[HideInInspector] __DstBlend("__dst", Float) = 0.0
		[HideInInspector] __ZWrite("__zw", Float) = 1.0
    	
    	_MainTex("Main Texture", 2D) = "white" {}
		_Color("Color", Color) = (1.0, 1.0, 1.0, 1.0)
    	_EmissionEnabled("Emission enabled", Float) = 1.0
    	_EmissionMap("Emission Map", 2D) = "white" {}
    	// DimensionObject
		_Inverse("Inverted (true: 1, false: 0)", Int) = 0
    	// Hardcoded 8 here because no way to read NUM_CHANNELS from DimensionShaderHelpers.cginc
    	_Channel("Channel", Range(0, 8)) = 0
    	// DissolveObject
        [HideInInspector] _DissolveColorAt0("Color at 0", Color) = (0,0,0,0)
        _DissolveColorAt1("Color at 1", Color) = (1,1,1,0)
        _DissolveTex("Dissolve Texture", 2D) = "white" {}
        _DissolveValue("Dissolve Amount", Range(0.0, 1.0)) = 0
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
	}
	SubShader {
        Tags { "Queue"="Geometry" "RenderType"="Suberspective" }
        LOD 100
        
    	Cull[__CullMode]
    	BlendOp[__BlendOp]
    	Blend[__SrcBlend][__DstBlend]
    	ZWrite[__ZWrite]

		Pass {
			CGPROGRAM
			#pragma vertex SuberspectiveVert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "Assets/Resources/Shaders/Suberspective/SuberspectiveHelpers.cginc"

			float4 _AngleFadeColor, _AngleFadeColor2;
			float4 _EmissionColor, _EmissionColor2;
			float3 _ColorChangePoint;
			float3 _ColorChangeAxis;
			float _ColorChangeWidth;
			float3 _ZeroDegreesVector, _NinetyDegreesVector;

			int _InvertAngleFade;

			float inverseLerp(float a, float b, float value) {
				return (value - a) / (b - a);
			}

			float float3InverseLerp(float3 a, float3 b, float3 value) {
				float3 AB = b - a;
				float3 AV = value - a;
				return dot(AV, AB) / dot(AB, AB);
			}

			float3 proj(float3 p, float3 planePoint, float3 planeNormal) {
				float distance = dot((p - planePoint), planeNormal);
				return p - planeNormal * distance;
			}

			float getLerpValueOfPos(float3 p) {
				float3 projectedPos = proj(p, _ColorChangePoint, normalize(_ColorChangeAxis));
				float actualLerpPos = float3InverseLerp(normalize(_ZeroDegreesVector), normalize(_NinetyDegreesVector), normalize(projectedPos-_ColorChangePoint));

				float startValue = .5 - _ColorChangeWidth/2.0;
				float endValue = .5 + _ColorChangeWidth/2.0;

				return saturate(inverseLerp(startValue, endValue, actualLerpPos));
			}
			
			fixed4 frag(SuberspectiveV2F i) : SV_Target {
				float t = getLerpValueOfPos(i.worldPos);
				t = _InvertAngleFade*(1-t) + (1-_InvertAngleFade)*t;

				// sample the texture
				fixed4 col = lerp(_AngleFadeColor, _AngleFadeColor2, t);

				half unused = 1;
				SuberspectiveRender(i, col, unused);
				// apply fog
				col += lerp(_EmissionColor, _EmissionColor2, t);
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
	//CustomEditor "SuberspectiveGUI"
}
