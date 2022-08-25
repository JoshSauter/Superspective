using System.Collections.Generic;


namespace Nova.InternalNamespace_0.InternalNamespace_4
{
    internal class InternalType_155<T17> : InternalType_156<InternalType_157<T17>, T17> { }

    internal class InternalType_157<T> : List<T> { }

    internal class InternalType_158<T21,T22> : InternalType_156<InternalType_159<T21, T22>, KeyValuePair<T21, T22>> { }

    internal class InternalType_159<T23,T24> : Dictionary<T23, T24> { }

    internal class InternalType_156<TCollection,T> where TCollection : ICollection<T>, new()
    {
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly static Queue<TCollection> InternalField_449 = new Queue<TCollection>();

        public static TCollection InternalMethod_740()
        {
            if (InternalField_449.Count > 0)
            {
                TCollection InternalVar_1 = InternalField_449.Dequeue();
                InternalVar_1.Clear();
                return InternalVar_1;
            }

            return new TCollection();
        }

        public static void InternalMethod_741(TCollection InternalParameter_572)
        {
            if (InternalParameter_572 == null)
            {
                return;
            }

            InternalField_449.Enqueue(InternalParameter_572);
        }
    }
}
