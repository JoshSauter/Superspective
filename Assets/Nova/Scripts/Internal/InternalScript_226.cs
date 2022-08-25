using Nova.InternalNamespace_0.InternalNamespace_10;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Nova.InternalNamespace_0
{
    [StructLayoutAttribute(LayoutKind.Sequential)]
    internal struct InternalType_109 : IEquatable<InternalType_109>
    {
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public Color InternalField_348;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public bool InternalField_349;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public bool InternalField_350;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public bool InternalProperty_188
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => InternalField_349 && InternalField_350;
        }

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public bool InternalProperty_189
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => InternalField_349 && !InternalField_350;
        }

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public InternalType_322 InternalProperty_763
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (InternalProperty_188)
                {
                    return InternalType_322.InternalField_1084;
                }
                else if (InternalProperty_189)
                {
                    return InternalType_322.InternalField_1083;
                }
                else
                {
                    return InternalType_322.InternalField_1085;
                }
            }
        }

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        internal static readonly InternalType_109 InternalField_351 = new InternalType_109()
        {
            InternalField_348 = Color.white,
            InternalField_349 = true,
        };

        public bool Equals(InternalType_109 other)
        {
            return InternalField_348.Equals(other.InternalField_348) && InternalField_349 == other.InternalField_349;
        }
    }
}

