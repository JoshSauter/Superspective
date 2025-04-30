using System;

namespace SuperspectiveAttributes {
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true)]
    public class DoNotSaveAttribute : Attribute { }
}
