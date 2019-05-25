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