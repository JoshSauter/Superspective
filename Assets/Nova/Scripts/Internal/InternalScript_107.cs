using Nova.InternalNamespace_0.InternalNamespace_4;
using System;
using UnityEngine;

namespace Nova.InternalNamespace_0
{
    internal enum InternalType_77
    {
        InternalField_251 = 0, 
        InternalField_252 = 1, 
        InternalField_253 = 2, 
        InternalField_254 = 3, 
        InternalField_255 = 4, 
    }

    internal static class InternalType_86
    {
        public static bool InternalMethod_490(this InternalType_77 InternalParameter_353)
        {
            return InternalParameter_353 != InternalType_77.InternalField_251;
        }

        public static bool InternalMethod_491(this InternalType_77 InternalParameter_354)
        {
            return InternalParameter_354.InternalMethod_492() || InternalParameter_354 == InternalType_77.InternalField_253;
        }

        public static bool InternalMethod_492(this InternalType_77 InternalParameter_355)
        {
            return InternalParameter_355 == InternalType_77.InternalField_255 || InternalParameter_355 == InternalType_77.InternalField_254;
        }

        public static int InternalMethod_493(this InternalType_77 InternalParameter_356)
        {
            return (int)InternalParameter_356;
        }
    }

    internal interface InternalType_75
    {
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        bool InternalProperty_741 { get; }

        InternalType_77 InternalMethod_3340<T>(UnityEngine.Ray InternalParameter_329, InternalType_76<T> InternalParameter_328, InternalType_76<T> InternalParameter_2232, Transform InternalParameter_2233) where T : unmanaged, IEquatable<T>;
    }

    internal interface InternalType_74
    {
        event InternalType_12.InternalType_15 InternalEvent_2;
        event InternalType_12.InternalType_17 InternalEvent_3;
        event InternalType_12.InternalType_16 InternalEvent_4;

        void InternalMethod_460(InternalType_75 InternalParameter_320);
        void InternalMethod_461(InternalType_75 InternalParameter_321);
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        InternalType_521<InternalType_75> InternalProperty_164 { get; }
        bool InternalMethod_463<T>() where T : unmanaged, IEquatable<T>;
        InternalType_76<T>? InternalMethod_464<T>(uint InternalParameter_322) where T : unmanaged, IEquatable<T>;
        bool InternalMethod_465(uint InternalParameter_323, out InternalType_78 InternalParameter_324);
        void InternalMethod_466<T>(InternalType_78 InternalParameter_325, InternalType_76<T>? InternalParameter_326) where T : unmanaged, IEquatable<T>;
        void InternalMethod_467(InternalType_78 InternalParameter_327);
        void InternalMethod_468();
    }
}
