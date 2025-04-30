

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SuperspectiveUtils {
    /// <summary>
    /// Collections which will automatically purge null references from the collection before iterating over it.
    /// This comes at a performance cost, but is useful for collections of UnityEngine.Objects,
    /// especially for collections used in low-level APIs such as CommandBuffers to avoid crashes.
    /// </summary>
    public abstract class NullSafeCollectionBase<T> where T : UnityEngine.Object {
        protected readonly Func<T, bool> IsValid;

        internal NullSafeCollectionBase(Func<T, bool> isValid = null) {
            IsValid = isValid ?? (obj => obj);
        }

        protected bool IsInvalid(T obj) => !IsValid(obj);
    }

    // --- NullSafeList<T> ---
    public class NullSafeList<T> : NullSafeCollectionBase<T>, IEnumerable<T> where T : UnityEngine.Object {
        private readonly List<T> backing = new();

        public NullSafeList(Func<T, bool> isValid = null) : base(isValid) { }
        
        public static implicit operator NullSafeList<T>(List<T> list) {
            var nullSafeList = new NullSafeList<T>();
            foreach (var item in list) {
                nullSafeList.Add(item);
            }
            return nullSafeList;
        }

        private void Clean() {
            backing.RemoveAll(IsInvalid);
        }

        public void Add(T item) => backing.Add(item);

        public bool Remove(T item) {
            Clean();
            return backing.Remove(item);
        }

        public void Clear() => backing.Clear();

        public bool Contains(T item) {
            Clean();
            return backing.Contains(item);
        }

        public int Count {
            get {
                Clean();
                return backing.Count;
            }
        }

        public IEnumerator<T> GetEnumerator() {
            var snapshot = new List<T>(backing);
            foreach (var item in snapshot) {
                if (IsInvalid(item)) {
                    backing.Remove(item);
                    continue;
                }
                yield return item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    // --- NullSafeHashSet<T> ---
    public class NullSafeHashSet<T> : NullSafeCollectionBase<T>, IEnumerable<T> where T : UnityEngine.Object {
        private readonly HashSet<T> backing = new();

        public NullSafeHashSet(Func<T, bool> isValid = null) : base(isValid) { }

        private void Clean() {
            var toRemove = new List<T>();
            foreach (var item in backing) {
                if (IsInvalid(item)) toRemove.Add(item);
            }
            foreach (var item in toRemove) backing.Remove(item);
        }

        public void Add(T item) => backing.Add(item);

        public bool Remove(T item) {
            Clean();
            return backing.Remove(item);
        }

        public void Clear() => backing.Clear();

        public bool Contains(T item) {
            Clean();
            return backing.Contains(item);
        }

        public int Count {
            get {
                Clean();
                return backing.Count;
            }
        }

        public IEnumerator<T> GetEnumerator() {
            var snapshot = new List<T>(backing);
            foreach (var item in snapshot) {
                if (IsInvalid(item)) {
                    backing.Remove(item);
                    continue;
                }
                yield return item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    // --- NullSafeDictionary<TKey, TValue> ---
    public class NullSafeDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
        where TKey : UnityEngine.Object
        where TValue : UnityEngine.Object {

        private readonly Dictionary<TKey, TValue> backing = new();
        private readonly Func<TKey, bool> isKeyValid;
        private readonly Func<TValue, bool> isValueValid;

        public NullSafeDictionary(
            Func<TKey, bool> isKeyValid = null,
            Func<TValue, bool> isValueValid = null) {
            this.isKeyValid = isKeyValid ?? (k => k != null);
            this.isValueValid = isValueValid ?? (v => v != null);
        }

        private bool IsInvalid(TKey key) => !isKeyValid(key) || !isValueValid(backing.TryGetValue(key, out var v) ? v : null);

        private void Clean() {
            var toRemove = new List<TKey>();
            foreach (var kvp in backing) {
                if (!isKeyValid(kvp.Key) || !isValueValid(kvp.Value)) {
                    toRemove.Add(kvp.Key);
                }
            }
            foreach (var key in toRemove) backing.Remove(key);
        }

        public void Add(TKey key, TValue value) => backing.Add(key, value);

        public bool Remove(TKey key) {
            Clean();
            return backing.Remove(key);
        }

        public bool TryGetValue(TKey key, out TValue value) {
            Clean();
            return backing.TryGetValue(key, out value);
        }

        public bool ContainsKey(TKey key) {
            Clean();
            return backing.ContainsKey(key);
        }

        public void Clear() => backing.Clear();

        public int Count {
            get {
                Clean();
                return backing.Count;
            }
        }

        public TValue this[TKey key] {
            get {
                Clean();
                return backing[key];
            }
            set {
                Clean();
                backing[key] = value;
            }
        }
        
        public IEnumerable<TKey> Keys {
            get {
                var keys = new List<TKey>(backing.Keys);
                foreach (var key in keys) {
                    if (!isKeyValid(key) || !isValueValid(backing[key])) {
                        backing.Remove(key);
                        continue;
                    }
                    yield return key;
                }
            }
        }

        public IEnumerable<TValue> Values {
            get {
                var keys = new List<TKey>(backing.Keys);
                foreach (var key in keys) {
                    if (!isKeyValid(key) || !isValueValid(backing[key])) {
                        backing.Remove(key);
                        continue;
                    }
                    yield return backing[key];
                }
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
            var keys = new List<TKey>(backing.Keys);
            foreach (var key in keys) {
                if (!isKeyValid(key) || !isValueValid(backing[key])) {
                    backing.Remove(key);
                    continue;
                }
                yield return new KeyValuePair<TKey, TValue>(key, backing[key]);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    
    // --- NullSafeValueDictionary<TKey, TValue> --- For when only the values need to be checked
    public class NullSafeValueDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
        where TValue : UnityEngine.Object {

        private readonly Dictionary<TKey, TValue> backing = new();
        private readonly Func<TValue, bool> isValueValid;

        public NullSafeValueDictionary(Func<TValue, bool> isValueValid = null) {
            this.isValueValid = isValueValid ?? (v => v != null);
        }

        private void Clean() {
            var toRemove = new List<TKey>();
            foreach (var kvp in backing) {
                if (!isValueValid(kvp.Value)) {
                    toRemove.Add(kvp.Key);
                }
            }
            foreach (var key in toRemove) {
                backing.Remove(key);
            }
        }

        public void Add(TKey key, TValue value) => backing.Add(key, value);

        public bool Remove(TKey key) {
            Clean();
            return backing.Remove(key);
        }

        public bool TryGetValue(TKey key, out TValue value) {
            Clean();
            return backing.TryGetValue(key, out value);
        }

        public bool ContainsKey(TKey key) {
            Clean();
            return backing.ContainsKey(key);
        }

        public void Clear() => backing.Clear();

        public int Count {
            get {
                Clean();
                return backing.Count;
            }
        }

        public TValue this[TKey key] {
            get {
                Clean();
                return backing[key];
            }
            set {
                Clean();
                backing[key] = value;
            }
        }

        public IEnumerable<TKey> Keys {
            get {
                var keys = new List<TKey>(backing.Keys);
                foreach (var key in keys) {
                    if (!isValueValid(backing[key])) {
                        backing.Remove(key);
                        continue;
                    }
                    yield return key;
                }
            }
        }

        public IEnumerable<TValue> Values {
            get {
                var keys = new List<TKey>(backing.Keys);
                foreach (var key in keys) {
                    if (!isValueValid(backing[key])) {
                        backing.Remove(key);
                        continue;
                    }
                    yield return backing[key];
                }
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
            var keys = new List<TKey>(backing.Keys);
            foreach (var key in keys) {
                if (!isValueValid(backing[key])) {
                    backing.Remove(key);
                    continue;
                }
                yield return new KeyValuePair<TKey, TValue>(key, backing[key]);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
