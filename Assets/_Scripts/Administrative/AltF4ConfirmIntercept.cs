using NovaMenuUI;
using UnityEditor;
using UnityEngine;
using UnityEngine.Diagnostics;

// Yeah yeah anti-patterns, sue me if it works
public static class AltF4ConfirmIntercept {
    public static bool userHasConfirmed = false;

    [RuntimeInitializeOnLoadMethod]
    static void RunOnStart() {
        Application.wantsToQuit += QuitIntercept;
    }

    private static bool QuitIntercept() {
        if (!userHasConfirmed) {
            userHasConfirmed = true;
            
            NovaPauseMenu.instance.OpenPauseMenu(NovaPauseMenu.instance.currentMenuState != NovaPauseMenu.MenuState.Off);
            NovaPauseMenu.instance.ExitGameDialogWindow.Open();
            
            return false;
        }

        return true;
    }
}
