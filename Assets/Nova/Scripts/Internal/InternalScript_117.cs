using Nova.InternalNamespace_0.InternalNamespace_4;
using Nova.InternalNamespace_0.InternalNamespace_2;
using Nova.InternalNamespace_0.InternalNamespace_12;
using Nova.InternalNamespace_0.InternalNamespace_10;
using Nova.InternalNamespace_0.InternalNamespace_5;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Nova.InternalNamespace_0.InternalNamespace_11
{
    internal interface InternalType_439
    {
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        InternalType_131 InternalProperty_344 { get; }
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        float3 InternalProperty_345 { get; }
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        float3 InternalProperty_346 { get; }
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        float InternalProperty_347 { get; }
    }

    internal struct InternalType_442 : IComparer<InternalType_442>, InternalType_439
    {
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public InternalType_131 InternalField_1706;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public float InternalField_1707;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public float3 InternalField_1708;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public float3 InternalField_1709;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public InternalType_498 InternalField_1686;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        InternalType_131 InternalType_439.InternalProperty_344 => InternalField_1706;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        float3 InternalType_439.InternalProperty_345 => InternalField_1708;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        float3 InternalType_439.InternalProperty_346 => InternalField_1709;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        float InternalType_439.InternalProperty_347 => InternalField_1707;

        public int Compare(InternalType_442 x, InternalType_442 y)
        {
            if (x.InternalField_1686.InternalProperty_684 != y.InternalField_1686.InternalProperty_684 || InternalType_187.InternalMethod_922(x.InternalField_1707, y.InternalField_1707))
            {
                return y.InternalField_1686.CompareTo(x.InternalField_1686);
            }
            else
            {
                return x.InternalField_1707.CompareTo(y.InternalField_1707);
            }
        }
    }

    [BurstCompile]
    internal unsafe struct InternalType_443 : InternalType_437<InternalType_434, InternalType_442>
    {
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public UnityEngine.Ray InternalField_1712;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public int InternalField_1880;

        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public InternalType_161<InternalType_133, InternalType_643> InternalField_1713;
        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public InternalType_161<InternalType_643, InternalType_131> InternalField_319;

        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public InternalType_270 InternalField_1714;

        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeList<InternalType_53.InternalType_55> InternalField_1715;
        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeHashMap<InternalType_131, InternalType_133> InternalField_1716;

        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeList<float3> InternalField_1717;
        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeList<float3> InternalField_1718;
        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeList<float4x4> InternalField_1719;
        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeList<float4x4> InternalField_1720;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public bool InternalField_1721;

        [NativeDisableUnsafePtrRestriction]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private float4x4* InternalField_1722;
        [NativeDisableUnsafePtrRestriction]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private float4x4* InternalField_1723;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private float3 InternalField_1724;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private InternalType_434 InternalField_1725;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public InternalType_434 InternalMethod_1710()
        {
            InternalField_1725 = new InternalType_434(InternalField_1712);
            InternalField_1722 = (float4x4*)InternalField_1719.GetUnsafeReadOnlyPtr();
            InternalField_1723 = (float4x4*)InternalField_1720.GetUnsafeReadOnlyPtr();
            InternalField_1724 = InternalType_187.InternalField_530;

            return InternalField_1725;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public InternalType_447 InternalMethod_1711(ref InternalType_434 ray, InternalType_133 InternalParameter_1937)
        {
            InternalType_447 InternalVar_1 = InternalType_447.InternalField_1788;

            InternalType_187.InternalMethod_895(InternalField_1717[InternalParameter_1937], out float3x3 InternalVar_2);
            InternalType_187.InternalMethod_895(InternalField_1718[InternalParameter_1937], out float3x3 InternalVar_3);

            int InternalVar_4 = InternalVar_2.c1.z == 0 ? 2 : 3;

            for (int InternalVar_5 = 0; InternalVar_5 < InternalVar_4; ++InternalVar_5)
            {
                bool2 InternalVar_6 = InternalType_187.InternalMethod_906(ref ray.InternalField_1678, ref ray.InternalField_1681, ref InternalVar_3, ref InternalVar_2, InternalParameter_831: InternalVar_5);
                InternalVar_1.InternalMethod_1732(InternalParameter_1967: InternalVar_5, InternalParameter_1968: false, InternalParameter_1969: InternalVar_6.x);
                InternalVar_1.InternalMethod_1732(InternalParameter_1967: InternalVar_5, InternalParameter_1968: true, InternalParameter_1969: InternalVar_6.y);
            }

            return InternalVar_1;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool InternalMethod_1713(ref InternalType_434 ray, InternalType_133 InternalParameter_1943, InternalType_131 InternalParameter_1944, out InternalType_442 InternalParameter_1945)
        {
            InternalParameter_1945 = default;

            int InternalVar_1 = InternalField_1714.InternalField_847[InternalParameter_1943].InternalField_983.InternalField_232;

            if (((1 << InternalVar_1) & InternalField_1880) == 0)
            {
                return false;
            }

            float3 InternalVar_2 = InternalType_448.InternalMethod_1751(InternalParameter_1943, ref InternalField_1715).InternalProperty_398.InternalProperty_124;

            bool InternalVar_4 = InternalType_187.InternalMethod_901(ref ray.InternalField_1677, ref ray.InternalField_1680, ref InternalField_1724, ref InternalVar_2, out float InternalVar_3);
            bool InternalVar_5 = InternalVar_4;

            float InternalVar_6 = InternalVar_3;
            float3 InternalVar_7 = InternalType_187.InternalField_535;
            float3 InternalVar_8 = InternalType_187.InternalField_535;

            if (InternalVar_4)
            {
                float4x4 InternalVar_9 = *(InternalField_1723 + InternalParameter_1943);
                float3 InternalVar_10 = ray.InternalMethod_1706(InternalVar_3);
                float3 InternalVar_11 = InternalType_441.InternalMethod_1726(ref InternalVar_2, ref InternalVar_10);
                InternalVar_7 = math.transform(InternalVar_9, InternalVar_10);
                InternalVar_8 = math.normalize(math.rotate(InternalVar_9, InternalVar_11));
                InternalVar_6 = math.length(InternalField_1725.InternalField_1676 - InternalVar_7);

                if (!InternalField_1721)
                {
                    InternalType_643 InternalVar_12 = InternalField_1713[InternalParameter_1943];

                    if (InternalVar_12.InternalProperty_761)
                    {
                        InternalType_133 InternalVar_13 = InternalField_1716[InternalField_319[InternalVar_12]];
                        float2 InternalVar_14 = math.transform(*(InternalField_1722 + InternalVar_13), InternalVar_7).xy;

                        float2 InternalVar_15 = InternalType_448.InternalMethod_1751(InternalVar_13, ref InternalField_1715).InternalProperty_398.InternalProperty_124.xy * InternalType_187.InternalField_521;

                        InternalVar_5 = math.all((InternalVar_14 >= -InternalVar_15) & (InternalVar_14 <= InternalVar_15));
                    }
                }
            }

            if (InternalVar_5)
            {
                InternalParameter_1945.InternalField_1706 = InternalParameter_1944;
                InternalParameter_1945.InternalField_1686 = InternalField_1714.InternalMethod_1939(InternalParameter_1943);
                InternalParameter_1945.InternalField_1708 = InternalVar_7;
                InternalParameter_1945.InternalField_1709 = InternalVar_8;
                InternalParameter_1945.InternalField_1707 = InternalVar_6;
            }

            return InternalVar_5;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool InternalMethod_1712(ref InternalType_434 ray, InternalType_133 InternalParameter_1939, InternalType_131 InternalParameter_1940, out InternalType_434 rayInLocalSpace)
        {
            float3 InternalVar_1 = InternalField_1717[InternalParameter_1939];
            float3 InternalVar_2 = InternalField_1718[InternalParameter_1939];

            float3 InternalVar_3 = math.transform(*(InternalField_1722 + InternalParameter_1939), ray.InternalField_1676);
            float3 InternalVar_4 = math.normalize(math.rotate(*(InternalField_1722 + InternalParameter_1939), ray.InternalField_1675));

            if (InternalType_187.InternalMethod_899(ref InternalVar_3, ref InternalVar_4, ref InternalVar_2, ref InternalVar_1))
            {
                rayInLocalSpace = new InternalType_434(InternalVar_3, InternalVar_4);
                return InternalType_187.InternalMethod_901(ref rayInLocalSpace.InternalField_1677, ref rayInLocalSpace.InternalField_1680, ref InternalVar_2, ref InternalVar_1, out _);
            }

            rayInLocalSpace = default;
            return false;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InternalMethod_1714(ref InternalType_434 ray, InternalType_133 InternalParameter_1947, out InternalType_434 rayInLocalSpace)
        {
            float3 InternalVar_1 = math.transform(*(InternalField_1722 + InternalParameter_1947), ray.InternalField_1676);
            float3 InternalVar_2 = math.normalize(math.rotate(*(InternalField_1722 + InternalParameter_1947), ray.InternalField_1675));
            rayInLocalSpace = new InternalType_434(InternalVar_1, InternalVar_2);
        }
    }
}
