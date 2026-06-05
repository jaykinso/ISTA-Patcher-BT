// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTestA.ISTAvalon.ViewModels;

using Avalonia.Media;
using global::ISTAvalon.Converters;
using global::ISTAvalon.Models;
using Serilog.Events;

public class LogMessageHighlighterTests
{
    [Test]
    public void Highlight_NonAnsiMessage_HighlightsQuotedStringsAndNumbers()
    {
        const string message = "value \"quoted\" count 42 and 'single'";

        var runs = LogMessageHighlighter.Highlight(message, LogEventLevel.Debug);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(string.Concat(runs.Select(r => r.Text)), Is.EqualTo("value quoted count 42 and single"));
            Assert.That(runs.Any(run => run.Text == "quoted" && ReferenceEquals(run.Foreground, LogPanelPalette.StringBrush)), Is.True);
            Assert.That(runs.Any(run => run.Text == "single" && ReferenceEquals(run.Foreground, LogPanelPalette.StringBrush)), Is.True);
            Assert.That(runs.Any(run => run.Text == "42" && ReferenceEquals(run.Foreground, LogPanelPalette.NumberBrush)), Is.True);
        }
    }

    [Test]
    public void Highlight_EmptyMessage_ReturnsSingleLevelRun()
    {
        var runs = LogMessageHighlighter.Highlight(string.Empty, LogEventLevel.Error);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(runs, Has.Count.EqualTo(1));
            Assert.That(runs[0].Text, Is.EqualTo(string.Empty));
            Assert.That(runs[0].Foreground, Is.SameAs(LogPanelPalette.ErrorBrush));
        }
    }

    [TestCase(LogEventLevel.Verbose, nameof(LogPanelPalette.VerboseBrush))]
    [TestCase(LogEventLevel.Debug, nameof(LogPanelPalette.DebugBrush))]
    [TestCase(LogEventLevel.Information, nameof(LogPanelPalette.InformationBrush))]
    [TestCase(LogEventLevel.Warning, nameof(LogPanelPalette.WarningBrush))]
    [TestCase(LogEventLevel.Error, nameof(LogPanelPalette.ErrorBrush))]
    [TestCase(LogEventLevel.Fatal, nameof(LogPanelPalette.FatalBrush))]
    public void Highlight_PlainMessage_UsesLevelBrush(LogEventLevel level, string palettePropertyName)
    {
        var expected = typeof(LogPanelPalette).GetProperty(palettePropertyName)!.GetValue(null);

        var runs = LogMessageHighlighter.Highlight("plain text", level);

        Assert.That(runs.Single().Foreground, Is.SameAs(expected));
    }

    [Test]
    public void Highlight_AnsiRedAndReset_RendersWithoutEscapeCodes()
    {
        const string message = "\e[31mERR\e[0m normal";

        var runs = LogMessageHighlighter.Highlight(message, LogEventLevel.Information);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(string.Concat(runs.Select(r => r.Text)), Is.EqualTo("ERR normal"));
            Assert.That(runs[0].Foreground, Is.SameAs(LogPanelPalette.AnsiRedBrush));
            Assert.That(runs[^1].Foreground, Is.SameAs(LogPanelPalette.InformationBrush));
        }
    }

    [Test]
    public void Highlight_AnsiBrightYellowAndDefaultReset_AppliesExpectedBrushes()
    {
        const string message = "\e[93mWARN\e[39m done";

        var runs = LogMessageHighlighter.Highlight(message, LogEventLevel.Warning);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(string.Concat(runs.Select(r => r.Text)), Is.EqualTo("WARN done"));
            Assert.That(runs[0].Foreground, Is.SameAs(LogPanelPalette.AnsiBrightYellowBrush));
            Assert.That(runs[^1].Foreground, Is.SameAs(LogPanelPalette.WarningBrush));
        }
    }

    [Test]
    public void Highlight_AnsiTrueColorForeground_AppliesRgbBrush()
    {
        const string message = "\e[38;2;12;200;34mRGB\e[0m";

        var runs = LogMessageHighlighter.Highlight(message, LogEventLevel.Information);

        var brush = (SolidColorBrush)runs[0].Foreground!;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(string.Concat(runs.Select(r => r.Text)), Is.EqualTo("RGB"));
            Assert.That(runs[0].Foreground, Is.TypeOf<SolidColorBrush>());
            Assert.That(brush.Color, Is.EqualTo(Color.FromRgb(12, 200, 34)));
        }
    }

    [Test]
    public void Highlight_AnsiTrueColorForeground_ClampsRgbValues()
    {
        const string message = "\e[38;2;300;999;34mRGB\e[0m";

        var runs = LogMessageHighlighter.Highlight(message, LogEventLevel.Information);

        var brush = (SolidColorBrush)runs[0].Foreground!;
        Assert.That(brush.Color, Is.EqualTo(Color.FromRgb(255, 255, 34)));
    }

    [Test]
    public void Highlight_Ansi256Foreground_AppliesPaletteBrush()
    {
        const string message = "\e[38;5;196mALERT\e[0m";

        var runs = LogMessageHighlighter.Highlight(message, LogEventLevel.Information);

        var brush = (SolidColorBrush)runs[0].Foreground!;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(string.Concat(runs.Select(r => r.Text)), Is.EqualTo("ALERT"));
            Assert.That(runs[0].Foreground, Is.TypeOf<SolidColorBrush>());
            Assert.That(brush.Color, Is.EqualTo(Color.FromRgb(255, 0, 0)));
        }
    }

    [Test]
    public void Highlight_Ansi256Foreground_MapsSixteenColorAndGrayscaleRanges()
    {
        const string message = "\e[38;5;9mRED\e[38;5;232mGRAY\e[0m";

        var runs = LogMessageHighlighter.Highlight(message, LogEventLevel.Information);

        var redBrush = (SolidColorBrush)runs[0].Foreground!;
        var grayBrush = (SolidColorBrush)runs[1].Foreground!;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(string.Concat(runs.Select(r => r.Text)), Is.EqualTo("REDGRAY"));
            Assert.That(redBrush.Color, Is.EqualTo(Color.FromRgb(255, 0, 0)));
            Assert.That(grayBrush.Color, Is.EqualTo(Color.FromRgb(8, 8, 8)));
        }
    }

    [Test]
    public void Highlight_InvalidAnsiCodes_KeepCurrentBrush()
    {
        const string message = "\e[31mred\e[38m still-red";

        var runs = LogMessageHighlighter.Highlight(message, LogEventLevel.Information);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(string.Concat(runs.Select(r => r.Text)), Is.EqualTo("red still-red"));
            Assert.That(runs[0].Foreground, Is.SameAs(LogPanelPalette.AnsiRedBrush));
            Assert.That(runs[1].Foreground, Is.SameAs(LogPanelPalette.AnsiRedBrush));
        }
    }
}
