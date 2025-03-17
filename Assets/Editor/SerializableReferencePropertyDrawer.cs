using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Saving;
using SerializableClasses;
using Sirenix.Utilities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Editor {
    [CustomPropertyDrawer(typeof(SuperspectiveReference))]
    public class SerializableReferencePropertyDrawer : PropertyDrawer {
        public class CachedReference {
            public SuperspectiveObject cachedReference;
            public string cachedId;
        }

        private static Dictionary<string, CachedReference> cachedReferences = new Dictionary<string, CachedReference>();
        private bool viewAsReference = true;
        private bool invalidReference = false;

        public override void OnGUI(
            Rect position,
            SerializedProperty property,
            GUIContent label
        ) {
            EditorGUI.BeginProperty(position, label, property);

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            SerializedProperty referencedObjId = property.FindPropertyRelative("referencedObjId");

            if (!cachedReferences.ContainsKey(referencedObjId.stringValue)) {
                cachedReferences.Add(referencedObjId.stringValue, new CachedReference());
            }

            CachedReference cached = cachedReferences[referencedObjId.stringValue];

            float height = (position.height) / (viewAsReference ? 2.0f : 3.0f);

            Rect sameSceneRect = new Rect(position.x, position.y, position.width, height);
            Rect referenceRect = new Rect(position.x, position.y + 20, position.width, height);
            Rect idRect = new Rect(position.x, position.y + 20, position.width, height);

            bool prevViewAsReference = viewAsReference;
            viewAsReference = EditorGUI.Toggle(sameSceneRect, "View as reference?", viewAsReference);

            if (viewAsReference) {
                Type genericReferenceType = GetSaveableObjectType();
                string cachedId = cached.cachedId;
                string cachedReferenceId = cached.cachedReference?.ID ?? "";

                if (cachedId != referencedObjId.stringValue || cachedReferenceId != referencedObjId.stringValue) {
                    cachedId = referencedObjId.stringValue;

                    // If the current ID & sceneName are present, try to update the cached reference accordingly
                    if (!string.IsNullOrEmpty(cachedId)) {
                            SuperspectiveObject match = SaveManager.FindObjectById<SuperspectiveObject>(cachedId, invalidReference && prevViewAsReference == viewAsReference);
                            if (genericReferenceType.IsInstanceOfType(match)) {
                                invalidReference = false;
                                cached.cachedReference = match;
                                cached.cachedId = cachedId;
                            }
                            else if (match == null) {
                                invalidReference = true;
                            }
                    }
                }

                SuperspectiveObject prevReference = cached.cachedReference;
                if (cachedId.IsNullOrWhitespace()) {
                    cached.cachedReference = null;
                }
                cached.cachedReference = EditorGUI.ObjectField(
                    referenceRect,
                    label.text,
                    invalidReference ? (UnityEngine.Object)null : cached.cachedReference,
                    genericReferenceType,
                    viewAsReference
                ) as SuperspectiveObject;

                if (cached.cachedReference != prevReference && cached.cachedReference != null) {
                    invalidReference = false;
                    referencedObjId.stringValue = cached.cachedReference.ID;
                }
            }
            else {
                EditorGUI.PropertyField(idRect, referencedObjId);
            }
        }

        protected virtual Type GetSaveableObjectType() {
            return typeof(SuperspectiveObject);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return viewAsReference ? 40 : 50;
        }
    }
    
    [CustomPropertyDrawer(typeof(SuperspectiveReference<,>))]
    public class SerializableReferencePropertyDrawerTyped : SerializableReferencePropertyDrawer {
        protected override Type GetSaveableObjectType() {
            // Gets the T in SaveableObject<T, S>
            return fieldInfo.FieldType.IsArray
                ? fieldInfo.FieldType.GetElementType().GetGenericArguments()[0]
                : fieldInfo.FieldType.GetGenericArguments()[0];
        }
    }
    
}