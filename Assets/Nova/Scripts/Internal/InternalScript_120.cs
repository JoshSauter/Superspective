using Nova.Compat;
using Nova.InternalNamespace_0.InternalNamespace_4;
using Nova.InternalNamespace_0.InternalNamespace_2;
using Nova.InternalNamespace_0.InternalNamespace_9;
using System;
using Unity.Collections;
using Unity.Mathematics;

namespace Nova.InternalNamespace_0.InternalNamespace_12
{
    internal struct InternalType_478
    {
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public float3 InternalField_2145;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public bool InternalField_2146;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public bool InternalField_2147;

        public void InternalMethod_1902(ref InternalType_478 InternalParameter_2124)
        {
            InternalField_2147 = !InternalField_2145.Equals(InternalParameter_2124.InternalField_2145) || InternalField_2146 != InternalParameter_2124.InternalField_2146;
        }
    }

    internal partial class InternalType_457
    {
        public static void InternalMethod_1860()
        {
            InternalProperty_190.InternalProperty_409.InternalMethod_1861();
        }

        internal class InternalType_458 : IDisposable
        {
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public NativeHashMap<InternalType_131, InternalType_478> InternalField_1895;
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            private bool InternalField_1896 = false;
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            private bool InternalField_212 = false;

            public void InternalMethod_1861()
            {
                InternalField_1896 = true;
            }

            public void InternalMethod_1862()
            {
                if (!InternalField_1896)
                {
                    return;
                }

                InternalMethod_1863();

                InternalField_1896 = false;
            }

            public void InternalMethod_1945()
            {
                if (!InternalField_212)
                {
                    return;
                }

                NativeArray <InternalType_131> InternalVar_1 = InternalField_1895.GetKeyArray(Allocator.Temp);

                int InternalVar_2 = InternalVar_1.Length;
                for (int InternalVar_3 = 0; InternalVar_3 < InternalVar_2; ++InternalVar_3)
                {
                    InternalType_131 InternalVar_4 = InternalVar_1[InternalVar_3];
                    if (InternalField_1895.TryGetValue(InternalVar_4, out InternalType_478 InternalVar_5))
                    {
                        InternalVar_5.InternalField_2147 = false;
                        InternalField_1895[InternalVar_4] = InternalVar_5;
                    }
                }

                InternalVar_1.Dispose();
                InternalField_212 = false;
            }

            private void InternalMethod_1863()
            {
                NativeList<InternalType_131> InternalVar_1 = InternalType_253.InternalProperty_190.InternalProperty_260;

                InternalType_159<InternalType_131, InternalType_478> InternalVar_2 = InternalType_158<InternalType_131, InternalType_478>.InternalMethod_740();
                NativeArray<InternalType_131> InternalVar_3 = InternalField_1895.GetKeyArray(Allocator.Temp);

                for (int InternalVar_4 = 0; InternalVar_4 < InternalVar_3.Length; ++InternalVar_4)
                {
                    InternalType_131 InternalVar_5 = InternalVar_3[InternalVar_4];
                    InternalVar_2.Add(InternalVar_5, InternalField_1895[InternalVar_5]);
                }

                InternalVar_3.Dispose();

                InternalField_1895.Clear();

                int InternalVar_6 = 0;
                for (int InternalVar_7 = 0; InternalVar_7 < InternalVar_1.Length; ++InternalVar_7)
                {
                    InternalType_131 InternalVar_8 = InternalVar_1[InternalVar_7];
                    UIBlock InternalVar_9 = InternalType_253.InternalProperty_190.InternalField_413[InternalVar_8] as UIBlock;

                    if (!SceneViewUtils.IsInCurrentPrefabStage(InternalVar_9.gameObject))
                    {
                        continue;
                    }

                    InternalType_99 InternalVar_10 = new InternalType_99((InternalType_83)(int)InternalVar_9.AutoSize.X, (InternalType_83)(int)InternalVar_9.AutoSize.Y, (InternalType_83)(int)InternalVar_9.AutoSize.Z);
                    InternalType_53 InternalVar_11 = InternalVar_9.Size.InternalMethod_3();

                    InternalVar_9.PreviewSize = InternalVar_9.SizeMinMax.Clamp(math.select(InternalVar_11.InternalProperty_116, InternalVar_9.PreviewSize, InternalVar_11.InternalProperty_118));
                    InternalType_478 InternalVar_12 = new InternalType_478() { InternalField_2145 = InternalVar_9.PreviewSize, InternalField_2146 = SceneViewUtils.IsInCurrentStage(InternalVar_9.gameObject) };

                    if (!InternalVar_2.TryGetValue(InternalVar_8, out InternalType_478 InternalVar_13))
                    {
                        InternalVar_13 = default;
                    }

                    InternalVar_12.InternalMethod_1902(ref InternalVar_13);

                    InternalField_212 |= InternalVar_12.InternalField_2147;
                    InternalVar_6++;

                    InternalField_1895.Add(InternalVar_8, InternalVar_12);
                }

                InternalType_158<InternalType_131, InternalType_478>.InternalMethod_741(InternalVar_2);
            }

            public void InternalMethod_1864(ref InternalType_460.InternalType_461 InternalParameter_2059)
            {
                InternalField_1895 = new NativeHashMap<InternalType_131, InternalType_478>(NovaApplication.IsEditor ? 4 : 0, Allocator.Persistent);
                InternalField_1896 = NovaApplication.IsEditor ? true : false;

                InternalParameter_2059.InternalField_1913 = InternalField_1895;
            }

            public void Dispose()
            {
                InternalField_1895.Dispose();
            }
        }
    }
}
