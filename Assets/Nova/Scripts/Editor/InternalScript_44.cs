using Nova.InternalNamespace_17.InternalNamespace_20;
using System;
using UnityEditor;
using static Nova.InternalNamespace_17.InternalNamespace_20.InternalType_592;

namespace Nova.InternalNamespace_17.InternalNamespace_18
{
    [CustomEditor(typeof(SortGroup))]
    [CanEditMultipleObjects]
    internal class InternalType_546 : InternalType_539<SortGroup>
    {
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private InternalType_633 InternalField_2424 = new InternalType_633();

        protected override void OnEnable()
        {
            base.OnEnable();
            InternalField_2424.InternalProperty_954 = serializedObject.FindProperty(InternalType_646.InternalType_673.InternalField_3040);

            Undo.undoRedoPerformed += InternalMethod_2182;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= InternalMethod_2182;
        }

        private void InternalMethod_2182()
        {
            InternalMethod_2183();
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            InternalType_573.InternalMethod_2238(InternalType_554.InternalType_565.InternalField_2512, InternalField_2424.InternalProperty_783, Int16.MinValue, Int16.MaxValue);
            InternalType_573.InternalMethod_2238(InternalType_554.InternalType_565.InternalField_2513, InternalField_2424.InternalProperty_785, 0, 5000);
            InternalType_573.InternalMethod_2235(InternalType_554.InternalType_565.InternalField_207, InternalField_2424.InternalProperty_251);
            if (EditorGUI.EndChangeCheck())
            {
                InternalMethod_2183();
            }
        }

        private void InternalMethod_2183()
        {
            serializedObject.ApplyModifiedProperties();
            for (int InternalVar_1 = 0; InternalVar_1 < InternalField_2385.Count; ++InternalVar_1)
            {
                InternalField_2385[InternalVar_1].InternalMethod_301();
            }
        }
    }
}

