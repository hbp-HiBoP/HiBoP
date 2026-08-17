using HBP.Core.DLL;
using UnityEditor;

[InitializeOnLoad]
internal static class DLLDebugManagerReloadGuard
{
    static DLLDebugManagerReloadGuard()
    {
        AssemblyReloadEvents.beforeAssemblyReload += DLLDebugManager.ResetNativeLoggers;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.quitting += OnEditorQuitting;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.ExitingPlayMode)
        {
            DLLDebugManager.ResetNativeLoggers();
        }
    }

    private static void OnEditorQuitting()
    {
        DLLDebugManager.ResetNativeLoggers();
    }
}
