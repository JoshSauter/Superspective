using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Nova.InternalNamespace_0.InternalNamespace_5.InternalNamespace_6
{
    internal static class InternalType_211
    {
        public static void InternalMethod_1041<K, V>(this ref UnsafeHashMap<K, V> InternalParameter_1050, int InternalParameter_1051 = 0)
            where K : struct, System.IEquatable<K>
            where V : struct
        {
            InternalParameter_1050 = new UnsafeHashMap<K, V>(InternalParameter_1051, Allocator.Persistent);
        }
    }
}