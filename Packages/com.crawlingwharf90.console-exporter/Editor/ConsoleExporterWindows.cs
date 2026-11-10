using System.Collections.Generic;

using UnityEditor;
using UnityEngine;


namespace CrawlingWharf90.ConsoleExporter
{
public class ConsoleExportSettingsWindow : EditorWindow
{
    private ConsoleLogExporter.ExportFilter selectedFilter;
    private bool selectOnExport;

    public static void Open()
    {
        ConsoleExportSettingsWindow window =
            GetWindow<ConsoleExportSettingsWindow>(
                true,
                "Console Export Settings",
                true
            );

        window.minSize = new Vector2(360, 280);
        window.maxSize = new Vector2(360, 280);

        window.selectedFilter = ConsoleLogExporter.CurrentFilter;
        window.selectOnExport = ConsoleLogExporter.SelectOnExport;

        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(12);

        EditorGUILayout.LabelField(
            "Export Configuration",
            EditorStyles.boldLabel
        );

        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField("Log Filtering");

        selectedFilter =
            (ConsoleLogExporter.ExportFilter)EditorGUILayout.EnumPopup(
                selectedFilter
            );

        EditorGUILayout.Space(6);

        EditorGUILayout.HelpBox(
            GetDescription(selectedFilter),
            MessageType.Info
        );

        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField(
            "Export Behaviour",
            EditorStyles.boldLabel
        );

        selectOnExport = EditorGUILayout.ToggleLeft(
            new GUIContent(
                "Select On Export",
                "Choose individual Console entries before exporting."
            ),
            selectOnExport
        );

        EditorGUILayout.HelpBox(
            selectOnExport
                ? "A selection window will open before exporting. Only the entries you select will be exported."
                : "All entries matching the selected filter will be exported immediately.",
            MessageType.None
        );

        EditorGUILayout.Space(12);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Reset"))
            {
                selectedFilter =
                    ConsoleLogExporter.ExportFilter.FullConsole;

                selectOnExport = false;
            }

            if (GUILayout.Button("Cancel"))
                Close();

            if (GUILayout.Button("Apply"))
            {
                ConsoleLogExporter.CurrentFilter = selectedFilter;
                ConsoleLogExporter.SelectOnExport = selectOnExport;

                Close();
            }
        }
    }

    private string GetDescription(
        ConsoleLogExporter.ExportFilter filter)
    {
        switch (filter)
        {
            case ConsoleLogExporter.ExportFilter.FullConsole:
                return "Exports the complete Editor log in TXT. Other formats export all available Console entries.";

            case ConsoleLogExporter.ExportFilter.MinimalConsole:
                return "Exports informational messages, warnings and errors.";

            case ConsoleLogExporter.ExportFilter.WarningsAndErrors:
                return "Exports warnings and errors, excluding informational messages.";

            case ConsoleLogExporter.ExportFilter.InfoOnly:
                return "Exports informational messages, such as Debug.Log.";

            case ConsoleLogExporter.ExportFilter.WarningsOnly:
                return "Exports warnings only.";

            case ConsoleLogExporter.ExportFilter.ErrorsOnly:
                return "Exports errors only.";

            default:
                return "";
        }
    }
}


public class ConsoleEntrySelectionWindow : EditorWindow
{
    private List<ConsoleLogExporter.ConsoleEntry> entries;
    private bool[] selected;

    private Vector2 scroll;

    private ConsoleLogExporter.ExportFilter filter;
    private ConsoleLogExporter.ExportFormat format;

    public static void Open(
        List<ConsoleLogExporter.ConsoleEntry> entries,
        ConsoleLogExporter.ExportFilter filter,
        ConsoleLogExporter.ExportFormat format)
    {
        ConsoleEntrySelectionWindow window =
            CreateInstance<ConsoleEntrySelectionWindow>();

        window.entries = entries;
        window.filter = filter;
        window.format = format;

        window.selected = new bool[entries.Count];

        for (int i = 0; i < entries.Count; i++)
            window.selected[i] = true;

        window.titleContent =
            new GUIContent("Select Console Entries");

        window.minSize = new Vector2(550, 400);

        window.ShowUtility();
    }

    private void OnGUI()
    {
        if (entries == null)
            return;

        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField(
            $"Select Console Entries ({entries.Count})",
            EditorStyles.boldLabel
        );

        EditorGUILayout.Space(5);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Select All"))
                SetAll(true);

            if (GUILayout.Button("Deselect All"))
                SetAll(false);
        }

        EditorGUILayout.Space(5);

        scroll = EditorGUILayout.BeginScrollView(scroll);

        for (int i = 0; i < entries.Count; i++)
        {
            ConsoleLogExporter.ConsoleEntry entry = entries[i];

            using (new EditorGUILayout.VerticalScope(
                       EditorStyles.helpBox))
            {
                string firstLine =
                    entry.message.Split('\n')[0];

                selected[i] = EditorGUILayout.ToggleLeft(
                    $"[{entry.type}] {firstLine}",
                    selected[i]
                );

                EditorGUILayout.LabelField(
                    entry.timestamp,
                    EditorStyles.miniLabel
                );

                if (!string.IsNullOrEmpty(entry.file))
                {
                    EditorGUILayout.LabelField(
                        $"{entry.file}:{entry.line}",
                        EditorStyles.miniLabel
                    );
                }
            }
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(8);

        int selectedCount = CountSelected();

        EditorGUILayout.LabelField(
            $"Selected: {selectedCount} / {entries.Count}"
        );

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Cancel"))
                Close();

            using (new EditorGUI.DisabledScope(selectedCount == 0))
            {
                if (GUILayout.Button("Export Selected"))
                {
                    List<ConsoleLogExporter.ConsoleEntry> result =
                        new List<ConsoleLogExporter.ConsoleEntry>();

                    for (int i = 0; i < entries.Count; i++)
                    {
                        if (selected[i])
                            result.Add(entries[i]);
                    }

                    ConsoleLogExporter.ExportSelectedEntries(
                        result,
                        filter,
                        format
                    );

                    Close();
                }
            }
        }
    }

    private int CountSelected()
    {
        int count = 0;

        foreach (bool value in selected)
        {
            if (value)
                count++;
        }

        return count;
    }

    private void SetAll(bool value)
    {
        for (int i = 0; i < selected.Length; i++)
            selected[i] = value;
    }
}


public class ConsolePresetManager : EditorWindow
{
    private bool createMode;
    private string presetName = "My Preset";

    public static void Open(bool create = false)
    {
        ConsolePresetManager window =
            GetWindow<ConsolePresetManager>(
                true,
                "Console Presets",
                true
            );

        window.createMode = create;
        window.presetName = "My Preset";

        window.minSize = new Vector2(380, 300);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);

        if (createMode)
            DrawCreatePreset();
        else
            DrawPresetList();
    }

    private void DrawCreatePreset()
    {
        EditorGUILayout.LabelField(
            "Save Current Configuration",
            EditorStyles.boldLabel
        );

        EditorGUILayout.Space(10);

        presetName = EditorGUILayout.TextField(
            "Preset Name",
            presetName
        );

        EditorGUILayout.Space(10);

        using (new EditorGUI.DisabledScope(
                   string.IsNullOrWhiteSpace(presetName)))
        {
            if (GUILayout.Button("Save Preset"))
            {
                var collection =
                    ConsoleLogExporter.LoadPresets();

                collection.presets.Add(
                    new ConsoleExportPreset
                    {
                        name = presetName.Trim(),

                        filter = (int)
                            ConsoleLogExporter.CurrentFilter,

                        format = (int)
                            ConsoleLogExporter.CurrentFormat,

                        selectOnExport =
                            ConsoleLogExporter.SelectOnExport
                    }
                );

                ConsoleLogExporter.SavePresets(collection);

                Close();
            }
        }

        if (GUILayout.Button("Cancel"))
            Close();
    }

    private void DrawPresetList()
    {
        EditorGUILayout.LabelField(
            "Saved Presets",
            EditorStyles.boldLabel
        );

        EditorGUILayout.Space(8);

        var collection = ConsoleLogExporter.LoadPresets();

        for (int i = 0; i < collection.presets.Count; i++)
        {
            var preset = collection.presets[i];

            using (new EditorGUILayout.HorizontalScope())
            {
                preset.name = EditorGUILayout.TextField(
                    preset.name
                );

                if (GUILayout.Button(
                        "Delete",
                        GUILayout.Width(60)))
                {
                    collection.presets.RemoveAt(i);

                    ConsoleLogExporter.SavePresets(collection);

                    GUIUtility.ExitGUI();
                }
            }
        }

        if (GUI.changed)
            ConsoleLogExporter.SavePresets(collection);

        EditorGUILayout.Space(12);

        if (GUILayout.Button("Create New Preset"))
        {
            createMode = true;
            presetName = "My Preset";
        }

        if (GUILayout.Button("Close"))
            Close();
    }
}
}
