using Nova.InternalNamespace_0.InternalNamespace_4;
using Nova.InternalNamespace_0.InternalNamespace_2;
using Nova.InternalNamespace_0.InternalNamespace_5;
using Nova.InternalNamespace_0.InternalNamespace_5.InternalNamespace_6;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Nova.InternalNamespace_0.InternalNamespace_10
{
    [BurstCompile]
    [StructLayoutAttribute(LayoutKind.Sequential)]
    internal struct InternalType_399 : InternalType_192
    {
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public int InternalField_1428;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public int InternalField_1429;

        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeList<InternalType_131> InternalField_1430;
        [NativeDisableContainerSafetyRestriction]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeHashMap<InternalType_131, InternalType_302> InternalField_1431;
        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeHashMap<InternalType_324, InternalType_326> InternalField_1432;
        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeHashMap<InternalType_323, InternalType_327> InternalField_1433;
        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeHashMap<InternalType_131, InternalType_101> InternalField_1435;
        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public InternalType_161<InternalType_643, InternalType_109> InternalField_1434;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeHashMap<InternalType_131, InternalType_162<InternalType_301, InternalType_326>> InternalField_1436;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeList<InternalType_172<InternalType_327, InternalType_324>> InternalField_1437;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeList<InternalType_323> InternalField_1438;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public InternalType_326 InternalProperty_336
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => InternalField_1428 + InternalField_1437.Length;
        }

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public InternalType_327 InternalProperty_337
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => InternalField_1429 + InternalField_1438.Length;
        }

        public void Execute()
        {
            for (int InternalVar_1 = 0; InternalVar_1 < InternalField_1430.Length; InternalVar_1++)
            {
                InternalMethod_1616(InternalField_1430[InternalVar_1]);
            }
        }

        private void InternalMethod_1616(InternalType_131 InternalParameter_1749)
        {
            InternalType_302 InternalVar_1 = InternalField_1431[InternalParameter_1749];
            InternalType_162<InternalType_301, InternalType_326> InternalVar_2 = InternalField_1436.InternalMethod_1006(InternalParameter_1749);

            for (int InternalVar_3 = 0; InternalVar_3 < InternalVar_1.InternalField_991.InternalProperty_216; InternalVar_3++)
            {
                InternalVar_2.InternalMethod_761(InternalMethod_1617(InternalParameter_1749, ref InternalVar_1.InternalField_991.InternalMethod_758(InternalVar_3)));
            }

            InternalField_1436[InternalParameter_1749] = InternalVar_2;
        }

        private InternalType_326 InternalMethod_1617(InternalType_131 InternalParameter_1750, ref InternalType_276 InternalParameter_1751)
        {
            InternalType_324 InternalVar_1 = new InternalType_324()
            {
                InternalField_1100 = InternalParameter_1751.InternalField_903,
                InternalField_2244 = new InternalType_323()
                {
                    InternalField_1095 = InternalParameter_1751.InternalField_907.InternalField_909,
                    InternalField_1096 = InternalParameter_1751.InternalProperty_293,
                    InternalField_1094 = InternalParameter_1751.InternalField_902,
                },
            };

            bool InternalVar_3 = InternalField_1435.TryGetValue(InternalParameter_1750, out InternalType_101 InternalVar_2);

            if (InternalVar_3 && InternalVar_2.InternalField_491)
            {
                InternalVar_1.InternalField_583 = true;
            }

            if (InternalParameter_1751.InternalProperty_293 == InternalType_281.InternalField_921)
            {
                InternalVar_1.InternalField_1101 = InternalVar_3 ? InternalVar_2.InternalField_317 : InternalType_178.InternalField_472;
            }
            else
            {
                InternalVar_1.InternalField_1101 = InternalType_178.InternalField_473;
            }

            if (InternalParameter_1751.InternalProperty_294)
            {
                InternalVar_1.InternalField_1100 |= InternalField_1434[InternalParameter_1751.InternalField_900].InternalProperty_763;
            }

            if (InternalVar_1.InternalField_2244.InternalProperty_731)
            {
                InternalVar_1.InternalField_1102 = InternalParameter_1751.InternalField_906.InternalField_918;
            }
            else
            {
                InternalVar_1.InternalField_1102 = 0;
            }

            if (InternalField_1432.TryGetValue(InternalVar_1, out InternalType_326 InternalVar_4))
            {
                return InternalVar_4;
            }

            if (InternalMethod_1618(ref InternalVar_1, out int InternalVar_5))
            {
                return InternalField_1428 + InternalVar_5;
            }

            InternalVar_4 = InternalProperty_336;
            InternalField_1437.Add(new InternalType_172<InternalType_327, InternalType_324>(InternalMethod_1619(ref InternalVar_1), InternalVar_1));
            return InternalVar_4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool InternalMethod_1618(ref InternalType_324 InternalParameter_1752, out int InternalParameter_1753)
        {
            for (int InternalVar_1 = 0; InternalVar_1 < InternalField_1437.Length; InternalVar_1++)
            {
                if (!InternalField_1437[InternalVar_1].InternalField_463.Equals(InternalParameter_1752))
                {
                    continue;
                }

                InternalParameter_1753 = InternalVar_1;
                return true;
            }

            InternalParameter_1753 = -1;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private InternalType_327 InternalMethod_1619(ref InternalType_324 InternalParameter_1754)
        {
            if (InternalField_1433.TryGetValue(InternalParameter_1754.InternalField_2244, out InternalType_327 InternalVar_1))
            {
                return InternalVar_1;
            }

            if (InternalField_1438.InternalMethod_1011(InternalParameter_1754.InternalField_2244, out int InternalVar_2))
            {
                return InternalField_1429 + InternalVar_2;
            }

            InternalVar_1 = InternalProperty_337;
            InternalField_1438.Add(InternalParameter_1754.InternalField_2244);
            return InternalVar_1;
        }
    }
}

