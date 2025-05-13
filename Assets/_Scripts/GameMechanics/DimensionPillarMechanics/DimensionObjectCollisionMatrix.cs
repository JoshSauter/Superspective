
using System;
#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
#endif

namespace DimensionObjectMechanics {
    [Serializable]
    public class DimensionObjectCollisionMatrix {
        private const int COLLISION_MATRIX_ROWS = 4;
        // First 4 columns are VisibilityStates, 5 is Player and 6 is Other Non-DimensionObjects
        private const int COLLISION_MATRIX_COLS = 6;

        public readonly bool[] collisionMatrix = new bool[COLLISION_MATRIX_ROWS*COLLISION_MATRIX_COLS] {
            true, false, false, false, false, false,
            false,  true, false, false, false, false,
            false, false,  true, false,  true,  true,
            false, false, false,  true,  true,  true
        };
        
#region Get Collisions
        public bool ShouldCollideWithNonDimensionObjects(VisibilityState thisVisibilityState) {
            return collisionMatrix[(int)thisVisibilityState * COLLISION_MATRIX_COLS + COLLISION_MATRIX_COLS - 1];
        }

        public bool ShouldCollideWithPlayer(VisibilityState thisVisibilityState) {
            return collisionMatrix[(int)thisVisibilityState * COLLISION_MATRIX_COLS + COLLISION_MATRIX_COLS - 2];
        }

        public bool ShouldCollide(VisibilityState thisVisibilityState, VisibilityState otherVisibilityState) {
            return collisionMatrix[(int)thisVisibilityState * COLLISION_MATRIX_COLS + (int)otherVisibilityState];
        }
        #endregion
        
#region Set Collisions

        public void SetCollision(VisibilityState thisVisibilityState, VisibilityState otherVisibilityState, bool shouldCollide) {
            collisionMatrix[(int)thisVisibilityState * COLLISION_MATRIX_COLS + (int)otherVisibilityState] = shouldCollide;
            collisionMatrix[(int)otherVisibilityState * COLLISION_MATRIX_COLS + (int)thisVisibilityState] = shouldCollide;
        }
        
        public void SetCollisionWithNonDimensionObjects(VisibilityState thisVisibilityState, bool shouldCollide) {
            collisionMatrix[(int)thisVisibilityState * COLLISION_MATRIX_COLS + COLLISION_MATRIX_COLS - 1] = shouldCollide;
        }
        
        public void SetCollisionWithPlayer(VisibilityState thisVisibilityState, bool shouldCollide) {
            collisionMatrix[(int)thisVisibilityState * COLLISION_MATRIX_COLS + COLLISION_MATRIX_COLS - 2] = shouldCollide;
        }
#endregion
    }
    
#if UNITY_EDITOR
    public class DimensionObjectCollisionMatrixDrawer : OdinValueDrawer<DimensionObjectCollisionMatrix> {
        static bool foldout = true;
        static bool help = false;

        static readonly System.Collections.Generic.Dictionary<int, string> visibilityStateLabels = new() {
            { 0, "I" }, { 1, "PV" }, { 2, "V" }, { 3, "PI" }
        };

        protected override void DrawPropertyLayout(GUIContent label) {
            foldout = SirenixEditorGUI.Foldout(foldout, label);
            if (!foldout) return;

            help = GUILayout.Toggle(help, "?", "Button", GUILayout.Width(20));
            if (help) {
                EditorGUILayout.HelpBox("Click toggles to set collisions.\nSymmetric for VisibilityStates.\nOne-way for Player and Other.", MessageType.Info);
            }

            EditorGUI.indentLevel++;

            const int cols = 6;
            const int rows = 4;
            float cellSize = 20f;

            // Header row
            GUILayout.BeginHorizontal();
            GUILayout.Label(" ", GUILayout.Width(80));
            for (int j = 0; j < rows; j++) {
                GUILayout.Label(visibilityStateLabels[j], GUILayout.Width(cellSize));
            }
            GUILayout.Label("P", GUILayout.Width(cellSize));
            GUILayout.Label("O", GUILayout.Width(cellSize));
            GUILayout.EndHorizontal();

            // Matrix grid with toggle boxes
            for (int i = 0; i < rows; i++) {
                GUILayout.BeginHorizontal();
                GUILayout.Label(visibilityStateLabels[i], GUILayout.Width(80));

                for (int j = 0; j < rows; j++) {
                    bool current = ValueEntry.SmartValue.ShouldCollide((VisibilityState)i, (VisibilityState)j);
                    bool newValue = GUILayout.Toggle(current, GUIContent.none, GUILayout.Width(cellSize));
                    if (newValue != current) {
                        ValueEntry.SmartValue.SetCollision((VisibilityState)i, (VisibilityState)j, newValue);
                    }
                }

                bool currentPlayer = ValueEntry.SmartValue.ShouldCollideWithPlayer((VisibilityState)i);
                bool newPlayer = GUILayout.Toggle(currentPlayer, GUIContent.none, GUILayout.Width(cellSize));
                if (newPlayer != currentPlayer) {
                    ValueEntry.SmartValue.SetCollisionWithPlayer((VisibilityState)i, newPlayer);
                }

                bool currentOther = ValueEntry.SmartValue.ShouldCollideWithNonDimensionObjects((VisibilityState)i);
                bool newOther = GUILayout.Toggle(currentOther, GUIContent.none, GUILayout.Width(cellSize));
                if (newOther != currentOther) {
                    ValueEntry.SmartValue.SetCollisionWithNonDimensionObjects((VisibilityState)i, newOther);
                }

                GUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel--;
        }
    }
#endif


}
