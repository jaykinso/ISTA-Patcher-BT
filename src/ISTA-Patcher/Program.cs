// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2026 TautCony

using DotMake.CommandLine;
using ISTAPatcher.Commands;
using ISTAPatcher.Tasks;

TaskProvider.GatherTasks<IStartupTask>().Run(args);
var theme = new CliTheme(CliTheme.Default)
{
    DefaultStyle = new CliStyle(ConsoleColor.DarkGray, OperatingSystem.IsWindows() ? (CliColor?)ConsoleColor.Black : null, null),
    HeadingStyle = new CliStyle(ConsoleColor.Blue, null, null),
    FirstColumnStyle = new CliStyle(ConsoleColor.Cyan, null, null),
    SecondColumnStyle = new CliStyle(ConsoleColor.Green, null, null),
};
return await Cli.RunAsync<RootCommand>(args, new CliSettings { Theme = theme });
