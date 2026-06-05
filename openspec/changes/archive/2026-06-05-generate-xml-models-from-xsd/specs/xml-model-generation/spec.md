## ADDED Requirements

### Requirement: XSD-backed Rheingold XML Models

Rheingold XML entity classes SHALL be generated from checked-in XSD schema files during compilation.

#### Scenario: License XML models are generated

- **GIVEN** the license schema is included in the project as a generator input
- **WHEN** the project is compiled
- **THEN** `LicenseInfo`, `LicensePackage`, and `LicenseType` are available with the existing XML serialization contract.

#### Scenario: Dealer data XML models are generated

- **GIVEN** the dealer data schema is included in the project as a generator input
- **WHEN** the project is compiled
- **THEN** dealer data classes and enums are available with the existing XML serialization contract.

#### Scenario: Non-schema behavior is preserved

- **GIVEN** generated XML model classes are used by existing code
- **WHEN** callers clone licenses or serialize dealer master data
- **THEN** those handwritten behaviors continue to work through partial class extensions.
