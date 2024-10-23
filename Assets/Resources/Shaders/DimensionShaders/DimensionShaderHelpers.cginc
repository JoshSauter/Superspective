#ifndef DIMENSION_SHADER_HELPERS
#define DIMENSION_SHADER_HELPERS

// If you change this make sure you change the channel range in DimensionObject.cs to match
#pragma multi_compile __ USE_ADVANCED_CHANNEL_LOGIC

#include "../Suberspective/SuberspectiveUniforms.cginc"

inline fixed4 ColorFromChannel(int channel) {
	// Split evenly across red, blue, green colors to avoid floating point errors
	const int numChannelsPerColor = ceil(NUM_CHANNELS / 3.0);

	int rValue = 0;
	int gValue = 0;
	int bValue = 0;

	if (channel < numChannelsPerColor) {
		rValue += pow(2, channel);
	}
	else if (channel < numChannelsPerColor*2) {
		gValue += pow(2, channel-numChannelsPerColor);
	}
	else {
		bValue += pow(2, channel-(2*numChannelsPerColor));
	}

	const float maxValue = pow(2, numChannelsPerColor) - 1;
	float r = rValue / maxValue;
	float g = gValue / maxValue;
	float b = bValue / maxValue;

	return fixed4(r,g,b,1);
}

// These methods are GPU equivalent of the CPU-based code in DimensionUtils.cs
// When logic in one changes, update the other
inline int ChannelFromColor(fixed4 rgb) {
	const uint numChannelsPerColor = ceil(NUM_CHANNELS / 3.0);
	const float maxValue = pow(2, numChannelsPerColor) - 1;

	int rValue = round(rgb.r * maxValue);
	int gValue = round(rgb.g * maxValue);
	int bValue = round(rgb.b * maxValue);

	return rValue + (gValue << numChannelsPerColor) + (bValue << (numChannelsPerColor*2));
}

// These methods are GPU equivalent of the CPU-based code in DimensionUtils.cs
// When logic in one changes, update the other
// Returns 1 if channel is included in rgb mask value, 0 otherwise
inline int TestChannelFromColor(uint channel, fixed4 rgb) {
	const uint numChannelsPerColor = ceil(NUM_CHANNELS / 3.0);
	const float maxValue = pow(2, numChannelsPerColor) - 1;

	if (channel < numChannelsPerColor) {
		// r
		int rValue = round(rgb.r * maxValue);
		return saturate(rValue & (1 << channel));
	}
	else if (channel < 2*numChannelsPerColor) {
		// g
		// local channel value in range [0, numChannelsPerColor)
		channel %= numChannelsPerColor;
		
		int gValue = round(rgb.g * maxValue);
		return saturate(gValue & (1 << channel));
	}
	else {
		// b
		// local channel value in range [0, numChannelsPerColor)
		channel %= numChannelsPerColor;
		
		int bValue = round(rgb.b * maxValue);
		return saturate(bValue & (1 << channel));
	}
}

fixed4 ClipDimensionObjectFromScreenSpaceCoords(float2 screenPos, float disabled = 0.0) {
	fixed4 dimensionSample = tex2D(_DimensionMask, screenPos);
	#ifdef USE_ADVANCED_CHANNEL_LOGIC
	const int maxMaskValue = (1 << NUM_CHANNELS) - 1;
	int maskValue = clamp(ChannelFromColor(dimensionSample), 0, maxMaskValue);
	float dimensionTest = _AcceptableMaskValues[maskValue];
	#else
	float dimensionTest = TestChannelFromColor(_Channel, dimensionSample);
	#endif

	clip(disabled-(((1 - dimensionTest)*_Inverse + dimensionTest*(1 - _Inverse)) == 0));

	return dimensionTest;
}

float ClipDimensionObject(float2 vertex, float disabled = 0.0) {
	float2 viewportVertex = float2(vertex.x / _ResolutionX, vertex.y / _ResolutionY);
	return ClipDimensionObjectFromScreenSpaceCoords(viewportVertex, disabled);
}

#endif