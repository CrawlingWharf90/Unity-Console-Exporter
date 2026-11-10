using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;


namespace CrawlingWharf90.ConsoleExporter
{
public static partial class ConsoleLogExporter
{
    private static readonly GUIContent ButtonContent = new GUIContent(
        "Console Exporter",
        "Export the Unity Console"
    );

    /// <summary>
    /// Editor time at which the popup last closed. Used so that clicking the
    /// toolbar button while the popup is open closes it instead of instantly
    /// re-opening it.
    /// </summary>
    internal static double PopupClosedAt;

    private static void DrawToolbar()
    {
        // Reserve the button rect ourselves so we know exactly where it is,
        // instead of asking for "the last rect" afterwards.
        Rect buttonRect = GUILayoutUtility.GetRect(
            ButtonContent,
            EditorStyles.toolbarDropDown,
            GUILayout.Width(125)
        );

        if (EditorGUI.DropdownButton(
                buttonRect,
                ButtonContent,
                FocusType.Passive,
                EditorStyles.toolbarDropDown))
        {
            if (EditorApplication.timeSinceStartup - PopupClosedAt < 0.25)
                return;

            // buttonRect is in this window's GUI space, which is what
            // PopupWindow.Show expects. The popup opens right below it.
            PopupWindow.Show(buttonRect, new ConsoleExporterPopup());
        }
    }
}

internal sealed class ConsoleExporterPopup : PopupWindowContent
{
    private const float Width = 280f;
    private const float Pad = 8f;
    private const float Gap = 4f;
    private const float RowHeight = 22f;
    private const float LabelHeight = 16f;
    private const float SeparatorHeight = 10f;
    private const float PresetRowHeight = 22f;
    private const int MaxVisiblePresets = 6;

    private static readonly string[] FormatNames =
        Enum.GetNames(typeof(ConsoleLogExporter.ExportFormat));

    private readonly List<ConsoleExportPreset> presets;

    private Vector2 presetScroll;
    private Action pendingAction;
    private bool closing;

    public ConsoleExporterPopup()
    {
        presets = ConsoleLogExporter.LoadPresets().presets;
    }

    public override Vector2 GetWindowSize()
    {
        // Same layout pass as drawing, just without drawing anything,
        // so the window height always matches the content.
        return new Vector2(Width, Layout(false) + Pad - Gap);
    }

    public override void OnGUI(Rect rect)
    {
        Layout(true);
    }

    public override void OnClose()
    {
        ConsoleLogExporter.PopupClosedAt = EditorApplication.timeSinceStartup;

        // Anything that opens a window or a file dialog runs only after the
        // popup is gone, never from inside the popup's OnGUI.
        Action action = pendingAction;
        pendingAction = null;

        if (action != null)
            EditorApplication.delayCall += () => action();
    }

    // ---------------------------------------------------------
    // LAYOUT (manual rects, so measuring and drawing share one path)
    // ---------------------------------------------------------

    private float Layout(bool draw)
    {
        float w = Width - Pad * 2f;
        float y = Pad;

        // ----- Actions that open a dialog/window: close the popup -----

        if (Button(draw, ref y, w, "Export Console"))
            CloseThen(ConsoleLogExporter.ExportConsole);

        if (Button(draw, ref y, w, "Export Settings..."))
            CloseThen(ConsoleLogExporter.OpenSettings);

        Separator(draw, ref y, w);

        // ----- File type: popup stays open -----

        Label(draw, ref y, w, "File Type");

        Rect typeRect = NextRect(ref y, w, RowHeight);
        int current = (int)ConsoleLogExporter.CurrentFormat;

        if (draw && !closing)
        {
            int picked = GUI.Toolbar(
                typeRect, current, FormatNames, EditorStyles.toolbarButton);

            if (picked != current)
            {
                ConsoleLogExporter.CurrentFormat =
                    (ConsoleLogExporter.ExportFormat)picked;

                editorWindow.Repaint();
            }
        }

        // ----- Change type and export now: closes (file dialog) -----

        Label(draw, ref y, w, "Change Type and Export");

        Rect exportRect = NextRect(ref y, w, RowHeight);

        if (draw && !closing)
        {
            int clicked = GUI.Toolbar(
                exportRect, -1, FormatNames, EditorStyles.toolbarButton);

            if (clicked >= 0)
            {
                ConsoleLogExporter.ExportFormat format =
                    (ConsoleLogExporter.ExportFormat)clicked;

                CloseThen(() => ConsoleLogExporter.SwitchFormat(format, true));
            }
        }

        Separator(draw, ref y, w);

        // ----- Presets -----

        Label(draw, ref y, w, "Presets");

        if (presets.Count == 0)
        {
            Rect emptyRect = NextRect(ref y, w, LabelHeight);

            if (draw && !closing)
                GUI.Label(emptyRect, "No Saved Presets",
                    EditorStyles.centeredGreyMiniLabel);
        }
        else
        {
            int visible = Mathf.Min(presets.Count, MaxVisiblePresets);
            Rect viewRect = NextRect(ref y, w, visible * PresetRowHeight);

            if (draw && !closing)
                DrawPresetList(viewRect);
        }

        if (Button(draw, ref y, w, "Save Current Settings..."))
            CloseThen(ConsoleLogExporter.SaveCurrentPreset);

        if (Button(draw, ref y, w, "Manage Presets..."))
            CloseThen(() => ConsolePresetManager.Open());

        return y;
    }

    private void DrawPresetList(Rect viewRect)
    {
        bool scrolls = presets.Count > MaxVisiblePresets;

        Rect content = new Rect(
            0,
            0,
            viewRect.width - (scrolls ? 16f : 0f),
            presets.Count * PresetRowHeight
        );

        presetScroll = GUI.BeginScrollView(viewRect, presetScroll, content);

        for (int i = 0; i < presets.Count; i++)
        {
            Rect row = new Rect(
                0, i * PresetRowHeight, content.width, PresetRowHeight - 2f);

            // Applying a preset only changes settings: popup stays open.
            if (GUI.Button(row, presets[i].name, EditorStyles.miniButton))
            {
                ConsoleLogExporter.ApplyPreset(presets[i]);
                editorWindow.Repaint();
            }
        }

        GUI.EndScrollView();
    }

    // ---------------------------------------------------------
    // HELPERS
    // ---------------------------------------------------------

    private void CloseThen(Action action)
    {
        pendingAction = action;
        closing = true;
        editorWindow.Close();
    }

    private static Rect NextRect(ref float y, float width, float height)
    {
        Rect rect = new Rect(Pad, y, width, height);
        y += height + Gap;
        return rect;
    }

    private bool Button(bool draw, ref float y, float width, string text)
    {
        Rect rect = NextRect(ref y, width, RowHeight);
        return draw && !closing && GUI.Button(rect, text);
    }

    private void Label(bool draw, ref float y, float width, string text)
    {
        Rect rect = NextRect(ref y, width, LabelHeight);

        if (draw && !closing)
            GUI.Label(rect, text, EditorStyles.miniBoldLabel);
    }

    private void Separator(bool draw, ref float y, float width)
    {
        if (draw && !closing)
        {
            EditorGUI.DrawRect(
                new Rect(Pad, y + SeparatorHeight * 0.5f - 1f, width, 1f),
                new Color(0.5f, 0.5f, 0.5f, 0.4f)
            );
        }

        y += SeparatorHeight;
    }
}
}
