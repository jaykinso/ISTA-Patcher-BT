// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTestA.ISTAvalon.Headless;

using global::ISTAvalon.Services;
using global::ISTAvalon.ViewModels;

/// <summary>
/// Test-facing wrapper for the application GUI state dumper.
/// </summary>
internal static class ControlTreeDumper
{
    public static string DumpYaml(MainWindowViewModel vm) => GuiStateDumper.DumpYaml(vm);
}
