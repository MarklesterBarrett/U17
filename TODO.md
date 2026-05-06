# TODO

## Site Settings Design Token Backlog

Use this as the working checklist for tightening the Site Settings design-token system. Tackle one section at a time so the CMS editor model, generated CSS, and component style pickers stay aligned.

## 1. Token Model Audit

- [ ] Decide the final token layers:
  - Primitive tokens: raw reusable values.
  - Semantic tokens: named usage intent.
  - Component tokens: optional per-component decisions.
- [ ] Confirm whether Site Settings should expose primitives only, semantic tokens only, or both.
- [ ] Rename editor labels consistently:
  - Use "Primitive Colours" or "Base Colours", not both.
  - Use "Spacing" instead of mixing "Spaces" and "Spacing".
  - Use "Typography" instead of "Fonts & Line Heights" if the group will also include weights and letter spacing.
- [ ] Add short descriptions to all empty token fields in `designtokens.config`.
- [ ] Confirm naming convention for aliases:
  - Recommended CSS custom property form: `--color-brand`, `--space-md`, `--radius-sm`, `--font-size-base`.
  - Avoid mixing raw names, semantic names, and component names in the same picker.

## 2. Colour Tokens

- [ ] Keep primitive/base colours as the raw colour palette source.
- [ ] Confirm required primitive colour fields:
  - Label.
  - Alias.
  - Value.
  - Optional built-in palette source.
- [ ] Decide whether built-in colours and custom colours should stay as separate block types or become one colour token block with a value source.
- [ ] Review semantic colour tokens:
  - Brand.
  - Brand hover.
  - Surface page.
  - Surface panel.
  - Surface panel muted.
  - Surface header.
  - Surface footer.
  - Text default.
  - Text strong.
  - Text muted.
  - Text inverse.
  - Border subtle.
  - Border strong.
  - Action primary background.
  - Action primary background hover.
  - Action primary text.
  - Action accent.
  - Focus ring.
- [ ] Decide if semantic colours should be editable block-list entries only, rather than fixed C# properties.
- [ ] Add missing semantic colour choices if needed:
  - Surface raised.
  - Surface overlay.
  - Text link.
  - Text link hover.
  - Border focus.
  - Status success/warning/error/info.
- [ ] Ensure generated CSS uses one naming pattern for colour variables.
- [ ] Check all colour pickers use semantic colours, not raw primitive colours, where the setting is usage-based.

## 3. Spacing Tokens

- [ ] Unify the spacing token system.
- [ ] Current duplication exists between numeric CSS variables such as `--space-050`, `--space-100`, `--space-150`, `--space-200`, `--space-300`, `--space-400`, `--space-600` and semantic aliases such as `space-none`, `space-xs`, `space-sm`, `space-md`, `space-lg`, `space-xl`.
- [ ] Choose one canonical spacing scale.
- [ ] Preferred direction: make semantic spacing tokens the source of truth everywhere.
- [ ] Add `space-none` as a real token or document it as a reserved value handled by rendering code.
- [ ] Confirm whether spacing should remain responsive by breakpoint:
  - Mobile.
  - Tablet.
  - Laptop.
  - Desktop.
- [ ] Decide if spacing tokens should be fixed fields or editable block-list entries.
- [ ] Align CSS custom properties with the chosen spacing token names.
- [ ] Update Tailwind/theme usage so spacing utilities and component styles reference the canonical spacing scale.
- [ ] Remove old numeric spacing variable names only after all CSS, Tailwind config, and component usage has migrated.

## 4. Radius Tokens

- [ ] Unify the radius token system.
- [ ] Current duplication exists between CSS variables such as `--radius-sm`, `--radius-md`, `--radius-lg`, `--radius-pill` and semantic aliases such as `radius-none`, `radius-sm`, `radius-md`, `radius-lg`, `radius-full`.
- [ ] Choose one canonical radius scale.
- [ ] Preferred direction: make semantic radius tokens the source of truth everywhere.
- [ ] Decide whether both `radius-pill` and `radius-full` are needed.
- [ ] Add `radius-none` as a real token or document it as a reserved value handled by rendering code.
- [ ] Align CSS custom properties with the chosen radius token names.
- [ ] Update Tailwind/theme usage so radius utilities and component styles reference the canonical radius scale.
- [ ] Remove legacy radius variable names only after all CSS, Tailwind config, and component usage has migrated.
- [ ] Remove any hardcoded radius alias maps from rendering/generation code once radius values are fully token-backed.

## 5. Typography Tokens

- [ ] Split typography into clear primitive groups:
  - Font families.
  - Font sizes.
  - Font weights.
  - Line heights.
  - Letter spacing.
- [ ] Add missing font weight tokens if components need consistent weights.
- [ ] Add missing letter spacing tokens only if there is a real design need.
- [ ] Decide whether type scale values should be fixed fields or editable block-list entries.
- [ ] Consider semantic typography tokens:
  - Text body.
  - Text small.
  - Heading sm/md/lg.
  - Display.
- [ ] Ensure generated CSS exposes typography variables consistently.
- [ ] Confirm component styles use typography tokens instead of hardcoded sizes.

## 6. Layout Tokens

- [ ] Review current layout tokens:
  - Layout gutter.
  - Content width.
  - Reading width.
  - Header height.
- [ ] Add missing layout primitives if useful:
  - Container narrow.
  - Container wide.
  - Section max width.
  - Grid columns.
  - Sidebar width.
- [ ] Decide whether layout values should be responsive like spacing.
- [ ] Align page, grid, and block layout code to the same layout tokens.
- [ ] Confirm header/footer settings use layout tokens where applicable.

## 7. Border Tokens

- [ ] Separate border colour tokens from border size/style primitives.
- [ ] Add border width tokens if needed:
  - Border width none.
  - Border width thin.
  - Border width thick.
- [ ] Add border style tokens only if more than `solid` is genuinely needed.
- [ ] Ensure border colour pickers use semantic colour tokens.

## 8. Shadow and Elevation Tokens

- [ ] Review current shadow tokens:
  - Raised.
  - Lifted.
  - Focus.
- [ ] Decide if `shadow-focus` belongs with shadows or interaction/focus tokens.
- [ ] Add elevation levels only if the UI has a real hierarchy:
  - Shadow none.
  - Shadow sm.
  - Shadow md.
  - Shadow lg.
- [ ] Ensure generated CSS and component usage use the same shadow names.

## 9. Motion Tokens

- [ ] Decide whether motion tokens belong in core Site Settings.
- [ ] Add duration primitives if needed:
  - Duration fast.
  - Duration normal.
  - Duration slow.
- [ ] Add easing primitives if needed:
  - Ease standard.
  - Ease in.
  - Ease out.
- [ ] Add motion tokens through `Additional Tokens` first unless they become common enough for dedicated fields.

## 10. Opacity and Z-Index Tokens

- [ ] Decide whether opacity primitives are needed:
  - Disabled opacity.
  - Overlay opacity.
  - Muted opacity.
- [ ] Decide whether z-index primitives are needed:
  - Base.
  - Header.
  - Dropdown.
  - Modal.
  - Toast.
- [ ] Keep these in `Additional Tokens` unless they are actively used in multiple places.

## 11. Additional Tokens

- [ ] Define what belongs in `Additional Tokens`.
- [ ] Avoid using `Additional Tokens` as a permanent dumping ground for core tokens.
- [ ] Use it for experimental or uncommon primitives:
  - Motion.
  - Opacity.
  - Z-index.
  - Rare layout values.
- [ ] Promote frequently used additional tokens into dedicated token groups.
- [ ] Review whether optional background surfaces such as `surface-secondary` should become semantic colour tokens instead of additional values.

## 12. CMS Editor Experience

- [ ] Group fields in the order editors should complete them:
  - Brand and identity.
  - Primitive colours.
  - Semantic colours.
  - Spacing.
  - Typography.
  - Layout.
  - Radius.
  - Borders.
  - Shadows.
  - Advanced.
- [ ] Make required fields mandatory where an incomplete token would break generated CSS.
- [ ] Add validation patterns for aliases where possible.
- [ ] Add examples to field descriptions.
- [ ] Make picker labels editor-friendly while keeping aliases developer-friendly.
- [ ] Check inline block editing works cleanly for all token block types.

## 13. Code and CSS Generation

- [ ] Reduce fixed token lists in `CmsDesignTokenProvider` once CMS block lists are the source of truth.
- [ ] Keep fallback support for legacy aliases only until content migration is complete.
- [ ] Ensure generated CSS has deterministic ordering.
- [ ] Decide whether generated token CSS should include comments grouped by token type.
- [ ] Validate that invalid or empty CMS values do not generate broken CSS.
- [ ] Confirm cache invalidation runs when Site Settings token content changes.

## 14. Component Usage

- [ ] Audit common style settings and grid style settings.
- [ ] Confirm background, text, and border colour settings use semantic colour tokens.
- [ ] Confirm spacing settings use canonical spacing tokens.
- [ ] Confirm radius settings use canonical radius tokens.
- [ ] Add missing token pickers only where components genuinely need editor control.
- [ ] Avoid exposing primitive tokens directly to component editors unless the setting is intentionally low-level.

## 15. Migration and Cleanup

- [ ] List all existing CSS custom properties and map each one to its target token.
- [ ] List all Tailwind theme values and map each one to its target token.
- [ ] Search templates, views, and CSS for old token names before removing aliases.
- [ ] Migrate content from fixed fields to block-list tokens if that becomes the chosen direction.
- [ ] Keep temporary fallback mappings only during migration.
- [ ] Remove legacy aliases once the CMS content and frontend code are fully migrated.

## Suggested Order

1. Token Model Audit.
2. Colour Tokens.
3. Spacing Tokens.
4. Radius Tokens.
5. Typography Tokens.
6. Layout Tokens.
7. Code and CSS Generation.
8. Component Usage.
9. Migration and Cleanup.
