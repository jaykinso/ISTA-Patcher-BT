// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTestA;

using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using ISTAvalon.ViewModels;
using ISTAvalon.Views;

/// <summary>
/// Headless UI tests for <see cref="MainWindow"/>.
/// Each <c>[AvaloniaTest]</c> runs on the Avalonia UI thread with the headless
/// Skia renderer active, so <c>CaptureRenderedFrame()</c> produces real pixel
/// output that the agent can inspect with <c>view_image</c>.
///
/// Screenshots are written to <c>TestResults/Screenshots/</c> relative to the
/// test working directory.  After a test run the agent can call
/// <c>view_image</c> on those PNGs to "see" the GUI and decide what to fix.
/// </summary>
[TestFixture]
public class MainWindowHeadlessTests
{
    private static string ScreenshotDir =>
        Path.Combine(TestContext.CurrentContext.WorkDirectory, "Screenshots");

    [SetUp]
    public void SetUp() => Directory.CreateDirectory(ScreenshotDir);

    // -------------------------------------------------------------------------
    // Smoke tests
    // -------------------------------------------------------------------------

    [AvaloniaTest]
    public void MainWindow_Renders_InitialState()
    {
        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm, Width = 1100, Height = 700 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var frame = window.CaptureRenderedFrame();
        frame?.Save(Path.Combine(ScreenshotDir, "MainWindow_Initial.png"));

        Assert.That(frame, Is.Not.Null, "CaptureRenderedFrame returned null — renderer may not be active");
        window.Close();
    }

    [AvaloniaTest]
    public void MainWindow_HasExpectedCommandTabs()
    {
        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm };
        window.Show();

        Assert.That(vm.CommandTabs, Is.Not.Empty, "CommandTabs should be populated by discovery");
        Assert.That(vm.SelectedTab, Is.Not.Null, "A tab should be selected by default");
        window.Close();
    }

    // -------------------------------------------------------------------------
    // Per-tab screenshot: agent can open each PNG to review layout
    // -------------------------------------------------------------------------

    [AvaloniaTest]
    public void AllCommandTabs_Render_WithoutException()
    {
        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm, Width = 1100, Height = 700 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        foreach (var tab in vm.CommandTabs)
        {
            vm.SelectedTab = tab;
            Dispatcher.UIThread.RunJobs();

            var frame = window.CaptureRenderedFrame();
            var safeName = string.Join("_", tab.Name.Split(Path.GetInvalidFileNameChars()));
            frame?.Save(Path.Combine(ScreenshotDir, $"Tab_{safeName}.png"));

            Assert.That(frame, Is.Not.Null, $"Tab '{tab.Name}' rendered null frame");
        }

        window.Close();
    }

    // -------------------------------------------------------------------------
    // Theme switching
    // -------------------------------------------------------------------------

    [AvaloniaTest]
    public void MainWindow_DarkTheme_Renders()
    {
        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm, Width = 1100, Height = 700 };
        window.Show();

        vm.IsDarkTheme = true;
        Dispatcher.UIThread.RunJobs();

        var frame = window.CaptureRenderedFrame();
        frame?.Save(Path.Combine(ScreenshotDir, "MainWindow_Dark.png"));

        Assert.That(frame, Is.Not.Null);
        window.Close();
    }

    [AvaloniaTest]
    public void ToggleThemeCommand_SwitchesTheme()
    {
        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm };
        window.Show();

        var initialDark = vm.IsDarkTheme;
        vm.ToggleThemeCommand.Execute(null);

        Assert.That(vm.IsDarkTheme, Is.Not.EqualTo(initialDark));
        window.Close();
    }

    // -------------------------------------------------------------------------
    // ViewModel binding checks
    // -------------------------------------------------------------------------

    [AvaloniaTest]
    public void CommandTab_SelectedCommand_ChangesParameters()
    {
        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm };
        window.Show();

        var tab = vm.SelectedTab;
        Assert.That(tab, Is.Not.Null);

        if (tab!.AvailableCommands.Count > 1)
        {
            var second = tab.AvailableCommands[1];
            tab.SelectedCommand = second;
            Dispatcher.UIThread.RunJobs();

            Assert.That(tab.SelectedCommand, Is.EqualTo(second));
        }

        window.Close();
    }
}
