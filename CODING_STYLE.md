# Coding Style

## Core Rules
- Do not use namespaces.
- Expose inspector fields via `[SerializeField]` on private fields only.
- Naming conventions:
- Fields: `_camelCase`
- Locals and parameters: `camelCase`
- Methods and properties: `PascalCase`
- Anything not specified here follows the .NET standard style.
- Prefer minimal state and immutability.

## Notes
- For Unity serialization, prefer `[SerializeField] private` over `public` fields.
- Keep state changes explicit and localized; avoid hidden side effects.
