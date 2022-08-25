using Nova.InternalNamespace_0.InternalNamespace_4;
using Nova.InternalNamespace_0.InternalNamespace_3;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;

namespace Nova.InternalNamespace_0.InternalNamespace_5.InternalNamespace_6
{
    internal static class InternalType_206
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool InternalMethod_995<T>(this List<T> InternalParameter_971, out T InternalParameter_972)
        {
            if (InternalParameter_971.Count > 0)
            {
                InternalParameter_972 = InternalParameter_971[InternalParameter_971.Count - 1];
                InternalParameter_971.RemoveAt(InternalParameter_971.Count - 1);
                return true;
            }
            else
            {
                InternalParameter_972 = default;
                return false;
            }
        }

        public static InternalType_521<T> InternalMethod_2043<T>(this List<T> InternalParameter_2367)
        {
            return new InternalType_521<T>(InternalParameter_2367);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InternalMethod_997<T>(this List<T> InternalParameter_974) where T : IDisposable
        {
            for (int InternalVar_1 = 0; InternalVar_1 < InternalParameter_974.Count; ++InternalVar_1)
            {
                InternalParameter_974[InternalVar_1].Dispose();
            }
            InternalParameter_974.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InternalMethod_998<T>(this List<NativeList<T>> InternalParameter_975, ref NativeList<T> InternalParameter_976) where T : unmanaged
        {
            if (!InternalParameter_975.InternalMethod_995(out InternalParameter_976))
            {
                InternalParameter_976.InternalMethod_1020();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InternalMethod_999<K, V>(this List<NativeHashMap<K, V>> InternalParameter_977, ref NativeHashMap<K, V> InternalParameter_978)
            where K : unmanaged, IEquatable<K>
            where V : unmanaged
        {
            if (!InternalParameter_977.InternalMethod_995(out InternalParameter_978))
            {
                InternalParameter_978.InternalMethod_1009();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InternalMethod_1000<T>(this List<T> InternalParameter_979, ref T InternalParameter_980) where T : struct, InternalType_147
        {
            if (!InternalParameter_979.InternalMethod_995(out InternalParameter_980))
            {
                InternalParameter_980.InternalMethod_702();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InternalMethod_1001<T>(this List<T> InternalParameter_981, ref T InternalParameter_982, int InternalParameter_983 = 0) where T : struct, InternalType_148
        {
            if (!InternalParameter_981.InternalMethod_995(out InternalParameter_982))
            {
                InternalParameter_982.InternalMethod_703(InternalParameter_983);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InternalMethod_1002<T>(this List<T> InternalParameter_984, ref T InternalParameter_985) where T : struct, InternalType_150
        {
            InternalParameter_985.InternalMethod_705();
            InternalParameter_984.Add(InternalParameter_985);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InternalMethod_1003<T>(this List<NativeList<T>> InternalParameter_986, ref NativeList<T> InternalParameter_987) where T : unmanaged
        {
            InternalParameter_987.Clear();
            InternalParameter_986.Add(InternalParameter_987);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InternalMethod_1004<K, V>(this List<NativeHashMap<K, V>> InternalParameter_988, ref NativeHashMap<K, V> InternalParameter_989)
            where K : unmanaged, IEquatable<K>
            where V : unmanaged
        {
            InternalParameter_989.Clear();
            InternalParameter_988.Add(InternalParameter_989);
        }
    }
}

