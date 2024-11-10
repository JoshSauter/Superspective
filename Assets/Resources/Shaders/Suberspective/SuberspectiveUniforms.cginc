#ifndef SUBERSPECTIVE_UNIFORMS
#define SUBERSPECTIVE_UNIFORMS
// Shared uniforms for Suberspective shaders
#ifndef UNITY_BUILT_IN_COLOR
#define UNITY_BUILT_IN_COLOR
uniform float4 _Color;
#endif
#ifndef UNITY_BUILT_IN_EMISSION_ENABLED
#define UNITY_BUILT_IN_EMISSION_ENABLED
uniform float _EmissionEnabled;
#endif
#ifndef UNITY_BUILT_IN_EMISSION_COLOR
#define UNITY_BUILT_IN_EMISSION_COLOR
uniform float4 _EmissionColor;
#endif
#ifndef UNITY_BUILT_IN_TEXTURE
#define UNITY_BUILT_IN_TEXTURE
uniform sampler2D _MainTex;
#endif
#ifndef UNITY_BUILT_IN_EMISSION_MAP
#define UNITY_BUILT_IN_EMISSION_MAP
uniform sampler2D _EmissionMap;
#endif
#ifndef UNITY_BUILT_IN_TEXTURE_ST
#define UNITY_BUILT_IN_TEXTURE_ST
uniform float4 _MainTex_ST;
#endif
#ifndef UNITY_BUILT_IN_EMISSION_MAP_ST
#define UNITY_BUILT_IN_EMISSION_MAP_ST
uniform float4 _EmissionMap_ST;
#endif

// Dissolve shader uniforms
uniform sampler2D _DissolveTex;
uniform float4 _DissolveTex_ST;
uniform float _DissolveAmount;
uniform float _DissolveValue;
uniform float _DissolveBurnSize;
uniform sampler2D _DissolveBurnRamp;
uniform float4 _DissolveBurnColor;
uniform float _DissolveEmissionAmount;

// Dimension shader uniforms
#define NUM_CHANNELS 8
// 8 uints to pack 1 << NUM_CHANNELS = 256 possible channel combinations
// Note that 8 is purely coincidental due to the size of uint and float, and not related to NUM_CHANNELS
uniform float _MaskSolution[8];
uniform float _ResolutionX;
uniform float _ResolutionY;
uniform sampler2D _DimensionMask;
uniform float4 _DimensionMask_ST;

// PowerTrail shader uniforms
#define MAX_NODES 512
uniform float3 _NodePositions[MAX_NODES];
uniform int _StartPositionIDs[MAX_NODES];
uniform int _EndPositionIDs[MAX_NODES];
uniform float _InterpolationValues[MAX_NODES];
uniform float _CapsuleRadius;
uniform int _ReverseVisibility;
uniform int _HiddenPowerTrail;
uniform int _UseCylinder;

// Shutter shader uniforms
#define NUM_SHUTTERS 32
uniform int _ShutterInverse;
uniform sampler2D _ShutterNoise;
uniform float _Shutters[NUM_SHUTTERS];
uniform float3 _ShutterCenter;
uniform float _ShutterHeight;

// PortalCopy shader uniforms
uniform float3 _PortalPos;
uniform float3 _PortalNormal;
uniform float _FudgeDistance;

// RenderInZone shader uniforms
uniform float3 _MinRenderZone;
uniform float3 _MaxRenderZone;

#endif