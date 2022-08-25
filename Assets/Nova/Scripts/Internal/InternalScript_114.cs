using Nova.Compat;
using Nova.InternalNamespace_0.InternalNamespace_3;
using Nova.InternalNamespace_0.InternalNamespace_2;
using Nova.InternalNamespace_0.InternalNamespace_9;
using Nova.InternalNamespace_0.InternalNamespace_12;
using Nova.InternalNamespace_0.InternalNamespace_10;
using Nova.InternalNamespace_0.InternalNamespace_5;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Nova.InternalNamespace_0.InternalNamespace_11
{
    internal struct InternalType_428 : System.IEquatable<InternalType_428>
    {
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public InternalType_5 InternalField_1642;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public float3 InternalField_1643;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public float3 InternalField_1644;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public GameObject InternalProperty_343 => InternalField_1642.InternalProperty_203 ? null : InternalField_1642.InternalProperty_202.gameObject;

        public bool Equals(InternalType_428 other)
        {
            return InternalField_1642 == other.InternalField_1642;
        }

        public override int GetHashCode()
        {
            return InternalField_1642.GetHashCode();
        }
    }

    internal struct InternalType_427
    {
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public UIBlock InternalField_1638;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public Bounds InternalField_1639;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public Vector3 InternalField_1640;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public bool InternalField_1641;
    }

    internal class InternalType_429 : InternalType_136<InternalType_429>
    {
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public const int InternalField_1645 = InternalType_178.InternalField_469;

        public struct InternalType_430<T> : InternalType_147 where T : unmanaged, IComparer<T>
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public NativeList<T> InternalField_1656;
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public NativeList<InternalType_133> InternalField_1657;

            public void Dispose()
            {
                InternalField_1656.Dispose();
                InternalField_1657.Dispose();
            }

            public void InternalMethod_702()
            {
                InternalField_1656 = new NativeList<T>(InternalType_178.InternalField_3012, Allocator.Persistent);
                InternalField_1657 = new NativeList<InternalType_133>(InternalType_178.InternalField_3013, Allocator.Persistent);
            }
        }

        private struct InternalType_431
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public int InternalField_1658;
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public float InternalField_1659;
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public int InternalField_1660;
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public System.Func<InternalType_5, bool> InternalField_1661;
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public bool InternalField_1936;

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public static readonly InternalType_431 InternalField_1662 = new InternalType_431() { InternalField_1660 = int.MaxValue, InternalField_1659 = float.PositiveInfinity };

            public bool InternalMethod_1698(InternalType_5 InternalParameter_1920)
            {
                return InternalMethod_1699(InternalParameter_1920) && (InternalField_1661 == null || InternalField_1661.Invoke(InternalParameter_1920));
            }

            private bool InternalMethod_1699(InternalType_5 InternalParameter_1921)
            {
                MonoBehaviour InternalVar_1 = InternalParameter_1921 as MonoBehaviour;

                if (InternalVar_1 == null)
                {
                    return false;
                }

#pragma warning disable CS0162 
                if (NovaApplication.ConstIsEditor && InternalField_1936 && NovaApplication.IsPlaying)
                {
                    if (!Application.IsPlaying(InternalVar_1.gameObject))
                    {
                        return false;
                    }
                }
#pragma warning restore CS0162 

                return (InternalField_1658 & 1 << InternalVar_1.gameObject.layer) != 0;
            }
        }


        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private InternalType_430<InternalType_442> InternalField_1649;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private InternalType_433<InternalType_443, InternalType_434, InternalType_442> InternalField_1650;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private InternalType_433<InternalType_446, InternalType_432, InternalType_442> InternalField_1651;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private List<InternalType_428> InternalField_1652 = new List<InternalType_428>();

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private static InternalType_735<InternalType_175> InternalField_1654;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private static InternalType_735<InternalType_175> InternalField_1655;


        
        public void InternalMethod_1686(Ray InternalParameter_1878, List<InternalType_428> InternalParameter_1879, float InternalParameter_1880, int InternalParameter_1881)
        {
            InternalMethod_950(InternalParameter_1878, InternalParameter_1881, ref InternalField_1649);
            InternalType_431 InternalVar_1 = new InternalType_431() { InternalField_1660 = int.MaxValue, InternalField_1659 = InternalParameter_1880, InternalField_1658 = InternalParameter_1881, InternalField_1936 = true };
            InternalMethod_1696(ref InternalField_1649.InternalField_1656, InternalParameter_1879, ref InternalVar_1);
        }

        public void InternalMethod_1687(Ray InternalParameter_1882, List<InternalType_428> InternalParameter_1883, float InternalParameter_1884, System.Func<InternalType_5, bool> InternalParameter_1885, int InternalParameter_1886 = InternalField_1645)
        {
            InternalMethod_950(InternalParameter_1882, InternalParameter_1886, ref InternalField_1649, true);
            InternalType_431 InternalVar_1 = new InternalType_431() { InternalField_1660 = int.MaxValue, InternalField_1658 = InternalParameter_1886, InternalField_1659 = InternalParameter_1884, InternalField_1661 = InternalParameter_1885 };
            InternalMethod_1696(ref InternalField_1649.InternalField_1656, InternalParameter_1883, ref InternalVar_1);
        }

        public bool InternalMethod_1688(Ray InternalParameter_1887, out InternalType_428 InternalParameter_1888, System.Func<InternalType_5, bool> InternalParameter_1889)
        {
            InternalMethod_950(InternalParameter_1887, InternalField_1645, ref InternalField_1649, true);
            InternalType_431 InternalVar_1 = new InternalType_431() { InternalField_1660 = int.MaxValue, InternalField_1658 = InternalField_1645, InternalField_1661 = InternalParameter_1889 };
            InternalParameter_1888.InternalField_1642 = InternalMethod_1697(ref InternalField_1649.InternalField_1656, ref InternalVar_1, out float InternalVar_2, out float3 InternalVar_3);
            InternalParameter_1888.InternalField_1643 = InternalParameter_1887.GetPoint(InternalVar_2);
            InternalParameter_1888.InternalField_1644 = InternalVar_3;

            return InternalParameter_1888.InternalField_1642 != null;
        }

        
        public void InternalMethod_1689(InternalType_432 InternalParameter_1890, List<InternalType_428> InternalParameter_1891, int InternalParameter_1892)
        {
            InternalMethod_15(InternalParameter_1890, InternalParameter_1892, ref InternalField_1649);
            InternalType_431 InternalVar_1 = new InternalType_431() { InternalField_1660 = int.MaxValue, InternalField_1659 = InternalParameter_1890.InternalField_1664, InternalField_1658 = InternalParameter_1892, InternalField_1936 = true };
            InternalMethod_1696(ref InternalField_1649.InternalField_1656, InternalParameter_1891, ref InternalVar_1);
        }

        
        public bool InternalMethod_1690(InternalType_432 InternalParameter_1893, out InternalType_428 InternalParameter_1894, int InternalParameter_1895)
        {
            InternalMethod_15(InternalParameter_1893, InternalParameter_1895, ref InternalField_1649);
            InternalType_431 InternalVar_1 = new InternalType_431() { InternalField_1660 = 1, InternalField_1659 = InternalParameter_1893.InternalField_1664, InternalField_1658 = InternalParameter_1895, InternalField_1936 = true };

            InternalMethod_1696(ref InternalField_1649.InternalField_1656, InternalField_1652, ref InternalVar_1);

            if (InternalField_1652.Count > 0)
            {
                InternalParameter_1894 = InternalField_1652[0];
                return true;
            }

            InternalParameter_1894 = default;
            return false;
        }

        
        public bool InternalMethod_1691(Ray InternalParameter_1896, out InternalType_428 InternalParameter_1897, float InternalParameter_1898, int InternalParameter_1899)
        {
            InternalMethod_950(InternalParameter_1896, InternalParameter_1899, ref InternalField_1649);
            InternalType_431 InternalVar_1 = new InternalType_431() { InternalField_1660 = 1, InternalField_1659 = InternalParameter_1898, InternalField_1658 = InternalParameter_1899, InternalField_1936 = true };
            InternalMethod_1696(ref InternalField_1649.InternalField_1656, InternalField_1652, ref InternalVar_1);

            if (InternalField_1652.Count > 0)
            {
                InternalParameter_1897 = InternalField_1652[0];
                return true;
            }

            InternalParameter_1897 = default;
            return false;
        }


        private void InternalMethod_950(Ray InternalParameter_1622, int InternalParameter_1621, ref InternalType_430<InternalType_442> InternalParameter_1603, bool InternalParameter_1602 = false)
        {
            if (!InternalType_64.InternalProperty_200.InternalProperty_742)
            {
                return;
            }

            InternalField_1650.InternalField_1665.InternalField_1721 = NovaApplication.IsEditor ? InternalParameter_1602 : false;
            InternalField_1650.InternalField_1665.InternalField_1712 = InternalParameter_1622;
            InternalField_1650.InternalField_1665.InternalField_1880 = InternalParameter_1621;
            InternalField_1650.InternalField_1667 = InternalParameter_1603.InternalField_1656;
            InternalField_1650.InternalField_1666 = InternalParameter_1603.InternalField_1657;

            unsafe
            {
                InternalField_1654.InternalField_3352.Invoke(UnsafeUtility.AddressOf(ref InternalField_1650));
            }
        }

        private void InternalMethod_15(InternalType_432 InternalParameter_1557, int InternalParameter_1556, ref InternalType_430<InternalType_442> InternalParameter_1555)
        {
            if (!InternalType_64.InternalProperty_200.InternalProperty_742)
            {
                return;
            }

            InternalField_1651.InternalField_1665.InternalField_1764 = InternalParameter_1557;
            InternalField_1651.InternalField_1665.InternalField_1879 = InternalParameter_1556;
            InternalField_1651.InternalField_1667 = InternalParameter_1555.InternalField_1656;
            InternalField_1651.InternalField_1666 = InternalParameter_1555.InternalField_1657;

            unsafe
            {
                InternalField_1655.InternalField_3352.Invoke(UnsafeUtility.AddressOf(ref InternalField_1651));
            }
        }

        
        private void InternalMethod_1696<T>(ref NativeList<T> InternalParameter_1913, List<InternalType_428> InternalParameter_1914, ref InternalType_431 InternalParameter_1915) where T : unmanaged, InternalType_439
        {
            InternalParameter_1914.Clear();

            Dictionary<InternalType_131, InternalType_145> InternalVar_1 = InternalType_253.InternalProperty_190.InternalField_413;

            int InternalVar_2 = InternalParameter_1913.Length;

            for (int InternalVar_3 = 0; InternalVar_3 < InternalVar_2 && InternalParameter_1914.Count < InternalParameter_1915.InternalField_1660; ++InternalVar_3)
            {
                T InternalVar_4 = InternalParameter_1913[InternalVar_3];

                if (InternalVar_4.InternalProperty_347 > InternalParameter_1915.InternalField_1659)
                {
                    break;
                }

                if (!InternalVar_1.TryGetValue(InternalVar_4.InternalProperty_344, out InternalType_145 InternalVar_5))
                {
                    continue;
                }

                if (InternalVar_5.InternalProperty_203)
                {
                    continue;
                }

                InternalType_5 InternalVar_6 = InternalVar_5 as InternalType_5;

                if (InternalVar_6.InternalProperty_203 || !InternalParameter_1915.InternalMethod_1698(InternalVar_6))
                {
                    continue;
                }

                InternalParameter_1914.Add(new InternalType_428()
                {
                    InternalField_1642 = InternalVar_6,
                    InternalField_1643 = InternalVar_4.InternalProperty_345,
                    InternalField_1644 = InternalVar_4.InternalProperty_346
                });
            }
        }

        
        private InternalType_5 InternalMethod_1697(ref NativeList<InternalType_442> InternalParameter_1916, ref InternalType_431 InternalParameter_1917, out float InternalParameter_1918, out float3 InternalParameter_1919)
        {
            InternalParameter_1918 = float.NaN;
            InternalParameter_1919 = InternalType_187.InternalField_535;
            InternalType_5 InternalVar_1 = null;

            for (int InternalVar_2 = 0; InternalVar_2 < InternalParameter_1916.Length; ++InternalVar_2)
            {
                if (InternalType_253.InternalProperty_190.InternalField_413.TryGetValue(InternalParameter_1916[InternalVar_2].InternalField_1706, out InternalType_145 InternalVar_3))
                {

                    InternalType_5 InternalVar_4 = InternalVar_3 as InternalType_5;

                    if (InternalVar_4.InternalProperty_203 || !InternalParameter_1917.InternalMethod_1698(InternalVar_4))
                    {
                        continue;
                    }

                    InternalParameter_1918 = InternalParameter_1916[InternalVar_2].InternalField_1707;
                    InternalParameter_1919 = InternalParameter_1916[InternalVar_2].InternalField_1709;
                    InternalVar_1 = InternalVar_4;
                    break;
                }
            }

            return InternalVar_1;
        }

        protected override void InternalMethod_656()
        {
            unsafe
            {
                InternalField_1654 = new InternalType_735<InternalType_175>(InternalType_433<InternalType_443, InternalType_434, InternalType_442>.InternalMethod_1703);
                InternalField_1655 = new InternalType_735<InternalType_175>(InternalType_433<InternalType_446, InternalType_432, InternalType_442>.InternalMethod_1703);
            }


            InternalField_1649.InternalMethod_702();

            InternalType_253 InternalVar_1 = InternalType_253.InternalProperty_190;
            InternalType_457 InternalVar_2 = InternalType_457.InternalProperty_190;

            InternalField_1650 = new InternalType_433<InternalType_443, InternalType_434, InternalType_442>()
            {
                InternalField_1670 = InternalVar_1.InternalProperty_263,
                InternalField_1671 = InternalVar_1.InternalProperty_264,
                InternalField_1669 = InternalVar_1.InternalProperty_260,

                InternalField_1668 = InternalVar_2.InternalField_1861,

                InternalField_1665 = new InternalType_443()
                {
                    InternalField_1714 = InternalType_268.InternalField_406.InternalProperty_284,
                    InternalField_1713 = InternalType_274.InternalProperty_190.InternalField_863.InternalField_1235,
                    InternalField_319 = InternalType_274.InternalProperty_190.InternalField_874.InternalField_1154,

                    InternalField_1716 = InternalVar_1.InternalProperty_264,

                    InternalField_1715 = InternalVar_2.InternalField_1843,
                    InternalField_1717 = InternalVar_2.InternalField_1854,
                    InternalField_1718 = InternalVar_2.InternalField_1855,
                    InternalField_1719 = InternalVar_2.InternalField_430,
                    InternalField_1720 = InternalVar_2.InternalField_431,
                }
            };

            InternalField_1651 = new InternalType_433<InternalType_446, InternalType_432, InternalType_442>()
            {
                InternalField_1670 = InternalVar_1.InternalProperty_263,
                InternalField_1671 = InternalVar_1.InternalProperty_264,
                InternalField_1669 = InternalVar_1.InternalProperty_260,

                InternalField_1668 = InternalVar_2.InternalField_1861,

                InternalField_1665 = new InternalType_446()
                {
                    InternalField_1766 = InternalType_268.InternalField_406.InternalProperty_284,
                    InternalField_1765 = InternalType_274.InternalProperty_190.InternalField_863.InternalField_1235,
                    InternalField_292 = InternalType_274.InternalProperty_190.InternalField_874.InternalField_1154,

                    InternalField_1768 = InternalVar_1.InternalProperty_264,

                    InternalField_1767 = InternalVar_2.InternalField_1843,
                    InternalField_1769 = InternalVar_2.InternalField_1854,
                    InternalField_1770 = InternalVar_2.InternalField_1855,
                    InternalField_1771 = InternalVar_2.InternalField_430,
                    InternalField_1772 = InternalVar_2.InternalField_431,
                }
            };

        }

        protected override void InternalMethod_657()
        {
            InternalField_1649.Dispose();
        }
    }
}
