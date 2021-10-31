Shader "Suberspective/SuberspectiveLit" {
	Properties {
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
	}
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

			// user defined variables
			uniform float4 _Color;
            float _EmissionEnabled;
			float4 _EmissionColor;
			uniform float4 _SpecColor;
			uniform float _Shininess;

			uniform float _DiffuseMagnitude;
			uniform float _SpecularMagnitude;
			uniform float _AmbientMagnitude;

			// unity defined variables
			uniform float4 _LightColor0;
			
			#include "SuberspectiveLitHelpers.cginc"

			float4 frag(SuberspectiveV2F i) : COLOR {
				// vectors
				float3 normalDirection = i.normalDir;
				float atten = 1.0;

				// lighting
				float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
				float3 diffuseReflection = atten * _LightColor0.xyz * max(0.0, dot(normalDirection, lightDirection));

				// specular direction
				float3 lightReflectDirection = reflect(-lightDirection, normalDirection);
				float3 viewDirection = normalize(float3(float4(_WorldSpaceCameraPos.xyz, 1.0) - i.worldPos.xyz));
				// Following line makes a cool rainbow-glass effect, might want to use it:
				//return abs(float4(viewDirection.x, viewDirection.y, viewDirection.z, _Color.a));
				float3 lightSeeDirection = max(0.0,dot(lightReflectDirection, viewDirection));
				float3 shininessPower = pow(lightSeeDirection, _Shininess);

				float3 specularReflection = atten * _SpecColor.rgb  * shininessPower;
				float3 lightFinal = _DiffuseMagnitude*diffuseReflection +
					_SpecularMagnitude*specularReflection +
					_AmbientMagnitude*UNITY_LIGHTMODEL_AMBIENT;

                fixed4 col = tex2D(_MainTex, i.uv_MainTex) * _Color;
				col = float4(lightFinal + col.rgb, col.a);

				float emissionEnabled = 1.0;
				SuberspectiveRender(i, col, emissionEnabled);
				
            	fixed4 emissionSample = tex2D(_EmissionMap, i.uv_EmissionMap);
				
            	// Emission Sample Color -> Grayscale conversion:
            	float emissionBrightness = _EmissionEnabled * dot(emissionSample.rgb, float3(0.2126, 0.7152, 0.0722));
            	col += emissionEnabled * emissionBrightness * emissionSample.a * _EmissionColor;
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
			}
			ENDCG
		}
	}
	CustomEditor "SuberspectiveLitGUI"
}
