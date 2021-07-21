// If you change this make sure you change the channel range in DimensionObject.cs to match
#define NUM_CHANNELS 8
#pragma multi_compile __ USE_ADVANCED_CHANNEL_LOGIC

float _ResolutionX;
float _ResolutionY;
sampler2D _DimensionMask;

#ifdef USE_ADVANCED_CHANNEL_LOGIC
int _AcceptableMaskValues[1 << NUM_CHANNELS];
#else
int _Channel;
#endif
int _Inverse;

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
inline int MaskValueFromSample(fixed4 rgb) {
	const uint numChannelsPerColor = ceil(NUM_CHANNELS / 3.0);
	const float maxValue = pow(2, numChannelsPerColor) - 1;

	int rValue = round(rgb.r * maxValue);
	int gValue = round(rgb.g * maxValue);
	int bValue = round(rgb.b * maxValue);

	return rValue + (gValue << numChannelsPerColor) + (bValue << (numChannelsPerColor*2));
}

// These methods are GPU equivalent of the CPU-based code in DimensionUtils.cs
// When logic in one changes, update the other
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

float ClipDimensionObject(float2 vertex) {
	float2 viewportVertex = float2(vertex.x / _ResolutionX, vertex.y / _ResolutionY);

	fixed4 dimensionSample = tex2D(_DimensionMask, viewportVertex);
#ifdef USE_ADVANCED_CHANNEL_LOGIC
	const int maxMaskValue = (1 << NUM_CHANNELS) - 1;
	int maskValue = clamp(MaskValueFromSample(dimensionSample), 0, maxMaskValue);
	float dimensionTest = _AcceptableMaskValues[maskValue];
#else
	float dimensionTest = TestChannelFromColor(_Channel, dimensionSample);
#endif

	clip(-(((1 - dimensionTest)*_Inverse + dimensionTest*(1 - _Inverse)) == 0));

	return dimensionTest;
}

fixed4 ClipDimensionObjectFromScreenSpaceCoords(float2 screenPos) {
	fixed4 dimensionSample = tex2D(_DimensionMask, screenPos);
#ifdef USE_ADVANCED_CHANNEL_LOGIC
	const int maxMaskValue = (1 << NUM_CHANNELS) - 1;
	int maskValue = clamp(MaskValueFromSample(dimensionSample), 0, maxMaskValue);
	float dimensionTest = _AcceptableMaskValues[maskValue];
#else
	float dimensionTest = TestChannelFromColor(_Channel, dimensionSample);
#endif

	clip(-(((1 - dimensionTest)*_Inverse + dimensionTest*(1 - _Inverse)) == 0));

	return dimensionTest;
}
