using Nova.InternalNamespace_0.InternalNamespace_4;
using Nova.InternalNamespace_0.InternalNamespace_2;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace Nova.InternalNamespace_0.InternalNamespace_10
{
    internal struct InternalType_356
    {
        
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
                public float4x4 InternalField_1242;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public float4x4 InternalField_1243;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public InternalType_306 InternalField_1244;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public float3 InternalField_1245;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public int InternalField_1246;

        public void InternalMethod_1546()
        {
            InternalType_306 InternalVar_1 = InternalType_306.InternalMethod_1377(ref InternalField_1243, ref InternalField_1244);
            InternalField_1245 = InternalVar_1.InternalMethod_1363();
        }

        public InternalType_356(ref float4x4 InternalParameter_1669, ref float4x4 InternalParameter_1670)
        {
            InternalField_1242 = InternalParameter_1669;
            InternalField_1243 = InternalParameter_1670;
            InternalField_1244 = default;
            InternalField_1245 = default;
            InternalField_1246 = 0;
        }
    }

    
    internal struct InternalType_19 : IEquatable<InternalType_19>
    {
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public InternalType_131 InternalField_806;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public InternalType_304 InternalField_795;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(InternalType_19 other)
        {
            return InternalField_806 == other.InternalField_806 && InternalField_795 == other.InternalField_795;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            int InternalVar_1 = 13;
            InternalVar_1 = (InternalVar_1 * 7) + InternalField_806.GetHashCode();
            InternalVar_1 = (InternalVar_1 * 7) + InternalField_795.GetHashCode();
            return InternalVar_1;
        }
    }

    internal static class InternalType_357
    {
        public static ref T InternalMethod_1297<T>(this ref NativeHashMap<InternalType_131, InternalType_162<InternalType_304, T>> InternalParameter_1367, ref InternalType_19 InternalParameter_1366) where T : unmanaged
        {
            return ref InternalParameter_1367[InternalParameter_1366.InternalField_806].InternalMethod_758(InternalParameter_1366.InternalField_795);
        }
    }
}