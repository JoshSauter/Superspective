Shader "Custom/Water" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		[NoScaleOffset] _FlowMap ("Flow (RG, A noise)", 2D) = "black" {}
		[NoScaleOffset] _DerivHeightMap ("Deriv (AG) Height (B)", 2D) = "black" {}
		_HeightScale ("Height Scale, Constant", Float) = 0.25
		_HeightScaleModulated ("Height Scale, by FlowSpeed", Float) = 0.75
		_Tiling ("Tiling", Float) = 1
		_Speed ("Speed", Float) = 1
		_FlowStrength ("Flow Strength", Float) = 1
		_FlowOffset ("Flow Offset", Range(-.5, 0)) = 0
		_WaterFogColor ("Water Fog Color", Color) = (0, 0, 0, 0)
		_WaterFogDensity ("Water Fog Density", Range(0, 2)) = 0.1
		_RefractionStrength ("Refraction Strength", Range(0, 1)) = 0.25
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}
	SubShader {
		Tags { "RenderType"="Transparent" "Queue"="Transparent+1" }
		LOD 200

		GrabPass { "_WaterBackground" }

		CGPROGRAM
		#pragma surface surf Standard alpha finalcolor:ResetAlpha
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _FlowMap;
		// DerivativeHeightMap is used because it is cheaper to compute normals and we can scale the height of the waves appropriately
		sampler2D _DerivHeightMap;
		sampler2D _CameraDepthTexture;
		float4 _CameraDepthTexture_TexelSize;
		sampler2D _WaterBackground;

		struct Input {
			float2 uv_MainTex;
			float4 screenPos;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		float _Tiling;
		float _Speed;
		float _FlowStrength;
		float _FlowOffset;
		float _HeightScale;
		float _HeightScaleModulated;
		float3 _WaterFogColor;
		float _WaterFogDensity;
		float _RefractionStrength;

		void ResetAlpha(Input IN, SurfaceOutputStandard o, inout fixed4 color) {
			color.a = 1;
		}

		float2 AlignWithGrabTexel(float2 uv) {
			#if UNITY_UV_STARTS_AT_TOP
				if (_CameraDepthTexture_TexelSize.y < 0) {
					uv.y = 1 - uv.y;
				}
			#endif

			return (floor(uv * _CameraDepthTexture_TexelSize.zw) + 0.5) * abs(_CameraDepthTexture_TexelSize.xy);
		}

		float3 ColorBelowWater(float4 screenPos, float3 tangentSpaceNormal) {
			// Refract the UV value we sample from the background by the direction of the surface normal
			float2 uvOffset = tangentSpaceNormal.xy * _RefractionStrength;
			// Normalize the offset in the X and Y directions
			uvOffset.y *= _CameraDepthTexture_TexelSize.z * abs(_CameraDepthTexture_TexelSize.y);
			float2 uv = AlignWithGrabTexel((screenPos.xy + uvOffset) / screenPos.w);

			float backgroundDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv));
			float surfaceDepth = UNITY_Z_0_FAR_FROM_CLIPSPACE(screenPos.z);
			float depthDifference = backgroundDepth - surfaceDepth;

			// Remove refraction rendering if the sample is above the water
			uvOffset *= saturate(depthDifference);
			uv = AlignWithGrabTexel((screenPos.xy + uvOffset) / screenPos.w);

			backgroundDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv));
			depthDifference = backgroundDepth - surfaceDepth;
			
	
			float3 backgroundColor = tex2D(_WaterBackground, uv).rgb;
			float fogFactor = exp2(-_WaterFogDensity * depthDifference);
			// The following is a cheap bandaid on water not appearing through portals properly
			return lerp(_WaterFogColor, backgroundColor, .1/*fogFactor*/);
		}

		float3 UnpackDerivativeHeight(float4 textureData) {
			float3 dh = textureData.agb;
			dh.xy = dh.xy * 2 - 1;
			return dh;
		}

		float3 FlowUVW (float2 uv, float2 flowVector, float time, float phaseOffset) {
			float progress = frac(time + phaseOffset);
			float3 uvw;
			uvw.xy = uv - flowVector * (progress + _FlowOffset);
			uvw.xy *= _Tiling;
			uvw.xy += phaseOffset;

			// Jump artificially increases the period of repetition by offsetting the UV by different amounts
			// Using (6/25, 5/24) causes a period of 600 phases (10 minutes at 1 phase per second)
			float2 jump = float2(.24, 0.2083333);
			uvw.xy += (time - progress) * jump;
			uvw.z = 1 - abs(1 - 2 * progress);
			return uvw;
		}

		void surf(Input IN, inout SurfaceOutputStandard o) {
			float3 flow = tex2D(_FlowMap, IN.uv_MainTex).rgb;
			flow.xy = flow.xy * 2 - 1;
			flow *= _FlowStrength;

			float noise = tex2D(_FlowMap, IN.uv_MainTex).a;
			float time = _Time.y * _Speed + noise;
			float3 uvw1 = FlowUVW(IN.uv_MainTex, flow.xy, time, 0);
			float3 uvw2 = FlowUVW(IN.uv_MainTex, flow.xy, time, 0.5);
			
			float finalHeightScale = flow.z * 10 * _HeightScaleModulated + _HeightScale;
			float3 dh1 = UnpackDerivativeHeight(tex2D(_DerivHeightMap, uvw1.xy)) * uvw1.z * finalHeightScale;
			float3 dh2 = UnpackDerivativeHeight(tex2D(_DerivHeightMap, uvw2.xy)) * uvw2.z * finalHeightScale;
			o.Normal = normalize(float3(-(dh1.xy + dh2.xy), 1));

			fixed4 color1 = tex2D(_MainTex, uvw1.xy) * uvw1.z;
			fixed4 color2 = tex2D(_MainTex, uvw2.xy) * uvw2.z;
			fixed4 c = (color1 + color2) * _Color;

			o.Albedo = c.rgb;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
			
			o.Emission = ColorBelowWater(IN.screenPos, o.Normal) * (1 - c.a);
		}
		ENDCG
	}
}