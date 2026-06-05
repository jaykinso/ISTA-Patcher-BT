## Context

The latest coverage artifact is `TestResults/20260605-203248/coverage.cobertura.xml`. It reports:

- Overall line coverage: 27.6%
- `ISTA-Patcher`: 0.0%
- `ISTAvalon`: 67.4%
- `ISTAlter`: 14.2%

The largest uncovered regions are not all equally useful targets. Some functions require real ISTA assemblies, platform-specific APIs, registry state, or Sentry/global process state. The coverage plan should prioritize deterministic tests that improve confidence without introducing flaky environment coupling.

## Goals / Non-Goals

**Goals:**

- Identify concrete functions that can raise coverage with stable, focused tests.
- Prioritize tests by expected value and implementation risk.
- Keep test changes in `src/ISTestA` unless a small test seam is needed.
- Re-run `python scripts/test-coverage.py --html` and compare against `TestResults/20260605-203248`.

**Non-Goals:**

- Do not chase coverage by invoking real ISTA installations or patching unknown third-party binaries.
- Do not require Windows registry, macOS Carbon/CoreFoundation, Linux machine IDs, or real network services for unit coverage.
- Do not test Sentry delivery or external telemetry side effects.

## Decisions

### Prioritize Pure and Deterministic Functions First

P1 targets are pure or near-pure functions with high confidence and low setup cost:

- `ISTAvalon.Services.GuiObservationOptions.From`
  - Current coverage: 0/41 lines in `From`, 0/1 in `Prefix`, 0/3 in `ParsePort`, 0/5 in `IsTruthy`.
  - Test cases: env-only enablement, CLI enablement, falsey assignment, host via split and equals syntax, port via split and equals syntax, invalid port fallback, blank host fallback, `Prefix` formatting.
- `ISTAvalon.Converters.LogMessageHighlighter.Highlight`
  - Current uncovered areas: non-ANSI quote/number path, empty messages, every level brush branch, ANSI reset/unknown/extended color edge cases.
  - Test cases: quoted strings strip quotes, numeric tokens use number brush, empty message returns one run, each `LogEventLevel` maps, ANSI 16-color/256-color/true-color/reset/invalid code behavior.
- `ISTAPatcher.Utils.AvailablePorts.GetAvailablePort`
  - Current coverage: 0/13 lines.
  - Test cases: `startingPort > 65535` throws; starting from a valid high port returns a value in range. Avoid asserting a specific globally available port.

Alternative considered: start with large uncovered `PatchUtils.Optional` methods. Rejected for first phase because they need careful dnlib fixture assemblies and carry more risk.

### Add Focused Utility and Serialization Coverage Second

P2 targets require small fixtures but are still deterministic:

- `ISTAlter.Models.Rheingold.LicenseManagement.LicenseStatusChecker`
  - Current coverage: 0%.
  - Test cases: null `LicenseKey` returns false; generated key validates with matching RSA key; tampered license fails; deformatter creation uses expected hash algorithm path.
- `ISTAlter.Utils.RegistryUtils.GenerateMockRegFile`
  - Current coverage: 0%.
  - Test cases: creates a `.reg` file without ISTA core DLL and defaults to native 64-bit hive; existing file is not overwritten when `force` is false; force overwrites. Assert generated text contains escaped license XML and `ForceDealerData`.
- `ISTAlter.Utils.ResourceUtils`
  - Current coverage: 0%.
  - Test cases: update existing resource stream entry, preserve non-target entries, missing resource returns without throwing, missing file logs but preserves resource, `GetFromResource` returns target stream or throws when missing.

Alternative considered: cover `AddWatermark` immediately. Deferred because it uses SkiaSharp image decoding/fonts and should be a separate small image-fixture task after resource table coverage.

### Treat Patch and CLI Integration Paths as Third Phase

P3 targets are valuable but need test seams or generated fixture assemblies:

- `ISTAlter.Core.PatchUtils.Base`
  - Candidate functions: `HavePatchedMark`, `AddPatchedAttribute`, `SetPatchedMarkInner`, `IsVersionInRange`, `IsPatchApplicable`, `PatchFunction`, `PatchGetter`, `PatchAsyncFunction`.
  - Test strategy: build in-memory or temporary dnlib fixture assemblies with tiny types/methods and known attributes.
- `ISTA-Patcher` commands and controllers
  - Current package coverage: 0%.
  - Test strategy: start with utility-level command dependencies (`AvailablePorts`), then add command tests only where file/network/console side effects can be isolated.
- `TelemetryBootstrap.Initialize`
  - Current coverage: 0%.
  - Test strategy: either exclude from unit target or introduce a small test seam around Sentry/repository discovery before testing idempotence and tag fallback behavior.

## Risks / Trade-offs

- [Risk] Tests that assert exact available ports can be flaky on busy machines. → Mitigation: assert range and validity, not exact port, except for invalid input.
- [Risk] Environment variable tests can leak process state. → Mitigation: save and restore `ISTA_GUI_DUMP_HTTP*` variables in setup/teardown.
- [Risk] RSA/XML signature tests can become brittle if serialization changes intentionally. → Mitigation: assert round-trip validity and tamper failure, not fixed signature bytes.
- [Risk] dnlib patch tests can overfit implementation details. → Mitigation: test public helper outcomes and assembly metadata changes before testing specific IL rewrites.
