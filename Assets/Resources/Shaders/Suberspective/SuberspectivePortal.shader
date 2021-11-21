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
		Tags { "RenderType" = "PortalMaterial" "Queue" = "Geometry" "PortalTag" = "Standard" }

        Pass {
			ZWrite On

			CGPROGRAM
				#pragma vertex SuberspectiveVert
				#pragma fragment frag

				#include "SuberspectiveHelpers.cginc"

				uniform sampler2D _DepthNormals;

				fixed4 frag(SuberspectiveV2F i) : SV_Target {
					float2 uv = i.screenPos.xy / i.screenPos.w;
					
		            #ifdef DISSOLVE_OBJECT
		            SuberspectiveClipOnly(i.clipPos, i.dissolveUV, i.worldPos);
		            #else
		            SuberspectiveClipOnly(i);
		            #endif

					fixed4 col = tex2D(_MainTex, uv);
					return col;
				}
			ENDCG
        }
    }
	CustomEditor "SuberspectiveGUI"
}
