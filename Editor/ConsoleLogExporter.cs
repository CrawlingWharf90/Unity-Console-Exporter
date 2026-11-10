using System;
using System.Collections.Generic;
using System.IO;

using UnityEditor;
using UnityEngine;


namespace CrawlingWharf90.ConsoleExporter
{
[Serializable]
public class ConsoleExportPreset
{
    public string name;
    public int filter;
    public int format;
    public bool selectOnExport;
}

[Serializable]
public class ConsoleExportPresetCollection
{
    public List<ConsoleExportPreset> presets = new List<ConsoleExportPreset>();
}

[InitializeOnLoad]
public static partial class ConsoleLogExporter
{
    private const string FilterKey = "ConsoleExporter.Filter";
    private const string FormatKey = "ConsoleExporter.Format";
    private const string SelectOnExportKey = "ConsoleExporter.SelectOnExport";
    private static string PresetKey => "ConsoleExporter.Presets." + Application.dataPath;

    private static readonly ConsoleBackendBase backend;
    private static readonly Action toolbarCallback;

    // ---------------------------------------------------------
    // SETTINGS
    // ---------------------------------------------------------

    public enum ExportFilter
    {
        FullConsole,
        MinimalConsole,
        WarningsAndErrors,
        InfoOnly,
        WarningsOnly,
        ErrorsOnly
    }

    public enum ExportFormat
    {
        TXT,
        JSON,
        CSV,
        XML
    }

    public static ExportFilter CurrentFilter
    {
        get => (ExportFilter)EditorPrefs.GetInt(
            FilterKey, (int)ExportFilter.FullConsole);

        set => EditorPrefs.SetInt(FilterKey, (int)value);
    }

    public static ExportFormat CurrentFormat
    {
        get => (ExportFormat)EditorPrefs.GetInt(
            FormatKey, (int)ExportFormat.TXT);

        set => EditorPrefs.SetInt(FormatKey, (int)value);
    }

    public static bool SelectOnExport
    {
        get => EditorPrefs.GetBool(SelectOnExportKey, false);
        set => EditorPrefs.SetBool(SelectOnExportKey, value);
    }

    // ---------------------------------------------------------
    // PRESETS
    // ---------------------------------------------------------

    internal static ConsoleExportPresetCollection LoadPresets()
    {
        string json = EditorPrefs.GetString(PresetKey, "");

        if (string.IsNullOrEmpty(json))
            return new ConsoleExportPresetCollection();

        return JsonUtility.FromJson<ConsoleExportPresetCollection>(json)
            ?? new ConsoleExportPresetCollection();
    }

    internal static void SavePresets(ConsoleExportPresetCollection collection)
    {
        EditorPrefs.SetString(
            PresetKey,
            JsonUtility.ToJson(collection)
        );
    }

    internal static void SaveCurrentPreset()
    {
        ConsolePresetManager.Open(true);
    }

    internal static void ApplyPreset(ConsoleExportPreset preset)
    {
        CurrentFilter = (ExportFilter)preset.filter;
        CurrentFormat = (ExportFormat)preset.format;
        SelectOnExport = preset.selectOnExport;
    }

    // ---------------------------------------------------------
    // INITIALIZATION / VERSION BACKEND
    // ---------------------------------------------------------

    static ConsoleLogExporter()
    {
        backend = ResolveBackend();
        toolbarCallback = DrawToolbar;

        if (backend != null)
            EditorApplication.update += TryInstall;
    }

    /// <summary>
    /// Finds the single ConsoleBackendBase implementation that was compiled
    /// for the Unity version currently running.
    /// </summary>
    private static ConsoleBackendBase ResolveBackend()
    {
        Type match = null;
        int count = 0;

        foreach (Type type in TypeCache.GetTypesDerivedFrom<ConsoleBackendBase>())
        {
            if (type.IsAbstract)
                continue;

            match = type;
            count++;
        }

        if (count != 1)
        {
            Debug.LogError(
                $"[ConsoleExporter] Expected exactly one Console backend for " +
                $"Unity {Application.unityVersion}, found {count}. " +
                "Check the #if conditions in the ConsoleBackend.*.cs files."
            );

            return null;
        }

        try
        {
            return (ConsoleBackendBase)Activator.CreateInstance(match, true);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            return null;
        }
    }

    private static void TryInstall()
    {
        backend.TryInstall(toolbarCallback);
    }

    [MenuItem("Tools/Console Exporter/Log Backend Info")]
    private static void LogBackendInfo()
    {
        Debug.Log(
            $"[ConsoleExporter] Running on Unity {Application.unityVersion}. " +
            $"Active backend: {(backend != null ? backend.Name : "none")}"
        );
    }

    // ---------------------------------------------------------
    // EXPORT ENTRY POINTS
    // ---------------------------------------------------------

    internal static void OpenSettings()
    {
        ConsoleExportSettingsWindow.Open();
    }

    internal static void ExportConsole()
    {
        Export(CurrentFilter, CurrentFormat);
    }

    internal static void SwitchFormat(ExportFormat format, bool exportInmediately)
    {
        CurrentFormat = format;

        if (exportInmediately)
            Export(CurrentFilter, format);
    }

    private static void Export(ExportFilter filter, ExportFormat format)
    {
        if (SelectOnExport)
        {
            List<ConsoleEntry> entries = GetConsoleEntries(filter);

            ConsoleEntrySelectionWindow.Open(
                entries,
                filter,
                format
            );

            return;
        }

        string extension = format.ToString().ToLowerInvariant();

        string fileName =
            $"UnityConsole_{DateTime.Now:yyyyMMdd_HHmmss}.{extension}";

        string destination = EditorUtility.SaveFilePanel(
            "Export Unity Console",
            Environment.GetFolderPath(
                Environment.SpecialFolder.Desktop),
            fileName,
            extension
        );

        if (string.IsNullOrEmpty(destination))
            return;

        try
        {
            // Preserve the original full-log export behavior.
            if (filter == ExportFilter.FullConsole &&
                format == ExportFormat.TXT)
            {
                ExportFullEditorLog(destination);
            }
            else
            {
                List<ConsoleEntry> entries = GetConsoleEntries(filter);
                WriteEntries(destination, entries, filter, format);
            }

            EditorUtility.DisplayDialog(
                "Export Successful",
                $"Console exported successfully!\n\n{destination}",
                "OK"
            );
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);

            EditorUtility.DisplayDialog(
                "Export Failed",
                exception.Message,
                "OK"
            );
        }
    }

    internal static void ExportSelectedEntries(
        List<ConsoleEntry> entries,
        ExportFilter filter,
        ExportFormat format)
    {
        string extension = format.ToString().ToLowerInvariant();

        string fileName =
            $"UnityConsole_Selected_{DateTime.Now:yyyyMMdd_HHmmss}.{extension}";

        string destination = EditorUtility.SaveFilePanel(
            "Export Selected Console Entries",
            Environment.GetFolderPath(
                Environment.SpecialFolder.Desktop),
            fileName,
            extension
        );

        if (string.IsNullOrEmpty(destination))
            return;

        try
        {
            WriteEntries(destination, entries, filter, format);

            EditorUtility.DisplayDialog(
                "Export Successful",
                $"Exported {entries.Count} selected entries.",
                "OK"
            );
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);

            EditorUtility.DisplayDialog(
                "Export Failed",
                exception.Message,
                "OK"
            );
        }
    }

    private static void WriteEntries(
        string destination,
        List<ConsoleEntry> entries,
        ExportFilter filter,
        ExportFormat format)
    {
        switch (format)
        {
            case ExportFormat.TXT:
                ExportTxt(destination, entries);
                break;

            case ExportFormat.JSON:
                ExportJson(destination, entries, filter);
                break;

            case ExportFormat.CSV:
                ExportCsv(destination, entries);
                break;

            case ExportFormat.XML:
                ExportXml(destination, entries, filter);
                break;
        }
    }

    private static List<ConsoleEntry> GetConsoleEntries(ExportFilter filter)
    {
        if (backend == null)
            throw new InvalidOperationException(
                "No Console backend is available for Unity " +
                Application.unityVersion + "."
            );

        return backend.ReadEntries(type => ShouldExport(type, filter));
    }

    private static void ExportFullEditorLog(string destination)
    {
        string source = Application.consoleLogPath;

        if (string.IsNullOrEmpty(source) || !File.Exists(source))
            throw new FileNotFoundException(
                "Could not locate the Unity Editor log.",
                source
            );

        File.Copy(source, destination, true);
    }

    private static bool ShouldExport(string type, ExportFilter filter)
    {
        switch (filter)
        {
            case ExportFilter.FullConsole:
                return true;

            case ExportFilter.MinimalConsole:
                return type == "Info" ||
                       type == "Warning" ||
                       type == "Error";

            case ExportFilter.WarningsAndErrors:
                return type == "Warning" ||
                       type == "Error";

            case ExportFilter.InfoOnly:
                return type == "Info";

            case ExportFilter.WarningsOnly:
                return type == "Warning";

            case ExportFilter.ErrorsOnly:
                return type == "Error";

            default:
                return true;
        }
    }
}
}
