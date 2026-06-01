// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

// Assembly attribute must precede the namespace declaration.
[assembly: Avalonia.Headless.AvaloniaTestApplication(typeof(ISTestA.HeadlessApp))]

namespace ISTestA;

using Avalonia;
using Avalonia.Headless;
using Avalonia.Themes.Fluent;
using Optris.Icons.Avalonia;
using Optris.Icons.Avalonia.FontAwesome;

/// <summary>
/// Minimal Avalonia application used exclusively by headless UI tests.
/// Deliberately avoids production side-effects (Sentry, Serilog, file I/O).
/// </summary>
public class HeadlessApp : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        IconProvider.Current.Register<FontAwesomeIconProvider>();
        return AppBuilder.Configure<HeadlessApp>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
    }
}
