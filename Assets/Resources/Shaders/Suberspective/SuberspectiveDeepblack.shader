// Shader which will render the object in deepblack where the emission is enabled (color does not matter)
Shader "Suberspective/SuberspectiveDeepblack" {
    Properties {
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
        _DissolveAmount("Dissolve Amount", Float) = 0
        _DissolveValue("Dissolve Value", Float) = 0
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
        Tags { "Queue"="Geometry" "RenderType"="Suberspective" }
        LOD 100
        
    	Cull[__CullMode]
    	BlendOp[__BlendOp]
    	Blend[__SrcBlend][__DstBlend]
    	ZWrite[__ZWrite]

        Pass {
            CGPROGRAM
            #include "SuberspectiveHelpers.cginc"
            #pragma vertex SuberspectiveVert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                UNITY_FOG_COORDS(1)
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            	float2 emissionUV : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
            };
            
            fixed4 frag (SuberspectiveV2F i) : SV_Target {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv_MainTex) * _Color;
            	
            	// This is possibly set to false by PowerTrail
            	half emissionEnabled = _EmissionEnabled;
            	SuberspectiveRender(i, col, emissionEnabled);
            	
            	if (emissionEnabled > 0) {
            		// Return deepblack color (all channels 0) where emission is enabled
            		return 0;
            	}
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
	CustomEditor "SuberspectiveGUI"
}
