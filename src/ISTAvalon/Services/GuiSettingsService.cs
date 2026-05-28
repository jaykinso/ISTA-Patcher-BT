// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTAvalon.Services;

using System.Text.Json;
using System.Text.Json.Serialization;
using ISTAvalon.Models;
using Serilog;

public static class GuiSettingsService
{
    /// <summary>
    /// Preset template — always copied fresh from Assets/gui-settings.json by the build system.
    /// Do NOT write runtime state here.
    /// </summary>
    private static readonly string SettingsFilePath =
        Path.Combine(AppContext.BaseDirectory, "gui-settings.json");

    /// <summary>
    /// Runtime user preferences (e.g. theme). Created at runtime, never touched by the build system.
    /// </summary>
    private static readonly string UserPreferencesFilePath =
        Path.Combine(AppContext.BaseDirectory, "user-preferences.json");

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static GuiSettings Load()
    {
        var settings = LoadPresets();
        OverlayUserPreferences(settings);
        return settings;
    }

    /// <summary>Persists only runtime user preferences (theme). Never touches gui-settings.json.</summary>
    public static void Save(GuiSettings settings)
    {
        try
        {
            var prefs = new UserPreferences { Theme = settings.Theme };
            var json = JsonSerializer.Serialize(prefs, SerializerOptions);
            File.WriteAllText(UserPreferencesFilePath, json);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to save user preferences to {Path}", UserPreferencesFilePath);
        }
    }

    private static GuiSettings LoadPresets()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                return JsonSerializer.Deserialize<GuiSettings>(json, SerializerOptions) ?? CreateDefault();
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to load GUI settings from {Path}", SettingsFilePath);
        }

        return CreateDefault();
    }

    private static void OverlayUserPreferences(GuiSettings settings)
    {
        try
        {
            if (File.Exists(UserPreferencesFilePath))
            {
                var json = File.ReadAllText(UserPreferencesFilePath);
                var prefs = JsonSerializer.Deserialize<UserPreferences>(json, SerializerOptions);
                if (!string.IsNullOrEmpty(prefs?.Theme))
                {
                    settings.Theme = prefs.Theme;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to load user preferences from {Path}", UserPreferencesFilePath);
        }
    }

    /// <summary>
    /// In-memory fallback used when gui-settings.json is missing or unreadable.
    /// The canonical defaults live in Assets/gui-settings.json.
    /// </summary>
    private static GuiSettings CreateDefault() => new()
    {
        Theme = "Default",
        Presets = new Dictionary<string, Dictionary<string, string>>
        {
            ["patch"] = new Dictionary<string, string>
            {
                ["PatchType"] = "BMW",
                ["MaxDegreeOfParallelism"] = "4",
            },
        },
    };
}
