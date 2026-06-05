// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTestA.ISTAvalon.Services;

using global::ISTAvalon.Services;

public class GuiObservationOptionsTests
{
    private const string EnabledVariable = "ISTA_GUI_DUMP_HTTP";
    private const string HostVariable = "ISTA_GUI_DUMP_HTTP_HOST";
    private const string PortVariable = "ISTA_GUI_DUMP_HTTP_PORT";

    private readonly Dictionary<string, string?> _environment = [];

    [SetUp]
    public void Setup()
    {
        Capture(EnabledVariable);
        Capture(HostVariable);
        Capture(PortVariable);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var (name, value) in _environment)
        {
            Environment.SetEnvironmentVariable(name, value);
        }

        _environment.Clear();
    }

    [TestCase("1")]
    [TestCase("true")]
    [TestCase("yes")]
    [TestCase("on")]
    public void From_TruthyEnvironmentValue_EnablesDumpHttp(string value)
    {
        SetEnvironment(EnabledVariable, value);

        var options = GuiObservationOptions.From([]);

        Assert.That(options.Enabled, Is.True);
    }

    [TestCase("0")]
    [TestCase("false")]
    [TestCase("no")]
    [TestCase("off")]
    [TestCase("")]
    public void From_FalseyEnvironmentValue_DoesNotEnableDumpHttp(string? value)
    {
        SetEnvironment(EnabledVariable, value);

        var options = GuiObservationOptions.From([]);

        Assert.That(options.Enabled, Is.False);
    }

    [Test]
    public void From_SplitCliFlag_EnablesDumpHttpAndReadsHostAndPort()
    {
        var options = GuiObservationOptions.From(
        [
            "--gui-dump-http",
            "--gui-dump-http-host",
            "localhost",
            "--gui-dump-http-port",
            "9010",
        ]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(options.Enabled, Is.True);
            Assert.That(options.Host, Is.EqualTo("localhost"));
            Assert.That(options.Port, Is.EqualTo(9010));
            Assert.That(options.Prefix, Is.EqualTo("http://localhost:9010/"));
        }
    }

    [Test]
    public void From_EqualsCliSyntax_OverridesEnvironmentValues()
    {
        SetEnvironment(EnabledVariable, "1");
        SetEnvironment(HostVariable, "env-host");
        SetEnvironment(PortVariable, "8766");

        var options = GuiObservationOptions.From(
        [
            "--gui-dump-http=false",
            "--gui-dump-http-host=cli-host",
            "--gui-dump-http-port=9123",
        ]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(options.Enabled, Is.False);
            Assert.That(options.Host, Is.EqualTo("cli-host"));
            Assert.That(options.Port, Is.EqualTo(9123));
        }
    }

    [TestCase("0")]
    [TestCase("-1")]
    [TestCase("65536")]
    [TestCase("not-a-port")]
    public void From_InvalidPort_FallsBackToPreviousValidPort(string value)
    {
        SetEnvironment(PortVariable, "9001");

        var options = GuiObservationOptions.From(["--gui-dump-http-port", value]);

        Assert.That(options.Port, Is.EqualTo(9001));
    }

    [Test]
    public void From_BlankHost_FallsBackToLocalhost()
    {
        SetEnvironment(HostVariable, " ");

        var options = GuiObservationOptions.From([]);

        Assert.That(options.Host, Is.EqualTo("127.0.0.1"));
    }

    private void Capture(string name)
    {
        _environment[name] = Environment.GetEnvironmentVariable(name);
        Environment.SetEnvironmentVariable(name, null);
    }

    private static void SetEnvironment(string name, string? value)
    {
        Environment.SetEnvironmentVariable(name, value);
    }
}
