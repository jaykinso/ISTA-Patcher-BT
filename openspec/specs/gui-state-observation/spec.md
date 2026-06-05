## ADDED Requirements

### Requirement: GUI SHALL expose state as a structured YAML document
The system SHALL provide a `GuiStateDumper` that serializes the full GUI ViewModel state into a structured YAML document. The dump SHALL include window-level metadata (title, selected tab, theme), a per-tab breakdown (command name, selected subcommand, parameters with types and current values, execution state, log panel state), and log panel entries. The dumped YAML SHALL use `underscored` naming convention and SHALL omit null values.

#### Scenario: Dump window metadata
- **WHEN** the state dumper serializes the main window ViewModel
- **THEN** the output includes `title`, `selected_tab`, and `theme` sections with `current` and `toggle_button` details

#### Scenario: Dump per-tab parameter state
- **WHEN** the state dumper serializes a command tab ViewModel
- **THEN** each tab entry includes `name`, `selected_command`, `available_commands`, `controls` (parameter editors with `role`, `type`, `label`, `is_required`, `current_value`, `has_value`), `is_executing`, `has_preset`, and `status`

#### Scenario: Dump log panel state with capped entries
- **WHEN** the log panel contains more than 200 entries
- **THEN** the dump includes at most the 200 most recent log entries and records a `skipped` count of the remaining entries

#### Scenario: Log entry dump includes level and description
- **WHEN** a log entry is serialized
- **THEN** each entry includes its `level` and `message`

### Requirement: GUI SHALL expose state via a local-only HTTP endpoint
The system SHALL provide a `GuiObservationServer` that listens on a configurable loopback address and serves the GUI state YAML. The server SHALL accept only loopback connections and SHALL reject non-loopback requests with HTTP 403. The server SHALL support `GET /dump.yaml` for the state dump and `GET /health` for readiness probing. Non-GET requests SHALL receive HTTP 405.

#### Scenario: Serve dump.yaml on localhost
- **WHEN** the observation server is started and a local client requests `GET /dump.yaml`
- **THEN** the server responds with HTTP 200 and the current GUI state as `application/x-yaml`

#### Scenario: Reject non-loopback requests
- **WHEN** the observation server receives a request from a non-loopback IP address
- **THEN** the server responds with HTTP 403 Forbidden

#### Scenario: Serve health endpoint
- **WHEN** a client requests `GET /health`
- **THEN** the server responds with HTTP 200 and body `ok`

#### Scenario: Reject non-GET methods
- **WHEN** a client sends a POST or other non-GET request to any endpoint
- **THEN** the server responds with HTTP 405 Method Not Allowed

#### Scenario: Return 404 for unknown paths
- **WHEN** a client requests an unrecognised path
- **THEN** the server responds with HTTP 404 Not Found

### Requirement: Observation server SHALL be configurable via CLI args and environment variables
The observation server configuration SHALL be read from CLI arguments (`--gui-dump-http`, `--gui-dump-http-port`, `--gui-dump-http-host`) and environment variables (`ISTA_GUI_DUMP_HTTP`, `ISTA_GUI_DUMP_HTTP_PORT`, `ISTA_GUI_DUMP_HTTP_HOST`). The default host SHALL be `127.0.0.1` and the default port SHALL be `8765`.

#### Scenario: Enable observation via CLI flag
- **WHEN** ISTAvalon is started with `--gui-dump-http`
- **THEN** the observation server starts on the default host and port

#### Scenario: Customise port via CLI
- **WHEN** ISTAvalon is started with `--gui-dump-http --gui-dump-http-port 9000`
- **THEN** the observation server listens on port 9000

#### Scenario: Enable observation via environment variable
- **WHEN** `ISTA_GUI_DUMP_HTTP=1` is set in the environment
- **THEN** the observation server starts without requiring the CLI flag

### Requirement: Observation server SHALL fail safe on startup
The observation server SHALL log a warning rather than throwing when startup fails, and the main application SHALL continue to launch normally.

#### Scenario: Server fails to bind
- **WHEN** the observation server cannot bind to the configured port
- **THEN** a warning is logged and the main application window still launches
