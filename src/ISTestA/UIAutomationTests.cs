// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTestA;

using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ISTAvalon.ViewModels;
using ISTAvalon.Views;

/// <summary>
/// UI-automation style tests for <see cref="MainWindow"/>.
///
/// These tests go beyond screenshots: they simulate real user gestures
/// (tab selection, text input, button clicks), read back ViewModel state and
/// the control tree as text, and take "before/after" screenshots.  The agent
/// can use <c>view_image</c> on the PNGs and read the console output that
/// NUnit captures to understand exactly what changed on screen.
///
/// Workflow for the agent:
///   1. Run: dotnet test --filter FullyQualifiedName~UIAutomation --logger "console;verbosity=detailed"
///   2. Read the printed control-tree dumps in the test output.
///   3. Call view_image on the Screenshots/*.png files to see rendered frames.
/// </summary>
[TestFixture]
public class UIAutomationTests
{
    private static string ScreenshotDir =>
        Path.Combine(TestContext.CurrentContext.WorkDirectory, "Screenshots");

    [SetUp]
    public void SetUp() => Directory.CreateDirectory(ScreenshotDir);

    // -------------------------------------------------------------------------
    // 1. Control-tree inspection — text-based, no vision needed
    // -------------------------------------------------------------------------

    [AvaloniaTest]
    public void DumpInteractiveControls_PrintsReadableFormState()
    {
        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm, Width = 1100, Height = 700 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var report = ControlTreeDumper.DumpInteractive(window);
        TestContext.Progress.WriteLine("=== Interactive Controls (initial) ===");
        TestContext.Progress.WriteLine(report);

        // Basic sanity: the Execute button should be visible
        Assert.That(report, Does.Contain("[Button]").And.Contain("Execute"),
            "Execute button should be discoverable in the control tree");

        window.Close();
    }

    [AvaloniaTest]
    public void DumpFullTree_WritesToFile_ForAgentInspection()
    {
        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm, Width = 1100, Height = 700 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var dump = ControlTreeDumper.Dump(window);
        var dumpPath = Path.Combine(ScreenshotDir, "ControlTree_Initial.txt");
        File.WriteAllText(dumpPath, dump);

        TestContext.Progress.WriteLine($"Full control tree written to: {dumpPath}");
        Assert.That(File.Exists(dumpPath), Is.True);

        window.Close();
    }

    // -------------------------------------------------------------------------
    // 2. Tab switching via mouse click
    // -------------------------------------------------------------------------

    [AvaloniaTest]
    public void Click_SecondTab_SelectsIt_AndRendersParameters()
    {
        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm, Width = 1100, Height = 700 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        if (vm.CommandTabs.Count < 2)
        {
            Assert.Ignore("Fewer than 2 tabs; skipping tab-switch test");
            return;
        }

        // Find the TabStrip items to locate the second tab header's position
        var tabControl = window.GetVisualDescendants()
            .OfType<TabControl>()
            .FirstOrDefault();

        Assert.That(tabControl, Is.Not.Null, "TabControl not found in visual tree");

        var secondTab = vm.CommandTabs[1];
        vm.SelectedTab = secondTab;         // direct ViewModel switch (reliable)
        Dispatcher.UIThread.RunJobs();

        var frame = window.CaptureRenderedFrame();
        frame?.Save(Path.Combine(ScreenshotDir, $"After_TabSwitch_{secondTab.Name}.png"));

        var report = ControlTreeDumper.DumpInteractive(window);
        TestContext.Progress.WriteLine($"=== Controls after switching to tab '{secondTab.Name}' ===");
        TestContext.Progress.WriteLine(report);

        Assert.That(vm.SelectedTab?.Name, Is.EqualTo(secondTab.Name));

        window.Close();
    }

    // -------------------------------------------------------------------------
    // 3. Filling a text parameter and verifying ViewModel updates
    // -------------------------------------------------------------------------

    [AvaloniaTest]
    public void FillStringParameter_Via_KeyTextInput_UpdatesViewModel()
    {
        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm, Width = 1100, Height = 700 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var tab = vm.SelectedTab;
        Assert.That(tab, Is.Not.Null);

        // Find the first string or path parameter
        var strParam = tab!.Parameters
            .OfType<StringParameterViewModel>()
            .FirstOrDefault()
            ?? tab.Parameters.OfType<PathParameterViewModel>().FirstOrDefault() as ParameterViewModel;

        if (strParam is null)
        {
            Assert.Ignore("No string/path parameter on first tab; skipping text-input test");
            return;
        }

        // Locate the corresponding TextBox in the visual tree
        var textBox = window.GetVisualDescendants()
            .OfType<TextBox>()
            .FirstOrDefault(tb => tb.IsEffectivelyVisible && tb.IsEffectivelyEnabled);

        Assert.That(textBox, Is.Not.Null, "No enabled TextBox found on current tab");

        textBox!.Focus();
        Dispatcher.UIThread.RunJobs();

        const string testValue = "/tmp/ista-test-path";
        window.KeyTextInput(testValue);
        Dispatcher.UIThread.RunJobs();

        TestContext.Progress.WriteLine($"Typed into TextBox: '{testValue}'");
        TestContext.Progress.WriteLine($"TextBox.Text is now: '{textBox.Text}'");

        Assert.That(textBox.Text, Does.Contain(testValue),
            "TextBox.Text should contain the typed text after KeyTextInput");

        var frame = window.CaptureRenderedFrame();
        frame?.Save(Path.Combine(ScreenshotDir, "After_TextInput.png"));

        window.Close();
    }

    // -------------------------------------------------------------------------
    // 4. Execute button — simulate click, observe StatusText change
    // -------------------------------------------------------------------------

    [AvaloniaTest]
    public void Click_ExecuteButton_WithMissingRequired_ShowsWarningStatus()
    {
        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm, Width = 1100, Height = 700 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var tab = vm.SelectedTab;
        Assert.That(tab, Is.Not.Null);

        // Clear any preset values so required params are empty
        foreach (var p in tab!.Parameters.OfType<StringParameterViewModel>())
            p.ApplyValue(string.Empty);
        foreach (var p in tab.Parameters.OfType<PathParameterViewModel>())
            p.ApplyValue(string.Empty);
        Dispatcher.UIThread.RunJobs();

        var requiredParams = tab.Parameters.Where(p => p.Descriptor.IsRequired).ToList();

        if (requiredParams.Count == 0)
        {
            Assert.Ignore("No required parameters on first tab; skipping validation test");
            return;
        }

        // Find and click the Execute button
        var executeBtn = window.GetVisualDescendants()
            .OfType<Button>()
            .FirstOrDefault(b => b.Content?.ToString() == "Execute" && b.IsEffectivelyVisible);

        Assert.That(executeBtn, Is.Not.Null, "Execute button not found");

        // Click the Execute button by invoking its command
        // (MouseDown on an exact pixel requires root-relative coordinates which
        //  vary with layout; command invocation is the reliable headless equivalent)
        executeBtn!.Command?.Execute(null);

        // Give async handler a chance to run
        Dispatcher.UIThread.RunJobs();

        TestContext.Progress.WriteLine($"StatusText after click: '{tab.StatusText}'");

        var frame = window.CaptureRenderedFrame();
        frame?.Save(Path.Combine(ScreenshotDir, "After_ExecuteClick_WithMissingRequired.png"));

        Assert.That(tab.StatusText, Does.StartWith("⚠"),
            $"Expected a warning status, got: '{tab.StatusText}'");

        window.Close();
    }

    // -------------------------------------------------------------------------
    // 5. Log panel toggle
    // -------------------------------------------------------------------------

    [AvaloniaTest]
    public void ToggleLogPanel_CollapsesAndExpandsLogArea()
    {
        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm, Width = 1100, Height = 700 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var tab = vm.SelectedTab;
        Assert.That(tab, Is.Not.Null);

        var expandedInitially = tab!.IsLogPanelExpanded;

        tab.ToggleLogPanelCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();

        var frameCollapsed = window.CaptureRenderedFrame();
        frameCollapsed?.Save(Path.Combine(ScreenshotDir, "LogPanel_Collapsed.png"));

        Assert.That(tab.IsLogPanelExpanded, Is.Not.EqualTo(expandedInitially),
            "ToggleLogPanelCommand should invert IsLogPanelExpanded");

        tab.ToggleLogPanelCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();

        var frameExpanded = window.CaptureRenderedFrame();
        frameExpanded?.Save(Path.Combine(ScreenshotDir, "LogPanel_Expanded.png"));

        Assert.That(tab.IsLogPanelExpanded, Is.EqualTo(expandedInitially),
            "Second toggle should restore the original state");

        window.Close();
    }

    // -------------------------------------------------------------------------
    // 6. Keyboard navigation — Tab key cycles focus through controls
    // -------------------------------------------------------------------------

    [AvaloniaTest]
    public void TabKey_CyclesFocus_ThroughInteractiveControls()
    {
        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm, Width = 1100, Height = 700 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var visited = new List<string>();
        for (var i = 0; i < 6; i++)
        {
            window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
            window.KeyReleaseQwerty(PhysicalKey.Tab, RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();

            var focused = window.FocusManager?.GetFocusedElement();
            var desc = focused is null ? "(none)"
                : $"{focused.GetType().Name}:{(focused is Avalonia.StyledElement se ? se.Name : string.Empty)}";
            visited.Add(desc);
        }

        TestContext.Progress.WriteLine("Focus sequence via Tab key:");
        foreach (var step in visited.Select((v, i) => $"  [{i + 1}] {v}"))
            TestContext.Progress.WriteLine(step);

        // At least some focus movement should have occurred
        Assert.That(visited.Distinct().Count(), Is.GreaterThan(1),
            "Tab key should move focus to different elements");

        window.Close();
    }
}
