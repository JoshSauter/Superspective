using System;
using UnityEngine;

namespace SuperspectiveUtils {
    public class LazyUtils {
        public static T Lazy<T>(ref T source, Func<T> provider, T flagValue = default) {
            if (source == null || source.Equals(flagValue)) {
                source = provider.Invoke();
            }

            return source;
        }
    }
}