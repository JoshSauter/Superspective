using UnityEngine;
#if UNITY_EDITOR
#if UNITY_2021_1_OR_NEWER
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
#else
using UnityEditor.Experimental.SceneManagement;
using UnityEngine.SceneManagement;
#endif
#endif

namespace Nova.Compat
{
    internal static class PrefabStageUtils
    {
#if UNITY_EDITOR
        private static PrefabStage currentPrefabStage = null;
#endif

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public static bool IsInPrefabStage =>
#if UNITY_EDITOR
            currentPrefabStage != null;
#else
            false;
#endif

#if UNITY_EDITOR
        public static bool TryGetPrefabScene(out Scene scene)
        {
            if (!IsInPrefabStage)
            {
                scene = default;
                return false;
            }

            scene = currentPrefabStage.scene;
            return true;
        }
#endif

        public static bool TryGetCurrentStageRoot(out GameObject root)
        {
#if UNITY_EDITOR
            if (currentPrefabStage != null)
            {
                root = currentPrefabStage.prefabContentsRoot;
                return true;
            }
            else
            {
                root = null;
                return false;
            }
#else
            root = null;
            return false;
#endif
        }

        public static void Init()
        {
#if UNITY_EDITOR
            PrefabStage.prefabStageOpened += HandlePrefabStageOpened;
            PrefabStage.prefabStageClosing += HandlePrefabStageClosed;
            currentPrefabStage = PrefabStageUtility.GetCurrentPrefabStage();
#endif
        }

#if UNITY_EDITOR
        private static void HandlePrefabStageOpened(PrefabStage obj)
        {
            currentPrefabStage = obj;
        }

        private static void HandlePrefabStageClosed(PrefabStage obj)
        {
            // We need to query the current prefab stage because if someone has recursed into 
            // several prefab stages, and then they exit one it goes back to the others without
            // firing an "open" event and we can't cache the previous prefab stages because if a domain
            // reload happens we'll lose the cache state
            currentPrefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        }
#endif
    }
}
