using System;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
#endif

namespace DimensionObjectMechanics {
	public readonly struct DimensionObjectBitmask {
		// number of bits in the bitmask (256)
		private const int BITMASK_SIZE = 1 << DimensionObject.NUM_CHANNELS;
		// number of uints (32 bits each) required to store the bitmask (8)
		private const int BITMASK_ARRAY_SIZE = BITMASK_SIZE / 32;
		
		// Store the 256 bit bitmask as 8 uints (32 bits each)
		private readonly uint[] bitmask;
		
		public uint[] RawBitmask => bitmask;

		/// <summary>
		/// Returns a new DimensionObjectBitmask with all values set to 1
		/// </summary>
		private static DimensionObjectBitmask _one;
		public static DimensionObjectBitmask One {
			get {
				// Cache result to avoid unnecessary allocations
				if (_one.HasBitmaskSet) return _one;
				
				uint[] resultBitmask = new uint[BITMASK_ARRAY_SIZE];

				for (int i = 0; i < BITMASK_ARRAY_SIZE; i++) {
					// Set all bits to 1
					resultBitmask[i] = 0xFFFFFFFF;
				}

				_one = new DimensionObjectBitmask(resultBitmask);
				return _one;
			}
		}

		/// <summary>
		/// Returns a new DimensionObjectBitmask with all values set to 0
		/// </summary>
		public static DimensionObjectBitmask Zero { get; } = new DimensionObjectBitmask(new uint[BITMASK_ARRAY_SIZE]);

		public bool HasBitmaskSet => bitmask != null && bitmask.Length == BITMASK_ARRAY_SIZE;
		
		/// <summary>
		/// Returns true if every bit is 0, false otherwise
		/// </summary>
		public bool IsEmpty => bitmask.All(bit => bit == 0);
		
		/// <summary>
		/// Returns true if every bit is 1, false otherwise
		/// </summary>
		public bool IsEverything => bitmask.All(bit => bit == 0xFFFFFFFF);
		
		// Conversion to float[] for use in passing to shaders
		public float[] ShaderData => bitmask
			// Reinterpret the uint as a float by preserving the binary bits
			// We'll use the bits from the float to convert back to a uint in the shader before using it as a bitmask
			.Select(bitmaskElement => BitConverter.ToSingle(BitConverter.GetBytes(bitmaskElement), 0))
			.ToArray();

		/// <summary>
		/// Constructor for a DimensionObjectBitmask with a single acceptable channel
		/// </summary>
		/// <param name="channel">Channel to set to allow</param>
		/// <exception cref="ArgumentOutOfRangeException">Invalid channel supplied</exception>
		public DimensionObjectBitmask(int channel) {
			// Check if the channel is within the valid range
			if (channel < 0 || channel >= DimensionObject.NUM_CHANNELS) {
				throw new ArgumentOutOfRangeException(nameof(channel), channel, "Channel must be between 0 and " + (DimensionObject.NUM_CHANNELS - 1));
			}

			// Initialize the 256 bit bitmask as 8 uints (32 bits each)
			this.bitmask = new uint[BITMASK_ARRAY_SIZE];

			for (uint i = 0; i < BITMASK_SIZE; i++) {
				// If the `channel` is active in this combination (i.e., bit `channel` is 1)
				if ((i & (1 << channel)) != 0) {
					uint bitmaskIndex = i / 32; // which element of the bitmask array are we affecting
					uint bitmaskValue = 1u << (int) (i % 32); // which bit of the bitmask element are we setting
					
					// Set the bit to 1 with a bitwise OR operation
					this.bitmask[bitmaskIndex] |= bitmaskValue;
				}
			}
		}

		/// <summary>
		/// Construct a DimensionObjectBitmask from a 256-element int[] representation of the bitmask
		/// </summary>
		/// <param name="bitmask">Int array representing a 256 bit bitmask, where each element is 1 or 0</param>
		public DimensionObjectBitmask(int[] bitmask) {
			if (bitmask.Length != BITMASK_SIZE) {
				throw new ArgumentOutOfRangeException(nameof(bitmask), bitmask.Length, "Bitmask must be of length " + (1 << DimensionObject.NUM_CHANNELS));
			}
			
			this.bitmask = new uint[BITMASK_ARRAY_SIZE];
			for (uint i = 0; i < BITMASK_SIZE; i++) {
				if (bitmask[i] != 0) {
					uint bitmaskIndex = i / 32; // which element of the bitmask array are we affecting
					uint bitmaskValue = 1u << (int) (i % 32); // which bit of the bitmask element are we setting
					
					// Set the bit to 1 with a bitwise OR operation
					this.bitmask[bitmaskIndex] |= bitmaskValue;
				}
			}
		}
		
		/// <summary>
		/// Construct a DimensionObjectBitmask directly from an 8-element uint array representation of the bitmask
		/// </summary>
		/// <param name="bitmask">The optimized 8-element uint array representation of the bitmask</param>
		private DimensionObjectBitmask(uint[] bitmask) {
			if (bitmask.Length != BITMASK_ARRAY_SIZE) {
				throw new ArgumentOutOfRangeException(nameof(bitmask), bitmask.Length, "Bitmask must be of length " + BITMASK_ARRAY_SIZE);
			}
			
			this.bitmask = bitmask;
		}

		/// <summary>
		/// Bitwise AND of two DimensionObjectBitmasks
		/// </summary>
		/// <param name="a, b">The DimensionObjectBitmasks to do the bitwise AND with</param>
		/// <returns>A new DimensionObjectBitmask representing the bitwise AND of the two</returns>
		public static DimensionObjectBitmask operator &(DimensionObjectBitmask a, DimensionObjectBitmask b) {
			uint[] bitmaskResult = new uint[BITMASK_ARRAY_SIZE];
			
			for (int i = 0; i < BITMASK_ARRAY_SIZE; i++) {
				bitmaskResult[i] = a.bitmask[i] & b.bitmask[i];
			}
			
			return new DimensionObjectBitmask(bitmaskResult);
		}

		/// <summary>
		/// Bitwise NOT of a DimensionObjectBitmask
		/// </summary>
		/// <param name="dimensionObjBitmask">The DimensionObjectBitmask to do the bitwise NOT with</param>
		/// <returns>A new DimensionObjectBitmask representing the bitwise NOT of the input</returns>
		public static DimensionObjectBitmask operator ~(DimensionObjectBitmask dimensionObjBitmask) {
			uint[] resultBitmask = new uint[BITMASK_ARRAY_SIZE];
			
			for (int i = 0; i < BITMASK_ARRAY_SIZE; i++) {
				resultBitmask[i] = ~dimensionObjBitmask.bitmask[i];
			}

			return new DimensionObjectBitmask(resultBitmask);
		}

		/// <summary>
		/// Test if a value is set in the bitmask
		/// </summary>
		/// <param name="value">Value to test, between 0 and BITMASK_SIZE (256)</param>
		/// <returns>True if the value passes the bitmask, false otherwise</returns>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public bool Test(int value) {
			if (value < 0 || value >= BITMASK_SIZE) {
				throw new ArgumentOutOfRangeException(nameof(value), value, "Value must be between 0 and " + (BITMASK_SIZE - 1));
			}
			
			return (bitmask[value / 32] & (1u << (value % 32))) != 0;
		}

		// Function to return the list of accepted channel combinations
		public string DebugPrettyPrint() {
			List<int> acceptedChannels = new List<int>();

			// Iterate over each uint and check which bits are set
			for (int i = 0; i < BITMASK_ARRAY_SIZE; i++) {
				for (int bit = 0; bit < 32; bit++) {
					if ((bitmask[i] & (1u << bit)) != 0) {
						acceptedChannels.Add(i * 32 + bit);  // Calculate the actual bit position
					}
				}
			}

			// Prints the channel as a binary string
			// e.g. input of 255 would return "11111111"
			string PrintChannel(int index) {
				return Convert.ToString(index, 2).PadLeft(DimensionObject.NUM_CHANNELS, '0');
			}

			if (IsEmpty) return "No accepted channels.";
			if (IsEverything) return "All channels accepted.";
			
			return "Accepted channels:\n[76543210] <- Channels\n " + string.Join(" \n ", acceptedChannels.Select(PrintChannel));
		}
	}

#if UNITY_EDITOR
	public class DimensionObjectBitmaskDrawer : OdinValueDrawer<DimensionObjectBitmask> {
		static bool rawBitmaskFoldout = true;
		static bool prettyPrintFoldout = true;

		protected override void DrawPropertyLayout(GUIContent label) {
			bool isExpanded = Property.State.Expanded;
			isExpanded = SirenixEditorGUI.Foldout(isExpanded, label);
			Property.State.Expanded = isExpanded;

			if (!isExpanded) {
				return;
			}

			EditorGUI.indentLevel++;

			if (ValueEntry.SmartValue.RawBitmask == null) {
				EditorGUILayout.LabelField("No bitmask set.");
				EditorGUI.indentLevel--;
				return;
			}

			SirenixEditorGUI.InfoMessageBox($"IsEmpty: {ValueEntry.SmartValue.IsEmpty}\nIsEverything: {ValueEntry.SmartValue.IsEverything}");

			rawBitmaskFoldout = SirenixEditorGUI.Foldout(rawBitmaskFoldout, "Raw Bitmask (Hex)");
			if (rawBitmaskFoldout) {
				EditorGUI.indentLevel++;
				for (int i = 0; i < ValueEntry.SmartValue.RawBitmask.Length; i++) {
					EditorGUILayout.SelectableLabel($"[{i}] 0x{ValueEntry.SmartValue.RawBitmask[i]:X8}", GUILayout.Height(EditorGUIUtility.singleLineHeight));
				}
				EditorGUI.indentLevel--;
			}

			prettyPrintFoldout = SirenixEditorGUI.Foldout(prettyPrintFoldout, "Debug Pretty Print");
			if (prettyPrintFoldout) {
				EditorGUI.indentLevel++;
				EditorGUILayout.HelpBox(ValueEntry.SmartValue.DebugPrettyPrint(), MessageType.None);
				EditorGUI.indentLevel--;
			}

			EditorGUI.indentLevel--;
		}
	}
#endif
}
