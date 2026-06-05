// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2026 TautCony

namespace ISTAlter.Models.Rheingold.LicenseManagement.CoreFramework;

public partial class LicenseInfo
{
    public object Clone()
    {
        var clone = (LicenseInfo)this.MemberwiseClone();
        clone.SubLicenses = this.SubLicenses?.ToList();
        clone.ComputerCharacteristics = (byte[]?)this.ComputerCharacteristics?.Clone();
        clone.LicenseKey = (byte[]?)this.LicenseKey?.Clone();
        return clone;
    }
}
