## Why

Rheingold XML model classes currently duplicate schema information in handwritten C# attributes. This makes XML contract changes error-prone because the schema and serializer attributes are not represented as first-class build inputs.

## What

- Add XSD files for the existing Rheingold license and dealer data XML contracts.
- Generate the corresponding C# entity classes from those XSD files during compilation.
- Keep non-schema behavior, such as cloning and serialization helpers, in handwritten partial classes.
- Preserve existing public type names, namespaces, XML serialization attributes, and runtime behavior.

## Impact

- Affects `ISTAlter` XML model build inputs.
- Extends `ISTgenerAtor` with XSD-driven model generation.
- Existing code should continue using the same model type names.
