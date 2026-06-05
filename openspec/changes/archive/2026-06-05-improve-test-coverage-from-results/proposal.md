## Why

The latest coverage run in `TestResults/20260605-203248` shows overall line coverage at 27.6%, with `ISTA-Patcher` at 0%, `ISTAlter` at 14.2%, and several pure parsing or utility paths still uncovered. A focused test plan can raise useful coverage without requiring real ISTA installations, registry state, or platform-specific native calls.

## What Changes

- Add a prioritized coverage-improvement plan based on the latest Cobertura report.
- Focus first on deterministic functions with clear inputs and outputs.
- Defer high-cost integration paths that require real patched assemblies, OS registry access, P/Invoke, or long-running servers.
- Track test additions by target function and expected coverage value.

## Capabilities

### New Capabilities

- `test-coverage-improvement`: Defines how coverage gaps are selected, prioritized, and verified using generated coverage reports.

### Modified Capabilities

None.

## Impact

- Affected tests: `src/ISTestA/ISTAvalon/`, `src/ISTestA/ISTAlter/`, and potentially `src/ISTestA/ISTA-Patcher/`.
- Affected production code only if seams are needed for deterministic tests.
- No CLI, GUI, XML, or patching behavior changes are intended.
