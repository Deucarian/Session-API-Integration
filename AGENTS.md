# Deucarian Session API Integration Agent Notes

Package ID: `com.deucarian.session.api-integration`
Repository: `Deucarian/Session-API-Integration`

Follow the canonical Deucarian governance docs in [Package Registry](https://github.com/Deucarian/Package-Registry/blob/main/ARCHITECTURE.md), especially capability ownership and dependency rules.

## Ownership

This package owns:

- session-aware API adapter behavior.

This package must not own:

- Own independent framework behavior outside the Session and API adapter boundary.
- Duplicate target-package implementation logic.

## Dependencies

Allowed dependency shape:

- May depend only on declared target packages and small infrastructure packages it directly uses. Current target boundary: Session and API.

Required dependencies and why:

- `com.deucarian.api`: HTTP/API capability target.
- `com.deucarian.session`: session lifecycle target.

Optional/version-defined dependencies:

- None.

Architecture exceptions:

- Integration package dependencies are hard requirements for the Integration package itself.

## Policies

- Logging: Use Logging only if declared and directly used; no direct Unity Debug calls.
- Common: Do not add Common unless this adapter directly owns transient Unity cleanup.
- Editor UI: No editor shell ownership.
- Diagnostics: No diagnostics ownership unless an explicit optional diagnostics integration is approved.
- Testing: Tests should cover adapter behavior between declared targets only.

## Validation

Run the shared validator before committing:

```powershell
python C:/Repositories/Package-Registry/Tools/deucarian_package_validator.py --registry-root C:/Repositories/Package-Registry --repository-root . --config deucarian-package.json
```

Also run existing repository tests when changing code or asmdefs. Documentation-only updates should still run `git diff --check`.

## Codex Guidance

- Inspect current files before changing anything.
- Work on `develop`; do not edit or merge `main` unless the task is promotion-only.
- Do not edit `Library/PackageCache`.
- Do not guess package versions or dependency versions.
- Do not add package dependencies casually; update asmdefs, `package.json`, `deucarian-package.json`, Package Registry, and fallback catalogs together when a dependency is truly required.
- Do not create local copies of shared helpers.
- Keep commits focused and report exactly what changed and what was validated.

## Before Adding Code

- Confirm the change fits this package's ownership boundary.
- Reuse existing local patterns and helpers.
- Avoid broad refactors without audit support.
- Preserve runtime/editor behavior unless the task explicitly asks to change it.

## Before Adding A Dependency

- Is the capability already owned by that package?
- Is it used by production code, editor code, sample code, or tests?
- Does the asmdef reference match `package.json`?
- Does `deucarian-package.json` need updating?
- Does Package Registry need updating?
- Does Package Installer fallback catalog need updating?
- Does Bootstrap fallback catalog need updating?
- Are exact versions propagated without guessing?

## Before Adding A Helper

- Is this package the capability owner?
- Is this behavior repeated in at least three production packages?
- Is there an existing owner package?
- Should this remain local?
- Has the audit been updated?

## Debug And Unity Object Lifetime

- Do not add direct Unity Debug calls. Add Logging only if this package directly needs logging and governance approves the dependency.
- Do not copy Common lifetime helpers. Add Common only if production code directly owns transient Unity object cleanup.
- Test fixture teardown may use `DestroyImmediate` directly.
