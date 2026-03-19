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
- Use a constrained design-token layer driven by CSS custom properties
- Maximum CSS variable budget: 60 global variables
- Do not create ad hoc variables unless approved
- Prefer token reuse over new token creation
- Use utility classes first, component extraction second
- Avoid custom CSS unless Tailwind or tokens cannot express the requirement cleanly
- support for 320px baseline phones, from 768px tablet, and 1024 laptop, 1440 desktop.

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
