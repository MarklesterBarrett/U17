# Production Checklist

This implementation is new. It does not preserve legacy token output or legacy CSS variable aliases.

## Build Configuration

- Register the design token services through `DesignTokenManagementComposer`.
- Keep `DesignTokenManagementOptions.EnableTailwindOutput` aligned with deployment needs.
- Leave usage scanning disabled in production unless the scan scope and runtime cost are explicitly approved.
- Keep `DesignTokenManagementOptions.MaxPreviewJsonLength` at a safe size limit for backoffice preview requests.

## Rebuild Strategy

- Treat activation as the main rebuild trigger.
- Use manual rebuild only for operational recovery or output refresh.
- Keep rebuilds serialized through the management service to avoid overlapping writes and status races.
- Do not expose rebuild endpoints publicly.

## Cache Behaviour

- Public generated output is file-based and survives process restarts.
- Preview CSS is in-memory/backoffice-only and temporary.
- If the deployment platform caches static CSS aggressively, include cache-busting on deployment or rebuild events.

## Backup and Recovery

- Back up the token document store under `App_Data/DesignTokens/`.
- Back up generated outputs if deployment rollback expects them.
- Recovery order:
  1. restore token document storage
  2. verify the active document
  3. trigger rebuild
  4. confirm generated CSS and Tailwind output

## Generated File Paths

- CSS output default: `css/generated-tokens.css`
- Tailwind output default: `App_Data/DesignTokens/generated-tailwind-theme.json`
- Build status default: `App_Data/DesignTokens/build-status.json`

## Startup Behaviour

- The active document remains the source of truth after startup.
- Generated files are file-based and available immediately after deployment if persisted.
- If generated files are deployment artifacts instead of persistent runtime files, run a rebuild after deployment.

## Deployment Considerations

- Deploy code, then verify the active document still parses against the current runtime.
- Trigger a rebuild when moving between environments if generated outputs are not promoted with the release.
- Ensure the app identity can write the generated CSS, Tailwind, and build-status paths.
- Do not let public users access preview endpoints.

## Failure Safety

- Invalid imports must stay draft or invalid and never become active.
- Failed CSS or Tailwind writes must preserve the previous valid output.
- Preview builds must never write active output files.
- Atomic file replacement should remain enabled for CSS and Tailwind writers.

## Monitoring and Logging

Recommended checks:

- latest build success/failure
- latest successful build date
- active document id and name
- CSS write failures
- Tailwind write failures
- repeated validation failures
- unexpected usage scan cost if enabled

Recommended operational alerts:

- rebuild failure
- activation failure
- missing generated CSS file
- missing generated Tailwind file when enabled

## Troubleshooting Steps

1. Check the active document and latest build status.
2. Run Validate in backoffice.
3. Review diagnostics by stage.
4. Run Preview for isolated confirmation.
5. Trigger Rebuild.
6. Confirm generated file timestamps and contents.
7. If output write failed, inspect filesystem permissions and path configuration.

## Final Verification

- active document validates cleanly
- generated CSS exists and matches the active registry
- responsive declarations are present where expected
- theme selectors are present where expected
- Tailwind JSON references CSS variables
- picker results reflect active tokens
- preview remains isolated from active output
- no documentation claims legacy compatibility support
