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

// Supports dimension values from [0-123] (444 in base 5 - 1)
fixed4 DimensionValueToColor(int dimensionValue) {
	// Avoid (0,0,0) as a color by adding 1
	dimensionValue += 1;
	float b = (dimensionValue % (uint)5) / 4.0;
	float g = ((dimensionValue / (uint)5) % (uint)5) / 4.0;
	float r = ((dimensionValue / (uint)25) % (uint)5) / 4.0;
	return fixed4(r,g,b,0);
}

// Returns dimension values from [0-123] (444 in base 5 - 1)
int ColorToDimensionValue(fixed4 color) {
	return round((color.r * 100) + (color.g * 20) + (color.b * 4.0) - 1);
}

void ClipDimensionObject(float2 vertex, int dimension, int channel) {
	float2 viewportVertex = float2(vertex.x / _ResolutionX, vertex.y / _ResolutionY);
	fixed4 dimensionTest = fixed4(0,0,0,0);
	if (channel == 0) {
		dimensionTest = tex2D(_DimensionMask0, viewportVertex);
	}
	else /*if (channel == 1)*/ {
		dimensionTest = tex2D(_DimensionMask1, viewportVertex);
	}

	int dimensionValue = ColorToDimensionValue(dimensionTest);
	clip(-(dimensionValue != dimension));
}

void ClipInverseDimensionObject(float2 vertex, int dimension, int channel) {
	float2 viewportVertex = float2(vertex.x / _ResolutionX, vertex.y / _ResolutionY);
	fixed4 dimensionTest = fixed4(0,0,0,0);
	if (channel == 0) {
		dimensionTest = tex2D(_DimensionMask0, viewportVertex);
	}
	else /*if (channel == 1)*/ {
		dimensionTest = tex2D(_DimensionMask1, viewportVertex);
	}

	int dimensionValue = ColorToDimensionValue(dimensionTest);
	clip(-(dimensionValue != -1));
}