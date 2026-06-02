// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTAvalon.Services;

using System.Net;
using System.Text;
using Avalonia.Threading;
using ISTAvalon.ViewModels;
using Serilog;

public sealed class GuiObservationServer : IDisposable
{
    private readonly MainWindowViewModel _viewModel;
    private readonly GuiObservationOptions _options;
    private readonly HttpListener _listener = new();
    private readonly CancellationTokenSource _cts = new();
    private Task? _serverTask;
    private bool _disposed;

    public GuiObservationServer(MainWindowViewModel viewModel, GuiObservationOptions options)
    {
        _viewModel = viewModel;
        _options = options;
        _listener.Prefixes.Add(options.Prefix);
    }

    public void Start()
    {
        _listener.Start();
        _serverTask = Task.Run(() => RunAsync(_cts.Token));
        Log.Information("GUI observation endpoint listening on {Url}", _options.Prefix);
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _listener.IsListening)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (HttpListenerException) when (!_listener.IsListening)
            {
                break;
            }
            catch (InvalidOperationException) when (!_listener.IsListening)
            {
                break;
            }

            _ = Task.Run(() => HandleAsync(context, cancellationToken), cancellationToken);
        }
    }

    private async Task HandleAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        try
        {
            if (!IsLoopback(context.Request.RemoteEndPoint?.Address))
            {
                await WriteAsync(context.Response, 403, "text/plain; charset=utf-8", "Forbidden", cancellationToken);
                return;
            }

            if (!context.Request.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase))
            {
                await WriteAsync(context.Response, 405, "text/plain; charset=utf-8", "Method Not Allowed", cancellationToken);
                return;
            }

            var path = context.Request.Url?.AbsolutePath.TrimEnd('/') ?? string.Empty;
            switch (path)
            {
                case "":
                case "/":
                    await WriteAsync(
                        context.Response,
                        200,
                        "text/plain; charset=utf-8",
                        "GET /dump.yaml for the current GUI state.\nGET /health for readiness.\n",
                        cancellationToken);
                    break;
                case "/health":
                    await WriteAsync(context.Response, 200, "text/plain; charset=utf-8", "ok\n", cancellationToken);
                    break;
                case "/dump":
                case "/dump.yaml":
                    var yaml = await Dispatcher.UIThread.InvokeAsync(() => GuiStateDumper.DumpYaml(_viewModel));
                    await WriteAsync(context.Response, 200, "application/x-yaml; charset=utf-8", yaml, cancellationToken);
                    break;
                default:
                    await WriteAsync(context.Response, 404, "text/plain; charset=utf-8", "Not Found", cancellationToken);
                    break;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while handling GUI observation request");
            if (context.Response.OutputStream.CanWrite)
            {
                await WriteAsync(context.Response, 500, "text/plain; charset=utf-8", "Internal Server Error", cancellationToken);
            }
        }
    }

    private static bool IsLoopback(IPAddress? address) =>
        address is null || IPAddress.IsLoopback(address);

    private static async Task WriteAsync(
        HttpListenerResponse response,
        int statusCode,
        string contentType,
        string content,
        CancellationToken cancellationToken)
    {
        var buffer = Encoding.UTF8.GetBytes(content);
        response.StatusCode = statusCode;
        response.ContentType = contentType;
        response.ContentEncoding = Encoding.UTF8;
        response.ContentLength64 = buffer.Length;
        response.Headers["Cache-Control"] = "no-store";
        await response.OutputStream.WriteAsync(buffer, cancellationToken);
        response.Close();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cts.Cancel();

        if (_listener.IsListening)
        {
            _listener.Stop();
        }

        _listener.Close();
        _cts.Dispose();
    }
}
