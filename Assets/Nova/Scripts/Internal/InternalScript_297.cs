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
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public int InternalField_3382;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public int InternalField_3383;

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
        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeHashMap<InternalType_131, InternalType_101> InternalField_3384;
        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeHashMap<InternalType_324, InternalType_326> InternalField_3385;
        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeHashMap<InternalType_323, InternalType_327> InternalField_3386;
        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeHashMap<InternalType_131, int> InternalField_3387;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeHashMap<InternalType_131, InternalType_408> InternalField_858;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeList<InternalType_131> InternalField_836;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public InternalType_161<InternalType_643, InternalType_337> InternalField_771;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeHashMap<InternalType_643, byte> InternalField_763;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeHashMap<InternalType_131, InternalType_162<InternalType_301, InternalType_326>> InternalField_3388;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeList<InternalType_172<InternalType_327, InternalType_324>> InternalField_3389;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeList<InternalType_323> InternalField_3390;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeHashMap<InternalType_131, InternalType_101> InternalField_3391;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private InternalType_326 InternalProperty_1054
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => InternalField_3382 + InternalField_3389.Length;
        }

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private InternalType_327 InternalProperty_1055
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => InternalField_3383 + InternalField_3390.Length;
        }

        public void Execute()
        {
            InternalMethod_2242();
            InternalMethod_2251();
            InternalMethod_3370();
        }

        private void InternalMethod_3370()
        {
            for (int InternalVar_1 = 0; InternalVar_1 < InternalField_1144.Length; InternalVar_1++)
            {
                InternalMethod_3371(InternalField_1144[InternalVar_1]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InternalMethod_3371(InternalType_131 InternalParameter_225)
        {
            InternalType_302 InternalVar_1 = InternalField_1142[InternalParameter_225];
            InternalType_162<InternalType_301, InternalType_326> InternalVar_2 = InternalField_3388.InternalMethod_1006(InternalParameter_225);

            for (int InternalVar_3 = 0; InternalVar_3 < InternalVar_1.InternalField_991.InternalProperty_216; InternalVar_3++)
            {
                InternalVar_2.InternalMethod_761(InternalMethod_3372(InternalParameter_225, ref InternalVar_1.InternalField_991.InternalMethod_758(InternalVar_3)));
            }

            InternalField_3388[InternalParameter_225] = InternalVar_2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private InternalType_326 InternalMethod_3372(InternalType_131 InternalParameter_224, ref InternalType_276 InternalParameter_2114)
        {
            InternalType_324 InternalVar_1 = new InternalType_324()
            {
                InternalField_1100 = InternalParameter_2114.InternalField_903,
                InternalField_2244 = new InternalType_323()
                {
                    InternalField_1095 = InternalParameter_2114.InternalField_907.InternalField_909,
                    InternalField_1096 = InternalParameter_2114.InternalProperty_293,
                    InternalField_1094 = InternalParameter_2114.InternalField_902,
                },
            };

            bool InternalVar_3 = InternalField_3391.TryGetValue(InternalParameter_224, out InternalType_101 InternalVar_2);

            if (InternalVar_3 && InternalVar_2.InternalField_491)
            {
                InternalVar_1.InternalField_583 = true;
            }

            if (InternalParameter_2114.InternalProperty_293 == InternalType_281.InternalField_921)
            {
                InternalVar_1.InternalField_1101 = InternalVar_3 ? InternalVar_2.InternalField_317 : InternalType_178.InternalField_472;
            }
            else
            {
                InternalVar_1.InternalField_1101 = InternalType_178.InternalField_473;
            }

            if (InternalParameter_2114.InternalProperty_294)
            {
                InternalVar_1.InternalField_1100 |= InternalField_1141[InternalParameter_2114.InternalField_900].InternalProperty_763;
            }

            if (InternalVar_1.InternalField_2244.InternalProperty_731)
            {
                InternalVar_1.InternalField_1102 = InternalParameter_2114.InternalField_906.InternalField_918;
            }
            else
            {
                InternalVar_1.InternalField_1102 = 0;
            }

            if (InternalField_3385.TryGetValue(InternalVar_1, out InternalType_326 InternalVar_4))
            {
                return InternalVar_4;
            }

            if (InternalMethod_3373(ref InternalVar_1, out int InternalVar_5))
            {
                return InternalField_3382 + InternalVar_5;
            }

            InternalVar_4 = InternalProperty_1054;
            InternalField_3389.Add(new InternalType_172<InternalType_327, InternalType_324>(InternalMethod_3374(ref InternalVar_1), InternalVar_1));
            return InternalVar_4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool InternalMethod_3373(ref InternalType_324 InternalParameter_2115, out int InternalParameter_2230)
        {
            for (int InternalVar_1 = 0; InternalVar_1 < InternalField_3389.Length; InternalVar_1++)
            {
                if (!InternalField_3389[InternalVar_1].InternalField_463.Equals(InternalParameter_2115))
                {
                    continue;
                }

                InternalParameter_2230 = InternalVar_1;
                return true;
            }

            InternalParameter_2230 = -1;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private InternalType_327 InternalMethod_3374(ref InternalType_324 InternalParameter_2162)
        {
            if (InternalField_3386.TryGetValue(InternalParameter_2162.InternalField_2244, out InternalType_327 InternalVar_1))
            {
                return InternalVar_1;
            }

            if (InternalField_3390.InternalMethod_1011(InternalParameter_2162.InternalField_2244, out int InternalVar_2))
            {
                return InternalField_3383 + InternalVar_2;
            }

            InternalVar_1 = InternalProperty_1055;
            InternalField_3390.Add(InternalParameter_2162.InternalField_2244);
            return InternalVar_1;
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
            InternalField_3391.Clear();

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

                if (!InternalField_3384.TryGetValue(InternalVar_2, out InternalType_101 InternalVar_4))
                {
                    continue;
                }

                if (InternalField_3387.ContainsKey(InternalParameter_2206) &&
                    InternalField_3384.TryGetValue(InternalParameter_2206, out InternalType_101 InternalVar_5))
                {
                    InternalVar_4.InternalField_317 = InternalVar_5.InternalField_317;
                    InternalVar_4.InternalField_491 = InternalVar_5.InternalField_491;
                }

                InternalField_3391[InternalVar_2] = InternalVar_4;
            }
        }
    }
}

