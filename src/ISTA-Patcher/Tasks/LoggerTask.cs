// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024-2026 TautCony

namespace ISTAPatcher.Tasks;

using JetBrains.Annotations;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

[UsedImplicitly]
public class LoggerTask : IStartupTask
{
    public const string LOGFILE = "ista-patcher.log";

    /// <summary>
    /// Output template that includes <c>{PatchName:l}</c>: when a
    /// <see cref="ISTAlter.Core.PatchUtils.BeginPatchScope"/> is active the value is
    /// <c> [PatchName]</c> (with leading space and brackets); otherwise it is an empty
    /// string supplied by the fallback <c>Enrich.WithProperty</c> enricher.
    /// </summary>
    private const string OutputTemplate =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zz} [{Level:u3}]{PatchName:l} {Message:lj}{NewLine}{Exception}";

    public void Execute(object?[]? parameters)
    {
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("PatchName", string.Empty)
            .MinimumLevel.ControlledBy(Global.LevelSwitch)
            .WriteTo.Console(outputTemplate: OutputTemplate, theme: AnsiConsoleTheme.Code)
            .WriteTo.File(LOGFILE, outputTemplate: OutputTemplate, rollingInterval: RollingInterval.Day)
            .WriteTo.Sentry(LogEventLevel.Error, LogEventLevel.Debug)
            .CreateLogger();
    }
}
