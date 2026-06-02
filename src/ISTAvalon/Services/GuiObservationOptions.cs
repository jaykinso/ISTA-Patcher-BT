// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTAvalon.Services;

public sealed class GuiObservationOptions
{
    public const int DefaultPort = 8765;

    public bool Enabled { get; init; }
    public string Host { get; init; } = "127.0.0.1";
    public int Port { get; init; } = DefaultPort;

    public string Prefix => $"http://{Host}:{Port}/";

    public static GuiObservationOptions From(string[] args)
    {
        var enabled = IsTruthy(Environment.GetEnvironmentVariable("ISTA_GUI_DUMP_HTTP"));
        var host = Environment.GetEnvironmentVariable("ISTA_GUI_DUMP_HTTP_HOST") ?? "127.0.0.1";
        var port = ParsePort(Environment.GetEnvironmentVariable("ISTA_GUI_DUMP_HTTP_PORT")) ?? DefaultPort;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.Equals("--gui-dump-http", StringComparison.OrdinalIgnoreCase))
            {
                enabled = true;
                continue;
            }

            if (arg.StartsWith("--gui-dump-http=", StringComparison.OrdinalIgnoreCase))
            {
                enabled = IsTruthy(arg["--gui-dump-http=".Length..]);
                continue;
            }

            if (arg.Equals("--gui-dump-http-host", StringComparison.OrdinalIgnoreCase) &&
                i + 1 < args.Length)
            {
                host = args[++i];
                continue;
            }

            if (arg.StartsWith("--gui-dump-http-host=", StringComparison.OrdinalIgnoreCase))
            {
                host = arg["--gui-dump-http-host=".Length..];
                continue;
            }

            if (arg.Equals("--gui-dump-http-port", StringComparison.OrdinalIgnoreCase) &&
                i + 1 < args.Length)
            {
                port = ParsePort(args[++i]) ?? port;
                continue;
            }

            if (arg.StartsWith("--gui-dump-http-port=", StringComparison.OrdinalIgnoreCase))
            {
                port = ParsePort(arg["--gui-dump-http-port=".Length..]) ?? port;
            }
        }

        return new GuiObservationOptions
        {
            Enabled = enabled,
            Host = string.IsNullOrWhiteSpace(host) ? "127.0.0.1" : host,
            Port = port,
        };
    }

    private static int? ParsePort(string? value) =>
        int.TryParse(value, out var port) && port is > 0 and <= 65535
            ? port
            : null;

    private static bool IsTruthy(string? value) =>
        value is not null &&
        (value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
         value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
         value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
         value.Equals("on", StringComparison.OrdinalIgnoreCase));
}
