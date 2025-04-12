using Saving;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Library.Functional;
using NovaMenuUI;
using Sirenix.Utilities;
using SuperspectiveUtils;
using UnityEngine.Serialization;

namespace SerializableClasses {
	using DynamicObjectReference = Either<DynamicObject, DynamicObject.DynamicObjectSave>;
	/// <summary>
	/// Since unity doesn't flag the Vector2 as serializable, we
	/// need to create our own version. This one will automatically convert
	/// between Vector2 and SerializableVector2
	/// </summary>
	[Serializable]
	public struct SerializableVector2 {
		public float x;
		public float y;

		// Constructor
		public SerializableVector2(float rX, float rY) {
			x = rX;
			y = rY;
		}

		// Returns a string representation of the object
		public override string ToString() {
			return string.Format("[{0}, {1}]", x, y);
		}

		// Automatic conversion from SerializableVector2 to Vector2
		public static implicit operator Vector2(SerializableVector2 rValue) {
			return new Vector2(rValue.x, rValue.y);
		}

		// Automatic conversion from Vector2 to SerializableVector2
		public static implicit operator SerializableVector2(Vector2 rValue) {
			return new SerializableVector2(rValue.x, rValue.y);
		}
	}

	/// <summary>
	/// Since unity doesn't flag the Vector3 as serializable, we
	/// need to create our own version. This one will automatically convert
	/// between Vector3 and SerializableVector3
	/// </summary>
	[Serializable]
	public struct SerializableVector3 {
		public float x;
		public float y;
		public float z;

		// Constructor
		public SerializableVector3(float rX, float rY, float rZ) {
			x = rX;
			y = rY;
			z = rZ;
		}

		// Returns a string representation of the object
		public override string ToString() {
			return string.Format("[{0}, {1}, {2}]", x, y, z);
		}

		// Automatic conversion from SerializableVector3 to Vector3
		public static implicit operator Vector3(SerializableVector3 rValue) {
			return new Vector3(rValue.x, rValue.y, rValue.z);
		}

		// Automatic conversion from Vector3 to SerializableVector3
		public static implicit operator SerializableVector3(Vector3 rValue) {
			return new SerializableVector3(rValue.x, rValue.y, rValue.z);
		}
	}

	/// <summary>
	/// Since unity doesn't flag the Quaternion as serializable, we
	/// need to create our own version. This one will automatically convert
	/// between Quaternion and SerializableQuaternion
	/// </summary>
	[Serializable]
	public struct SerializableQuaternion {
		public float x;
		public float y;
		public float z;
		public float w;

		// Constructor
		public SerializableQuaternion(float rX, float rY, float rZ, float rW) {
			x = rX;
			y = rY;
			z = rZ;
			w = rW;
		}

		// Returns a string representation of the object
		public override string ToString() {
			return String.Format("[{0}, {1}, {2}, {3}]", x, y, z, w);
		}

		// Automatic conversion from SerializableQuaternion to Quaternion
		public static implicit operator Quaternion(SerializableQuaternion rValue) {
			return new Quaternion(rValue.x, rValue.y, rValue.z, rValue.w);
		}

		// Automatic conversion from Quaternion to SerializableQuaternion
		public static implicit operator SerializableQuaternion(Quaternion rValue) {
			return new SerializableQuaternion(rValue.x, rValue.y, rValue.z, rValue.w);
		}
	}

	[Serializable]
	public class SerializableColor {

		public float[] colorStore = new float[4] { 1F, 1F, 1F, 1F };
		public Color Color {
			get { return new Color(colorStore[0], colorStore[1], colorStore[2], colorStore[3]); }
			set { colorStore = new float[4] { value.r, value.g, value.b, value.a }; }
		}

		//makes this class usable as Color, Color normalColor = mySerializableColor;
		public static implicit operator Color(SerializableColor instance) {
			return instance.Color;
		}

		//makes this class assignable by Color, SerializableColor myColor = Color.white;
		public static implicit operator SerializableColor(Color color) {
			return new SerializableColor { Color = color };
		}
	}

	// Used for SerializableGradient
	internal static class TwoDArrayExtensions {
		public static void ClearTo(this float[,] a, float val) {
			for (int i = a.GetLowerBound(0); i <= a.GetUpperBound(0); i++) {
				for (int j = a.GetLowerBound(1); j <= a.GetUpperBound(1); j++) {
					a[i, j] = val;
				}
			}
		}
	}

	[Serializable]
	public struct SerializableKeyframe {
		public float time;
		public float value;
		public float inTangent;
		public float outTangent;
		public float inWeight;
		public float outWeight;
		public WeightedMode weightedMode;

		public SerializableKeyframe(float time, float value, float inTangent, float outTangent, float inWeight, float outWeight, WeightedMode weightedMode) {
			this.time = time;
			this.value = value;
			this.inTangent = inTangent;
			this.outTangent = outTangent;
			this.inWeight = inWeight;
			this.outWeight = outWeight;
			this.weightedMode = weightedMode;
	}

		// Automatic conversion from SerializableKeyframe to Keyframe
		public static implicit operator Keyframe(SerializableKeyframe keyframe) {
			return new Keyframe {
				time = keyframe.time,
				value = keyframe.value,
				inTangent = keyframe.inTangent,
				outTangent = keyframe.outTangent,
				inWeight = keyframe.inWeight,
				outWeight = keyframe.outWeight,
				weightedMode = keyframe.weightedMode
			};
		}

		// Automatic conversion from Keyframe to SerializableKeyframe
		public static implicit operator SerializableKeyframe(Keyframe keyframe) {
			return new SerializableKeyframe(keyframe.time, keyframe.value, keyframe.inTangent, keyframe.outTangent, keyframe.inWeight, keyframe.outWeight, keyframe.weightedMode);
		}
	}

	[Serializable]
	public class SerializableAnimationCurve {
		public SerializableKeyframe[] keys;
		int preWrapMode;
		int postWrapMode;

		public AnimationCurve Curve {
			get {
				AnimationCurve curve = new AnimationCurve(keys.Select<SerializableKeyframe, Keyframe>(k => k).ToArray()) {
					preWrapMode = (WrapMode)preWrapMode,
					postWrapMode = (WrapMode)postWrapMode
				};
				return curve;
			}
			set {
				keys = value.keys.Select<Keyframe, SerializableKeyframe>(k => k).ToArray();
				preWrapMode = (int)value.preWrapMode;
				postWrapMode = (int)value.postWrapMode;
			}
		}

		//makes this class usable as AnimationCurve, AnimationCurve normalAnimationCurve = mySerializableAnimationCurve;
		public static implicit operator AnimationCurve(SerializableAnimationCurve instance) {
			return instance?.Curve;
		}

		//makes this class assignable by AnimationCurve, SerializableAnimationCurve myAnimationCurve = animationCurve;
		public static implicit operator SerializableAnimationCurve(AnimationCurve curve) {
			return new SerializableAnimationCurve { Curve = curve };
		}
	}

	[Serializable]
	public class SerializableGradient : ISerializationCallbackReceiver {
		const int GRADIENT_ARRAY_SIZE = 10;
		
		private const int COLOR_SIZE = 4;
		private const int ALPHA_VALUE_SIZE = 2;
		// RGB + Key Time
		public float[,] colors = new float[GRADIENT_ARRAY_SIZE, COLOR_SIZE];
		// Value + Key Time
		public float[,] alphaValues = new float[GRADIENT_ARRAY_SIZE, ALPHA_VALUE_SIZE];

		public Gradient Gradient {
			get {
				Gradient newGradient = new Gradient();
				List<GradientColorKey> colorKeys = new List<GradientColorKey>();
				List<GradientAlphaKey> alphaKeys = new List<GradientAlphaKey>();

				for (int i = 0; i < colors.GetLength(0); i++) {
					// Flag end of array with -1 Time value
					if (colors[i, 3] == -1) break;

					GradientColorKey colorKey = new GradientColorKey {
						color = new Color(colors[i, 0], colors[i, 1], colors[i, 2], 1f),
						time = colors[i, 3]
					};
					colorKeys.Add(colorKey);
				}
				for (int i = 0; i < alphaValues.GetLength(0); i++) {
					// Flag end of array with -1 Time value
					if (alphaValues[i, 1] == -1) break;

					GradientAlphaKey alphaKey = new GradientAlphaKey {
						alpha = alphaValues[i, 0],
						time = alphaValues[i, 1]
					};
					alphaKeys.Add(alphaKey);
				}

				// TODO: Migrate GRADIENT_ARRAY_SIZE to 8 to match Unity's max
				newGradient.colorKeys = colorKeys.Take(8).ToArray();
				newGradient.alphaKeys = alphaKeys.Take(8).ToArray();
				return newGradient;
			}
			set {
				colors.ClearTo(-1);
				alphaValues.ClearTo(-1);

				for (int i = 0; i < value.colorKeys.Length; i++) {
					Color c = value.colorKeys[i].color;
					float t = value.colorKeys[i].time;
					colors[i, 0] = c.r;
					colors[i, 1] = c.g;
					colors[i, 2] = c.b;
					colors[i, 3] = t;
				}
				for (int i = 0; i < value.alphaKeys.Length; i++) {
					float alpha = value.alphaKeys[i].alpha;
					float t = value.alphaKeys[i].time;
					alphaValues[i, 0] = alpha;
					alphaValues[i, 1] = t;
				}
			}
		}

		//makes this class usable as Gradient, Gradient normalGradient = mySerializableGradient;
		public static implicit operator Gradient(SerializableGradient instance) {
			return instance.Gradient;
		}

		//makes this class assignable by Gradient, SerializableGradient myGradient = gradientReference;
		public static implicit operator SerializableGradient(Gradient gradient) {
			return new SerializableGradient { Gradient = gradient };
		}

		[SerializeField]
		private float[] flattenedColors;
		[SerializeField]
		private float[] flattenedAlphaValues;

		public void OnBeforeSerialize() {
			float[] FlattenArray(float[,] array) {
				int rows = array.GetLength(0);
				int cols = array.GetLength(1);

				float[] flattenedArray = new float[rows * cols];

				for (int i = 0; i < rows; i++) {
					for (int j = 0; j < cols; j++) {
						flattenedArray[i * cols + j] = array[i, j];
					}
				}

				return flattenedArray;
			}
			
			flattenedColors = FlattenArray(colors);
			flattenedAlphaValues = FlattenArray(alphaValues);
		}

		public void OnAfterDeserialize() {
			// Helper method to restore a 2D array from a flattened 1D array
			float[,] UnflattenArray(float[] flattenedArray, int size) {
				int rows = flattenedArray.Length / size;
				int cols = size;

				float[,] newArray = new float[rows, cols];

				for (int i = 0; i < rows; i++) {
					for (int j = 0; j < cols; j++) {
						newArray[i, j] = flattenedArray[i * cols + j];
					}
				}

				return newArray;
			}

			if (flattenedColors != null && flattenedAlphaValues != null) {
				colors = UnflattenArray(flattenedColors, COLOR_SIZE);
				alphaValues = UnflattenArray(flattenedAlphaValues, ALPHA_VALUE_SIZE);
			}
		}
	}

	/// <summary>
	/// Untyped reference to a SuperspectiveObject, with no knowledge of its script or save data.
	/// Use this when you only need a reference to the GameObject and not a particular script.
	/// </summary>
	[Serializable]
	public class SuperspectiveReference {
		public string referencedObjId;
		
		// Implicit SerializableReference creation from SaveableObject
		public static implicit operator SuperspectiveReference(SuperspectiveObject obj) {
			return obj != null ? new SuperspectiveReference<SuperspectiveObject, SaveObject<SuperspectiveObject>> { Reference = obj } : null;
		}
		
		// Implicit SerializableReference creation from SerializableSaveObject
		public static implicit operator SuperspectiveReference(SaveObject<SuperspectiveObject> serializedObj) {
			return serializedObj != null ? new SuperspectiveReference<SuperspectiveObject, SaveObject<SuperspectiveObject>> { Reference = serializedObj } : null;
		}

		public Either<SuperspectiveObject, SaveObject> Reference {
			get {
				return SaveManager.GetSuperspectiveObjectById(referencedObjId)?.Match(
					saveableObject => new Either<SuperspectiveObject, SaveObject>(saveableObject),
					serializedSaveObject => new Either<SuperspectiveObject, SaveObject>(serializedSaveObject)
				);
			}
			set {
				if (value != null) {
					value.MatchAction(
						saveableObject => {
							referencedObjId = saveableObject.ID;
						},
						serializedSaveObject => {
							referencedObjId = serializedSaveObject.ID;
						}
					);
				}
				else {
					referencedObjId = "";
				}
			}
		}
	}

	/// <summary>
	/// Singly typed reference to a SuperspectiveObject of a given type, with no knowledge of its save object data.
	/// Use this only when you know the reference you're trying to access is loaded.
	/// </summary>
	/// <typeparam name="T">Type of the loaded SuperspectiveObject script being referenced</typeparam>
	[Serializable]
	public class SuperspectiveReference<T> : SuperspectiveReference where T : SuperspectiveObject {
		public T GetOrNull() {
			return SaveManager.GetSuperspectiveObjectById(referencedObjId).LeftOrDefault() as T;
		}

		// Implicit SerializableReference creation from SaveableObject
		public static implicit operator SuperspectiveReference<T>(T obj) {
			return obj != null ? new SuperspectiveReference<T> {
				referencedObjId = obj.ID
			} : null;
		}
	}

	/// <summary>
	/// Fully typed reference to a SuperspectiveObject of a given type, with knowledge of its save object data.
	/// Use this when the reference may or may not be loaded, and you need to access its data either way.
	/// </summary>
	/// <typeparam name="T">Type of the loaded SuperspectiveObject script being referenced</typeparam>
	/// <typeparam name="S">Type of the serialized save data for the SuperspectiveObject being referenced</typeparam>
	[Serializable]
	public class SuperspectiveReference<T, S> : SuperspectiveReference
		where T : SuperspectiveObject
		where S : SaveObject<T> {

		public new Either<T, S> Reference {
			get {
				return SaveManager.GetSuperspectiveObjectById(referencedObjId)?.Match(
					// Map the results to the appropriate types
					saveableObject => new Either<T, S>(saveableObject as T),
					serializedSaveObject => new Either<T, S>(serializedSaveObject as S)
				);
			}
			set {
				if (value != null) {
					value.MatchAction(
						saveableObject => {
							referencedObjId = saveableObject.ID;
						},
						serializedSaveObject => {
							referencedObjId = serializedSaveObject.ID;
						}
					);
				}
				else {
					referencedObjId = "";
				}
			}
		}
		
		// Try to get the referenced object, returning true and setting obj if it exists, and false if it doesn't
		public bool TryGet(out T obj) {
			obj = GetOrNull();
			return obj != null;
		}

		// Get the referenced object if it exists, or null if it doesn't
		public T GetOrNull() {
			if (referencedObjId.IsNullOrWhitespace()) {
				Debug.LogWarning("Calling GetOrNull() on a SuperspectiveReference with no ID");
				return null;
			}
			return Reference?.Match(
				saveableObject => saveableObject,
				other => null
			);
		}

		// Implicit SerializableReference creation from SuperspectiveObject
		public static implicit operator SuperspectiveReference<T, S>(T obj) {
			return obj != null ? new SuperspectiveReference<T, S> { Reference = obj } : null;
		}
		
		// Implicit SerializableReference creation from SerializableSaveObject
		public static implicit operator SuperspectiveReference<T, S>(S serializedObj) {
			return serializedObj != null ? new SuperspectiveReference<T, S> { Reference = serializedObj } : null;
		}

		// Only use this if you know you have a SerializableReference of a particular type:
		// Actually don't use this unless you really need to (like for a migration script)
		public static SuperspectiveReference<T, S> FromGenericReference(SuperspectiveReference reference) {
			return new SuperspectiveReference<T, S>() {
				referencedObjId = reference.referencedObjId
			};
		}
	}

	public class SuperspectiveDynamicReference : SuperspectiveReference<DynamicObject, DynamicObject.DynamicObjectSave> {
		// Special-case reference should go through different method to find DynamicObjects
		public new DynamicObjectReference Reference {
			get {
				return SaveManager.GetDynamicObjectById(referencedObjId)?.Match<DynamicObjectReference>(
					// Map the results to the appropriate types
					dynamicObj => dynamicObj,
					serializedDynamicObject => serializedDynamicObject
				);
			}
			set {
				if (value != null) {
					value.MatchAction(
						dynamicObj => {
							referencedObjId = dynamicObj.ID;
						},
						serializedDynamicObject => {
							referencedObjId = serializedDynamicObject.ID;
						}
					);
				}
				else {
					referencedObjId = "";
				}
			}
		}
		
		// Implicit SerializableReference creation from SaveableObject
		public static implicit operator SuperspectiveDynamicReference(DynamicObject obj) {
			return obj != null ? new SuperspectiveDynamicReference { Reference = obj } : null;
		}
		
		// Implicit SerializableReference creation from SerializableSaveObject
		public static implicit operator SuperspectiveDynamicReference(DynamicObject.DynamicObjectSave save) {
			return save != null ? new SuperspectiveDynamicReference { Reference = save } : null;
		}
	}

	[Serializable]
	public class SerializableDictionary<K, V> {
		List<K> keys;
		List<V> values;
		
		public Dictionary<K, V> Dictionary {
			get {
				return keys
					.Zip(values, (key, value) => new KeyValuePair<K, V>(key, value))
					.ToDictionary();
			}
			set {
				keys = new List<K>(value.Keys);
				values = new List<V>(value.Values);
			}
		}

		public static implicit operator Dictionary<K, V>(SerializableDictionary<K, V> instance) {
			return instance?.Dictionary;
		}

		public static implicit operator SerializableDictionary<K, V>(Dictionary<K, V> dict) {
			return new SerializableDictionary<K, V>() { Dictionary = dict };
		}
	}

	[Serializable]
	public class SerializableParticleSystem {
		public uint randomSeed;
		public float time;

		public static implicit operator SerializableParticleSystem(ParticleSystem ps) {
			return new SerializableParticleSystem {
				randomSeed = ps.randomSeed,
				time = ps.time
			};
		}
	}

	public static class ParticleSystemExt {
		public static void LoadFromSerializable(this ParticleSystem ps, SerializableParticleSystem serializable) {
			ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
			ps.randomSeed = serializable.randomSeed;
			ps.Simulate(serializable.time);
			ps.Play();
		}
	}

	public abstract class SerializableSetting {
		public string key;
		public string value;

		public abstract void RestoreSettingValue(Setting setting);

		public static SerializableSetting From(Setting setting) {
			switch (setting) {
				case SmallIntSetting smallIntSetting:
					return SerializableFloatSetting.From(smallIntSetting);
				case FloatSetting floatSetting:
					return SerializableFloatSetting.From(floatSetting);
				case DropdownSetting dropdownSetting:
					return SerializableDropdownSetting.From(dropdownSetting);
				case KeybindSetting keybindSetting:
					return SerializableKeybindSetting.From(keybindSetting);
				case TextAreaSetting textAreaSetting:
					return SerializableTextAreaSetting.From(textAreaSetting);
				case ToggleSetting toggleSetting:
					return SerializableToggleSetting.From(toggleSetting);
				default: throw new ArgumentOutOfRangeException($"{setting.GetType()} not handled in switch statement");
			}
		}
	}

	[Serializable]
	public class SerializableFloatSetting : SerializableSetting {
		[FormerlySerializedAs("value")]
		public float floatValue;

		public override void RestoreSettingValue(Setting setting) {
			if (setting is FloatSetting fs) {
				fs.value = floatValue;
			}
		}

		public static SerializableSetting From(FloatSetting setting) {
			return new SerializableFloatSetting() {
				key = setting.key,
				floatValue = setting.value
			};
		}
	}

	[Serializable]
	public class SerializableDropdownSetting : SerializableSetting {
		public List<string> selectedKeys;

		public override void RestoreSettingValue(Setting setting) {
			if (setting is DropdownSetting ds) {
				ds.dropdownSelection.RestoreSelection(selectedKeys, (key, option) => option.DisplayName == key);
			}
		}

		public static SerializableSetting From(DropdownSetting setting) {
			return new SerializableDropdownSetting() {
				key = setting.key,
				selectedKeys = setting.dropdownSelection.allSelections.Keys.ToList()
			};
		}
	}

	[Serializable]
	public class SerializableKeybindSetting : SerializableSetting {
		public string primary;
		public string secondary;

		public override void RestoreSettingValue(Setting setting) {
			if (setting is not KeybindSetting kbs) return;

			Either<int, KeyCode> FromString(string s) {
				switch (s) {
					case "":
						return null;
					case "Left Mouse":
						return new Either<int, KeyCode>(0);
					case "Right Mouse":
						return new Either<int, KeyCode>(1);
					case "Middle Mouse":
						return new Either<int, KeyCode>(2);
					default: {
						if (s.StartsWith("MB") && int.TryParse(s.Substring(2), out int mouseButton)) {
							// We display the mouse button as 1 higher so subtract 1 (e.g. "MB4" actually has a mouse button value of 3)
							return new Either<int, KeyCode>(mouseButton - 1);
						}
						else if (KeyCode.TryParse(s, out KeyCode key)) {
							return new Either<int, KeyCode>(key);
						}
						else {
							return null;
						}
					}
				}
			}

			Either<int, KeyCode> primaryKeybind = FromString(this.primary);
			Either<int, KeyCode> secondaryKeybind = FromString(this.secondary);

			kbs.value.SetMapping(primaryKeybind, secondaryKeybind);
		}

		public static SerializableSetting From(KeybindSetting setting) {
			return new SerializableKeybindSetting() {
				key = setting.key,
				primary = setting.value.displayPrimary,
				secondary = setting.value.displaySecondary
			};
		}
	}

	[Serializable]
	public class SerializableTextAreaSetting : SerializableSetting {
		public string text;

		public override void RestoreSettingValue(Setting setting) {
			if (setting is not TextAreaSetting tas) return;
			
			tas.Text = text;
		}

		public static SerializableSetting From(TextAreaSetting setting) {
			return new SerializableTextAreaSetting() {
				key = setting.key,
				text = setting.Text
			};
		}
	}

	[Serializable]
	public class SerializableToggleSetting : SerializableSetting {
		[FormerlySerializedAs("value")]
		public bool boolValue;

		public override void RestoreSettingValue(Setting setting) {
			if (setting is not ToggleSetting ts) return;
			
			ts.value = boolValue;
		}

		public static SerializableSetting From(ToggleSetting setting) {
			return new SerializableToggleSetting() {
				key = setting.key,
				boolValue = setting.value
			};
		}
	}
}
