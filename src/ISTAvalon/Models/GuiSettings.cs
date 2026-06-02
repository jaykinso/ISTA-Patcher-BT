// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTAvalon.Models;

/// <summary>
/// Available application theme modes.
/// </summary>
public enum AppTheme
{
    /// <summary>Follow the OS system theme (Avalonia default).</summary>
    Default,

    /// <summary>Force light theme regardless of OS setting.</summary>
    Light,

    /// <summary>Force dark theme regardless of OS setting.</summary>
    Dark,
}

/// <summary>
/// Persisted GUI settings stored in gui-settings.json alongside the executable.
/// </summary>
public class GuiSettings
{
    /// <summary>
    /// UI theme: "Default", "Light", or "Dark".
    /// </summary>
    public string Theme { get; set; } = "Default";

    /// <summary>
    /// Per-command recommended default parameter values.
    /// Keys are top-level command names (e.g. "patch", "ilean").
    /// Inner keys are property names (e.g. "PatchType", "MaxDegreeOfParallelism").
    /// </summary>
    public Dictionary<string, Dictionary<string, string>> Presets { get; set; } = [];

    public IReadOnlyDictionary<string, string>? GetPresetFor(string commandName)
    {
        return Presets.GetValueOrDefault(commandName);
    }
}

/// <summary>
/// Runtime user preferences persisted in user-preferences.json (never touched by the build system).
/// </summary>
public class UserPreferences
{
    public string? Theme { get; set; }
}
