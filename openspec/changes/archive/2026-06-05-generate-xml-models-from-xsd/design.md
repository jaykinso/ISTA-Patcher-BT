## Approach

Use the existing `ISTgenerAtor` analyzer project to generate source from XSD files supplied as `AdditionalFiles` by `ISTAlter.csproj`. The XSD files become the canonical description of the XML entities. The generator maps the supported XSD subset used by these contracts to C# classes, enums, XML serialization attributes, and data contract attributes.

## Schema Scope

The generator supports the constructs required by the current models:

- top-level `xs:element`, `xs:complexType`, and `xs:simpleType`
- ordered `xs:sequence` child elements
- `xs:attribute` properties
- inline simple enum restrictions
- `maxOccurs="unbounded"` list properties
- `nillable`, unqualified local elements, and XML data type annotations

## Handwritten Extensions

Behavior that cannot be represented by XSD remains in partial classes:

- `LicenseInfo.Clone()`
- `DealerMasterData.Serialize<T>()`
- constructors that initialize nested objects and lists
- inheritance from `EntitySerializer<T>` for license entities

CLR-only generation hints are carried in `xs:annotation/xs:appinfo` under the
`urn:ista-patcher:xsd-codegen` namespace. These annotations are intentionally
outside the XML instance contract and use explicit names:

- `codegen:class`
- `xsdType`
- `clrBaseTypes`
- `clrUsingNamespace`

## Tradeoffs

The generator intentionally supports the repository's XSD subset instead of becoming a general-purpose XSD-to-C# compiler. That keeps implementation small and deterministic while still moving the source of truth to XSD.
