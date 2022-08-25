using Nova.InternalNamespace_0.InternalNamespace_4;
using Nova.InternalNamespace_0.InternalNamespace_2;
using Nova.InternalNamespace_0.InternalNamespace_5.InternalNamespace_6;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Nova.InternalNamespace_0.InternalNamespace_10
{
    [BurstCompile]
    internal struct InternalType_396 : InternalType_193
    {
        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeList<InternalType_131> InternalField_1387;
        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeHashMap<InternalType_131, InternalType_162<InternalType_288, InternalType_373>> InternalField_1388;
        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeHashMap<InternalType_131, InternalType_162<InternalType_304, InternalType_356>> InternalField_1389;
        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public InternalType_161<InternalType_133, InternalType_304> InternalField_1390;
        [NativeDisableContainerSafetyRestriction]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeList<float4x4> InternalField_1391;
        [NativeDisableContainerSafetyRestriction]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeList<float4x4> InternalField_1392;
        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeHashMap<InternalType_131, InternalType_133> InternalField_1393;
        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public InternalType_161<InternalType_643, InternalType_109> InternalField_1394;
        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeHashMap<InternalType_643, InternalType_306> InternalField_1395;
        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public InternalType_161<InternalType_133, InternalType_643> InternalField_1396;
        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeHashMap<InternalType_348, InternalType_259> InternalField_1397;
        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public InternalType_161<InternalType_643, InternalType_131> InternalField_1147;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public InternalType_376 InternalField_1398;

        public unsafe void Execute(int index)
        {
            if (!InternalType_395.InternalMethod_1604(index, ref InternalField_1387, ref InternalField_1388, out InternalType_131 InternalVar_1, out int InternalVar_2))
            {
                return;
            }

            InternalType_288 InternalVar_3 = InternalVar_2;
            ref InternalType_373 InternalVar_4 = ref InternalField_1388[InternalVar_1].InternalMethod_758(InternalVar_3);

            if (InternalVar_4.InternalField_1297 == InternalType_266.InternalField_787)
            {
                return;
            }

            switch (InternalVar_4.InternalField_1297)
            {
                case InternalType_266.InternalField_789:
                {
                    ref InternalType_375 InternalVar_5 = ref InternalField_1398.InternalField_1304.InternalMethod_748(InternalField_1397[InternalVar_4.InternalField_1295]);
                    InternalMethod_1606(ref InternalVar_4, ref InternalVar_5.InternalField_1302, ref InternalVar_1);
                    if (!InternalMethod_1606(ref InternalVar_4, ref InternalVar_5.InternalField_1301, ref InternalVar_1))
                    {
                        InternalVar_4.InternalField_1298 = true;
                    }
                    break;
                }
                default:
                {
                    ref InternalType_374 InternalVar_5 = ref InternalField_1398.InternalField_1303.InternalMethod_748(InternalVar_4.InternalField_1294);
                    if (!InternalMethod_1606(ref InternalVar_4, ref InternalVar_5, ref InternalVar_1))
                    {
                        InternalVar_4.InternalField_1298 = true;
                    }
                    break;
                }
            }
        }

        
        private bool InternalMethod_1606(ref InternalType_373 InternalParameter_1741, ref InternalType_374 InternalParameter_1742, ref InternalType_131 InternalParameter_1743)
        {
            bool InternalVar_1 = true;
            InternalType_643 InternalVar_2 = InternalField_1396[InternalParameter_1741.InternalField_1294];
            if (InternalVar_2.InternalProperty_761 && InternalField_1394[InternalVar_2].InternalField_349)
            {
                InternalType_133 InternalVar_3 = InternalField_1393[InternalField_1147[InternalVar_2]];
                ref float4x4 InternalVar_4 = ref InternalField_1391.ElementAt(InternalVar_3);
                float4x4 InternalVar_5 = math.mul(InternalVar_4, InternalParameter_1742.InternalField_1299);
                InternalType_306 InternalVar_6 = new InternalType_306(ref InternalVar_5);
                InternalType_306 InternalVar_7 = InternalField_1395[InternalVar_2];

                if (!InternalVar_7.InternalMethod_1374(ref InternalVar_6))
                {
                    InternalVar_1 = false;
                }

                InternalVar_6.InternalMethod_1367(ref InternalVar_7);
                InternalVar_5 = InternalVar_6.InternalMethod_1368();
                InternalParameter_1742.InternalField_1299 = math.mul(InternalField_1392.ElementAt(InternalVar_3), InternalVar_5);
            }

            InternalType_304 InternalVar_8 = InternalField_1390[InternalParameter_1741.InternalField_1294];
            ref InternalType_356 InternalVar_9 = ref InternalField_1389[InternalParameter_1743].InternalMethod_758((int)InternalVar_8);
            float4x4 InternalVar_10 = math.mul(InternalVar_9.InternalField_1242, InternalParameter_1742.InternalField_1299);
            InternalParameter_1742.InternalField_1300 = new InternalType_306(ref InternalVar_10);
            return InternalVar_1;
        }
    }
}

