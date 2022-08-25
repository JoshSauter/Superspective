using Nova.InternalNamespace_0.InternalNamespace_4;
using Nova.InternalNamespace_0.InternalNamespace_3;
using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using UnityEngine;

namespace Nova.InternalNamespace_0.InternalNamespace_5.InternalNamespace_6
{
    internal static class InternalType_207
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static V InternalMethod_1006<K, V>(this ref NativeHashMap<K, V> InternalParameter_992, K InternalParameter_993)
            where K : struct, IEquatable<K>
            where V : unmanaged, InternalType_150
        {
            V InternalVar_1 = InternalParameter_992[InternalParameter_993];
            InternalVar_1.InternalMethod_705();
            return InternalVar_1;
        }

        public static V InternalMethod_1007<K, V>(this ref NativeHashMap<K, V> InternalParameter_994, K InternalParameter_995, int InternalParameter_996)
            where K : struct, IEquatable<K>
            where V : unmanaged, InternalType_149
        {
            V InternalVar_1 = InternalParameter_994[InternalParameter_995];
            InternalVar_1.InternalProperty_216 = InternalParameter_996;
            return InternalVar_1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InternalMethod_1008<K, V>(this ref NativeHashMap<K, V> InternalParameter_997, K InternalParameter_998)
            where K : struct, IEquatable<K>
            where V : struct, InternalType_147
        {
            V InternalVar_1 = new V();
            InternalVar_1.InternalMethod_702();
            InternalParameter_997.Add(InternalParameter_998, InternalVar_1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InternalMethod_1009<K, V>(this ref NativeHashMap<K, V> InternalParameter_999, int InternalParameter_1000 = 4)
            where K : struct, IEquatable<K>
            where V : struct
        {
            InternalParameter_999 = new NativeHashMap<K, V>(InternalParameter_1000, Allocator.Persistent);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool InternalMethod_1010<K, V>(this ref NativeHashMap<K, V> InternalParameter_1001, K InternalParameter_1002, out V InternalParameter_1003)
            where K : struct, IEquatable<K>
            where V : struct
        {
            if (InternalParameter_1001.TryGetValue(InternalParameter_1002, out InternalParameter_1003))
            {
                InternalParameter_1001.Remove(InternalParameter_1002);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}

