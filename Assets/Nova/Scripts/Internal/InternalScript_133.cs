using Nova.InternalNamespace_0.InternalNamespace_2;
using Nova.InternalNamespace_0.InternalNamespace_9;
using Nova.InternalNamespace_0.InternalNamespace_5;
using Nova.InternalNamespace_0.InternalNamespace_5.InternalNamespace_6;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Jobs;

namespace Nova.InternalNamespace_0.InternalNamespace_12
{
    internal static partial class InternalType_482
    {
        
        [BurstCompile]
        internal struct InternalType_484 : IJobParallelForTransform
        {
            [ReadOnly]
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public NativeList<float3> InternalField_2193;
            [WriteOnly, NativeDisableParallelForRestriction]
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public NativeList<float4x4> InternalField_2194;
            [ReadOnly]
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public NativeList<InternalType_222> InternalField_2195;
            [ReadOnly]
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public NativeHashMap<InternalType_131, InternalType_133> InternalField_2196;
            [ReadOnly]
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public NativeList<InternalType_133> InternalField_2197;
            [ReadOnly]
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public NativeList<InternalType_142> InternalField_2198;
            [ReadOnly]
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public NativeList<InternalType_220> InternalField_2199;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Execute(int phyiscalTransformIndex, TransformAccess transform)
            {
                InternalType_133 InternalVar_1 = InternalField_2197[phyiscalTransformIndex];

                if (!InternalField_2199[InternalVar_1].InternalProperty_249)
                {
                    return;
                }

                InternalType_222 InternalVar_2 = InternalField_2195[InternalVar_1];

                float3 InternalVar_3 = InternalField_2193[InternalVar_1];

                if (InternalField_2196.TryGetValue(InternalVar_2.InternalField_586, out InternalType_133 InternalVar_4) && InternalField_2198[InternalVar_4].InternalField_426)
                {
                    InternalVar_3 += InternalField_2193[InternalVar_4];
                }

                transform.localPosition = InternalVar_3;

                if (!InternalVar_4.InternalProperty_194)
                {
                    InternalField_2194[InternalVar_1] = transform.localToWorldMatrix;
                }
            }
        }

        [BurstCompile]
        internal struct InternalType_485 : InternalType_192
        {
            [ReadOnly]
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public NativeList<InternalType_222> InternalField_2200;
            [ReadOnly]
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public NativeHashMap<InternalType_131, InternalType_133> InternalField_2201;
            [ReadOnly]
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public NativeList<InternalType_142> InternalField_2202;
            [ReadOnly]
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public NativeList<InternalType_133> InternalField_2203;

            [ReadOnly]
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public NativeList<quaternion> InternalField_2204;
            [ReadOnly]
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public NativeList<float3> InternalField_2205;
            [ReadOnly]
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public NativeList<float3> InternalField_2206;

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public NativeList<float4x4> InternalField_2207;
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public NativeList<float4x4> InternalField_2208;

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public NativeList<InternalType_220> InternalField_2209;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Execute()
            {
                int InternalVar_1 = InternalField_2203.Length;

                for (int InternalVar_2 = 0; InternalVar_2 < InternalVar_1; ++InternalVar_2)
                {
                    InternalType_133 InternalVar_3 = InternalField_2203[InternalVar_2];

                    ref InternalType_220 InternalVar_4 = ref InternalField_2209.ElementAt(InternalVar_3);

                    if (!InternalField_2201.TryGetValue(InternalField_2200[InternalVar_3].InternalField_586, out InternalType_133 InternalVar_5))
                    {
                        if (InternalVar_4.InternalProperty_249)
                        {
                            InternalField_2207.ElementAt(InternalVar_3) = math.inverse(InternalField_2208.ElementAt(InternalVar_3));
                        }

                        continue;
                    }

                    if (!InternalVar_4.InternalProperty_249 && !InternalField_2209[InternalVar_5].InternalProperty_249)
                    {
                        continue;
                    }

                    ref float4x4 InternalVar_6 = ref InternalField_2207.ElementAt(InternalVar_5);
                    float4x4 InternalVar_7;

                    if (InternalField_2202[InternalVar_3].InternalField_426)
                    {
                        InternalVar_7 = float4x4.Translate(-InternalField_2206[InternalVar_3]);
                    }
                    else
                    {
                        float3 InternalVar_8 = InternalField_2205[InternalVar_3];

                        float4x4 InternalVar_9 = float4x4.Scale(math.select(math.rcp(InternalVar_8), InternalType_187.InternalField_530, InternalVar_8 == InternalType_187.InternalField_530));
                        float4x4 InternalVar_10 = new float4x4(math.inverse(InternalField_2204[InternalVar_3]), InternalType_187.InternalField_530);
                        float4x4 InternalVar_11 = float4x4.Translate(-InternalField_2206[InternalVar_3]);

                        InternalVar_7 = math.mul(InternalVar_9, math.mul(InternalVar_10, InternalVar_11));
                    }

                    float4x4 InternalVar_12 = math.mul(InternalVar_7, InternalVar_6);

                    InternalField_2207.ElementAt(InternalVar_3) = InternalVar_12;
                    InternalField_2208.ElementAt(InternalVar_3) = math.inverse(InternalVar_12);

                    InternalVar_4 = InternalType_220.InternalMethod_1052(InternalVar_4, InternalType_220.InternalField_578);
                }
            }
        }
    }
}
