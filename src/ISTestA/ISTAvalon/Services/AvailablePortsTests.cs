// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTestA.ISTAvalon.Services;

using global::ISTAPatcher.Utils;

public class AvailablePortsTests
{
    [Test]
    public void GetAvailablePort_StartingPortAboveUInt16Range_Throws()
    {
        Assert.Throws<ArgumentException>(() => AvailablePorts.GetAvailablePort(ushort.MaxValue + 1));
    }

    [Test]
    public void GetAvailablePort_ValidStartingPort_ReturnsPortInRange()
    {
        const int startingPort = 65000;

        var port = AvailablePorts.GetAvailablePort(startingPort);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(port, Is.GreaterThanOrEqualTo(startingPort));
            Assert.That(port, Is.LessThanOrEqualTo(ushort.MaxValue));
        }
    }
}
