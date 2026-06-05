## P3 Evaluation

### Patch Function Helpers

`PatchFunction` and `PatchGetter` can be covered with temporary dnlib metadata fixtures. The implementation now accepts `ModuleDef`, which matches the members actually used by the helpers and allows `ModuleDefUser` in-memory tests. `PatchAsyncFunction` is partially covered through the missing-method branch.

Full positive-path `PatchAsyncFunction` coverage is deferred because a faithful async state-machine fixture requires constructing an `AsyncStateMachineAttribute`, nested generated type, override metadata, and a valid `MoveNext` body. That setup risks overfitting dnlib metadata details instead of testing stable behavior.

### Telemetry Bootstrap

`TelemetryBootstrap.Initialize` should remain out of the immediate unit coverage target. It initializes global Sentry state, reads git repository metadata, and is intentionally process-global/idempotent. Covering it well would require a seam around Sentry initialization and repository discovery. That seam is reasonable only if telemetry behavior changes.

### CLI Command Coverage

CLI command coverage should start with paths that avoid real ISTA files, live HTTP servers, global console state, or Sentry side effects:

- Utility-level dependencies such as `AvailablePorts`.
- Argument-to-model parsing where command descriptors can be constructed without executing patch operations.
- File-generation commands only when all file system effects are isolated under temporary directories.

Patch command execution, server startup, crypto file-list processing, and commands requiring real input files should remain integration-test candidates.
