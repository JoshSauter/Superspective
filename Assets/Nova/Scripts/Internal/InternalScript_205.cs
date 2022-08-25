using Nova.InternalNamespace_0.InternalNamespace_2;
using Nova.InternalNamespace_0.InternalNamespace_9;
using Nova.InternalNamespace_0.InternalNamespace_5.InternalNamespace_6;
using Unity.Burst;
using Unity.Collections;

namespace Nova.InternalNamespace_0.InternalNamespace_10
{
    [BurstCompile]
    internal struct InternalType_398 : InternalType_192
    {
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public bool InternalField_808;

        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeList<InternalType_131> InternalField_1421;
        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeList<InternalType_131> InternalField_1422;
        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeList<InternalType_133> InternalField_1423;
        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeList<InternalType_131> InternalField_1424;
        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeList<InternalType_224> InternalField_1425;
        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeHashMap<InternalType_131, InternalType_133> InternalField_1426;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeList<InternalType_131> InternalField_1427;

        public void Execute()
        {
            InternalMethod_1611();
        }

        private void InternalMethod_1611()
        {
            InternalField_1427.Clear();

            if (InternalField_808)
            {
                InternalMethod_1612(ref InternalField_1424);
            }
            else
            {
                InternalMethod_1612(ref InternalField_1421);
                InternalMethod_1612(ref InternalField_1422);

                for (int InternalVar_1 = 0; InternalVar_1 < InternalField_1423.Length; ++InternalVar_1)
                {
                    InternalType_224 InternalVar_2 = InternalField_1425[InternalField_1423[InternalVar_1]];
                    if (!InternalMethod_1613(InternalVar_2.InternalField_589))
                    {
                        continue;
                    }
                    InternalField_1427.Add(InternalVar_2.InternalField_589);
                }
            }
        }

        private void InternalMethod_1612(ref NativeList<InternalType_131> InternalParameter_1747)
        {
            for (int InternalVar_1 = 0; InternalVar_1 < InternalParameter_1747.Length; ++InternalVar_1)
            {
                if (!InternalMethod_1613(InternalParameter_1747[InternalVar_1]))
                {
                    continue;
                }
                InternalField_1427.Add(InternalParameter_1747[InternalVar_1]);
            }
        }

        private bool InternalMethod_1613(InternalType_131 InternalParameter_1748)
        {
            if (InternalField_1427.Contains(InternalParameter_1748) || !InternalField_1424.Contains(InternalParameter_1748))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}

