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
    [CustomPropertyDrawer(typeof(SerializableReference<,>))]
    public class SerializableReferencePropertyDrawer : PropertyDrawer {
        bool viewAsReference = true;
        SaveableObject cachedReference;
        string cachedSceneName;
        string cachedId;
        
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

            float height = (position.height) / (viewAsReference ? 2.0f : 3.0f);

            Rect sameSceneRect = new Rect(position.x, position.y, position.width, height);
            Rect referenceRect = new Rect(position.x, position.y + 20, position.width, height);
            Rect sceneNameRect = new Rect(position.x, position.y + 20, position.width, height);
            Rect idRect = new Rect(position.x, position.y + 40, position.width, height);
            
            viewAsReference = EditorGUI.Toggle(sameSceneRect, "View as reference?", viewAsReference);

            if (viewAsReference) {
                // Gets the T in SaveableObject<T, S>
                Type genericReferenceType = fieldInfo.FieldType.IsArray ?
                    fieldInfo.FieldType.GetElementType().GetGenericArguments()[0] :
                    fieldInfo.FieldType.GetGenericArguments()[0];
                
                if (cachedId != referencedObjId.stringValue || cachedSceneName != referencedSceneName.stringValue) {
                    cachedId = referencedObjId.stringValue;
                    cachedSceneName = referencedSceneName.stringValue;
                    
                    // If the current ID & sceneName are present, try to update the cached reference accordingly
                    if (!string.IsNullOrEmpty(cachedId) && !string.IsNullOrEmpty(cachedSceneName)) {
                        List<SaveableObject> matches = FindObjectById(cachedSceneName, cachedId);
                        if (matches.Count == 1 && matches[0].GetType() == genericReferenceType) {
                            cachedReference = matches[0];
                        }
                    }
                }
                
                SaveableObject prevReference = cachedReference;
                cachedReference = EditorGUI.ObjectField(referenceRect, cachedReference, genericReferenceType, viewAsReference) as SaveableObject;

                if (cachedReference != prevReference && cachedReference != null) {
                    referencedSceneName.stringValue = cachedReference.gameObject.scene.name;
                    referencedObjId.stringValue = cachedReference.ID;
                }
            }
            else {
                EditorGUI.PropertyField(sceneNameRect, referencedSceneName);
                EditorGUI.PropertyField(idRect, referencedObjId);
            }
            
            List<SaveableObject> FindObjectById(string sceneName, string id) {
                if (!EditorSceneManager.GetSceneByName(sceneName).isLoaded) {
                    return new List<SaveableObject>();
                }
                
                List<SaveableObject> matches = Resources.FindObjectsOfTypeAll<SaveableObject>()
                    .Where(s => HasValidId(s) && s.ID.Contains(id))
                    .Where(s => s.gameObject.scene.name == sceneName)
                    .ToList();

                if (matches.Count == 0) {
                    Debug.LogError($"No object with id {id} found in scene {sceneName}");
                    return null;
                }
                else if (matches.Count > 1) {
                    Debug.LogWarning($"Multiple objects with id {id} found in scene {sceneName}.");
                    return matches;
                }
                else
                    return matches;
            }

            bool HasValidId(ISaveableObject obj) {
                try {
                    string s = obj.ID;

                    return !string.IsNullOrEmpty(s);
                }
                catch {
                    return false;
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return viewAsReference ? 40 : 60;
        }
    }
    
}