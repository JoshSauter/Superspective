#define DEPTH_OFFSET 0.00488

uniform float4x4 _PortalScalingMatrix;
sampler2D_float _PortalMask;

#ifndef DEPTH_NORMALS_TEXTURE
#define DEPTH_NORMALS_TEXTURE
sampler2D_float _CameraDepthNormalsTexture;
#endif

bool SampleBehindPortal(float2 uv) {
	float4 depthNormalSample = tex2D(_CameraDepthNormalsTexture, uv);
	float depth, portalDepth;
	float3 normal;	// Normal unused, just needed for DecodeDepthNormal
	DecodeDepthNormal(depthNormalSample, depth, normal);

	float4 portalMaskSample = tex2D(_PortalMask, uv);
	DecodeDepthNormal(portalMaskSample, portalDepth, normal);

	return depth > portalDepth-DEPTH_OFFSET && portalDepth < 1;
}
