#define NUM_CHANNELS 2

float _ResolutionX;
float _ResolutionY;
sampler2D _DimensionMask0;
sampler2D _DimensionMask1;

inline sampler2D DimensionMaskForChannel(int channel) {
	if (channel == 0) {
		return _DimensionMask0;
	}
	else /*if (channel == 1)*/ {
		return _DimensionMask1;
	}
}

float ClipDimensionObject(float2 vertex, int channels[NUM_CHANNELS], int invert) {
	float2 viewportVertex = float2(vertex.x / _ResolutionX, vertex.y / _ResolutionY);
	
	float dimensionTest = step(channels[0], tex2D(_DimensionMask0, viewportVertex).r);
	dimensionTest *= step(channels[1], tex2D(_DimensionMask1, viewportVertex).r);

	clip(-(((1 - dimensionTest)*invert + dimensionTest*(1 - invert)) == 0));

	return dimensionTest;
}

fixed4 ClipDimensionObjectFromScreenSpaceCoords(float2 screenPos, int channels[NUM_CHANNELS], int invert) {
	float dimensionTest = step(channels[0], tex2D(_DimensionMask0, screenPos).r);
	dimensionTest *= step(channels[1], tex2D(_DimensionMask1, screenPos).r);

	clip(-(((1 - dimensionTest)*invert + dimensionTest*(1 - invert)) == 0));

	return dimensionTest;
}
