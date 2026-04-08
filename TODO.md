# TODO

## Design Tokens

- Unify the spacing token system.
- Current duplication exists between CSS variables such as `--space-050`, `--space-100`, `--space-150`, `--space-200`, `--space-300`, `--space-400`, `--space-600` and semantic design-token aliases such as `space-none`, `space-xs`, `space-sm`, `space-md`, `space-lg`, `space-xl`.
- Choose one canonical spacing scale.
- Preferred direction: make semantic spacing tokens the source of truth everywhere.
- Align CSS custom properties with the semantic spacing token names.
- Update Tailwind/theme usage so spacing utilities and component styles reference the canonical spacing token scale.
- Remove the old numeric spacing variable names only after all CSS, Tailwind config, and component usage has been migrated.

- Unify the radius token system.
- Current duplication exists between CSS variables such as `--radius-sm`, `--radius-md`, `--radius-lg`, `--radius-pill` and semantic design-token aliases such as `radius-none`, `radius-sm`, `radius-md`, `radius-lg`, `radius-full`.
- Choose one canonical radius scale.
- Preferred direction: make semantic radius tokens the source of truth everywhere.
- Align CSS custom properties with the semantic radius token names.
- Update Tailwind/theme usage so radius utilities and component styles reference the canonical radius token scale.
- Remove legacy radius variable names only after all CSS, Tailwind config, and component usage has been migrated.
- Remove the hardcoded radius alias map from the style renderer.
- Current hardcoded mapping in code is:
  - `radius-none` -> `0`
  - `radius-sm` -> `0.375rem`
  - `radius-md` -> `0.75rem`
  - `radius-lg` -> `1.25rem`
  - `radius-full` -> `999px`
- Replace this with token-backed radius data so radius values are not hardcoded in C#.
- Review whether `Additional Tokens` should carry optional background surfaces such as `surface-secondary`, and if so expose that as an additional background-color choice.
