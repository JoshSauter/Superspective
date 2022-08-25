using Nova.InternalNamespace_0.InternalNamespace_4;
using Nova.InternalNamespace_0.InternalNamespace_3;
using Nova.InternalNamespace_0.InternalNamespace_2;
using Nova.InternalNamespace_0.InternalNamespace_5;
using Nova.InternalNamespace_0.InternalNamespace_5.InternalNamespace_6;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Nova.InternalNamespace_0.InternalNamespace_10
{
    internal struct InternalType_337
    {
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public Matrix4x4 InternalField_1157;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public Vector4 InternalField_1162;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public Color InternalField_1156;
    }

    internal class InternalType_338 : InternalType_147
    {
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeHashMap<InternalType_131, InternalType_643> InternalField_1155;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public InternalType_161<InternalType_643, InternalType_109> InternalField_1164;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public InternalType_161<InternalType_643, InternalType_131> InternalField_1154;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public InternalType_161<InternalType_643, InternalType_337> InternalField_1166;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeHashMap<InternalType_643, InternalType_306> InternalField_1165;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public List<Texture> InternalField_1153 = new List<Texture>();

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private InternalType_274 InternalProperty_319 => InternalType_274.InternalProperty_190;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private Stack<InternalType_643> InternalField_1152 = new Stack<InternalType_643>();

        public void InternalMethod_1498(InternalType_131 InternalParameter_1604, InternalType_109 InternalParameter_1605, Texture InternalParameter_1606)
        {
            InternalParameter_1606 = InternalParameter_1605.InternalField_349 ? InternalParameter_1606 : null;

            if (InternalField_1155.TryGetValue(InternalParameter_1604, out InternalType_643 InternalVar_1))
            {
                InternalType_109 InternalVar_2 = InternalField_1164[InternalVar_1];

                if (InternalParameter_1605.Equals(InternalVar_2) && InternalParameter_1606 == InternalField_1153[InternalVar_1])
                {
                    return;
                }

                InternalField_1164[InternalVar_1] = InternalParameter_1605;
                InternalField_1153[InternalVar_1] = InternalParameter_1606;
                InternalProperty_319.InternalField_862.InternalField_1284.InternalMethod_837(InternalParameter_1604);
            }
            else
            {
                if (InternalField_1152.Count > 0)
                {
                    InternalVar_1 = InternalField_1152.Pop();
                    InternalField_1164[InternalVar_1] = InternalParameter_1605;
                    InternalField_1154[InternalVar_1] = InternalParameter_1604;
                    InternalField_1153[InternalVar_1] = InternalParameter_1606;
                }
                else
                {
                    InternalVar_1 = InternalField_1164.InternalProperty_216;
                    InternalField_1164.InternalMethod_751(InternalParameter_1605);
                    InternalField_1166.InternalMethod_751(default);
                    InternalField_1154.InternalMethod_751(InternalParameter_1604);
                    InternalField_1153.Add(InternalParameter_1606);
                }

                InternalField_1155.Add(InternalParameter_1604, InternalVar_1);
                InternalProperty_319.InternalField_862.InternalField_1284.InternalMethod_837(InternalParameter_1604);
            }

            if (InternalParameter_1605.InternalField_349)
            {
                InternalField_1165[InternalVar_1] = default;
            }
            else
            {
                InternalField_1165.Remove(InternalVar_1);
            }
        }

        public void InternalMethod_1499(InternalType_131 InternalParameter_1607)
        {
            if (!InternalField_1155.TryGetValue(InternalParameter_1607, out InternalType_643 InternalVar_1))
            {
                return;
            }

            InternalField_1152.Push(InternalVar_1);
            InternalField_1153[InternalVar_1] = null;
            InternalField_1155.Remove(InternalParameter_1607);
            InternalField_1165.Remove(InternalVar_1);
            InternalProperty_319.InternalField_862.InternalField_1284.InternalMethod_837(InternalParameter_1607);
        }

        public void Dispose()
        {
            InternalField_1155.Dispose();
            InternalField_1164.Dispose();
            InternalField_1165.Dispose();
            InternalField_1166.Dispose();
            InternalField_1154.Dispose();
        }

        public void InternalMethod_702()
        {
            InternalField_1155.InternalMethod_1009(InternalType_178.InternalField_3011);
            InternalField_1164.InternalMethod_703(InternalType_178.InternalField_3011);
            InternalField_1165.InternalMethod_1009(InternalType_178.InternalField_3011);
            InternalField_1166.InternalMethod_703(InternalType_178.InternalField_3011);
            InternalField_1154.InternalMethod_703(InternalType_178.InternalField_3011);
        }
    }
}

