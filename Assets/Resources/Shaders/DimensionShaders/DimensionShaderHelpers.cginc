// If you change this make sure you change the channel range in DimensionObject.cs to match
#define NUM_CHANNELS 16

float _ResolutionX;
float _ResolutionY;
sampler2D _DimensionMask;

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

float ClipDimensionObject(float2 vertex, int channels[NUM_CHANNELS], int invert) {
	float2 viewportVertex = float2(vertex.x / _ResolutionX, vertex.y / _ResolutionY);

	fixed4 dimensionSample = tex2D(_DimensionMask, viewportVertex);
	float dimensionTest = step(channels[0], TestChannelFromColor(0, dimensionSample));
	for (int i = 1; i < NUM_CHANNELS; i++) {
		dimensionTest *= step(channels[i], TestChannelFromColor(i, dimensionSample));
	}

	clip(-(((1 - dimensionTest)*invert + dimensionTest*(1 - invert)) == 0));

	return dimensionTest;
}

fixed4 ClipDimensionObjectFromScreenSpaceCoords(float2 screenPos, int channels[NUM_CHANNELS], int invert) {
	float dimensionTest = step(channels[0], TestChannelFromColor(0, tex2D(_DimensionMask, screenPos)));
	for (int i = 1; i < NUM_CHANNELS; i++) {
		dimensionTest *= step(channels[1], TestChannelFromColor(i, tex2D(_DimensionMask, screenPos)));
	}

	clip(-(((1 - dimensionTest)*invert + dimensionTest*(1 - invert)) == 0));

	return dimensionTest;
}
