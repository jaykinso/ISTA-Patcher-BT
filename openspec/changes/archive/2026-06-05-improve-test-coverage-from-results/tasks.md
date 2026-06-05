## 1. Baseline And Test Infrastructure

- [x] 1.1 Record `TestResults/20260605-203248` as the baseline coverage run for this change.
- [x] 1.2 Add helper setup/teardown for tests that modify `ISTA_GUI_DUMP_HTTP`, `ISTA_GUI_DUMP_HTTP_HOST`, or `ISTA_GUI_DUMP_HTTP_PORT`.
- [x] 1.3 Confirm `python scripts/test-coverage.py --html` works with the existing `reportgenerator` discovery fix.

## 2. P1 Deterministic Coverage Targets

- [x] 2.1 Add `GuiObservationOptions.From` tests for env enablement, CLI enablement, falsey values, host parsing, port parsing, invalid port fallback, blank host fallback, and `Prefix`.
- [x] 2.2 Add `LogMessageHighlighter.Highlight` tests for non-ANSI quoted strings, numeric tokens, empty messages, each log level brush, ANSI reset, invalid ANSI codes, 16-color ANSI, 256-color ANSI, true-color ANSI, and clamped colors.
- [x] 2.3 Add `AvailablePorts.GetAvailablePort` tests for invalid starting ports and valid range behavior without asserting one exact globally available port.

## 3. P2 Fixture-Based Coverage Targets

- [x] 3.1 Add `LicenseStatusChecker` tests for null `LicenseKey`, generated key validation, tamper failure, and deformatter creation.
- [x] 3.2 Add `RegistryUtils.GenerateMockRegFile` tests for default 64-bit hive generation without `RheingoldCoreFramework.dll`, no-overwrite behavior when `force` is false, forced overwrite, escaped XML content, and `ForceDealerData` payload presence.
- [x] 3.3 Add `ResourceUtils` tests for reading an embedded resource entry, updating a target entry, preserving non-target entries, missing resource behavior, and missing file behavior.

## 4. P3 Higher-Coupling Coverage Targets

- [x] 4.1 Add dnlib fixture tests for `PatchUtils.HavePatchedMark`, `AddPatchedAttribute`, `SetPatchedMarkInner`, `IsVersionInRange`, and `IsPatchApplicable`.
- [x] 4.2 Evaluate whether `PatchFunction`, `PatchGetter`, and `PatchAsyncFunction` can be covered with temporary fixture assemblies without overfitting IL internals.
- [x] 4.3 Decide whether `TelemetryBootstrap.Initialize` should remain untested, be excluded from coverage goals, or receive a small seam around Sentry and repository discovery.
- [x] 4.4 Identify CLI command tests that can run without real ISTA files, live servers, or global console state.

## 5. Verification

- [x] 5.1 Run focused tests for each new test file as it is added.
- [x] 5.2 Run `dotnet test src/ISTestA/ISTestA.csproj`.
- [x] 5.3 Run `python scripts/test-coverage.py --html`.
- [x] 5.4 Compare package and class coverage against `TestResults/20260605-203248`, documenting the largest improvements and remaining deferred targets.
