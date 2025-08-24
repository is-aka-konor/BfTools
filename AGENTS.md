# Repository Guidelines

## Project Structure & Module Organization
- `src/`
  - `BfSiteGen.Core`: C# models, IO reader/validation, canonical JSON, indexing, bundling.
  - `BfSiteGen.Cli`: CLI entry to build site bundles and manifest.
  - `Bfmd.Core` / `Bfmd.Extractors` / `Bfmd.Cli`: Markdown tooling and extractors.
- `src/frontend/`: Vite + Lit SPA (Tailwind + DaisyUI), offline search, service worker.
- `tests/`: C# unit/integration; `frontend/tests`: Vitest unit + Playwright e2e.
- Generated output: `dist-site/` (immutable data/index files, manifest), `output/` (source JSON).

## Build, Test, and Development Commands
- Backend
  - `dotnet build` — build all projects.
  - `dotnet test` — run all C# unit/integration tests.
  - `dotnet run --project src/BfSiteGen/BfSiteGen.Cli -- [outputRoot] [distRoot]` — produce bundles and manifest.
- Frontend (in `frontend/`)
  - `npm run dev` — Vite dev server.
  - `npm run build` / `npm run preview` — build and preview.
  - `npm run test:unit` — Vitest (jsdom) unit tests with coverage.
  - `npm run test:e2e` — Playwright smoke e2e (Node ≥ 18.19; install browsers).

## Coding Style & Naming Conventions
- C#: PascalCase types/members; camelCase locals/params; 4‑space indent; favor explicit, readable code. Keep category folders plural and files slug-driven.
- TypeScript: typed props, small Lit components, kebab‑case custom elements (e.g., `search-modal`).
- Central package versions: managed in `Directory.Packages.props`. Do not set `Version` on `PackageReference`s.

## Testing Guidelines
- Backend: xUnit v3 + NSubstitute. Place unit tests under `*.Tests.Unit`, integration under `*.Tests.Integration`.
- Frontend: Vitest + Testing Library + MSW + fake-indexeddb (`frontend/tests/unit/*.spec.ts`); Playwright in `frontend/tests/e2e/`.
- Coverage: Vitest uses V8 coverage; keep tests deterministic via fixtures/MSW.

## Commit & Pull Request Guidelines
- Commits: imperative mood, focused scope (e.g., "Add canonical hash for spells").
- PRs: include summary, linked issues, test plan, and screenshots/GIFs for UI changes. Ensure `dotnet test` and `npm run test:unit` pass.

## Security & Configuration Tips
- Dependencies are centrally pinned; upgrade via `Directory.Packages.props` (e.g., `System.Text.Json`).
- Unit tests must not hit the network; MSW mirrors `/dist-site/*` endpoints.
- Static bundling assumes immutable filenames (`<category>-<hash>.json`); avoid mutating existing hashes.

