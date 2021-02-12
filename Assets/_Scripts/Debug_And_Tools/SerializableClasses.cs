using Saving;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace SerializableClasses {
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
	public class SerializableGradient {
		const int GRADIENT_ARRAY_SIZE = 10;
		// RGB + Key Time
		public float[,] colors = new float[GRADIENT_ARRAY_SIZE, 4];
		// Value + Key Time
		public float[,] alphaValues = new float[GRADIENT_ARRAY_SIZE, 2];

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

				newGradient.colorKeys = colorKeys.ToArray();
				newGradient.alphaKeys = alphaKeys.ToArray();
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
	}

	[Serializable]
	public class SerializableReference<T> where T : MonoBehaviour, ISaveableObject {
		public string referencedSceneName;
		public string referencedObjId;

		public T Reference {
			get {
				SaveManagerForScene saveManagerForScene = SaveManager.GetSaveManagerForScene(referencedSceneName);
				if (saveManagerForScene != null) {
					ISaveableObject saveableObject = saveManagerForScene.GetSaveableObject(referencedObjId);
					if (saveableObject != null) {
						return saveableObject as T;
					}
					else {
						Debug.LogError($"Can't restore reference for id: {referencedObjId} in scene {referencedSceneName}");
						return null;
					}
				}
				else {
					Debug.LogError($"Can't restore reference for id: {referencedObjId} in scene {referencedSceneName}");
					return null;
				}
			}
			set {
				if (value != null) {
					referencedSceneName = value.gameObject.scene.name;
					referencedObjId = value.ID;
				}
				else {
					referencedSceneName = "";
					referencedObjId = "";
				}
			}
		}

		public static implicit operator T(SerializableReference<T> instance) {
			return instance?.Reference;
		}

		public static implicit operator SerializableReference<T>(T obj) {
			if (obj != null) {
				return new SerializableReference<T> { Reference = obj };
			}
			else {
				return null;
			}
		}
	}
}
