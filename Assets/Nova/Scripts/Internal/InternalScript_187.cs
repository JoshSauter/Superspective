using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Nova.InternalNamespace_0.InternalNamespace_10
{
    
    internal struct InternalType_311 : IEquatable<InternalType_311>
    {
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public Rect InternalField_1024;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public InternalType_310 InternalField_1025;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public InternalType_695 InternalField_1026;
        
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
                public int InternalField_1420;

        
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public float InternalProperty_308
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => InternalField_1024.width / (float)InternalField_1024.height;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public InternalType_311(InternalType_695 InternalParameter_1900)
        {
            InternalField_1026 = InternalParameter_1900;
            InternalField_1025 = InternalType_310.InternalField_1023;
            InternalField_1420 = 0;
            InternalField_1024 = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(InternalType_311 other)
        {
            return
                InternalField_1024.Equals(other.InternalField_1024) &&
                InternalField_1025.Equals(other.InternalField_1025) &&
                InternalField_1026 == other.InternalField_1026 &&
                InternalField_1420 == other.InternalField_1420;
        }

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public static InternalType_311 InternalField_1027 = new InternalType_311()
        {
            InternalField_1025 = InternalType_310.InternalField_1023,
            InternalField_1420 = 0,
        };
    }

    internal struct InternalType_321 : IEquatable<InternalType_321>
    {
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public int2 InternalField_1077;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public int InternalField_1078;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public GraphicsFormat InternalField_1079;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public bool InternalField_1080;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public bool InternalField_1081;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public bool InternalProperty_313
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => InternalField_1078 > 1;
        }

        public InternalType_321(Texture InternalParameter_1537)
        {
            InternalField_1077 = new int2(InternalParameter_1537.width, InternalParameter_1537.height);
            InternalField_1079 = InternalParameter_1537.graphicsFormat;
            InternalField_1078 = InternalParameter_1537.mipmapCount;
            InternalField_1080 = InternalParameter_1537 is Texture2D;
            InternalField_1081 = GraphicsFormatUtility.HasAlphaChannel(InternalParameter_1537.graphicsFormat);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(InternalType_321 other)
        {
            return
                InternalField_1077.Equals(other.InternalField_1077) &&
                InternalField_1078 == other.InternalField_1078 &&
                InternalField_1079 == other.InternalField_1079 &&
                InternalField_1080 == other.InternalField_1080 &&
                InternalField_1081 == other.InternalField_1081;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            int InternalVar_1 = 13;
            InternalVar_1 = (InternalVar_1 * 7) + InternalField_1077.GetHashCode();
            InternalVar_1 = (InternalVar_1 * 7) + InternalField_1078.GetHashCode();
            InternalVar_1 = (InternalVar_1 * 7) + ((int)InternalField_1079).GetHashCode();
            InternalVar_1 = (InternalVar_1 * 7) + InternalField_1080.GetHashCode();
            return InternalVar_1;
        }
    }

    internal struct InternalType_317
    {
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public int InternalField_1061;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public bool InternalField_1062;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public bool InternalField_1063;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public TextureFormat InternalField_1064;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public bool InternalProperty_312
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => InternalField_1061 != 0;
        }
    }
}