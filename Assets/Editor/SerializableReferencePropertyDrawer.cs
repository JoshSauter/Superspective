using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Saving;
using SerializableClasses;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Editor {
    [CustomPropertyDrawer(typeof(SerializableReference))]
    public class SerializableReferencePropertyDrawer : PropertyDrawer {
        public class CachedReference {
            public SaveableObject cachedReference;
            public string cachedId;
        }

        private static Dictionary<string, CachedReference> cachedReferences = new Dictionary<string, CachedReference>();
        private bool viewAsReference = false;

        public override void OnGUI(
            Rect position,
            SerializedProperty property,
            GUIContent label
        ) {
            EditorGUI.BeginProperty(position, label, property);

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            SerializedProperty referencedSceneName = property.FindPropertyRelative("referencedSceneName");
            SerializedProperty referencedObjId = property.FindPropertyRelative("referencedObjId");

            if (!cachedReferences.ContainsKey(referencedObjId.stringValue)) {
                cachedReferences.Add(referencedObjId.stringValue, new CachedReference());
            }

            CachedReference cached = cachedReferences[referencedObjId.stringValue];

            float height = (position.height) / (viewAsReference ? 2.0f : 3.0f);

            Rect sameSceneRect = new Rect(position.x, position.y, position.width, height);
            Rect referenceRect = new Rect(position.x, position.y + 20, position.width, height);
            Rect idRect = new Rect(position.x, position.y + 20, position.width, height);

            viewAsReference = EditorGUI.Toggle(sameSceneRect, "View as reference?", viewAsReference);

            if (viewAsReference) {
                Type genericReferenceType = GetSaveableObjectType();
                string cachedId = cached.cachedId;

                if (cachedId != referencedObjId.stringValue) {
                    cachedId = referencedObjId.stringValue;

                    // If the current ID & sceneName are present, try to update the cached reference accordingly
                    if (!string.IsNullOrEmpty(cachedId)) {
                        SaveableObject match = SaveManager.FindObjectById<SaveableObject>(cachedId);
                        if (genericReferenceType.IsInstanceOfType(match)) {
                            cached.cachedReference = match;
                        }
                    }
                }

                SaveableObject prevReference = cached.cachedReference;
                cached.cachedReference = EditorGUI.ObjectField(
                    referenceRect,
                    cached.cachedReference,
                    genericReferenceType,
                    viewAsReference
                ) as SaveableObject;

                if (cached.cachedReference != prevReference && cached.cachedReference != null) {
                    referencedObjId.stringValue = cached.cachedReference.ID;
                }
            }
            else {
                EditorGUI.PropertyField(idRect, referencedObjId);
            }
        }

        protected virtual Type GetSaveableObjectType() {
            return typeof(SaveableObject);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return viewAsReference ? 40 : 50;
        }
    }
    
    [CustomPropertyDrawer(typeof(SerializableReference<,>))]
    public class SerializableReferencePropertyDrawerTyped : SerializableReferencePropertyDrawer {
        protected override Type GetSaveableObjectType() {
            // Gets the T in SaveableObject<T, S>
            return fieldInfo.FieldType.IsArray
                ? fieldInfo.FieldType.GetElementType().GetGenericArguments()[0]
                : fieldInfo.FieldType.GetGenericArguments()[0];
        }
    }
    
}