Shader "Suberspective/SuberspectivePortal" {
    Properties {
		_DepthNormals("Depth Normals Texture", 2D) = "white" {}
    	
	    // These are my internal cached values for the inspector enum
    	[HideInInspector] __SuberspectiveBlendMode("InternalBlendModeCache", Int) = 0
        [HideInInspector] __CullMode("__cull_mode", Float) = 2.0
        [HideInInspector] __BlendOp("__blend_op", Float) = 0.0
        [HideInInspector] __SrcBlend("__src", Float) = 1.0
		[HideInInspector] __DstBlend("__dst", Float) = 0.0
		[HideInInspector] __ZWrite("__zw", Float) = 1.0
		
        _PortalScaleFactor("Portal scale factor", Range(0.000001, 1000000.0)) = 1
    	_PortalRenderingDisabled("Portal Rendering Disabled (true: 1, false: 0)", Float) = 0.0
    	
    	_MainTex("Main Texture", 2D) = "white" {}
		_Color("Main Color", Color) = (1.0, 1.0, 1.0, 1.0)
    	_EmissionEnabled("Emission enabled", Float) = 1.0
    	_EmissionMap("Emission Map", 2D) = "white" {}
		[HDR]
		_EmissionColor("Emissive Color", Color) = (0, 0, 0, 0)
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
    }
    SubShader {
		Tags { "RenderType" = "PortalMaterial" "Queue" = "Geometry" "PortalTag" = "Standard" }

        Pass {
			ZWrite On

			CGPROGRAM
				#pragma vertex SuberspectiveVert
				#pragma fragment frag

				#include "SuberspectiveHelpers.cginc"
				#include "../RecursivePortals/PortalSurfaceHelpers.cginc"
			
				#define GRADIENT_RESOLUTION 10	// 10 == MaxNumberOfKeysSetInGradient + 1 (for keyTime of 0) + 1 (for keyTime of 1)

				uniform sampler2D_float _DepthNormals;

				// Exposed here so we can read from the PortalMask replacement shader when adjusting depth through scaled Portals
				uniform float _PortalScaleFactor = 1;

				// When set to 1, the portal will not render anything
				uniform float _PortalRenderingDisabled = 0;

				// These are all defined so that they can be used in the EdgeDetectionColorsThroughPortal replacement shader
				//sampler2D _CameraDepthNormalsTexture;
            
            	// Edge Colors
				int _ColorMode;				// 0 == Simple color, 1 == Gradient from inspector, 2 == Color ramp (gradient) texture
				fixed4 _EdgeColor;
				// Edge gradient
				float3 _FrustumCorners[4];	// Used to convert depth-based gradient to distance-based gradient which doesn't change as camera looks around
				int _GradientMode;			// 0 == Blend, 1 == Fixed
				float _GradientAlphaKeyTimes[GRADIENT_RESOLUTION];
				float _AlphaGradient[GRADIENT_RESOLUTION];
				float _GradientKeyTimes[GRADIENT_RESOLUTION];
				fixed4 _EdgeColorGradient[GRADIENT_RESOLUTION];
				// Edge gradient from texture
				sampler2D _GradientTexture;

				float4 frag(SuberspectiveV2F i) : SV_Target {
					float2 uv = i.screenPos.xy / i.screenPos.w;
					
		            #ifdef DISSOLVE_OBJECT
		            SuberspectiveClipOnly(i.clipPos, i.uv_DissolveTex, i.worldPos);
		            #else
		            SuberspectiveClipOnly(i);
		            #endif

					if (_PortalRenderingDisabled > 0) {
						ClipDisabledPortalSurface(i.worldPos, _PortalNormal);
						float x = 0.5 * sin(_Time.x * 2 * 3.14159 + length(i.worldPos.xz) / 4.0) + 0.5;
						float y = 0.5 * cos(_Time.x * 2 * 3.14159 + length(i.worldPos.yz) / 4.0) + 0.5;
						return fixed4(0,x,y,1);
					}

					float4 col = tex2D(_MainTex, uv);
					return col;
				}
			ENDCG
        }
    }
	//CustomEditor "SuberspectiveGUI"
}
