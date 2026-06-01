// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTestA;

using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.VisualTree;

/// <summary>
/// Renders the visual/logical control tree as an indented text report.
/// An agent can read this output directly (no vision required) to understand
/// what is currently displayed in the headless window.
///
/// Each line format:
///   [indent][TypeName] Name="…" Text="…" IsVisible=… IsEnabled=… Bounds=(x,y,w,h)
/// </summary>
public static class ControlTreeDumper
{
    /// <summary>
    /// Returns a full indented tree of all visual descendants of <paramref name="root"/>.
    /// </summary>
    public static string Dump(Visual root, int maxDepth = 25)
    {
        var sb = new StringBuilder();
        DumpNode(root, sb, 0, maxDepth);
        return sb.ToString();
    }

    /// <summary>
    /// Returns only the interactive leaf nodes (Button, TextBox, ComboBox, CheckBox,
    /// NumericUpDown, TextBlock) with their current state — a condensed "form state" view
    /// that is much easier for an agent to parse than the full tree.
    /// </summary>
    public static string DumpInteractive(Visual root)
    {
        var sb = new StringBuilder();
        CollectInteractive(root, sb);
        return sb.ToString();
    }

    // -------------------------------------------------------------------------

    private static void DumpNode(Visual node, StringBuilder sb, int depth, int maxDepth)
    {
        if (depth > maxDepth) return;

        var indent = new string(' ', depth * 2);
        var typeName = node.GetType().Name;
        var extras = new List<string>();

        // x:Name
        if (node is StyledElement se && !string.IsNullOrEmpty(se.Name))
            extras.Add($"Name=\"{se.Name}\"");

        // Text / Content properties
        switch (node)
        {
            case TextBlock tb when !string.IsNullOrEmpty(tb.Text):
                extras.Add($"Text=\"{Truncate(tb.Text)}\"");
                break;
            case TextBox txb:
                extras.Add($"Text=\"{Truncate(txb.Text ?? string.Empty)}\"");
                extras.Add($"PlaceholderText=\"{Truncate(txb.PlaceholderText ?? string.Empty)}\"");
                break;
            case CheckBox cb:
                extras.Add($"IsChecked={cb.IsChecked}");
                extras.Add($"Content=\"{Truncate(cb.Content?.ToString() ?? string.Empty)}\"");
                break;
            case Button btn:
                extras.Add($"Content=\"{Truncate(btn.Content?.ToString() ?? string.Empty)}\"");
                extras.Add($"IsEnabled={btn.IsEffectivelyEnabled}");
                break;
            case ComboBox cmb:
                extras.Add($"SelectedIndex={cmb.SelectedIndex}");
                extras.Add($"SelectedItem=\"{Truncate(cmb.SelectedItem?.ToString() ?? string.Empty)}\"");
                break;
            case NumericUpDown nud:
                extras.Add($"Value={nud.Value}");
                break;
            case ContentControl cc:
                extras.Add($"Content=\"{Truncate(cc.Content?.ToString() ?? string.Empty)}\"");
                break;
        }

        if (node is Visual visual)
        {
            if (!visual.IsVisible) extras.Add("IsVisible=false");

            if (node is InputElement ie && !ie.IsEffectivelyEnabled)
                extras.Add("IsEnabled=false");

            var bounds = visual.Bounds;
            if (bounds != default)
                extras.Add($"Bounds=({bounds.X:F0},{bounds.Y:F0},{bounds.Width:F0}x{bounds.Height:F0})");
        }

        sb.Append(indent).Append(typeName);
        if (extras.Count > 0) sb.Append(' ').AppendJoin(' ', extras);
        sb.AppendLine();

        foreach (var child in node.GetVisualChildren())
            DumpNode(child, sb, depth + 1, maxDepth);
    }

    private static void CollectInteractive(Visual node, StringBuilder sb)
    {
        if (!node.IsVisible) return;

        switch (node)
        {
            case CheckBox cb:
                sb.AppendLine($"[CheckBox] Content=\"{Truncate(cb.Content?.ToString())}\"  IsChecked={cb.IsChecked}");
                break;
            case Button btn:
                sb.AppendLine($"[Button] Content=\"{Truncate(btn.Content?.ToString())}\"  IsEnabled={btn.IsEffectivelyEnabled}  Bounds={FormatBounds(btn.Bounds)}");
                break;
            case TextBox txb:
                sb.AppendLine($"[TextBox] Name=\"{txb.Name}\"  Text=\"{Truncate(txb.Text)}\"  Placeholder=\"{Truncate(txb.PlaceholderText)}\"  Bounds={FormatBounds(txb.Bounds)}");
                break;
            case ComboBox cmb:
                sb.AppendLine($"[ComboBox] SelectedItem=\"{Truncate(cmb.SelectedItem?.ToString())}\"  ItemCount={cmb.ItemCount}");
                break;
            case NumericUpDown nud:
                sb.AppendLine($"[NumericUpDown] Value={nud.Value}  Min={nud.Minimum}  Max={nud.Maximum}");
                break;
            case TextBlock tb when tb.IsEffectivelyVisible && !string.IsNullOrWhiteSpace(tb.Text):
                sb.AppendLine($"[TextBlock] Text=\"{Truncate(tb.Text)}\"");
                break;
        }

        foreach (var child in node.GetVisualChildren())
            CollectInteractive(child, sb);
    }

    private static string FormatBounds(Rect r) => $"({r.X:F0},{r.Y:F0},{r.Width:F0}x{r.Height:F0})";

    private static string Truncate(string? s, int max = 80) =>
        s is null ? string.Empty :
        s.Length > max ? s[..max] + "…" : s;
}
