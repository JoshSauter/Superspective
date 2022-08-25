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
    internal struct InternalType_438 : IComparer<InternalType_438>, InternalType_439
    {
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public InternalType_131 InternalField_1684;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public float InternalField_1685;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public InternalType_498 InternalField_1687;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public float3 InternalField_1688;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public float3 InternalField_1689;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        InternalType_131 InternalType_439.InternalProperty_344 => InternalField_1684;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        float3 InternalType_439.InternalProperty_345 => InternalField_1688;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        float3 InternalType_439.InternalProperty_346 => InternalField_1689;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        float InternalType_439.InternalProperty_347 => InternalField_1685;

        public int Compare(InternalType_438 x, InternalType_438 y)
        {
            if (x.InternalField_1687.InternalProperty_684 != y.InternalField_1687.InternalProperty_684 || InternalType_187.InternalMethod_922(x.InternalField_1685, y.InternalField_1685))
            {
                return y.InternalField_1687.CompareTo(x.InternalField_1687);
            }
            else
            {
                return y.InternalField_1685.CompareTo(x.InternalField_1685);
            }
        }
    }

    [BurstCompile]
    internal unsafe struct InternalType_440 : InternalType_437<InternalType_434, InternalType_438>
    {
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public UnityEngine.Ray InternalField_1690;

        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public InternalType_161<InternalType_133, InternalType_643> InternalField_1691;
        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public InternalType_161<InternalType_643, InternalType_131> InternalField_320;

        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public InternalType_270 InternalField_1692;

        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeList<InternalType_53.InternalType_55> InternalField_1693;
        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeHashMap<InternalType_131, InternalType_133> InternalField_1694;

        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeList<float3> InternalField_1695;
        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeList<float3> InternalField_1696;
        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeList<float4x4> InternalField_1697;
        [ReadOnly]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public NativeList<float4x4> InternalField_1698;

        [NativeDisableUnsafePtrRestriction]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private float4x4* InternalField_1699;
        [NativeDisableUnsafePtrRestriction]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private float4x4* InternalField_1700;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public InternalType_131 InternalField_1701;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private float3 InternalField_1702;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private InternalType_434 InternalField_1703;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public InternalType_434 InternalMethod_1710()
        {
            InternalField_1703 = new InternalType_434(InternalField_1690);
            InternalField_1699 = (float4x4*)InternalField_1697.GetUnsafeReadOnlyPtr();
            InternalField_1700 = (float4x4*)InternalField_1698.GetUnsafeReadOnlyPtr();
            InternalField_1702 = InternalType_187.InternalField_530;

            return InternalField_1703;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public InternalType_447 InternalMethod_1711(ref InternalType_434 ray, InternalType_133 InternalParameter_1937)
        {
            InternalType_447 InternalVar_1 = InternalType_447.InternalField_1787;

            float3 InternalVar_2 = InternalField_1695[InternalParameter_1937] * InternalType_187.InternalField_527;
            float3 InternalVar_3 = InternalField_1696[InternalParameter_1937];

            int InternalVar_4 = InternalVar_2.z == 0 ? 2 : 1;

            InternalType_441 InternalVar_5 = new InternalType_441() { InternalField_1704 = InternalVar_2 };

            for (int InternalVar_6 = 0; InternalVar_6 < InternalType_447.InternalField_1776; InternalVar_6 += InternalVar_4)
            {
                float3 InternalVar_7 = InternalVar_3 + InternalVar_2 * InternalType_447.InternalField_1789[InternalVar_6];

                InternalVar_5.InternalField_1705 = InternalVar_7;

                InternalVar_1[InternalVar_6] = InternalVar_5.InternalMethod_1721(ray.InternalField_1676) || math.dot(InternalVar_7 - ray.InternalField_1676, ray.InternalField_1675) > 0;
            }

            return InternalVar_1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool InternalMethod_1713(ref InternalType_434 ray, InternalType_133 InternalParameter_1943, InternalType_131 InternalParameter_1944, out InternalType_438 InternalParameter_1945)
        {
            InternalParameter_1945 = default;

            if (InternalParameter_1944 == InternalField_1701)
            {
                return false;
            }

            InternalType_448.InternalType_453 InternalVar_1 = InternalType_448.InternalMethod_1751(InternalParameter_1943, ref InternalField_1693);
            float3 InternalVar_2 = InternalVar_1.InternalProperty_398.InternalProperty_124;

            InternalType_441 InternalVar_3 = new InternalType_441(InternalType_187.InternalField_530, InternalVar_2);

            if (InternalVar_3.InternalMethod_1721(ray.InternalField_1676))
            {
                return false;
            }

            float3 InternalVar_4 = InternalVar_3.InternalMethod_1722(ray.InternalField_1676);
            float3 InternalVar_5 = InternalVar_4 - ray.InternalField_1676;

            float InternalVar_6 = math.dot(ray.InternalField_1675, InternalVar_5);

            if (InternalVar_6 <= 0)
            {
                return false;
            }

            float3 InternalVar_7 = math.normalize(InternalVar_5);
            float InternalVar_8 = math.lengthsq(InternalVar_5);

            float4x4 InternalVar_9 = *(InternalField_1700 + InternalParameter_1943);
            float3 InternalVar_10 = math.transform(InternalVar_9, ray.InternalField_1676 + InternalVar_7 * math.sqrt(InternalVar_8));
            float3 InternalVar_11 = InternalType_441.InternalMethod_1725(ref InternalVar_3, ref InternalVar_4);
            float3 InternalVar_12 = math.normalize(math.rotate(InternalVar_9, InternalVar_11));

            InternalType_643 InternalVar_13 = InternalField_1691[InternalParameter_1943];

            if (InternalVar_13.InternalProperty_761)
            {
                InternalType_133 InternalVar_14 = InternalField_1694[InternalField_320[InternalVar_13]];
                float2 InternalVar_15 = math.transform(*(InternalField_1699 + InternalVar_14), InternalVar_10).xy;

                float2 InternalVar_16 = InternalType_448.InternalMethod_1751(InternalVar_14, ref InternalField_1693).InternalProperty_398.InternalProperty_124.xy * InternalType_187.InternalField_521;

                if (!math.all((InternalVar_15 >= -InternalVar_16) & (InternalVar_15 <= InternalVar_16)))
                {
                    return false;
                }
            }

            InternalParameter_1945.InternalField_1684 = InternalParameter_1944;
            InternalParameter_1945.InternalField_1687 = InternalField_1692.InternalMethod_1939(InternalParameter_1943);
            InternalParameter_1945.InternalField_1685 = InternalVar_6 / InternalVar_8;
            InternalParameter_1945.InternalField_1689 = InternalVar_12;
            InternalParameter_1945.InternalField_1688 = InternalVar_10;

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool InternalMethod_1712(ref InternalType_434 ray, InternalType_133 InternalParameter_1939, InternalType_131 InternalParameter_1940, out InternalType_434 rayInLocalSpace)
        {
            rayInLocalSpace = default;

            float3 InternalVar_1 = math.transform(*(InternalField_1699 + InternalParameter_1939), ray.InternalField_1676);
            float3 InternalVar_2 = math.normalize(math.rotate(*(InternalField_1699 + InternalParameter_1939), ray.InternalField_1675));

            InternalType_441 InternalVar_3 = new InternalType_441(InternalField_1696[InternalParameter_1939], InternalField_1695[InternalParameter_1939]);

            float InternalVar_4 = math.dot(InternalVar_2, InternalVar_3.InternalField_1705 - InternalVar_1);
            bool InternalVar_5 = InternalVar_4 > 0 || InternalVar_3.InternalMethod_1721(InternalVar_1);

            if (InternalVar_5)
            {
                rayInLocalSpace = new InternalType_434() { InternalField_1676 = InternalVar_1, InternalField_1675 = InternalVar_2 };
            }

            return InternalVar_5;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InternalMethod_1714(ref InternalType_434 ray, InternalType_133 InternalParameter_1947, out InternalType_434 rayInLocalSpace)
        {
            float3 InternalVar_1 = math.transform(*(InternalField_1699 + InternalParameter_1947), ray.InternalField_1676);
            float3 InternalVar_2 = math.normalize(math.rotate(*(InternalField_1699 + InternalParameter_1947), ray.InternalField_1675));

            rayInLocalSpace = new InternalType_434() { InternalField_1676 = InternalVar_1, InternalField_1675 = InternalVar_2 };
        }
    }

    internal struct InternalType_441
    {
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public float3 InternalField_1704;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public float3 InternalField_1705;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public float3 InternalProperty_348 => InternalField_1705 - InternalField_1704;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public float3 InternalProperty_349 => InternalField_1705 + InternalField_1704;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool InternalMethod_1721(float3 InternalParameter_1949, float InternalParameter_1950 = 0)
        {
            float3 InternalVar_1 = InternalParameter_1950;
            return math.all(InternalParameter_1949 + InternalVar_1 >= InternalProperty_348 & InternalParameter_1949 - InternalVar_1 <= InternalProperty_349);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 InternalMethod_1722(float3 InternalParameter_1951)
        {
            return InternalType_187.InternalMethod_886(InternalParameter_1951, InternalProperty_348, InternalProperty_349);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool InternalMethod_1723(InternalType_441 InternalParameter_1952)
        {
            return math.all(InternalProperty_349 >= InternalParameter_1952.InternalProperty_348 & InternalProperty_348 <= InternalParameter_1952.InternalProperty_349);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public InternalType_441(float3 InternalParameter_1953, float3 InternalParameter_1954)
        {
            InternalField_1705 = InternalParameter_1953;
            InternalField_1704 = InternalType_187.InternalField_526 * InternalParameter_1954;
        }

        public static float3 InternalMethod_1725(ref InternalType_441 InternalParameter_1955, ref float3 InternalParameter_1956)
        {
            if (InternalParameter_1955.InternalField_1704.z == 0)
            {
                return InternalType_187.InternalField_506;
            }

            float3 InternalVar_1 = InternalParameter_1956 - InternalParameter_1955.InternalField_1705;
            float3 InternalVar_2 = InternalType_187.InternalField_532 * InternalParameter_1955.InternalField_1704;
            return InternalMethod_1726(ref InternalVar_2, ref InternalVar_1);
        }

        public static float3 InternalMethod_1726(ref float3 InternalParameter_1957, ref float3 InternalParameter_1958)
        {
            if (InternalParameter_1957.z == 0)
            {
                return InternalType_187.InternalField_506;
            }

            float3 InternalVar_1 = InternalType_187.InternalMethod_889(InternalParameter_1958);
            float3 InternalVar_2 = InternalVar_1 * InternalType_187.InternalField_526 * InternalParameter_1957;
            float3 InternalVar_3 = math.normalize(InternalVar_1 * InternalType_187.InternalMethod_887(InternalType_187.InternalMethod_927(ref InternalParameter_1958, ref InternalVar_2)));

            return InternalVar_3;
        }
    }
}
