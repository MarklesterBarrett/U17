# Design Tokens

This design token system provides a single pipeline for token import, source layering, validation, CSS generation, Tailwind export, diagnostics, backoffice editing, reference picking, preview, component tokens, theme variants, and optional usage scanning.

This implementation is new. It does not preserve legacy token output or legacy CSS variable aliases.

## Overview

The system accepts token JSON, merges it with the configured source layers, normalises raw values into typed values, resolves references, validates the resolved registry, and then generates CSS custom properties plus optional Tailwind theme output.

The same pipeline is used for:

- imports
- validation
- draft activation
- live preview
- diagnostics
- token picker data
- token usage scanning context

Supported token types:

- `color`
- `dimension`
- `typography`
- `fontFamily`
- `fontWeight`
- `shadow`
- `border`
- `duration`
- `number`

Responsive dimensions are expressed using the `dimension` token type with `mobile`, `tablet`, and `desktop` breakpoint values.

## Pipeline

The pipeline runs in this order:

1. `SourceMerge`
2. `Parse`
3. `Normalise`
4. `Resolve`
5. `Validate`
6. `CssGenerate`
7. `TailwindGenerate`
8. `CssWrite`
9. `TailwindWrite`

Notes:

- `CssWrite` and `TailwindWrite` only run for real output builds.
- preview builds stop at generated preview CSS and do not write public output files.
- diagnostics report errors and warnings by stage.

## Source Layers

Source priority is:

1. `Starter`
2. `Imported`
3. `CmsPrimitive`
4. `CmsSemantic`
5. `Component`

Rules:

- higher priority sources override lower priority sources
- same-priority duplicates are errors
- component tokens are normal `DesignToken` instances in the same registry

## Token Types

### `color`

```json
{
  "color": {
    "brand": {
      "primary": {
        "$type": "color",
        "$value": "#0055ff"
      }
    }
  }
}
```

### `dimension`

```json
{
  "space": {
    "md": {
      "$type": "dimension",
      "$value": {
        "value": 24,
        "unit": "px"
      }
    }
  }
}
```

### Responsive dimension

```json
{
  "space": {
    "page": {
      "$type": "dimension",
      "$value": {
        "mobile": { "value": 16, "unit": "px" },
        "tablet": { "value": 24, "unit": "px" },
        "desktop": { "value": 32, "unit": "px" }
      }
    }
  }
}
```

### `fontFamily`

```json
{
  "font": {
    "family": {
      "sans": {
        "$type": "fontFamily",
        "$value": "Inter, Arial, sans-serif"
      }
    }
  }
}
```

### `fontWeight`

```json
{
  "font": {
    "weight": {
      "bold": {
        "$type": "fontWeight",
        "$value": 700
      }
    }
  }
}
```

### `number`

```json
{
  "opacity": {
    "disabled": {
      "$type": "number",
      "$value": 0.5
    }
  }
}
```

### `duration`

```json
{
  "motion": {
    "fast": {
      "$type": "duration",
      "$value": {
        "value": 150,
        "unit": "ms"
      }
    }
  }
}
```

### `typography`

```json
{
  "font": {
    "family": {
      "sans": { "$type": "fontFamily", "$value": "Inter, Arial, sans-serif" }
    },
    "weight": {
      "regular": { "$type": "fontWeight", "$value": 400 }
    }
  },
  "typography": {
    "body": {
      "$type": "typography",
      "$value": {
        "fontFamily": "{font.family.sans}",
        "fontWeight": "{font.weight.regular}",
        "fontSize": { "value": 16, "unit": "px" },
        "lineHeight": 1.5,
        "letterSpacing": { "value": 0, "unit": "" }
      }
    }
  }
}
```

### `shadow`

```json
{
  "shadow": {
    "card": {
      "$type": "shadow",
      "$value": {
        "color": "#00000022",
        "offsetX": { "value": 0, "unit": "px" },
        "offsetY": { "value": 6, "unit": "px" },
        "blur": { "value": 16, "unit": "px" },
        "spread": { "value": 0, "unit": "" }
      }
    }
  }
}
```

### `border`

```json
{
  "border": {
    "default": {
      "$type": "border",
      "$value": {
        "width": { "value": 1, "unit": "px" },
        "style": "solid",
        "color": "#d1d5db"
      }
    }
  }
}
```

## References

Reference syntax is:

```text
{token.path}
```

Valid examples:

- `{color.brand.primary}`
- `{semantic.action.primary.background}`
- `{font.family.sans}`
- `{component.button.primary.background}`

Invalid examples:

- `color.brand.primary`
- `var(--color-brand-primary)`
- `{color.brand.primary`
- `prefix-{color.brand.primary}`
- `{color.brand.primary}.hover`

Rules:

- full-token references must match the expected token type
- typography fields resolve by field type
- border and shadow fields resolve by field type
- responsive dimension breakpoints can reference static dimensions, not responsive dimensions
- embedded references inside longer strings are not supported

Type compatibility examples:

- `typography.fontFamily` -> `fontFamily`
- `typography.fontWeight` -> `fontWeight`
- `typography.fontSize` -> `dimension`
- `typography.lineHeight` -> `number`
- `typography.letterSpacing` -> `dimension`
- `border.width` -> `dimension`
- `border.color` -> `color`
- `shadow.color` -> `color`
- `shadow.blur` -> `dimension`

Circular references are rejected during `Resolve`. The resolver reports the reference chain and no active output is generated from that invalid build.

## Naming Conventions

- Umbraco property aliases use camelCase
- internal token paths use dot notation
- generated CSS variables use kebab-case
- Tailwind keys use clean semantic names derived from token paths

Example:

- Umbraco: `actionPrimaryBackground`
- token path: `semantic.action.primary.background`
- CSS variable: `--semantic-action-primary-background`
- reference string: `{semantic.action.primary.background}`

## Import and Export

Import behaviour:

- imported JSON is validated through the full pipeline
- invalid imports can be saved as invalid drafts for inspection
- valid imports can be saved as drafts or activated

Export behaviour:

- active imported JSON can be exported directly
- starter JSON can be exported directly
- merged resolved output can be exported as formatted resolved JSON

## Generated CSS

Simple token output:

```css
:root {
  --color-brand-primary: #0055ff;
  --motion-fast: 150ms;
}
```

Typography output:

```css
:root {
  --typography-body-font-family: Inter, Arial, sans-serif;
  --typography-body-font-weight: 400;
  --typography-body-font-size: 16px;
  --typography-body-line-height: 1.5;
  --typography-body-letter-spacing: 0;
}
```

Shadow output:

```css
:root {
  --shadow-card: 0 6px 16px 0 #00000022;
}
```

Border output:

```css
:root {
  --border-default-width: 1px;
  --border-default-style: solid;
  --border-default-color: #d1d5db;
}
```

Responsive dimension output:

```css
:root {
  --space-page: 16px;
}

@media (min-width: 768px) {
  :root {
    --space-page: 24px;
  }
}

@media (min-width: 1024px) {
  :root {
    --space-page: 32px;
  }
}
```

Theme variant output:

```css
:root {
  --color-surface-page: #ffffff;
  --color-text-default: #111827;
}

[data-theme="dark"] {
  --color-surface-page: #111827;
  --color-text-default: #f9fafb;
}
```

## Tailwind Integration

Tailwind export produces a JSON object that maps supported tokens to `theme.extend` entries referencing generated CSS variables.

Supported mappings include:

- `color` -> `theme.extend.colors`
- spacing-like `dimension` tokens -> `theme.extend.spacing`
- `fontFamily` -> `theme.extend.fontFamily`
- `fontWeight` -> `theme.extend.fontWeight`
- `duration` -> `theme.extend.transitionDuration`
- opacity-like `number` tokens -> `theme.extend.opacity`
- `shadow` -> `theme.extend.boxShadow`
- `typography` -> `theme.extend.fontSize`

CommonJS example:

```js
const designTokens = require("./App_Data/DesignTokens/generated-tailwind-theme.json");

module.exports = {
  content: ["./Views/**/*.cshtml", "./src/**/*.{js,ts}"],
  theme: {
    extend: {
      ...designTokens.theme.extend
    }
  }
};
```

ESM example:

```js
import designTokens from "./App_Data/DesignTokens/generated-tailwind-theme.json" with { type: "json" };

export default {
  content: ["./Views/**/*.cshtml", "./src/**/*.{js,ts}"],
  theme: {
    extend: {
      ...designTokens.theme.extend
    }
  }
};
```

Example utility usage:

- `bg-brand-primary`
- `text-brand-primary`
- `p-md`
- `font-sans`
- `duration-fast`
- `shadow-card`

## Preview

Preview builds use the same parse, merge, normalise, resolve, validate, and CSS generation pipeline as active builds.

Preview behaviour:

- preview CSS is returned as a string
- preview CSS is injected only into the protected backoffice preview surface
- preview does not activate the draft
- preview does not replace `css/generated-tokens.css`
- invalid preview builds show diagnostics and do not replace the last valid preview surface

## Diagnostics

Diagnostics expose:

- build stage results
- grouped errors
- grouped warnings
- token list
- token detail
- dependency graph
- source traces
- optional usage scan findings
- latest build report

Warnings are separate from errors. Warnings do not block draft saving. Activation only fails on warnings when `FailOnWarnings` is enabled.

## Editor Workflow

1. Import or paste token JSON.
2. Validate the draft.
3. Save draft if needed.
4. Use Preview to build isolated preview CSS.
5. Review diagnostics, warnings, and token detail.
6. Activate only when the build is valid.
7. Export the active imported JSON when needed.
8. Trigger a rebuild if output files need to be regenerated.

The JSON editor remains the primary editing surface. The token picker inserts reference syntax only, for example `{color.brand.primary}`.

## Component Tokens

Component tokens define semantic roles for concrete UI components while still referencing primitive and semantic tokens.

Examples:

- `component.button.primary.background`
- `component.button.primary.text`
- `component.card.background`
- `component.input.focusRing`

## Theme Variants

Theme variants allow one token engine to output multiple scoped selectors such as:

- `:root`
- `[data-theme="light"]`
- `[data-theme="dark"]`
- `[data-theme="high-contrast"]`
- brand-specific selectors

The default theme still emits to `:root`.

## Token Usage Scanning

Usage scanning is optional and disabled by default.

When enabled it can report:

- generated CSS variables used in code
- token references used in code
- missing generated CSS variables
- missing token references
- unused generated tokens
- hardcoded design values

The scanner is read-only. It does not rewrite files, delete tokens, or fail production builds by default.

## Troubleshooting

Common issues:

- invalid JSON
  Use Validate first. Parse errors stop the pipeline early.
- unsupported `$type`
  The parser only accepts the documented token types.
- missing `$value`
  Token candidates must include both `$type` and `$value`.
- unresolved reference
  Check the exact referenced token path and token type compatibility.
- circular reference
  Break the dependency loop. The resolver reports the circular chain.
- invalid dimension unit
  Supported dimension units are `px`, `rem`, `em`, `%`, `vw`, `vh`, `vmin`, `vmax`, `ch`, and `ex`.
- non-zero dimension missing unit
  Non-zero dimensions require a unit.
- invalid colour value
  Use valid hex, rgb(a), or hsl(a) values.
- Tailwind mapping skipped
  Some token types or paths do not map to Tailwind. This is a warning, not a second token engine.
- CSS generation failed
  Check resolved values and diagnostics for missing or incompatible values.
- stale generated CSS
  Rebuild after activation. Failed builds do not replace the existing generated output.

## Developer Extension Guide

Add a new token type:

1. extend `DesignTokenType`
2. update `DesignTokenJsonParser` type mapping
3. add a normalized value model
4. add normalization logic
5. add reference resolution logic
6. add validation logic
7. add CSS generation logic
8. add diagnostics coverage
9. add tests

Add a new source layer:

1. extend `DesignTokenSourceType`
2. assign a priority in `DesignTokenSourcePriority`
3. update source loading
4. update source trace and merge tests

Add a new validation rule:

1. extend `DesignTokenValidator`
2. return explicit token path and field data
3. cover the new rule in validator and diagnostics tests

Add a new Tailwind mapping:

1. update `DesignTokenTailwindExporter`
2. keep mappings deterministic and registry-driven
3. add exporter tests and diagnostics warning coverage

Add a new diagnostics warning:

1. add it in `DesignTokenDiagnosticsService`
2. keep warnings separate from errors
3. include stage, token path, field, and message where applicable

## Example Files

See:

- [Examples/starter-tokens.json](./Examples/starter-tokens.json)
- [Examples/component-tokens.json](./Examples/component-tokens.json)
- [Examples/theme-variants.json](./Examples/theme-variants.json)
- [Tests/Fixtures](./Tests/Fixtures/)

## Parseable Documentation Snippets

The snippets below are full examples intended to stay parseable in tests.

<!-- parseable-json:basic -->
```json
{
  "color": {
    "brand": {
      "primary": {
        "$type": "color",
        "$value": "#0055ff"
      }
    }
  },
  "space": {
    "page": {
      "$type": "dimension",
      "$value": {
        "mobile": { "value": 16, "unit": "px" },
        "tablet": { "value": 24, "unit": "px" },
        "desktop": { "value": 32, "unit": "px" }
      }
    }
  }
}
```

<!-- parseable-json:component -->
```json
{
  "color": {
    "brand": {
      "primary": { "$type": "color", "$value": "#0055ff" }
    }
  },
  "semantic": {
    "action": {
      "primary": {
        "background": { "$type": "color", "$value": "{color.brand.primary}" }
      }
    }
  },
  "component": {
    "button": {
      "primary": {
        "background": {
          "$type": "color",
          "$value": "{semantic.action.primary.background}"
        }
      }
    }
  }
}
```

<!-- parseable-json:themes -->
```json
{
  "color": {
    "surface": {
      "page": { "$type": "color", "$value": "#ffffff" }
    }
  },
  "themes": {
    "dark": {
      "$selector": "[data-theme=\"dark\"]",
      "color": {
        "surface": {
          "page": { "$type": "color", "$value": "#111827" }
        }
      }
    }
  }
}
```
