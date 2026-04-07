# AGENTS.md

## Project

This repository is an Umbraco website built with plain cshtml views and Tailwind CSS.

## Core principles

- Must be simple, readable solutions over abstraction
- Use plain cshtml, minimal JavaScript, and server-rendered HTML by default
- Do not introduce React, Vue, Alpine, or client-side frameworks unless explicitly requested
- Do not add new dependencies unless explicitly justified
- Preserve accessible, semantic HTML
- Mobile-first always
- Keep components easy for content editors to use and hard to misuse

## Umbraco rules

- Prefer straightforward document types and compositions over clever structures
- Use blocklist, over block grid. Refer to Mobile-first always.
- Keep templates, partials, and view models simple
- Avoid unnecessary wrappers and deeply nested partial chains
- Think clean code. Avoid deep neted element.
- Do not introduce custom property editors unless explicitly requested
- Favour deterministic rendering over magic helpers

## CSS and Tailwind rules

- Tailwind is the default styling approach
- Use design tokens via CSS custom properties for color, spacing, type, radius, and motion

### Tokens

- Maximum: 60 global tokens (defined in `:root` or theme scope)
- New tokens require review and justification
- Prefer reuse of existing tokens over introducing new ones

### Tailwind usage

- Prefer utility classes for layout and styling
- Extract reusable classes only when the pattern repeats 3+ times or the class string becomes hard to maintain
- Do not mix multiple near-duplicate class patterns across templates

### Reusable classes

- Treat reusable classes as single sources of truth
- Define once, reuse across components
- Interactive states (`hover`, `focus`, `active`) must be included in the reusable class definition

### Custom CSS

- Avoid custom CSS unless Tailwind or tokens cannot express the requirement clearly
- Any custom CSS must use existing tokens

### Consistency

- Avoid ad hoc variables or one-off styling patterns
- Keep styling decisions centralised and predictable

### Breakpoints

- Mobile-first: 320px baseline
- Tablet: 768px+
- Laptop: 1024px+
- Desktop: 1440px+
- Responsive token values should use the keys `mobile`, `tablet`, `laptop`, and `desktop`
- Breakpoints must align with the Tailwind configuration

## Design-token goal

The visual identity should be transformable primarily by changing a controlled set of global CSS variables.
These variables should cover:

- color system
- typography
- spacing rhythm
- radius
- shadow
- layout widths
- motion
- component surface styling

## Accessibility

- All interactive controls must be keyboard accessible
- Maintain visible focus states
- Use semantic heading order
- Ensure forms have labels and error messaging
- Avoid relying on color alone

## Done means

Before completing a task:

- explain the plan briefly
- make the smallest sensible change
- keep diffs reviewable
- run relevant verification commands if available
- summarise changed files and any follow-up risks

## Behaviour

- For non-trivial tasks, plan first before editing
- If requirements are ambiguous, prefer the simplest implementation that matches this file
- Do not perform broad refactors unless asked
- Do not rename files or move folders unless necessary
