using UnityEngine;

namespace SuperspectiveUtils {
    // These methods are CPU equivalent of the GPU-based code in DimensionShaderHelpers.cginc
    // When logic in one changes, update the other
    public static class DimensionShaderUtils {
        private static int NUM_CHANNELS => DimensionObject.NUM_CHANNELS;
        static readonly int NUM_CHANNELS_PER_COLOR = Mathf.CeilToInt(DimensionObject.NUM_CHANNELS / 3.0f);
        static int MAX_VALUE_PER_CHANNEL = Mathf.RoundToInt(Mathf.Pow(2, NUM_CHANNELS_PER_COLOR) - 1);
        
        public static int ChannelFromColor(Color rgb) {
            int rValue = Mathf.RoundToInt(rgb.r * MAX_VALUE_PER_CHANNEL);
            int gValue = Mathf.RoundToInt(rgb.g * MAX_VALUE_PER_CHANNEL);
            int bValue = Mathf.RoundToInt(rgb.b * MAX_VALUE_PER_CHANNEL);

            return rValue + (gValue << NUM_CHANNELS_PER_COLOR) + (bValue << (NUM_CHANNELS_PER_COLOR*2));
        }

        public static bool ChannelIsOnForMaskValue(int channel, int maskValue) {
            return (maskValue & (1 << channel)) == (1 << channel);
        }
    }
}