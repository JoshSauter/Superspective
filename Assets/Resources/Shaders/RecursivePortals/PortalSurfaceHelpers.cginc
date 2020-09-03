#define DEPTH_OFFSET 0.00388

sampler2D _PortalMask;
sampler2D _CameraDepthNormalsTexture;

bool SampleBehindPortal(float2 uv) {
	float4 depthNormalSample = tex2D(_CameraDepthNormalsTexture, uv);
	float depth, portalDepth;
	float3 normal;	// Normal unused, just needed for DecodeDepthNormal
	DecodeDepthNormal(depthNormalSample, depth, normal);

	float4 portalMaskSample = tex2D(_PortalMask, uv);
	DecodeDepthNormal(portalMaskSample, portalDepth, normal);

	//return portalDepth < 0.2175;
	return depth > portalDepth-DEPTH_OFFSET && portalDepth < 1;
}
