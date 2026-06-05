## Coverage Results

Baseline:

- Run: `TestResults/20260605-203248`
- Overall lines: 27.67%
- Overall branches: 27.86%
- `ISTA-Patcher`: lines 0.00%, branches 0.00%
- `ISTAvalon`: lines 67.43%, branches 58.24%
- `ISTAlter`: lines 14.27%, branches 13.30%

Current:

- Run: `TestResults/20260605-205723`
- Overall lines: 37.24%
- Overall branches: 36.78%
- `ISTA-Patcher`: lines 1.94%, branches 1.13%
- `ISTAvalon`: lines 74.25%, branches 67.50%
- `ISTAlter`: lines 26.71%, branches 23.38%

Largest targeted improvements:

- `ISTAPatcher.Utils.AvailablePorts`: 0% -> 100%
- `ISTAvalon.Services.GuiObservationOptions`: 0% -> 100%
- `ISTAvalon.Converters.LogMessageHighlighter`: 51.4% -> 85.6%
- `ISTAlter.Models.Rheingold.LicenseManagement.LicenseStatusChecker`: 0% -> 100%
- `ISTAlter.Utils.RegistryUtils`: 0% -> 95%
- `ISTAlter.Utils.ResourceUtils`: 0% -> 57.1%
- `ISTAlter.Core.PatchUtils`: 2.2% -> 11.7%

Remaining deferred targets:

- Real patch orchestration in `ISTAlter.Core.Patch`
- High-complexity IL rewrites in `PatchUtils.Optional`
- CLI command execution paths requiring real files, servers, or global process state
- Platform/native utilities and telemetry initialization
