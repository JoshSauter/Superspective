using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LevelManagement;
using ObjectSerializationUtils;
using SerializableClasses;
using Sirenix.Utilities;
using SuperspectiveAttributes;
using UnityEngine;
using SuperspectiveUtils;

namespace Saving {
    [Serializable]
    public class SaveObject {
        ///////////////////
        // Explicit data //
        ///////////////////
        public string ID;
        // AssociationID is the UUID component of ID, or, if there is none, just ID
        // It is used to find all associated objects to be deregistered if a save object is Destroyed while unloaded
        public string associationID;
        public Levels level;
        public bool isGameObjectActive;
        public bool isScriptEnabled;
        
        // Normal Transforms
        public SerializableVector3 localPosition;
        public SerializableQuaternion localRotation;
        // Scale always handled by GrowShrinkObject
        
        // RectTransforms
        public SerializableVector2 anchoredPosition;
        public SerializableVector3 localScale;
        
        ///////////////////
        // Implicit data //
        ///////////////////
        public SerializableDictionary<string, object> implicitlySavedFields = new SerializableDictionary<string, object>();

        protected SaveObject() { }

        // Must be overridden by inheriting classes
        public SaveObject(SuperspectiveObject superspectiveObject) {
            try {
                this.ID = superspectiveObject.ID;
                this.associationID = superspectiveObject.AssociationID;
                this.level = superspectiveObject.Level;
                this.isGameObjectActive = superspectiveObject.gameObject.activeSelf;
                this.isScriptEnabled = superspectiveObject.enabled;
                if (superspectiveObject.transform is RectTransform rectTransform) {
                    this.anchoredPosition = rectTransform.anchoredPosition;
                    this.localRotation = rectTransform.localRotation;
                    this.localScale = rectTransform.localScale;
                }
                else {
                    superspectiveObject.debug.Log($"Saving local position as {superspectiveObject.transform.localPosition}");
                    this.localPosition = superspectiveObject.transform.localPosition;
                    this.localRotation = superspectiveObject.transform.localRotation;
                }


                // Collect all fields (by name) explicitly defined on this SaveObject
                HashSet<string> explicitlySavedFields = this.GetType().GetFields(SaveSerializationUtils.FIELD_TAGS)
                    .Select(f => f.Name)
                    .ToHashSet();

                // Collect all fields defined on the SuperspectiveObject being used to create this SaveObject
                // Remove any that are already explicitly defined, and any that are marked with the DoNotSaveAttribute
                // This HashSet is the short-list of fields that we will try to serialize. Some may not get serialized.
                HashSet<FieldInfo> implicitlySavedFieldCandidateInfos = SaveSerializationUtils.GetSerializableFields(superspectiveObject, explicitlySavedFields);

                Dictionary<FieldInfo, object> implicitlySavedData = new Dictionary<FieldInfo, object>();
                foreach (var field in implicitlySavedFieldCandidateInfos) {
                    try {
                        if (SaveSerializationUtils.TryGetSerializedData(field.GetValue(superspectiveObject), out object serializedData)) {
                            implicitlySavedData.Add(field, serializedData);
                            superspectiveObject.debug.Log($"Serializing field {field.Name} ({field.FieldType.GetReadableTypeName()}) as {serializedData?.GetType().GetReadableTypeName() ?? "(null)"}");
                        }
                        else {
                            superspectiveObject.debug.LogWarning($"Field {field.Name} ({field.FieldType.GetReadableTypeName()}) is not serializable. Skipping.");
                        }
                    }
                    catch (Exception e) {
                        superspectiveObject.debug.LogError($"Failed to serialize field {field.Name} ({field.FieldType.GetReadableTypeName()}). Caused by: {e}", true);
                    }
                }
                
                superspectiveObject.debug
                    .Log($"{implicitlySavedData.Count} implicitly saved fields:\n{GetDebugStringForImplicitlySavedFields(implicitlySavedData)}");
                
                implicitlySavedFields = implicitlySavedData.ToDictionary(kv => kv.Key.Name, kv => kv.Value);

#if UNITY_EDITOR
                // For debugging purposes, we will try serializing each field up front and log any errors
                // This is obviously wasteful and inefficient so we won't do it in builds
                foreach (var kv in implicitlySavedData) {
                    var key = kv.Key;
                    var val = kv.Value;
                    try {
                        val.SerializeToByteArray();
                    }
                    catch (Exception e) {
                        superspectiveObject.debug.LogError($"Failed to serialize implicitly saved field '{key.Name}' ({key.FieldType.GetReadableTypeName()}). Caused by:\n{e}", true);
                    }
                }
#endif
            }
            catch (Exception e) {
                if (superspectiveObject != null) {
                    superspectiveObject.debug.LogError(e, true);
                }
                else {
                    Debug.LogError($"Script is null! Can't save this object.");
                }
            }
        }

        private string GetDebugStringForImplicitlySavedFields(Dictionary<FieldInfo, object> implicitlySavedData) {
            return string.Join("\n", implicitlySavedData.Select(kv => {
                FieldInfo field = kv.Key;
                object serializedData = kv.Value;
                string serializedTypeName = serializedData?.GetType().GetReadableTypeName() ?? "(null)";
                string fieldTypeName = field.FieldType.GetReadableTypeName();

                string typeDetails = (serializedTypeName == fieldTypeName || serializedTypeName == "(null)") ? fieldTypeName : $"{fieldTypeName} -> {serializedTypeName}";
                string serializedDataString = serializedData?.ToString() ?? "(null)";
                return $"{field.Name} ({typeDetails}) = {serializedDataString}";
            }));
        }

        /// <summary>
        /// Unregisters this save object while in an unloaded scene.
        /// </summary>
        public virtual void Destroy() {
            SaveManager.Unregister(ID);
        }
    }

    [Serializable]
    public abstract class SaveObject<T> : SaveObject where T : SuperspectiveObject {
        protected SaveObject(T saveableObject) : base(saveableObject) {}
    }
}
