using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace SuperspectiveUtils {
    [Serializable]
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(DictionaryDebugView<,>))]
    // from: https://stackoverflow.com/questions/268321/bidirectional-1-to-1-dictionary-in-c-sharp
    public class TwoWayDictionary<TFirst, TSecond> : IDictionary<TFirst, TSecond>,
        IReadOnlyDictionary<TFirst, TSecond>,
        IDictionary {
        readonly IDictionary<TFirst, TSecond> _firstToSecond = new Dictionary<TFirst, TSecond>();

        [NonSerialized]
        readonly ReverseDictionary _reverseDictionary;

        [NonSerialized]
        readonly IDictionary<TSecond, TFirst> _secondToFirst = new Dictionary<TSecond, TFirst>();

        public TwoWayDictionary() {
            _reverseDictionary = new ReverseDictionary(this);
        }

        public IDictionary<TSecond, TFirst> Reverse => _reverseDictionary;

        object ICollection.SyncRoot => ((ICollection) _firstToSecond).SyncRoot;

        bool ICollection.IsSynchronized => ((ICollection) _firstToSecond).IsSynchronized;

        bool IDictionary.IsFixedSize => ((IDictionary) _firstToSecond).IsFixedSize;

        object IDictionary.this[object key] {
            get => ((IDictionary) _firstToSecond)[key];
            set {
                ((IDictionary) _firstToSecond)[key] = value;
                ((IDictionary) _secondToFirst)[value] = key;
            }
        }

        ICollection IDictionary.Keys => ((IDictionary) _firstToSecond).Keys;

        ICollection IDictionary.Values => ((IDictionary) _firstToSecond).Values;

        IDictionaryEnumerator IDictionary.GetEnumerator() {
            return ((IDictionary) _firstToSecond).GetEnumerator();
        }

        void IDictionary.Add(object key, object value) {
            ((IDictionary) _firstToSecond).Add(key, value);
            ((IDictionary) _secondToFirst).Add(value, key);
        }

        void IDictionary.Remove(object key) {
            IDictionary firstToSecond = (IDictionary) _firstToSecond;
            if (!firstToSecond.Contains(key))
                return;
            object value = firstToSecond[key];
            firstToSecond.Remove(key);
            ((IDictionary) _secondToFirst).Remove(value);
        }

        public bool Contains(object key) {
            return ((IDictionary) _firstToSecond).Contains(key);
        }

        public int Count => _firstToSecond.Count;

        public bool IsReadOnly => _firstToSecond.IsReadOnly || _secondToFirst.IsReadOnly;

        public TSecond this[TFirst key] {
            get => _firstToSecond[key];
            set {
                _firstToSecond[key] = value;
                _secondToFirst[value] = key;
            }
        }

        public TFirst this[TSecond key] {
            get => Reverse[key];
            set {
                Reverse[key] = value;
            }
        }

        public ICollection<TFirst> Keys => _firstToSecond.Keys;

        public ICollection<TSecond> Values => _firstToSecond.Values;

        public IEnumerator<KeyValuePair<TFirst, TSecond>> GetEnumerator() {
            return _firstToSecond.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public void Add(TFirst key, TSecond value) {
            _firstToSecond.Add(key, value);
            _secondToFirst.Add(value, key);
        }

        public void Add(KeyValuePair<TFirst, TSecond> item) {
            _firstToSecond.Add(item);
            _secondToFirst.Add(item.Reverse());
        }

        public bool ContainsKey(TFirst key) {
            return _firstToSecond.ContainsKey(key);
        }

        public bool ContainsValue(TSecond value) {
            return _secondToFirst.ContainsKey(value);
        }

        public bool Contains(KeyValuePair<TFirst, TSecond> item) {
            return _firstToSecond.Contains(item);
        }

        public bool TryGetValue(TFirst key, out TSecond value) {
            return _firstToSecond.TryGetValue(key, out value);
        }

        public bool Remove(TFirst key) {
            TSecond value;
            if (_firstToSecond.TryGetValue(key, out value)) {
                _firstToSecond.Remove(key);
                _secondToFirst.Remove(value);
                return true;
            }

            return false;
        }

        public bool Remove(TSecond value) {
            TFirst key;
            if (_secondToFirst.TryGetValue(value, out key)) {
                _secondToFirst.Remove(value);
                _firstToSecond.Remove(key);
                return true;
            }

            return false;
        }

        public bool Remove(KeyValuePair<TFirst, TSecond> item) {
            return _firstToSecond.Remove(item);
        }

        public void Clear() {
            _firstToSecond.Clear();
            _secondToFirst.Clear();
        }

        public void CopyTo(KeyValuePair<TFirst, TSecond>[] array, int arrayIndex) {
            _firstToSecond.CopyTo(array, arrayIndex);
        }

        IEnumerable<TFirst> IReadOnlyDictionary<TFirst, TSecond>.Keys =>
            ((IReadOnlyDictionary<TFirst, TSecond>) _firstToSecond).Keys;

        IEnumerable<TSecond> IReadOnlyDictionary<TFirst, TSecond>.Values =>
            ((IReadOnlyDictionary<TFirst, TSecond>) _firstToSecond).Values;

        void ICollection.CopyTo(Array array, int index) {
            ((IDictionary) _firstToSecond).CopyTo(array, index);
        }

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context) {
            _secondToFirst.Clear();
            foreach (KeyValuePair<TFirst, TSecond> item in _firstToSecond) {
                _secondToFirst.Add(item.Value, item.Key);
            }
        }

        class ReverseDictionary : IDictionary<TSecond, TFirst>, IReadOnlyDictionary<TSecond, TFirst>, IDictionary {
            readonly TwoWayDictionary<TFirst, TSecond> _owner;

            public ReverseDictionary(TwoWayDictionary<TFirst, TSecond> owner) {
                _owner = owner;
            }

            object ICollection.SyncRoot => ((ICollection) _owner._secondToFirst).SyncRoot;

            bool ICollection.IsSynchronized => ((ICollection) _owner._secondToFirst).IsSynchronized;

            bool IDictionary.IsFixedSize => ((IDictionary) _owner._secondToFirst).IsFixedSize;

            object IDictionary.this[object key] {
                get => ((IDictionary) _owner._secondToFirst)[key];
                set {
                    ((IDictionary) _owner._secondToFirst)[key] = value;
                    ((IDictionary) _owner._firstToSecond)[value] = key;
                }
            }

            ICollection IDictionary.Keys => ((IDictionary) _owner._secondToFirst).Keys;

            ICollection IDictionary.Values => ((IDictionary) _owner._secondToFirst).Values;

            IDictionaryEnumerator IDictionary.GetEnumerator() {
                return ((IDictionary) _owner._secondToFirst).GetEnumerator();
            }

            void IDictionary.Add(object key, object value) {
                ((IDictionary) _owner._secondToFirst).Add(key, value);
                ((IDictionary) _owner._firstToSecond).Add(value, key);
            }

            void IDictionary.Remove(object key) {
                IDictionary firstToSecond = (IDictionary) _owner._secondToFirst;
                if (!firstToSecond.Contains(key))
                    return;
                object value = firstToSecond[key];
                firstToSecond.Remove(key);
                ((IDictionary) _owner._firstToSecond).Remove(value);
            }

            public bool Contains(object key) {
                return ((IDictionary) _owner._secondToFirst).Contains(key);
            }

            public int Count => _owner._secondToFirst.Count;

            public bool IsReadOnly => _owner._secondToFirst.IsReadOnly || _owner._firstToSecond.IsReadOnly;

            public TFirst this[TSecond key] {
                get => _owner._secondToFirst[key];
                set {
                    _owner._secondToFirst[key] = value;
                    _owner._firstToSecond[value] = key;
                }
            }

            public ICollection<TSecond> Keys => _owner._secondToFirst.Keys;

            public ICollection<TFirst> Values => _owner._secondToFirst.Values;

            public IEnumerator<KeyValuePair<TSecond, TFirst>> GetEnumerator() {
                return _owner._secondToFirst.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() {
                return GetEnumerator();
            }

            public void Add(TSecond key, TFirst value) {
                _owner._secondToFirst.Add(key, value);
                _owner._firstToSecond.Add(value, key);
            }

            public void Add(KeyValuePair<TSecond, TFirst> item) {
                _owner._secondToFirst.Add(item);
                _owner._firstToSecond.Add(item.Reverse());
            }

            public bool ContainsKey(TSecond key) {
                return _owner._secondToFirst.ContainsKey(key);
            }

            public bool Contains(KeyValuePair<TSecond, TFirst> item) {
                return _owner._secondToFirst.Contains(item);
            }

            public bool TryGetValue(TSecond key, out TFirst value) {
                return _owner._secondToFirst.TryGetValue(key, out value);
            }

            public bool Remove(TSecond key) {
                TFirst value;
                if (_owner._secondToFirst.TryGetValue(key, out value)) {
                    _owner._secondToFirst.Remove(key);
                    _owner._firstToSecond.Remove(value);
                    return true;
                }

                return false;
            }

            public bool Remove(KeyValuePair<TSecond, TFirst> item) {
                return _owner._secondToFirst.Remove(item);
            }

            public void Clear() {
                _owner._secondToFirst.Clear();
                _owner._firstToSecond.Clear();
            }

            public void CopyTo(KeyValuePair<TSecond, TFirst>[] array, int arrayIndex) {
                _owner._secondToFirst.CopyTo(array, arrayIndex);
            }

            IEnumerable<TSecond> IReadOnlyDictionary<TSecond, TFirst>.Keys =>
                ((IReadOnlyDictionary<TSecond, TFirst>) _owner._secondToFirst).Keys;

            IEnumerable<TFirst> IReadOnlyDictionary<TSecond, TFirst>.Values =>
                ((IReadOnlyDictionary<TSecond, TFirst>) _owner._secondToFirst).Values;

            void ICollection.CopyTo(Array array, int index) {
                ((IDictionary) _owner._secondToFirst).CopyTo(array, index);
            }
        }
    }

    internal class DictionaryDebugView<TKey, TValue> {
        readonly IDictionary<TKey, TValue> _dictionary;

        public DictionaryDebugView(IDictionary<TKey, TValue> dictionary) {
            if (dictionary == null)
                throw new ArgumentNullException("dictionary");
            _dictionary = dictionary;
        }

        public KeyValuePair<TKey, TValue>[] Items {
            get {
                KeyValuePair<TKey, TValue>[] array = new KeyValuePair<TKey, TValue>[_dictionary.Count];
                _dictionary.CopyTo(array, 0);
                return array;
            }
        }
    }

    public static class KeyValuePairExts {
        public static KeyValuePair<TValue, TKey> Reverse<TKey, TValue>(this KeyValuePair<TKey, TValue> @this) {
            return new KeyValuePair<TValue, TKey>(@this.Value, @this.Key);
        }
    }
}