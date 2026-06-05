// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTestA.ISTAlter.Utils;

using System.Collections;
using System.Resources;
using dnlib.DotNet;
using global::ISTAlter.Utils;

public class ResourceUtilsTests
{
    private ModuleDefUser _module = null!;

    [SetUp]
    public void Setup()
    {
        _module = new ModuleDefUser("TestModule");
    }

    [TearDown]
    public void TearDown()
    {
        _module.Dispose();
    }

    [Test]
    public void GetFromResource_ExistingStreamEntry_ReturnsStream()
    {
        AddResource("App.Resources", ("target.bin", new byte[] { 1, 2, 3 }));

        using var stream = ResourceUtils.GetFromResource(_module, "App.Resources", "target.bin");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(stream, Is.Not.Null);
            Assert.That(ReadAllBytes(stream!), Is.EqualTo(new byte[] { 1, 2, 3 }));
        }
    }

    [Test]
    public void GetFromResource_MissingEntry_Throws()
    {
        AddResource("App.Resources", ("other.bin", new byte[] { 1 }));

        Assert.Throws<Exception>(() => ResourceUtils.GetFromResource(_module, "App.Resources", "missing.bin"));
    }

    [Test]
    public void UpdateResource_TargetEntry_ReplacesContentAndPreservesOtherEntries()
    {
        AddResource(
            "App.Resources",
            ("target.bin", new byte[] { 1, 2, 3 }),
            ("other.bin", new byte[] { 9, 8 }),
            ("label", "unchanged"));

        ResourceUtils.UpdateResource(_module, "App.Resources", "target.bin", [4, 5, 6]);

        using var target = ResourceUtils.GetFromResource(_module, "App.Resources", "target.bin");
        using var other = ResourceUtils.GetFromResource(_module, "App.Resources", "other.bin");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ReadAllBytes(target!), Is.EqualTo(new byte[] { 4, 5, 6 }));
            Assert.That(ReadAllBytes(other!), Is.EqualTo(new byte[] { 9, 8 }));
            Assert.That(ReadResourceValue<string>("App.Resources", "label"), Is.EqualTo("unchanged"));
        }
    }

    [Test]
    public void UpdateResource_MissingResource_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => ResourceUtils.UpdateResource(_module, "Missing.Resources", "target.bin", [1]));
    }

    [Test]
    public void UpdateResource_MissingFile_PreservesResource()
    {
        AddResource("App.Resources", ("target.bin", new byte[] { 1, 2, 3 }));

        ResourceUtils.UpdateResource(_module, "App.Resources", "missing.bin", [4, 5, 6]);

        using var target = ResourceUtils.GetFromResource(_module, "App.Resources", "target.bin");
        Assert.That(ReadAllBytes(target!), Is.EqualTo(new byte[] { 1, 2, 3 }));
    }

    private void AddResource(string name, params (string Key, object Value)[] entries)
    {
        using var stream = new MemoryStream();
        using (var writer = new ResourceWriter(stream))
        {
            foreach (var (key, value) in entries)
            {
                if (value is byte[] bytes)
                {
                    writer.AddResource(key, new MemoryStream(bytes), closeAfterWrite: true);
                }
                else
                {
                    writer.AddResource(key, value);
                }
            }
        }

        _module.Resources.Add(new EmbeddedResource(name, stream.ToArray(), ManifestResourceAttributes.Public));
    }

    private T? ReadResourceValue<T>(string resourceName, string fileName)
    {
        var resource = (EmbeddedResource)_module.Resources.Single(resource => resource.Name == resourceName);
        using var resourceStream = resource.CreateReader().AsStream();
        using var reader = new ResourceReader(resourceStream);
        return reader.Cast<DictionaryEntry>()
            .Where(entry => string.Equals(entry.Key.ToString(), fileName, StringComparison.Ordinal))
            .Select(entry => entry.Value)
            .OfType<T>()
            .FirstOrDefault();
    }

    private static byte[] ReadAllBytes(Stream stream)
    {
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
