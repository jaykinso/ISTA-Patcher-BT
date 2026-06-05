// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2026 TautCony

namespace ISTAlter.Core;

using Serilog.Context;

public static partial class PatchUtils
{
    /// <summary>
    /// Opens a Serilog log-context scope that enriches every log event emitted within
    /// the <c>using</c> block with a <c>PatchName</c> structured property.
    /// Requires <c>Enrich.FromLogContext()</c> on the logger configuration.
    /// </summary>
    /// <param name="patchName">The name of the patch being executed.</param>
    /// <returns>
    /// The <see cref="IDisposable"/> returned by <see cref="LogContext.PushProperty"/>;
    /// dispose it (or use a <c>using</c> statement) to pop the property from the context.
    /// </returns>
    public static IDisposable BeginPatchScope(string patchName)
        => LogContext.PushProperty("PatchName", $" [{patchName}]");
}
