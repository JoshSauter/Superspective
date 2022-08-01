using System;
using JetBrains.Annotations;
using UnityEngine;

namespace SuperspectiveUtils {
    public abstract class Option<T> {
        public static Option<T> Of(T value) {
            if (value == null) {
                return new None<T>();
            }
            else {
                return new Some<T>(value);
            }
        }

        public abstract bool IsEmpty();
        public bool IsDefined() => !IsEmpty();
        public abstract T Get();
        public T GetOrElse(T defaultValue) => IsEmpty() ? defaultValue : Get();
        public Option<T2> Map<T2>(Func<T, T2> f) => IsEmpty() ? new None<T2>() : new Some<T2>(f.Invoke(Get()));
        public Option<T2> FlatMap<T2>(Func<T, Option<T2>> f) => IsEmpty() ? new None<T2>() : f.Invoke(Get());
        public Option<T> Filter(Func<T, bool> p) => (IsEmpty() || p.Invoke(Get())) ? this : new None<T>();
        public Option<T> FilterNot(Func<T, bool> p) => (IsEmpty() || !p.Invoke(Get())) ? this : new None<T>();
        public bool Contains(T element) => !IsEmpty() && Get().Equals(element);
        public bool Exists(Func<T, bool> p) => !IsEmpty() && p.Invoke(Get());
        public bool ForAll(Func<T, bool> p) => IsEmpty() || p.Invoke(Get());
        public void ForEach(Action<T> f) {
            if (!IsEmpty()) {
                f.Invoke(Get());
            }
        }
        public Option<T> OrElse(Option<T> alternative) => IsEmpty() ? alternative : this;
        
    }

    public class None<T> : Option<T> {
        public None() {}
        
        public override bool IsEmpty() => true;
        public override T Get() {
            throw new NullReferenceException("None.Get()");
        }
    }

    public class Some<T> : Option<T> {
        private readonly T value;
        public Some(T value) {
            this.value = value;
        }

        public override bool IsEmpty() => value == null;
        public override T Get() => value;
    }

    public static class OptionHelpers {
        public static Option<T> GetMaybeComponent<T>(this GameObject go) where T : Component {
            if (go.TryGetComponent(out T foundComponent)) {
                return new Some<T>(foundComponent);
            }
            else {
                return new None<T>();
            }
        }

        public static Option<T> GetMaybeComponent<T>(this Component component) where T : Component {
            return GetMaybeComponent<T>(component.gameObject);
        }
        
        public static Option<T> GetMaybeComponentInChildren<T>(this GameObject go) where T : Component {
            foreach (Transform child in go.transform) {
                if (go.TryGetComponent(out T foundComponent)) {
                    return new Some<T>(foundComponent);
                }
            }
            
            return new None<T>();
        }

        public static Option<T> GetMaybeComponentInChildren<T>(this Component component) where T : Component {
            return GetMaybeComponentInChildren<T>(component.gameObject);
        }
    }
}