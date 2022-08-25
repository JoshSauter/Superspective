using Nova.Compat;
using Nova.InternalNamespace_0.InternalNamespace_12;
using Nova.InternalNamespace_0.InternalNamespace_5;
using Nova.InternalNamespace_0.InternalNamespace_5.InternalNamespace_6;
using UnityEditor;
using UnityEngine;
using static Nova.InternalNamespace_17.InternalNamespace_20.InternalType_592;

namespace Nova.InternalNamespace_17.InternalNamespace_22
{
    internal static class InternalType_65
    {
        
        public static void InternalMethod_3318(UIBlock InternalParameter_3121)
        {
            Vector3 InternalVar_1 = InternalParameter_3121.transform.localPosition;
            Vector3 InternalVar_2 = InternalParameter_3121.InternalMethod_2655();
            bool InternalVar_3 = InternalVar_1 != InternalVar_2;

            if (InternalVar_3)
            {
                Vector3 InternalVar_4 = InternalMethod_3320(InternalParameter_3121);

                InternalType_5 InternalVar_5 = InternalParameter_3121.InternalMethod_1034();
                Vector3 InternalVar_6 = InternalVar_5 != null ? (Vector3) InternalVar_5.InternalProperty_146.InternalProperty_139 : Vector3.zero;

                Vector3 InternalVar_7 = InternalType_182.InternalMethod_852(InternalVar_1, InternalParameter_3121.LayoutSize, InternalParameter_3121.CalculatedMargin.Offset, InternalVar_4, InternalVar_6, (Vector3)InternalParameter_3121.Alignment);
                InternalParameter_3121.Position.Raw = Length3.InternalMethod_2424(InternalVar_7, InternalParameter_3121.Position, InternalParameter_3121.PositionMinMax, InternalVar_4);
            }

            if (InternalVar_3 || InternalType_457.InternalProperty_190.InternalProperty_408)
            {


                SerializedObject InternalVar_4 = new SerializedObject(InternalParameter_3121);
                InternalType_600 InternalVar_5 = new InternalType_600() { InternalProperty_954 = InternalVar_4.FindProperty("layout") };
                InternalType_595 InternalVar_6 = InternalVar_5.InternalProperty_552;

                Vector3 InternalVar_7 = InternalParameter_3121.Position.Raw;
                InternalVar_6.InternalProperty_510.InternalProperty_506 = InternalVar_7.x;
                InternalVar_6.InternalProperty_512.InternalProperty_506 = InternalVar_7.y;
                InternalVar_6.InternalProperty_514.InternalProperty_506 = InternalVar_7.z;

                InternalVar_4.ApplyModifiedProperties();
                EditorUtility.SetDirty(InternalParameter_3121);
            }
        }

        public static bool InternalMethod_3319(Transform InternalParameter_3122) => InternalType_457.InternalProperty_190.InternalProperty_206.InternalMethod_1960(InternalParameter_3122);

        
        public static Vector3 InternalMethod_3320(UIBlock InternalParameter_3123)
        {
            InternalType_5 InternalVar_1 = InternalParameter_3123.InternalMethod_1034();
            bool InternalVar_2 = InternalVar_1 != null;

            if (!InternalVar_2)
            {
                return Vector3.zero;
            }

            return InternalVar_1.InternalProperty_148;
        }
    }
}
