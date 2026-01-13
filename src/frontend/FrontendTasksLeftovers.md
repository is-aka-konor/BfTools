FT-9 — E2E: PWA/Service Worker Behaviour

Goal: Validate SW registration, caches, and asset strategies.
Inputs: Workbox-built SW, Playwright.
Steps:

Assert navigator.serviceWorker.controller present; registration scope correct.

Inspect caches: app-shell files precached; data/*-<hash>.json and index/*-<hash>.json present after navigation to relevant pages.

Network strategy: set throttled/blocked network; verify cache hits for bundles; site-manifest.json uses network-first (falls back to cache if offline).

DoD: SW registered; expected cache entries exist; strategies behave as designed.

FT-10 — Visual Regression (Optional but Recommended)

Goal: Catch theme/styling regressions from `src/style.css`.
Inputs: Playwright screenshot assertions.
Steps:

Golden screenshots for: landing, spells list (default + filters active), rituals tab, talents list, a detail page, search modal open.

Compare with expect(page).toHaveScreenshot() on CI across Desktop/Mobile.

Add per-theme snapshots if multiple themes later.

DoD: Baselines committed; diffs cause CI failure with clear review artefacts.

FT-11 — Performance & PWA Audits (CI)

Goal: Enforce budgets and PWA best practices.
Inputs: Lighthouse CI or Playwright-Lighthouse integration in CI.
Steps:

Run Lighthouse against preview: assert PWA installable, SW active, fast FCP/LCP under agreed thresholds.

Fail CI if scores drop below budgets; output HTML reports as CI artefacts.

DoD: CI produces Lighthouse reports; budgets enforced.

FT-12 — CI Orchestration

Goal: Stable, reproducible test runs in CI.
Inputs: CI config (GitHub Actions/Azure Pipelines).
Steps:

Cache pnpm/npm; install; build; run test:unit with coverage.

Start preview server; run test:e2e (headless).

Upload Playwright traces/videos and coverage as artefacts.

Gate deploy on all tests green.

DoD: One-click pipeline runs unit + E2E + reports; deploy only on pass.

FT-13 — Test Documentation

Goal: Make tests easy to extend.
Inputs: /docs.
Steps:

docs/testing-frontend.md:

toolchain overview,

how to run unit/E2E locally,

fixtures policy,

adding a new category’s tests (list, detail, filters),

debugging tips (Playwright inspector, Vitest UI).

docs/testing-matrix.md: coverage by feature (routes, filters, search, PWA, a11y) with tick-boxes.

DoD: New contributors can add feature tests by following the doc without guidance.
