using Nova.InternalNamespace_0.InternalNamespace_4;
using Nova.InternalNamespace_0.InternalNamespace_2;
using Nova.InternalNamespace_0.InternalNamespace_12;
using Nova.InternalNamespace_0.InternalNamespace_5;
using Nova.InternalNamespace_0.InternalNamespace_5.InternalNamespace_6;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Nova.InternalNamespace_0.InternalNamespace_10
{
    internal struct InternalType_408
    {
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public InternalType_131 InternalField_1555;
        
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
                public int InternalField_1556;
    }

    [BurstCompile]
    internal struct InternalType_628 : InternalType_192
    {
        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeList<InternalType_131> InternalField_1145;
        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeList<InternalType_131> InternalField_1144;
        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeHashMap<InternalType_131, InternalType_164<InternalType_131>> InternalField_1143;
        [NativeDisableContainerSafetyRestriction]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeHashMap<InternalType_131, InternalType_302> InternalField_1142;
        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public InternalType_161<InternalType_643, InternalType_109> InternalField_1141;
        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeList<float4x4> InternalField_1140;
        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public InternalType_161<InternalType_643, InternalType_131> InternalField_1139;
        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeHashMap<InternalType_131, InternalType_133> InternalField_1138;
        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeList<InternalType_53.InternalType_55> InternalField_1137;
        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeList<InternalType_299<InternalType_71>> InternalField_1136;
        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public InternalType_161<InternalType_348, InternalType_80> InternalField_1135;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeHashMap<InternalType_131, InternalType_408> InternalField_858;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeList<InternalType_131> InternalField_836;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public InternalType_161<InternalType_643, InternalType_337> InternalField_771;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeHashMap<InternalType_643, byte> InternalField_763;

        public void Execute()
        {
            InternalMethod_2242();
            InternalMethod_2251();
        }

        private void InternalMethod_2251()
        {
            InternalField_763.Clear();

            for (int InternalVar_1 = 0; InternalVar_1 < InternalField_1144.Length; ++InternalVar_1)
            {
                InternalType_302 InternalVar_2 = InternalField_1142[InternalField_1144[InternalVar_1]];

                for (int InternalVar_3 = 0; InternalVar_3 < InternalVar_2.InternalField_991.InternalProperty_216; ++InternalVar_3)
                {
                    InternalType_276 InternalVar_4 = InternalVar_2.InternalField_991[InternalVar_3];
                    if (!InternalVar_4.InternalField_900.InternalProperty_761 || InternalField_763.ContainsKey(InternalVar_4.InternalField_900))
                    {
                        continue;
                    }

                    InternalField_763.Add(InternalVar_4.InternalField_900, 0);
                    InternalMethod_2250(InternalVar_4.InternalField_900);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InternalMethod_2250(InternalType_643 InternalParameter_2207)
        {
            InternalType_109 InternalVar_1 = InternalField_1141[InternalParameter_2207];
            InternalType_337 InternalVar_2 = default;

            InternalVar_2.InternalField_1156 = (Vector4)InternalVar_1.InternalField_348;

            InternalType_131 InternalVar_3 = InternalField_1139[InternalParameter_2207];
            InternalType_133 InternalVar_4 = InternalField_1138[InternalVar_3];
            InternalVar_2.InternalField_1157 = InternalField_1140[InternalVar_4];

            InternalType_448.InternalType_453 InternalVar_5 = InternalType_448.InternalMethod_1751(InternalVar_4, ref InternalField_1137);
            float3 InternalVar_6 = InternalVar_5.InternalProperty_398.InternalProperty_124;
            float3 InternalVar_7 = InternalType_187.InternalField_526 * InternalVar_6;

            float InternalVar_8 = 0f;
            var InternalVar_9 = InternalField_1136[InternalVar_4];
            if (InternalVar_9.InternalField_983.InternalField_234 == InternalType_72.InternalField_237)
            {
                float InternalVar_10 = math.cmin(InternalVar_7.xy);
                InternalType_80 InternalVar_11 = InternalField_1135[InternalVar_9.InternalField_984];
                switch (InternalVar_11.InternalField_267.InternalField_146)
                {
                    case InternalType_59.InternalField_201:
                        InternalVar_8 = math.clamp(InternalVar_11.InternalField_267.InternalField_145, 0, InternalVar_10);
                        break;
                    case InternalType_59.InternalField_202:
                        InternalVar_8 = InternalVar_11.InternalField_267.InternalField_145 * InternalVar_10;
                        break;
                }
            }

            float InternalVar_12 = math.cmax(InternalVar_7.xy);
            float InternalVar_13 = InternalVar_12 > InternalType_187.InternalField_494 ? 1.0f / InternalVar_12 : 0f;
            float2 InternalVar_14 = InternalVar_7.xy * InternalVar_13;
            float InternalVar_15 = InternalVar_8 * InternalVar_13;
            InternalVar_2.InternalField_1162 = new Vector4(InternalVar_14.x, InternalVar_14.y, InternalVar_13, InternalVar_15);
            InternalField_771[InternalParameter_2207] = InternalVar_2;
        }

        private void InternalMethod_2242()
        {
            InternalField_858.Clear();

            for (int InternalVar_1 = 0; InternalVar_1 < InternalField_1145.Length; InternalVar_1++)
            {
                InternalType_131 InternalVar_2 = InternalField_1145[InternalVar_1];
                InternalMethod_2224(ref InternalVar_2);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InternalMethod_2224(ref InternalType_131 InternalParameter_2206)
        {
            InternalField_836.Clear();
            InternalField_836.Add(InternalParameter_2206);

            int InternalVar_1 = 0;
            while (InternalField_836.InternalMethod_1012(out InternalType_131 InternalVar_2))
            {
                InternalField_858.Add(InternalVar_2, new InternalType_408()
                {
                    InternalField_1555 = InternalParameter_2206,
                    InternalField_1556 = InternalVar_1++,
                });

                InternalType_164<InternalType_131> InternalVar_3 = InternalField_1143[InternalVar_2];
                InternalField_836.InternalMethod_1014(ref InternalVar_3);
            }
        }
    }
}

