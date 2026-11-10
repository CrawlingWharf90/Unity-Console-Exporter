#if UNITY_6000_3_OR_NEWER

using UnityEditor;


namespace CrawlingWharf90.ConsoleExporter
{
// Compiled ONLY on Unity 6000.3 and newer (includes Unity 6.5).
// Uses the EntityId API that replaced the int InstanceID.
//
// The cutoff above is the one place to adjust if EntityId turns out to be
// available (or required) on a different version than 6000.3.

internal sealed class ConsoleBackendUnity6000_3Plus : ConsoleBackendBase
{
    public override string Name => "Unity 6000.3+ (EntityId)";

    protected override object GetWindowId(EditorWindow window)
    {
        return window.GetEntityId();
    }
}
}

#endif
