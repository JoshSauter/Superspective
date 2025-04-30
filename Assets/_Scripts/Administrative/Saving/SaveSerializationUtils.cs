using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SerializableClasses;
using StateUtils;
using SuperspectiveAttributes;
using SuperspectiveUtils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Saving {
	/// <summary>
	/// Utility class for serializing SuperspectiveObjects, collections, StateMachines, and other data.
	/// </summary>
    public static class SaveSerializationUtils {
	    public const BindingFlags FIELD_TAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
	    // Yeah, they're the same, but just in case I want to change it later
	    public const BindingFlags PROPERTY_TAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
		
#region Serialization
        public static bool TryGetSerializedData(object rawData, out object serializedData) {
            serializedData = null;
            if (rawData == null) {
                return true; // null is a valid value to save, the absence of a value is state
            }

            Type rawType = rawData.GetType();
            
            if (rawType.IsPrimitive || rawType.IsEnum || rawType == typeof(string)) {
                serializedData = rawData;
                return true;
            }

            if (!SerializableTypeRegistry.TryGetSerializer(rawType, out var serializableType, out SerializableTypeRegistry.ConverterFunc converter)) {
                return false;
            }

            // If this is a SuperspectiveObject, we need to check that the ID is valid
            if (rawData is SuperspectiveObject { HasValidId: false }) {
                return false;
            }

            try {
	            return converter(rawType, rawData, serializableType, ref serializedData);
            }
            catch {
	            Debug.LogError($"Failed to convert {rawType.GetReadableTypeName()} to {serializableType.GetReadableTypeName()}");
	            return false;
            }
        }
        
        public static HashSet<FieldInfo> GetSerializableFields(SuperspectiveObject superspectiveObject, HashSet<string> ignoreFields) {
	        Type type = superspectiveObject.GetType();
	        ignoreFields.UnionWith(type
		        .TraverseTypeHierarchy()
		        .SelectMany(t => t.GetProperties(PROPERTY_TAGS))
		        .Where(p => p.IsDefined(typeof(DoNotSaveAttribute), true))
		        .Select(p => $"<{p.Name}>k__BackingField")
		        .ToHashSet());
	        
            // This HashSet is the short-list of fields that we will try to serialize. Some may not get serialized.
            return type
	            .TraverseTypeHierarchy()
	            .SelectMany(t => t.GetFields(FIELD_TAGS))
                .Where(f => !ignoreFields.Contains(f.Name) && IsSerializable(f))
                .ToHashSet();
        }

        // Pass the field through a bunch of filters to determine if it's something we can potentially serialize
        private static bool IsSerializable(FieldInfo f) {
            // DoNotSave attribute -- obviously don't save it
            if (f.IsDefined(typeof(DoNotSaveAttribute), true)) return false;
            // Read-only fields should not be saved
            if (f.IsInitOnly) return false;
            // Delegates are not saved
            if (typeof(Delegate).IsAssignableFrom(f.FieldType)) return false;
            // Unity Object references are only saved if they are marked with SaveReferenceAttribute
            if (!f.IsDefined(typeof(SaveUnityObjectAttribute), true) && typeof(Object).IsAssignableFrom(f.FieldType)) return false;
            // Collections are only saved if they are marked with SaveCollectionAttribute
            if (!f.IsDefined(typeof(SaveCollectionAttribute), true) && IsCollection(f.FieldType)) return false;
            
            // If it passes all the filters, it's serializable
            return true;
        }
        
        private static bool InheritsFromGeneric(Type type, Type openGeneric) {
            return type.GetInterfaces()
                .Where(i => i.IsGenericType)
                .Select(i => i.GetGenericTypeDefinition())
                .Contains(openGeneric);
        }
        
        private static bool IsCollection(Type type) {
            return type.IsArray ||
                   InheritsFromGeneric(type, typeof(ICollection<>)) ||
                   InheritsFromGeneric(type, typeof(IDictionary<,>));
        }
#endregion

#region Deserialization
	    public static bool TryGetDeserializedData(object serializedData, Type targetType, ref object deserializedData) {
		    if (serializedData == null) return true; // null is valid

		    Type serializedDataType = serializedData.GetType();

		    if (serializedDataType.IsPrimitive || serializedDataType.IsEnum || serializedDataType == typeof(string)) {
			    deserializedData = serializedData;
			    return true;
		    }

		    if (!SerializableTypeRegistry.TryGetDeserializer(targetType, out SerializableTypeRegistry.ConverterFunc converter)) {
			    Debug.LogWarning($"[Deserialization] No deserializer found for type: {targetType.GetReadableTypeName()}");
			    return false;
		    }

		    return converter(serializedDataType, serializedData, targetType, ref deserializedData);
	    }
#endregion
    }
	
	/// <summary>
	/// Registry for types that can be serialized. This includes SuperspectiveObjects, collections, StateMachines, and other data.
	/// </summary>
	public static class SerializableTypeRegistry {
		/// <summary>
		/// Delegate for the converter function, used to convert fromData (of type fromType) to toData (of type toType).
		/// toData is passed by reference so that it can be modified in place (e.g. for StateMachines or ParticleSystems)
		/// </summary>
		public delegate bool ConverterFunc(Type fromType, object fromData, Type toType, ref object toData);
		
		public class Converter {
			public readonly Type fromType;
			public readonly Type toType;
			// The converter function takes the fromType and the fromData, and returns the toType and the converted data
			// This could be an implicit operator, a constructor, or some custom converter
			public readonly ConverterFunc converter;

			public Converter(Type fromType, Type toType, ConverterFunc converter) {
				this.fromType = fromType;
				this.toType = toType;
				this.converter = converter;
			}
		}
		
		// Known types and their serialized types. Starts with known non-generic type mappings, generic types are added as they are used for the first time
		private static readonly Dictionary<Type, Converter> serializers = new Dictionary<Type, Converter>() {
			{ typeof(Vector2), new Converter(typeof(Vector2), typeof(SerializableVector2), ImplicitSerializer) },
			{ typeof(Vector3), new Converter(typeof(Vector3), typeof(SerializableVector3), ImplicitSerializer) },
			{ typeof(Quaternion), new Converter(typeof(Quaternion), typeof(SerializableQuaternion), ImplicitSerializer) },
			{ typeof(Color), new Converter(typeof(Color), typeof(SerializableColor), ImplicitSerializer) },
			{ typeof(Keyframe), new Converter(typeof(Keyframe), typeof(SerializableKeyframe), ImplicitSerializer) },
			{ typeof(AnimationCurve), new Converter(typeof(AnimationCurve), typeof(SerializableAnimationCurve), ImplicitSerializer) },
			{ typeof(Gradient), new Converter(typeof(Gradient), typeof(SerializableGradient), ImplicitSerializer) },
			{ typeof(SuperspectiveObject), new Converter(typeof(SuperspectiveObject), typeof(SuperspectiveReference), ImplicitSerializer) },
			{ typeof(SaveObject), new Converter(typeof(SaveObject), typeof(SuperspectiveReference), ImplicitSerializer) },
			{ typeof(DynamicObject), new Converter(typeof(DynamicObject), typeof(SuperspectiveDynamicReference), ImplicitSerializer) },
			{ typeof(DynamicObject.DynamicObjectSave), new Converter(typeof(DynamicObject.DynamicObjectSave), typeof(SuperspectiveDynamicReference), ImplicitSerializer) },
			{ typeof(ParticleSystem), new Converter(typeof(ParticleSystem), typeof(SerializableParticleSystem), ImplicitSerializer) }
		};

		// Field types and known deserializers. Note that this is also keyed by the runtime type, because deserialization is driven by the field type, not the serialized type.
		private static readonly Dictionary<Type, Converter> deserializers = new Dictionary<Type, Converter>() {
			{ typeof(Vector2), new Converter(typeof(SerializableVector2), typeof(Vector2), ImplicitDeserializer) },
			{ typeof(Vector3), new Converter(typeof(SerializableVector3), typeof(Vector3), ImplicitDeserializer) },
			{ typeof(Quaternion), new Converter(typeof(SerializableQuaternion), typeof(Quaternion), ImplicitDeserializer) },
			{ typeof(Color), new Converter(typeof(SerializableColor), typeof(Color), ImplicitDeserializer) },
			{ typeof(Keyframe), new Converter(typeof(SerializableKeyframe), typeof(Keyframe), ImplicitDeserializer) },
			{ typeof(AnimationCurve), new Converter(typeof(SerializableAnimationCurve), typeof(AnimationCurve), ImplicitDeserializer) },
			{ typeof(Gradient), new Converter(typeof(SerializableGradient), typeof(Gradient), ImplicitDeserializer) }
		};

		public static bool ImplicitSerializer(Type fromType, object fromData, Type toType, ref object toData) {
			var implicitOp = ImplicitOp(toType, fromType, toType);

			if (implicitOp != null) {
				toData = implicitOp.Invoke(null, new object[] { fromData });
				return true;
			}

			return false;
		}
		
		public static bool ImplicitDeserializer(Type fromType, object fromData, Type toType, ref object toData) {
			var implicitOp = ImplicitOp(fromType, fromType, toType);

			if (implicitOp != null) {
				toData = implicitOp.Invoke(null, new object[] { fromData });
				return true;
			}

			return false;
		}

		private static MethodInfo ImplicitOp(Type serializedType, Type fromType, Type toType) {
			// Implicit converters are defined on the Serialized type, not the runtime type
			return serializedType.GetMethods(BindingFlags.Static | BindingFlags.Public)
				.FirstOrDefault(m =>
					m.Name == "op_Implicit" &&
					m.ReturnType == toType &&
					m.GetParameters().Length == 1 &&
					m.GetParameters()[0].ParameterType.IsAssignableFrom(fromType));
		}

		/// <summary>
		/// Registers a type and its conversion to a serialized type. This is used to cache the results of serialization lookups.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="serializedType"></param>
		/// <param name="converter"></param>
		public static void RegisterSerializer(Type type, Type serializedType, ConverterFunc converter) {
			serializers[type] = new Converter(type, serializedType, converter);
		}

		/// <summary>
		/// Registers a type and its conversion from a serialized type. This is used to cache the results of deserialization lookups.
		/// </summary>
		/// <param name="serializedType"></param>
		/// <param name="type"></param>
		/// <param name="converter"></param>
		public static void RegisterDeserializer(Type serializedType, Type type, ConverterFunc converter) {
			deserializers[type] = new Converter(serializedType, type, converter);
		}
		
#region Serialization
		/// <summary>
		/// Tries to get the serialized type and converter function for a given runtime type. This includes SuperspectiveObjects, collections, StateMachines, and other data.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="serializedType"></param>
		/// <returns>True if the type is serializable, false otherwise.</returns>
		public static bool TryGetSerializer(Type type, out Type serializedType, out ConverterFunc converter) {
			if (serializers.TryGetValue(type, out Converter converterDesc)) {
				converter = converterDesc.converter;
				serializedType = converterDesc.toType;
				return true;
			}
			
			// Handle StateMachine<T> → StateMachine<T>.StateMachineSave
			if (TryGetStateMachineSerializer(type, out serializedType, out converter)) {
				RegisterSerializer(type, serializedType, converter); // Cache this result for future lookups
				return true;
			}

			// Handle SuperspectiveObject<T, S> → SuperspectiveReference<T, S>
			if (TryGetSuperspectiveObjectSerializer(type, out serializedType, out converter)) {
				RegisterSerializer(type, serializedType, converter); // Cache this result for future lookups
				return true;
			}
			
			// Handle Dictionary<K, V> → SerializableDictionary<K, V>
			if (TryGetDictionarySerializer(type, out serializedType, out converter)) {
				RegisterSerializer(type, serializedType, converter); // Cache this result for future lookups
				return true;
			}

			return false;
		}
		
		private static bool TryGetStateMachineSerializer(Type type, out Type serializedType, out ConverterFunc converter) {
			serializedType = null;
			converter = null;

			if (!IsStateMachineType(type, out Type T)) return false;

			serializedType = typeof(StateMachineSave<>).MakeGenericType(T);

			converter = (Type fromType, object fromData, Type toType, ref object toData) => {
				if (fromData == null || !fromType.IsInstanceOfType(fromData)) return false;

				if (!IsStateMachineType(fromType, out Type _)) return false;

				MethodInfo toSaveMethod = fromType.GetMethod("ToSave", BindingFlags.Instance | BindingFlags.Public);
				if (toSaveMethod == null) return false;

				toData = toSaveMethod.Invoke(fromData, null);
				return toData != null;
			};

			return true;
		}
		
		private static bool TryGetSuperspectiveObjectSerializer(Type type, out Type serializedType, out ConverterFunc converter) {
			serializedType = null;
			converter = null;

			if (!IsSuperspectiveObjectType(type, out Type T, out Type S)) return false;
			serializedType = typeof(SuperspectiveReference<,>).MakeGenericType(T, S);

			// Define the converter
			converter = (Type fromType, object fromData, Type toType, ref object toData) => {
				if (fromData == null || !fromType.IsInstanceOfType(fromData)) {
					return false;
				}

				// Get implicit operator from SuperspectiveObject<T, S> to SuperspectiveReference<T, S>
				MethodInfo implicitOp = toType.GetMethods(BindingFlags.Public | BindingFlags.Static)
					.FirstOrDefault(m =>
						m.Name == "op_Implicit" &&
						m.ReturnType == toType &&
						m.GetParameters().Length == 1 &&
						m.GetParameters()[0].ParameterType.IsAssignableFrom(fromType)
					);

				if (implicitOp == null) {
					Debug.LogError($"No implicit operator found from {fromType.Name} to {toType.Name}");
					return false;
				}

				toData = implicitOp.Invoke(null, new[] { fromData });
				return toData != null;
			};

			return true;
		}

		
		private static bool TryGetDictionarySerializer(Type type, out Type serializedType, out ConverterFunc converter) {
			serializedType = null;
			converter = null;

			if (!IsDictionaryType(type, out Type K, out Type V)) return false;
			var serializableGenericType = typeof(SerializableDictionary<,>).MakeGenericType(K, V);

			serializedType = serializableGenericType;
			converter = ImplicitSerializer;
			return true;
		}

#endregion
		
#region Deserialization
		public static bool TryGetDeserializer(Type type, out ConverterFunc converter) {
			if (deserializers.TryGetValue(type, out Converter converterDesc)) {
				converter = converterDesc.converter;
				return true;
			}

			// Handle StateMachineSave<T> → StateMachine<T>
			if (TryGetStateMachineSaveDeserializer(type, out Type serializedType, out converter)) {
				RegisterDeserializer(serializedType, type, converter); // Cache this result for future lookups
				return true;
			}

			// Handle SuperspectiveReference<T, S> → SuperspectiveObject<T, S>, etc.
			if (TryGetSuperspectiveReferenceDeserializer(type, out serializedType, out converter)) {
				RegisterDeserializer(serializedType, type, converter); // Cache this result for future lookups
				return true;
			}

			// Handle SerializableDictionary<K, V> → Dictionary<K, V>
			if (TryGetSerializableDictionaryDeserializer(type, out serializedType, out converter)) {
				RegisterDeserializer(serializedType, type, converter); // Cache this result for future lookups
				return true;
			}
			
			// Handle SerializableParticleSystem → ParticleSystem
			if (TryGetParticleSystemDeserializer(type, out serializedType, out converter)) {
				RegisterDeserializer(serializedType, type, converter);
				return true;
			}

			converter = null;
			return false;
		}
		
		private static bool TryGetStateMachineSaveDeserializer(Type targetType, out Type serializedType, out ConverterFunc converter) {
			serializedType = null;
			converter = null;

			if (!IsStateMachineType(targetType, out Type T)) return false;
			serializedType = typeof(StateMachineSave<>).MakeGenericType(T);

			converter = (Type fromType, object fromData, Type toType, ref object toData) => {
				if (fromData == null || !fromType.IsInstanceOfType(fromData) || !toType.IsInstanceOfType(toData)) {
					return false;
				}

				MethodInfo loadFromSaveMethod = toType.GetMethod("LoadFromSave", BindingFlags.Instance | BindingFlags.Public);
				if (loadFromSaveMethod == null) {
					Debug.LogError($"[Deserialization] Could not find LoadFromSave method on {toType.Name}");
					return false;
				}

				loadFromSaveMethod.Invoke(toData, new[] { fromData, false });
				return true;
			};

			return true;
		}
		
		// Yup this is a hefty function. SuperspectiveReference<T, S> can be deserialized into a...
		// 1) SuperspectiveReference<T, S>
		// 2) SuperspectiveReference<T>
		// 3) SuperspectiveReference
		// 4) SuperspectiveObject<T, S> or subclass
		// 5) SaveObject<T> or subclass
		// 6) SuperspectiveObject (base class, untyped)
		// and we need to be able to handle any of these types of fields
		private static bool TryGetSuperspectiveReferenceDeserializer(Type targetType, out Type serializedType, out ConverterFunc converter) {
			// Analogy: You have a box with either an object, or a piece of paper describing that object. Someone makes a request to you about the box
			serializedType = null;
			converter = null;
			
			// Case 1: Deserializing into SuperspectiveReference<T, S> field
			// Example: SuperspectiveReference<PickupObject, PickupObjectSave> cubeRef;
			// Analogy: Give me the box, I know there's either an object I know about, or a piece of paper describing that object, inside
			if (IsSuperspectiveReferenceType(targetType, out Type targetT, out Type targetS)) {
				serializedType = typeof(SuperspectiveReference<,>).MakeGenericType(targetT, targetS);

				converter = (Type fromType, object fromData, Type toType, ref object toData) => {
					if (fromData == null || !IsSuperspectiveReferenceType(fromType, out Type T1, out Type S1)) {
						return false;
					}

					// Ensure the types match exactly or are assignable
					if (!targetT.IsAssignableFrom(T1) || !targetS.IsAssignableFrom(S1)) {
						Debug.LogWarning($"[Deserialization] Type mismatch when assigning SuperspectiveReference<{T1.GetReadableTypeName()}, {S1.GetReadableTypeName()}> to SuperspectiveReference<{targetT.GetReadableTypeName()}, {targetS.GetReadableTypeName()}>");
						return false;
					}

					// Use FromGenericReference to convert
					MethodInfo fromGenericRef = typeof(SuperspectiveReference<,>)
						.MakeGenericType(targetT, targetS)
						.GetMethod("FromGenericReference", BindingFlags.Public | BindingFlags.Static);

					if (fromGenericRef == null) {
						Debug.LogError($"[Deserialization] Could not find FromGenericReference on SuperspectiveReference<{targetT.GetReadableTypeName()}, {targetS.GetReadableTypeName()}>");
						return false;
					}

					toData = fromGenericRef.Invoke(null, new[] { fromData });
					return toData != null;
				};

				return true;
			}

			// Case 2: Deserializing into SuperspectiveReference<T> field
			// Example: SuperspectiveReference<PickupObject> cubeRef;
			// Analogy: Give me the box, I know there's an object in it and I know what it is
			if (IsSuperspectiveReferenceType(targetType, out targetT)) {
				targetS = typeof(SaveObject<>).MakeGenericType(targetT);
				serializedType = typeof(SuperspectiveReference<,>).MakeGenericType(targetT, targetS);

				converter = (Type fromType, object fromData, Type toType, ref object toData) => {
					if (fromData == null || !IsSuperspectiveReferenceType(fromType, out Type fromT, out Type fromS)) {
						return false;
					}

					if (!targetT.IsAssignableFrom(fromT)) {
						Debug.LogWarning($"[Deserialization] Type mismatch when assigning SuperspectiveReference<{fromT.GetReadableTypeName()}, {fromS.GetReadableTypeName()}> to SuperspectiveReference<{targetT.GetReadableTypeName()}>");
						return false;
					}

					MethodInfo fromGenericRef = typeof(SuperspectiveReference<>)
						.MakeGenericType(targetT)
						.GetMethod("FromGenericReference", BindingFlags.Public | BindingFlags.Static);

					if (fromGenericRef == null) {
						Debug.LogError($"[Deserialization] Could not find FromGenericReference on SuperspectiveReference<{targetT.GetReadableTypeName()}>");
						return false;
					}

					toData = fromGenericRef.Invoke(null, new[] { fromData });
					return toData != null;
				};

				return true;
			}

			// Case 3: Deserializing into SuperspectiveReference field (untyped)
			// Example: SuperspectiveReference somethingRef;
			// Analogy: Give me the box, I don't care what's in it
			if (targetType == typeof(SuperspectiveReference)) {
				serializedType = typeof(SuperspectiveReference);

				converter = (Type fromType, object fromData, Type toType, ref object toData) => {
					// We only want to accept fromData if it's also a SuperspectiveReference
					if (fromData == null || !typeof(SuperspectiveReference).IsAssignableFrom(fromType)) {
						return false;
					}

					toData = fromData;
					return true;
				};

				return true;
			}

			// Case 4: Deserializing into SuperspectiveObject<T, S> field (or subclass)
			// Example: PickupObject cube;
			// Analogy: I know about the object in that box. Give me the object inside the box, I don't care about the box
			if (IsSuperspectiveObjectType(targetType, out Type T, out Type S)) {
				serializedType = typeof(SuperspectiveReference<,>).MakeGenericType(T, S);

				converter = (Type fromType, object fromData, Type toType, ref object toData) => {
					if (fromData == null || !IsSuperspectiveReferenceType(fromType, out Type fromT, out Type fromS)) {
						return false;
					}

					// Validate that the types match or are assignable
					if (!T.IsAssignableFrom(fromT) || !S.IsAssignableFrom(fromS)) {
						Debug.LogWarning($"[Deserialization] Type mismatch when extracting runtime object from SuperspectiveReference<{fromT.GetReadableTypeName()}, {fromS.GetReadableTypeName()}> into field of type {T.GetReadableTypeName()}");
						return false;
					}

					// Extract the runtime object from the Left side of the Either<runtime, save>
					PropertyInfo referenceProp = fromType.GetProperty("Reference", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
					if (referenceProp == null) {
						Debug.LogError($"[Deserialization] Could not find 'Reference' property on {fromType.GetReadableTypeName()}");
						return false;
					}

					object either = referenceProp.GetValue(fromData);
					if (either == null) {
						Debug.LogError($"[Deserialization] Reference property on {fromType.GetReadableTypeName()} was null");
						return false;
					}

					PropertyInfo leftProp = either.GetType().GetProperty("LeftOrDefault");
					if (leftProp == null) {
						Debug.LogError($"[Deserialization] Could not find 'LeftOrDefault' property on Either<{T.GetReadableTypeName()}, {S.GetReadableTypeName()}>");
						return false;
					}

					toData = leftProp.GetValue(either);
					return toData != null;
				};

				return true;
			}

			// Case 5: Deserializing into SaveObject<T> field (or subclass) (uncommon)
			// Example: PickupObject.PickupObjectSave unloadedCube; // Not sure when this would be applicable, but we'll add support just in case
			// Analogy: I know there's a piece of paper in the box describing the object. Give me the piece of paper, I don't care about the box
			if (IsSaveObjectType(targetType, out targetT)) {
				targetS = targetType;
				serializedType = typeof(SuperspectiveReference<,>).MakeGenericType(targetT, targetS);

				converter = (Type fromType, object fromData, Type toType, ref object toData) => {
					if (fromData == null || !IsSuperspectiveReferenceType(fromType, out Type T, out Type S)) {
						Debug.LogWarning($"[Deserialization] Serialized data is not a valid SuperspectiveReference<,> for SaveObject field");
						return false;
					}

					PropertyInfo referenceProp = fromType.GetProperty("Reference", BindingFlags.Instance | BindingFlags.Public);
					if (referenceProp == null) {
						Debug.LogError($"[Deserialization] Could not find 'Reference' property on {fromType.GetReadableTypeName()}");
						return false;
					}

					object either = referenceProp.GetValue(fromData);
					if (either == null) {
						Debug.LogWarning($"[Deserialization] Reference property on {fromType.GetReadableTypeName()} was null");
						return false;
					}

					PropertyInfo rightProp = either.GetType().GetProperty("RightOrDefault");
					if (rightProp == null) {
						Debug.LogError($"[Deserialization] Could not find 'RightOrDefault' property on Either<{T.GetReadableTypeName()}, {S.GetReadableTypeName()}>");
						return false;
					}

					toData = rightProp.GetValue(either);
					return toData != null;
				};

				return true;
			}
			
			// Case 6: Deserializing into SuperspectiveObject (base class, untyped)
			// Example: SuperspectiveObject something;
			// Analogy: Give me the object in the box. I don't know nor care about what it is, and I don't care about the box
			if (typeof(SuperspectiveObject).IsAssignableFrom(targetType)) {
				serializedType = typeof(SuperspectiveReference);

				converter = (Type fromType, object fromData, Type toType, ref object toData) => {
					if (fromData == null || !typeof(SuperspectiveReference).IsAssignableFrom(fromType)) {
						return false;
					}

					PropertyInfo referenceProp = fromType.GetProperty("Reference", BindingFlags.Instance | BindingFlags.Public);
					if (referenceProp == null) {
						Debug.LogError($"[Deserialization] Could not find 'Reference' property on {fromType.GetReadableTypeName()}");
						return false;
					}

					object either = referenceProp.GetValue(fromData);
					if (either == null) {
						Debug.LogWarning($"[Deserialization] Reference property on {fromType.GetReadableTypeName()} was null");
						return false;
					}

					PropertyInfo leftProp = either.GetType().GetProperty("Left");
					if (leftProp == null) {
						Debug.LogError($"[Deserialization] Could not find 'Left' property on Either<runtime, save> type: {either.GetType().GetReadableTypeName()}");
						return false;
					}

					toData = leftProp.GetValue(either);
					return toData != null;
				};

				return true;
			}

			return false;
		}
		
		private static bool TryGetSerializableDictionaryDeserializer(Type targetType, out Type serializedType, out ConverterFunc converter) {
			serializedType = null;
			converter = null;

			if (!IsDictionaryType(targetType, out Type K, out Type V)) return false;
			serializedType = typeof(SerializableDictionary<,>).MakeGenericType(K, V);
			converter = ImplicitDeserializer;

			return true;
		}

		private static bool TryGetParticleSystemDeserializer(Type targetType, out Type serializedType, out ConverterFunc converter) {
			serializedType = typeof(SerializableParticleSystem);
			converter = null;

			if (targetType != typeof(ParticleSystem)) return false;

			converter = (Type fromType, object fromData, Type toType, ref object toData) => {
				if (fromData == null || !(fromData is SerializableParticleSystem sps)) return false;
				if (toData == null || !(toData is ParticleSystem ps)) return false;

				ps.LoadFromSerializable(sps);
				return true;
			};

			return true;
		}

#endregion
		
		private static bool IsDictionaryType(Type type, out Type K, out Type V) => TryMatchGenericType(type, typeof(Dictionary<,>), out K, out V);
		private static bool IsStateMachineType(Type type, out Type T) => TryMatchGenericType(type, typeof(StateMachine<>), out T);
		private static bool IsSuperspectiveObjectType(Type type, out Type T, out Type S) => TryMatchGenericType(type, typeof(SuperspectiveObject<,>), out T, out S);
		private static bool IsSaveObjectType(Type type, out Type T) => TryMatchGenericType(type, typeof(SaveObject<>), out T);
		private static bool IsSuperspectiveReferenceType(Type type, out Type T) => TryMatchGenericType(type, typeof(SuperspectiveReference<>), out T);
		private static bool IsSuperspectiveReferenceType(Type type, out Type T, out Type S) => TryMatchGenericType(type, typeof(SuperspectiveReference<,>), out T, out S);

		private static bool TryMatchGenericType(Type concreteType, Type genericTypeDef, out Type T) {
			T = null;

			while (concreteType != null && concreteType != typeof(object)) {
				if (concreteType.IsGenericType && concreteType.GetGenericTypeDefinition() == genericTypeDef) {
					var args = concreteType.GetGenericArguments();
					if (args.Length != 1) {
						Debug.LogError($"[Serialization] Expected 1 generic argument for {genericTypeDef.Name}, got {args.Length}");
						return false;
					}
					T = args[0];
					return true;
				}
				concreteType = concreteType.BaseType;
			}

			return false;
		}
		
		private static bool TryMatchGenericType(Type concreteType, Type genericTypeDef, out Type T, out Type S) {
			T = null;
			S = null;

			while (concreteType != null && concreteType != typeof(object)) {
				if (concreteType.IsGenericType && concreteType.GetGenericTypeDefinition() == genericTypeDef) {
					var args = concreteType.GetGenericArguments();
					if (args.Length != 2) {
						Debug.LogError($"[Serialization] Expected 2 generic arguments for {genericTypeDef.Name}, got {args.Length}");
						return false;
					}
					T = args[0];
					S = args[1];
					return true;
				}
				concreteType = concreteType.BaseType;
			}

			return false;
		}

	}
}