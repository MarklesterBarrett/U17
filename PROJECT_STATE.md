# Project State

## Summary

This project is moving the Umbraco CMS design-token model toward a more editor-friendly Theme model:

- Base Colours
- Style Settings
- semantic usage in components/styles

Editors define a controlled base palette first, then map theme/style roles to those base colours. Components should consume semantic style values, not raw base colour values.

## Current Theme Model

### Base Colours

Base Colours are authored as a multi-item Block List directly on the Site Settings document under the `Theme` tab.

The property is:

- name: `Base Colours`
- alias: `baseColors`
- file: `site/uSync/v17/ContentTypes/sitesettings.config`

The Base Colours list supports two block types:

- `Built-in colour`
  - content type alias: `colorToken`
  - file: `site/uSync/v17/ContentTypes/colortoken.config`
  - field: `Select a colour`
  - stores a built-in palette value such as `blue-100`
- `Custom colour`
  - content type alias: `customColorToken`
  - file: `site/uSync/v17/ContentTypes/customcolortoken.config`
  - fields: `Name`, `Select a colour`
  - the name is the base colour key shown to semantic/style editors

The `baseColors` property on `siteSettings` is a direct `Umbraco.BlockList` using:

- `site/uSync/v17/DataTypes/SiteSettingsThemeBaseColourItems.config`

### Spacings

Spacing settings are authored directly on the `Site Settings` document under the `Theme` tab.

Core properties:

- `spacingXs`
- `spacingSm`
- `spacingMd`
- `spacingLg`
- `spacingXl`
- `spacing2Xl`
- `spacingTokens`

### Typography

Typography settings are authored directly on the `Site Settings` document under the `Theme` tab.

Core properties:

- `fontFamilySans`
- `fontFamilyDisplay`
- `fontSizeSm`
- `fontSizeBase`
- `fontSizeLg`
- `fontSizeXl`
- `fontSize2Xl`
- `lineHeightTight`
- `lineHeightBase`

### Style Settings

The former editor-facing `Design Tokens` property is now `Style Settings` under the `Theme` tab.

The property is:

- name: `Style Settings`
- alias: `styleSettings`
- content type alias: `styleSettings`
- file: `site/uSync/v17/ContentTypes/designtokens.config`

Internal compatibility remains in runtime code for old aliases where needed:

- `designTokens`
- `primitiveColors`
- `primitiveAlias`

### Semantic Colours

Semantic colours are currently represented as fixed style properties on `styleSettings`.

Rules:

- fixed colour properties reference base colours by key
- built-in base keys are values like `blue-100`
- custom base keys come from the custom colour `Name`
- components should consume semantic values such as `surface-page`, `text-default`, or `action-primary-bg`

## Runtime / Provider Changes

The provider resolves the CMS colour model like this:

- reads `baseColors` from Site Settings
- falls back to legacy `primitiveColors` where present
- resolves built-in colours from palette aliases
- resolves custom colours from their eye-dropper value
- still supports old `semanticColors` block-list values as a compatibility path if present
- resolves fixed style colour properties from base colour references

Main file:

- `site/DesignTokens/CmsDesignTokenProvider.cs`

Supporting CMS data sources:

- `site/Contentment/PrimitiveColorTokenDataSource.cs`
- `site/Contentment/TailwindPrimitiveColorDataSource.cs`

Note: compatibility wrapper classes still exist for old Contentment data source names, so older database datatype config can resolve during transition.

## Backoffice UI Changes

The custom colour swatch picker supports a grouped palette-table layout:

- shade headers across the top
- family names down the left
- compact swatch grid
- selected swatch outline
- uses Umbraco's `UmbChangeEvent` for value changes

File:

- `site/wwwroot/App_Plugins/DesignTokens/color-swatches.element.js`

Base colour block previews show:

- a 30px colour swatch
- the base colour key, for example `blue-100`

File:

- `site/wwwroot/App_Plugins/DesignTokens/primitive-color-block-view.element.js`

Registered in:

- `site/wwwroot/App_Plugins/DesignTokens/umbraco-package.json`

## Radius / Style Renderer Changes

Earlier in the session:

- removed hard-coded radius value resolution from the style renderer
- radius now resolves from token-backed values like spacing does
- `none` / `radius-none` still intentionally resolve to `0`

File:

- `site/DesignTokens/DesignTokenStyleRenderer.cs`

## Known Issues / Incomplete Work

### 1. Backoffice needs restart/hard refresh after schema or plugin changes

New or changed backoffice extension files may require:

1. restart the site
2. hard refresh the backoffice
3. re-import uSync after content type/data type changes

### 2. Base colour selector depends on saved/published Base Colours

The base colour selector used by Style Settings is data-driven from saved site settings. Editors may need to:

1. create Base Colours
2. save/publish
3. reopen Style Settings editing

This is currently accepted as the pragmatic flow.

### 3. Normal build can fail if the site executable is running

The running site can lock files under `bin/Debug/net10.0`.

Use a separate output folder for verification when needed:

```powershell
dotnet build .\site\site.csproj --no-restore -o .\site\bin-temp\verify
```

## Last Confirmed Working State

Confirmed:

- `Design` tab is now `Theme`
- `Design Tokens` editor-facing wording is now `Style Settings`
- `Base Colours` sits directly under the Theme tab
- Base Colours uses a real multi-item `Umbraco.BlockList`
- Base Colours supports `Built-in colour` and `Custom colour`
- custom colours have `Name` and eye-dropper `Select a colour`
- block element aliases were kept as `colorToken` and `customColorToken` for Block List compatibility
- runtime provider resolves both new `baseColors` and legacy `primitiveColors`
- theme colour, spacing, and typography settings are authored directly on `siteSettings` without single-block wrappers
- verification build passed using a separate output folder

Current recurring warning:

- `MailKit` 4.14.1 has a known moderate severity vulnerability warning during build

## Recommended Next Steps

1. Re-import uSync.
   Confirm `Base Colours` allows adding both `Built-in colour` and `Custom colour` as list items.

2. Verify built-in base colour flow.
   Add `blue-100`, save/publish, then confirm it appears in Style Settings colour selection.

3. Verify custom base colour flow.
   Add a custom colour with a name, save/publish, then confirm it appears in Style Settings colour selection and resolves on the frontend.

## Important Files

- `site/DesignTokens/CmsDesignTokenProvider.cs`
- `site/DesignTokens/DesignTokenStyleRenderer.cs`
- `site/Contentment/ContentmentComposer.cs`
- `site/Contentment/PrimitiveColorTokenDataSource.cs`
- `site/Contentment/TailwindPrimitiveColorDataSource.cs`
- `site/wwwroot/App_Plugins/DesignTokens/color-swatches.element.js`
- `site/wwwroot/App_Plugins/DesignTokens/primitive-color-block-view.element.js`
- `site/wwwroot/App_Plugins/DesignTokens/umbraco-package.json`
- `site/uSync/v17/ContentTypes/sitesettings.config`
- `site/uSync/v17/ContentTypes/designtokens.config`
- `site/uSync/v17/ContentTypes/colortoken.config`
- `site/uSync/v17/ContentTypes/customcolortoken.config`
- `site/uSync/v17/ContentTypes/semanticcolortoken.config`
- `site/uSync/v17/DataTypes/PrimitiveColorTokenValues.config`
- `site/uSync/v17/DataTypes/SiteSettingsThemeBaseColourItems.config`
- `site/uSync/v17/DataTypes/SiteSettingsDesignSystemSemanticColorTokens.config`
- `site/uSync/v17/DataTypes/TailwindPrimitiveColorSwatches.config`
