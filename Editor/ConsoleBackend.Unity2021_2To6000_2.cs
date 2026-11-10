#if UNITY_2021_2_OR_NEWER && !UNITY_6000_3_OR_NEWER

using UnityEditor;


namespace CrawlingWharf90.ConsoleExporter
{
// Compiled ONLY on Unity 2021.2 up to (but not including) 6000.3.
// Uses the classic int InstanceID. 2021.2 is the floor because the rest of
// the exporter uses C# 9 syntax.
//
// Exactly one ConsoleBackend.*.cs file compiles for any given Unity version:
//   UNITY_6000_3_OR_NEWER                              -> Unity6000_3Plus
//   UNITY_2021_2_OR_NEWER && !UNITY_6000_3_OR_NEWER    -> this file
// If a version in this range needs different Console reflection, override
// the relevant member of ConsoleBackendBase here.

internal sealed class ConsoleBackendUnity2021_2To6000_2 : ConsoleBackendBase
{
    public override string Name => "Unity 2021.2 - 6000.2 (InstanceID)";

    protected override object GetWindowId(EditorWindow window)
    {
        return window.GetInstanceID();
    }
}
}

#endif
