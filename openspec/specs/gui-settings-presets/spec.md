## ADDED Requirements

### Requirement: GUI SHALL load parameter presets from a settings file
The system SHALL provide a `GuiSettingsService` that loads parameter preset values from `gui-settings.json` deployed alongside the application. The presets file SHALL map command names to property names to preset string values. When a command tab is initialised, the system SHALL apply matching presets to parameter editors.

#### Scenario: Apply preset values on tab initialisation
- **WHEN** a command tab is created for a command that has entries in `gui-settings.json`
- **THEN** parameter editors for matching properties are pre-filled with the preset values

#### Scenario: Preset file missing produces default settings
- **WHEN** `gui-settings.json` does not exist at application startup
- **THEN** the system creates a default `GuiSettings` instance with no presets and the application continues normally

#### Scenario: Presets apply only to matching command and property
- **WHEN** `gui-settings.json` contains a preset for a different command or property name
- **THEN** the preset does not affect parameters of the current command tab

### Requirement: GUI SHALL separate preset template from user preferences
The system SHALL treat `gui-settings.json` as a read-only preset template copied from source assets by the build system. Runtime user preferences (theme selection) SHALL be persisted separately in `user-preferences.json`. The system MUST NOT write to `gui-settings.json` at runtime.

#### Scenario: Theme change persists to user-preferences.json
- **WHEN** the user cycles the application theme
- **THEN** the new theme value is written to `user-preferences.json` and `gui-settings.json` is not modified

#### Scenario: User preferences override defaults on next launch
- **WHEN** the application starts and `user-preferences.json` exists with a theme value
- **THEN** the persisted theme is applied, overriding the default theme from `gui-settings.json`

#### Scenario: Runtime does not mutate preset file
- **WHEN** any runtime operation (theme change, preset application, command execution) occurs
- **THEN** `gui-settings.json` is never written to or modified

### Requirement: GUI SHALL support manual preset reset per command tab
Each command tab SHALL provide a "Reset to Preset" action that reapplies the preset values from `gui-settings.json` to all parameter editors for the current command.

#### Scenario: Reset restores preset values
- **WHEN** a user has modified parameter values away from presets and activates "Reset to Preset"
- **THEN** all parameter editors for the current command are restored to the values defined in `gui-settings.json`

#### Scenario: Reset has no effect when no presets exist
- **WHEN** a user activates "Reset to Preset" on a command tab that has no matching presets
- **THEN** parameter values remain unchanged

### Requirement: GUI SHALL indicate when presets are available
Each command tab SHALL expose a `HasPreset` boolean that is `true` when at least one parameter for the current command has a matching preset entry in `gui-settings.json`.

#### Scenario: Preset indicator is true when presets exist
- **WHEN** `gui-settings.json` contains at least one entry matching the current command name
- **THEN** `HasPreset` returns `true`

#### Scenario: Preset indicator is false when no presets exist
- **WHEN** `gui-settings.json` contains no entries for the current command name
- **THEN** `HasPreset` returns `false`
