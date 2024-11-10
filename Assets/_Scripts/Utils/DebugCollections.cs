using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// For debugging purposes, these are classes which will do noops when built in a non-editor environment
namespace DebugCollections {
    [Serializable]
    public class DebugList<T> : List<T> {
        public new void Add(T item) {
            #if UNITY_EDITOR
            base.Add(item);
            #endif
        }
        
        public new void Remove(T item) {
            #if UNITY_EDITOR
            base.Remove(item);
            #endif
        }
    }
}