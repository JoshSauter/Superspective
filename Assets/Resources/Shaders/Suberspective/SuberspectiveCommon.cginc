#pragma shader_feature DIMENSION_OBJECT
#pragma shader_feature DISSOLVE_OBJECT
#pragma shader_feature POWER_TRAIL_OBJECT
#pragma shader_feature SHUTTERED_OBJECT
#pragma shader_feature PORTAL_COPY_OBJECT
#pragma shader_feature RENDER_IN_ZONE_OBJECT

#include "UnityCG.cginc"

#ifdef DIMENSION_OBJECT
#include "../DimensionShaders/DimensionShaderHelpers.cginc"
#endif

#ifdef DISSOLVE_OBJECT
#include "../DissolveShaderHelpers.cginc"
#endif

#ifdef POWER_TRAIL_OBJECT
#include "../PowerTrail/PowerTrailHelpers.cginc"
#endif

#ifdef SHUTTERED_OBJECT
#include "../LevelSpecific/BlackRoom3/AffectedByShutterHelpers.cginc"
#endif

#ifdef PORTAL_COPY_OBJECT
#include "../PortalCopy/PortalCopyHelpers.cginc"
#endif

#ifdef RENDER_IN_ZONE_OBJECT
#include "../RenderInZone/RenderInZoneHelpers.cginc"
#endif

// Some shaders only need to know which pixels are being rendered or not, this skips the color calculations
inline void SuberspectiveClipOnly(float2 uv_DimensionMask, float2 uv_DissolveTex, float3 worldPos) {
	#ifdef DIMENSION_OBJECT
	ClipDimensionObject(uv_DimensionMask);
	#endif
	#ifdef DISSOLVE_OBJECT
	ClipDissolve(uv_DissolveTex);
	#endif
	#ifdef POWER_TRAIL_OBJECT
	clip(EmissionEnabled(worldPos)-_HiddenPowerTrail);
	#endif
	#ifdef SHUTTERED_OBJECT
	ClipShutteredAreas(worldPos, float4(1,1,1,1));
	#endif
	#ifdef PORTAL_COPY_OBJECT
	ClipPortalCopy(worldPos);
	#endif
	#ifdef RENDER_IN_ZONE_OBJECT
	ClipRenderZone(worldPos);
	#endif
}

inline void SuberspectiveRender(float2 uv_DimensionMask, float2 uv_DissolveTex, float3 worldPos, inout float4 color, inout half emissionEnabled) {
	#ifdef DIMENSION_OBJECT
	ClipDimensionObject(uv_DimensionMask);
	#endif
	#ifdef DISSOLVE_OBJECT
	color = Dissolve(uv_DissolveTex, color);
	#endif
	#ifdef POWER_TRAIL_OBJECT
	emissionEnabled = EmissionEnabled(worldPos);
	clip(emissionEnabled-_HiddenPowerTrail);
	#endif
	#ifdef SHUTTERED_OBJECT
	color = ClipShutteredAreas(worldPos, color);
	#endif
	#ifdef PORTAL_COPY_OBJECT
	ClipPortalCopy(worldPos);
	#endif
	#ifdef RENDER_IN_ZONE_OBJECT
	ClipRenderZone(worldPos);
	#endif
}