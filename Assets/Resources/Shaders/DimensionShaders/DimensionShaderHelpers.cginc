// Supports dimension values from [0-123] (444 - 1 in base 5)
fixed4 DimensionValueToColor(int dimensionValue) {
	// Avoid (0,0,0) as a color by adding 1
	dimensionValue += 1;
	float b = (dimensionValue % 5) / 4.0;
	float g = ((dimensionValue / 5) % 5) / 4.0;
	float r = ((dimensionValue / 25) % 5) / 4.0;
	return fixed4(r,g,b,0);
}

// Returns dimension values from [0-123] (444 - 1 in base 5)
int ColorToDimensionValue(fixed4 color) {
	return round((color.r * 100) + (color.g * 20) + (color.b * 4.0) - 1);
}
