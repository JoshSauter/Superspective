// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/JG/ColorfulFog"
{
	Properties
	{
		_MainTex("Base (RGB)", 2D) = "black" {}
	}

		CGINCLUDE

	#include "UnityCG.cginc"
	#pragma target 2.0
	uniform sampler2D _MainTex;
	uniform int _UseCustomDepth;
	uniform sampler2D_float _CameraDepthTexture;
	uniform sampler2D_float _CustomDepthTexture;
	samplerCUBE _Cube;
	sampler2D _Gradient;
	half4x4 _Colors;
	//int _ColorMode; //0=solid color, 1 = cubemap, 2 = gradient, 3 >= gradient texture
					// x = fog height
					// y = FdotC (CameraY-FogHeight)
					// z = k (FdotC > 0.0)
					// w = a/2
	uniform float4 _HeightParams;

	// x = start distance
	uniform float4 _DistanceParams;

	int4 _SceneFogMode; // x = fog mode, y = use radial flag
	float4 _SceneFogParams;

	half4 _FogDensity;
	uniform float4 _MainTex_TexelSize;

	// for fast world space reconstruction
	uniform float4x4 _FrustumCornersWS;
	uniform float4 _CameraWS;

	struct v2f
	{
		float4 pos : SV_POSITION;
		float2 uv : TEXCOORD0;
		float2 uv_depth : TEXCOORD1;
		float4 interpolatedRay : TEXCOORD2;
	};

	v2f vert(appdata_img v)
	{
		v2f o;
		half index = v.vertex.z;
		v.vertex.z = 0.1;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.uv = v.texcoord.xy;
		o.uv_depth = v.texcoord.xy;

#if UNITY_UV_STARTS_AT_TOP
		if (_MainTex_TexelSize.y < 0)
		{
			o.uv.y = 1 - o.uv.y;
		}
#endif				

		o.interpolatedRay = _FrustumCornersWS[(int)index];
		o.interpolatedRay.w = index;

		return o;
	}

	// Applies one of standard fog formulas, given fog coordinate (i.e. distance)
	half ComputeFogFactor(float coord)
	{
		float fogFac = 0.0;
		if (_SceneFogMode.x == 1) // linear
		{
			// factor = (end-z)/(end-start) = z * (-1/(end-start)) + (end/(end-start))
			fogFac = coord * _SceneFogParams.z + _SceneFogParams.w;
		}
		else if (_SceneFogMode.x == 2) // exp
		{
			// factor = exp(-density*z)
			fogFac = _SceneFogParams.y * coord; fogFac = exp2(-fogFac);
		}
		else //if (_SceneFogMode.x == 3) // exp2
		{
			// factor = exp(-(density*z)^2)
			fogFac = _SceneFogParams.x * coord; fogFac = exp2(-fogFac*fogFac);
		}
		return saturate(fogFac);
	}

	// Distance-based fog
	float ComputeDistance(float3 camDir, float zdepth)
	{
		float dist;
		if (_SceneFogMode.y == 1)
			dist = length(camDir);
		else
			dist = zdepth * _ProjectionParams.z;
		// Built-in fog starts at near plane, so match that by
		// subtracting the near value. Not a perfect approximation
		// if near plane is very large, but good enough.
		dist -= _ProjectionParams.y;
		return dist;
	}

	// Linear half-space fog, from https://www.terathon.com/lengyel/Lengyel-UnifiedFog.pdf
	float ComputeHalfSpace(float3 wsDir)
	{
		float3 wpos = _CameraWS + wsDir;
		float FH = _HeightParams.x;
		float3 C = _CameraWS;
		float3 V = wsDir;
		float3 P = wpos;
		float3 aV = _HeightParams.w * V;
		float FdotC = _HeightParams.y;
		float k = _HeightParams.z;
		float FdotP = P.y - FH;
		float FdotV = wsDir.y;
		float c1 = k * (FdotP + FdotC);
		float c2 = (1 - 2 * k) * FdotP;
		float g = min(c2, 0.0);
		g = -length(aV) * (c1 - g * g / abs(FdotV + 1.0e-5f));
		return g;
	}

	half4 ComputeFogColor(float3 dir, int mode)
	{
		half4 fogColor = _Colors[0];
		if (mode == 1) //cubemap color
		{
			fogColor = texCUBE(_Cube, dir);
		}
		else if (mode == 2) //gradient color
		{
			float k = normalize(dir).y;
			if (k > 0.0)
			{
				fogColor = lerp(_Colors[1], _Colors[0], k);
			}
			else
			{
				k += 1; //add 1 to normalize range to 0.0 - 1.0
				fogColor = lerp(_Colors[2], _Colors[1], k);
			}
		}
		else if (mode >= 3) //gradient texture
		{
			float k = (normalize(dir).y + 1) *0.5;
			fogColor = tex2D(_Gradient, k);
		}
		return fogColor;
	}

	half4 ComputeFog(v2f i, bool distance, bool height, int colorMode) : SV_Target
	{
		half4 sceneColor = tex2D(_MainTex, i.uv);

		float rawDepth;
		float dpth;

		if (_UseCustomDepth)
		{
			rawDepth = SAMPLE_DEPTH_TEXTURE(_CustomDepthTexture, i.uv_depth);
			dpth = rawDepth;
		}
		else //depth provided by unity.
		{
			rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture,i.uv_depth);
			dpth = Linear01Depth(rawDepth);
		}
		//return dpth;
		float4 wsDir = dpth * i.interpolatedRay;
		float4 wsPos = _CameraWS + wsDir;

		// Compute fog distance
		float g = _DistanceParams.x;
		if (distance)
			g += ComputeDistance(wsDir, dpth);
		if (height)
			g += ComputeHalfSpace(wsDir);

		// Compute fog amount
		half fogFac = ComputeFogFactor(max(0.0,g));

		// Do not fog skybox
		if (rawDepth >= 0.999999)
			fogFac = 1.0;

		// Compute fog color
		half4 fogColor = ComputeFogColor(i.interpolatedRay,colorMode);//_Colors[0];

																	  // Lerp between fog color & original scene color
		return lerp(fogColor, sceneColor, fogFac);
	}

		ENDCG

		SubShader
	{
		ZTest Always Cull Off ZWrite Off Fog{ Mode Off }
			// int mode : 0=solid color, 1 = cubemap, 2 = gradient, 3 >= gradient texture
			/*passes: 0 - 3 : distance & height,
			4 - 7 : distance,
			8 - 11 : height */
			// 0: distance + height , SOLID COLOR.
			Pass
		{
			CGPROGRAM
#pragma vertex vert
#pragma fragment frag
			half4 frag(v2f i) : SV_Target
		{
			return ComputeFog(i, true, true,0);
		}
			ENDCG
		}
			// 1: distance + height , CUBEMAP.
			Pass
		{
			CGPROGRAM
#pragma vertex vert
#pragma fragment frag
			half4 frag(v2f i) : SV_Target
		{
			return ComputeFog(i, true, true,1);
		}
			ENDCG
		}
			// 2: distance + height , GRADIENT.
			Pass
		{
			CGPROGRAM
#pragma vertex vert
#pragma fragment frag
			half4 frag(v2f i) : SV_Target
		{
			return ComputeFog(i, true, true,2);
		}
			ENDCG
		}
			// 3: distance + height , GRADIENT TEXTURE.
			Pass
		{
			CGPROGRAM
#pragma vertex vert
#pragma fragment frag
			half4 frag(v2f i) : SV_Target
		{
			return ComputeFog(i, true, true,3);
		}
			ENDCG
		}
			// 4: distance , SOLID COLOR.
			Pass
		{
			CGPROGRAM
#pragma vertex vert
#pragma fragment frag
			half4 frag(v2f i) : SV_Target
		{
			return ComputeFog(i, true, false,0);
		}
			ENDCG
		}
			// 5: distance , CUBEMAP.
			Pass
		{
			CGPROGRAM
#pragma vertex vert
#pragma fragment frag
			half4 frag(v2f i) : SV_Target
		{
			return ComputeFog(i, true, false,1);
		}
			ENDCG
		}
			// 6: distance , GRADIENT.
			Pass
		{
			CGPROGRAM
#pragma vertex vert
#pragma fragment frag
			half4 frag(v2f i) : SV_Target
		{
			return ComputeFog(i, true, false,2);
		}
			ENDCG
		}
			// 7: distance , GRADIENT TEXTURE.
			Pass
		{
			CGPROGRAM
#pragma vertex vert
#pragma fragment frag
			half4 frag(v2f i) : SV_Target
		{
			return ComputeFog(i, true, false,3);
		}
			ENDCG
		}
			// 8: height , SOLID COLOR.
			Pass
		{
			CGPROGRAM
#pragma vertex vert
#pragma fragment frag
			half4 frag(v2f i) : SV_Target
		{
			return ComputeFog(i, false, true,0);
		}
			ENDCG
		}
			// 9: height , CUBEMAP.
			Pass
		{
			CGPROGRAM
#pragma vertex vert
#pragma fragment frag
			half4 frag(v2f i) : SV_Target
		{
			return ComputeFog(i, false, true,1);
		}
			ENDCG
		}
			// 10: height , GRADIENT.
			Pass
		{
			CGPROGRAM
#pragma vertex vert
#pragma fragment frag
			half4 frag(v2f i) : SV_Target
		{
			return ComputeFog(i, false, true,2);
		}
			ENDCG
		}
			// 11: height , GRADIENT TEXTURE.
			Pass
		{
			CGPROGRAM
#pragma vertex vert
#pragma fragment frag
			half4 frag(v2f i) : SV_Target
		{
			return ComputeFog(i, false, true,3);
		}
			ENDCG
		}

	}
	Fallback off
}
