# AGENTS.md

## Project

This repository is an Umbraco website built with server-rendered cshtml views and Tailwind CSS.

---

# Decision priority

When rules conflict, prioritise:

1. Accessibility
2. Simplicity
3. Deterministic rendering
4. Editor usability
5. Visual consistency
6. Developer convenience

---

# Core principles

- Prefer simple, readable, deterministic solutions over abstraction
- Default to server-rendered HTML with minimal JavaScript
- Do not introduce React, Vue, Alpine, or other client-side frameworks unless explicitly requested
- Do not add dependencies unless clearly justified
- Preserve semantic, accessible HTML
- Mobile-first always
- Keep implementations maintainable for developers and predictable for content editors

---

# Umbraco guidelines

- Prefer straightforward document types and compositions over complex inheritance structures
- Prefer Block List over Block Grid unless a grid-based editorial experience is explicitly required
- Keep templates, partials, and view models focused and shallow
- Avoid deeply nested partial chains and unnecessary wrapper elements
- Avoid custom property editors unless explicitly required
- Prefer deterministic rendering over magic helpers or hidden conventions
- Keep content modelling explicit and editor-friendly
- Avoid introducing unnecessary abstraction layers in rendering pipelines

---

# Tailwind and CSS guidelines

## General

- Tailwind is the default styling approach
- Prefer utility classes for layout and styling
- Extract reusable classes only when:
  - a pattern repeats 3+ times
  - class strings become difficult to maintain
- Avoid near-duplicate utility patterns across templates
- Avoid custom CSS unless Tailwind or tokens cannot express the requirement clearly
- All custom CSS must consume existing tokens

## Reusable classes

- Reusable classes are single sources of truth
- Define once and reuse consistently
- Include interactive states (`hover`, `focus`, `active`) within reusable class definitions

## Consistency

- Avoid ad hoc variables and one-off styling patterns
- Keep styling decisions centralised and predictable
- Do not hardcode visual values inside components
- Prefer composition and reuse over duplication

---

# Design token architecture

## Principles

- Use CSS custom properties for all design tokens
- Tokens should represent design intent rather than raw appearance
- Prefer semantic naming such as:
  - `--color-text-primary`
  - `--surface-default`
  - `--space-content-gap`
- Avoid visual naming such as:
  - `--blue-500`
  - `--large-radius`

## Token hierarchy

Use the following token structure:

```text
primitive -> alias -> semantic -> component
```

### Primitive tokens

Primitive tokens contain raw visual values such as:

- colors
- spacing
- typography
- radius
- shadows
- motion values

Primitive tokens must never be consumed directly by components.

### Alias tokens

Alias tokens map primitives into brand or system-level decisions and isolate future visual changes.

### Semantic tokens

Semantic tokens represent UI intent, such as:

- surface
- text
- border
- action
- focus
- spacing roles

Components should consume semantic tokens by default.

### Component tokens

Component tokens compose semantic tokens for specific reusable UI patterns.

Component tokens must not define raw visual values.

## Token rules

- Do not skip token layers unless explicitly documented
- Prefer existing tokens over introducing new ones
- New global tokens require justification
- Keep token naming semantic, consistent, and purpose-led
- Theme switching should primarily happen through token substitution
- Global token definitions should remain intentionally controlled and limited
- Semantic and component-facing token aliases should use kebab-case unless explicitly documented otherwise
- Tokens should act as the single source of truth for visual styling

## Design token goals

The visual identity should be primarily transformable through controlled token changes.

Global tokens should cover:

- color system
- typography
- spacing rhythm
- radius
- shadow
- layout widths
- motion
- component surface styling

Goals:

- enforce consistency
- enable scalable theming
- reduce styling duplication
- keep design and implementation aligned

---

# Responsive design

## Breakpoints

Mobile-first breakpoints:

- mobile: 320px+
- tablet: 768px+
- desktop: 1024px+


Responsive token objects should use:

```json
{
  "mobile": "",
  "tablet": "",
  "desktop": ""
}
```

Breakpoints must align with the Tailwind configuration.

## Responsive behaviour

- Mobile styles are the default baseline
- Enhance progressively for larger breakpoints
- Avoid desktop-first overrides
- Prefer intrinsic and fluid layouts where possible

---

# Accessibility

- All interactive controls must be keyboard accessible
- Maintain visible focus states
- Use semantic heading hierarchy
- Forms must include labels and validation messaging
- Do not rely solely on color to communicate meaning
- Preserve logical tab order
- Ensure sufficient contrast ratios
- Prefer native HTML behaviour before ARIA enhancements

---

# Performance and rendering

- Prefer server-side rendering over client-side rendering
- Avoid unnecessary hydration and runtime JavaScript
- Keep markup shallow and efficient
- Avoid unnecessary DOM complexity
- Optimise for maintainability before micro-optimisation
- Prefer predictable rendering over implicit behaviour

---

# Workflow expectations

For non-trivial work:

- Briefly explain the implementation approach before making changes
- Make the smallest sensible change
- Keep diffs reviewable
- Run relevant verification commands where available
- Summarise changed files and any notable follow-up risks

---

# Change boundaries

- Prefer incremental change over broad refactors
- Do not rename files or move folders unless necessary
- If requirements are ambiguous, choose the simplest implementation aligned with this document
- Preserve existing architectural direction unless explicitly asked to change it