# Design Token Architecture

This implementation is a single design token pipeline. It does not preserve legacy token output or legacy CSS variable aliases.

## Pipeline Diagram

```text
JSON input
  |
  v
DesignTokenJsonSource
  |
  v
SourceMerge
  |
  v
Parse
  |
  v
Normalise
  |
  v
Resolve
  |
  v
Validate
  |
  +--> CssGenerate ------> CssWrite
  |
  +--> TailwindGenerate -> TailwindWrite
  |
  +--> Diagnostics / Picker / Preview / Usage Scan
```

Rules:

- every production build flows through the same ordered stages
- no stage skips an earlier stage
- preview stops before file writes
- picker data comes from diagnostics over the active registry

## Source Layering Diagram

```text
Lowest priority
  Starter
  Imported
  CmsPrimitive
  CmsSemantic
  Component
Highest priority
```

Rules:

- higher priority sources override lower priority sources
- same-priority duplicates are errors
- component tokens are part of the same registry, not a second engine

## Token Lifecycle

```text
Draft JSON
  -> parseable token candidates
  -> typed normalized values
  -> resolved references
  -> validated registry
  -> generated CSS variables
  -> optional Tailwind theme JSON
  -> diagnostics/build report
```

Important details:

- token references use `{token.path}`
- CSS variable names are derived from token paths
- diagnostics keep raw, normalized, and resolved values visible

## Responsive Flow

```text
dimension token
  |
  +--> static dimension -> single CSS variable declaration
  |
  +--> responsive dimension
         -> mobile root value
         -> tablet media override when changed
         -> desktop media override when changed
```

Responsive behaviour:

- root uses the best available fallback in mobile/tablet/desktop order
- duplicate media declarations are skipped
- preview and theme variants use the same responsive CSS generation rules

## Theme Variant Flow

```text
Base sources + theme variant sources
  -> per-variant merge
  -> per-variant parse/normalise/resolve/validate
  -> selector-scoped CSS generation
  -> combined CSS output
```

Theme rules:

- default theme outputs to `:root`
- named variants output to their configured selectors
- inactive variants are excluded because they are not part of the loaded theme set
- variants inherit by reusing lower-priority base sources before variant overrides are applied

## Tailwind Integration Flow

```text
Validated registry
  -> deterministic token-path inspection
  -> supported token mappings only
  -> JSON theme.extend payload
  -> CSS variable references such as var(--color-brand-primary)
```

Tailwind rules:

- Tailwind never resolves raw values itself
- Tailwind consumes generated CSS variables
- unsupported mappings are skipped safely and surfaced as warnings

## Diagnostics Flow

```text
Unified pipeline
  -> stage timing
  -> stage errors
  -> warnings
  -> source traces
  -> dependency graph
  -> token list
  -> token detail
  -> optional usage scan
```

Diagnostic stages:

- `SourceMerge`
- `Parse`
- `Normalise`
- `Resolve`
- `Validate`
- `CssGenerate`
- `TailwindGenerate`
- `CssWrite`
- `TailwindWrite`

## Editor and Runtime Surfaces

Backoffice surfaces:

- JSON-first token editor
- validation
- preview
- activation
- rebuild
- export
- diagnostics
- picker

Runtime surfaces:

- generated CSS file
- generated Tailwind theme JSON

These runtime outputs are derived from the validated registry only.
