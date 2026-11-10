using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

using UnityEditor;
using UnityEngine;


namespace CrawlingWharf90.ConsoleExporter
{
internal abstract class ConsoleBackendBase
{
    private const string ConsoleWindowTypeName = "UnityEditor.ConsoleWindow";
    private const string CallbackName = "drawCustomToolbarGui";

    private FieldInfo callbackField;
    private readonly HashSet<object> installedWindows = new HashSet<object>();

    private bool installedStatic;
    private bool warned;

    /// <summary>Human readable name, shown by "Log Backend Info".</summary>
    public abstract string Name { get; }

    /// <summary>
    /// A stable identity for an editor window. This is the API that differs
    /// between versions (InstanceID int vs EntityId).
    /// </summary>
    protected abstract object GetWindowId(EditorWindow window);

    // ---------------------------------------------------------
    // TOOLBAR HOOK
    // ---------------------------------------------------------

    public void TryInstall(Action drawToolbar)
    {
        Type consoleType = typeof(EditorWindow)
            .Assembly.GetType(ConsoleWindowTypeName);

        if (consoleType == null)
            return;

        if (callbackField == null)
        {
            callbackField = consoleType.GetField(
                CallbackName,
                BindingFlags.Static |
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic
            );

            if (callbackField == null)
            {
                if (!warned)
                {
                    Debug.LogWarning(
                        "[ConsoleExporter] Console toolbar callback not found " +
                        $"(backend: {Name}, Unity {Application.unityVersion})."
                    );

                    warned = true;
                }

                return;
            }
        }

        if (callbackField.IsStatic)
        {
            if (!installedStatic)
            {
                InstallCallback(null, drawToolbar);
                installedStatic = true;
            }
        }
        else
        {
            foreach (UnityEngine.Object found in
                     Resources.FindObjectsOfTypeAll(consoleType))
            {
                EditorWindow window = found as EditorWindow;

                if (window == null)
                    continue;

                if (!installedWindows.Add(GetWindowId(window)))
                    continue;

                InstallCallback(window, drawToolbar);
            }
        }
    }

    private void InstallCallback(object target, Action drawToolbar)
    {
        try
        {
            Delegate callback = Delegate.CreateDelegate(
                callbackField.FieldType,
                drawToolbar.Method
            );

            Delegate existing = callbackField.GetValue(target) as Delegate;

            if (existing != null)
            {
                foreach (Delegate item in existing.GetInvocationList())
                {
                    if (item.Equals(callback))
                        return;
                }
            }

            callbackField.SetValue(
                target,
                Delegate.Combine(existing, callback)
            );
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    // ---------------------------------------------------------
    // CONSOLE ENTRY RETRIEVAL
    // ---------------------------------------------------------

    public List<ConsoleLogExporter.ConsoleEntry> ReadEntries(
        Func<string, bool> accept)
    {
        List<ConsoleLogExporter.ConsoleEntry> result =
            new List<ConsoleLogExporter.ConsoleEntry>();

        Assembly editorAssembly = typeof(EditorWindow).Assembly;

        Type entriesType = editorAssembly.GetType("UnityEditor.LogEntries");
        Type entryType = editorAssembly.GetType("UnityEditor.LogEntry");

        if (entriesType == null || entryType == null)
            throw new Exception(
                "Unity's internal Console API could not be found."
            );

        const BindingFlags flags =
            BindingFlags.Static |
            BindingFlags.Public |
            BindingFlags.NonPublic;

        MethodInfo startMethod = entriesType.GetMethod(
            "StartGettingEntries", flags);

        MethodInfo endMethod = entriesType.GetMethod(
            "EndGettingEntries", flags);

        MethodInfo countMethod = entriesType.GetMethod(
            "GetCount", flags);

        MethodInfo getEntryMethod = entriesType.GetMethod(
            "GetEntryInternal", flags);

        MethodInfo timestampMethod = entriesType.GetMethod(
            "GetEntryTimestampInternal", flags);

        if (startMethod == null ||
            endMethod == null ||
            countMethod == null ||
            getEntryMethod == null)
        {
            throw new Exception(
                "Unity's Console entry methods have changed " +
                $"(backend: {Name}, Unity {Application.unityVersion})."
            );
        }

        int count = (int)startMethod.Invoke(null, null);

        try
        {
            FieldInfo messageField = entryType.GetField("message");
            FieldInfo modeField = entryType.GetField("mode");
            FieldInfo fileField = entryType.GetField("file");
            FieldInfo lineField = entryType.GetField("line");

            if (messageField == null || modeField == null)
                throw new Exception(
                    "Could not access Unity Console entry fields."
                );

            object entry = Activator.CreateInstance(entryType);

            for (int i = 0; i < count; i++)
            {
                object[] arguments = { i, entry };

                bool success = (bool)getEntryMethod.Invoke(null, arguments);

                entry = arguments[1];

                if (!success)
                    continue;

                int mode = (int)modeField.GetValue(entry);

                string type = GetSeverity(mode);

                if (!accept(type))
                    continue;

                string timestamp = "";

                if (timestampMethod != null)
                {
                    object[] timestampArguments = { i, null };

                    timestampMethod.Invoke(null, timestampArguments);

                    timestamp = ExtractTimestamp(
                        timestampArguments[1] as string);
                }

                result.Add(new ConsoleLogExporter.ConsoleEntry
                {
                    timestamp = timestamp,
                    type = type,
                    message = messageField.GetValue(entry) as string ?? "",
                    file = fileField?.GetValue(entry) as string ?? "",
                    line = lineField != null
                        ? (int)lineField.GetValue(entry)
                        : 0
                });
            }
        }
        finally
        {
            endMethod.Invoke(null, null);
        }

        return result;
    }

    private static readonly Regex LeadingTimestamp =
        new Regex(@"^\s*\[(\d{1,2}:\d{2}:\d{2}(?:[.,]\d+)?)\]");

    /// <summary>
    /// On current Unity versions the "timestamp" call does not return just the
    /// time: it returns the whole formatted entry, e.g.
    /// "[16:35:23] message...\nstack trace...". Keeping all of that in the
    /// timestamp field is what made every message appear twice in the export.
    /// This keeps only the time. Virtual so a version backend can change it.
    /// </summary>
    protected virtual string ExtractTimestamp(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return "";

        Match match = LeadingTimestamp.Match(raw);

        if (match.Success)
            return match.Groups[1].Value;

        // Older versions may return a plain, short, single-line timestamp.
        if (raw.Length <= 40 && raw.IndexOf('\n') < 0)
            return raw.Trim();

        return "";
    }

    /// <summary>
    /// Maps Unity's internal LogMessageFlags to Error / Warning / Info.
    /// Virtual because the bit layout is an internal detail that a future
    /// Unity version could change.
    /// </summary>
    protected virtual string GetSeverity(int mode)
    {
        const int errors =
            (1 << 0) |    // Error
            (1 << 1) |    // Assert
            (1 << 4) |    // Fatal
            (1 << 6) |    // Asset import error
            (1 << 8) |    // Scripting error
            (1 << 11) |   // Script compile error
            (1 << 17) |   // Scripting exception
            (1 << 21);    // Scripting assertion

        const int warnings =
            (1 << 7) |    // Asset import warning
            (1 << 9) |    // Scripting warning
            (1 << 12);    // Script compile warning

        const int info =
            (1 << 2) |    // General log
            (1 << 10);    // Scripting log

        if ((mode & errors) != 0)
            return "Error";

        if ((mode & warnings) != 0)
            return "Warning";

        if ((mode & info) != 0)
            return "Info";

        return "Other";
    }
}
}
