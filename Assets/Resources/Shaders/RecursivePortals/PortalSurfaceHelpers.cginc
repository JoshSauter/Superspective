#define DEPTH_OFFSET 0.00488

uniform float4x4 _PortalScalingMatrix;
sampler2D_float _PortalMask;
sampler2D_float _PortalMaskWithScale;

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

void ClipDisabledPortalSurface(float3 worldPos, float3 portalNormal) {
	// Normalize the portal normal to ensure stability
	portalNormal = normalize(portalNormal);

	// Construct orthogonal basis vectors
	float3 tangent = abs(portalNormal.y) < 0.9 ? float3(0, 1, 0) : float3(1, 0, 0);
	float3 planeX = normalize(cross(tangent, portalNormal)); // Ensure orthogonal to portalNormal
	float3 planeY = normalize(cross(portalNormal, planeX));  // Ensure orthogonal to both portalNormal and planeX

	// Debugging: Visualize planeX and planeY
	// return float4(abs(planeX), 1.0);
	// return float4(abs(planeY), 1.0);

	// Project the world position onto the portal's plane
	float dist = dot(worldPos, portalNormal);                // Distance to the plane along the portal's normal
	float3 projectedPos = worldPos - dist * portalNormal;    // Project worldPos onto the portal's plane

	// Convert projected position to a 2D grid (local plane space)
	float2 gridPos = float2(dot(projectedPos, planeX), dot(projectedPos, planeY)); // 2D coordinates in the plane

	// Debugging: Visualize gridPos
	// return float4(gridPos.xy, 0.0, 1.0);

	// Create a mask based on the unit grid in the portal's plane
	float2 grid = abs(frac(gridPos) - 0.5);                  // Fractional part centered on grid cells
	float mask = step(grid.x, 0.1) + step(grid.y, 0.1);    // Lines 1-pixel wide
	mask = saturate(mask);

	// Debugging: Visualize mask
	// return float4(mask, mask, mask, 1.0);

	// Clip everything except for the grid
	clip(-(1 - mask));
}
