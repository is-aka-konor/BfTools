# BfSiteGen – Static Site Bundler

BfSiteGen takes the extracted content (DTO JSONs) and produces a static site data bundle with:

- Immutable, hashed data bundles per category under `data/`
- Prebuilt search indexes under `index/`
- A site manifest (`site-manifest.json`) describing versions/counts
- Deep‑link route stubs for static hosts
- Optional copied SPA assets (index.html, JS/CSS, icons) when using Publish
- Optional ZIP package for distribution (Publish)

This document describes the step‑by‑step generation process, expected outputs after each step, and how to run it locally.

## Prerequisites

- .NET SDK 9+
- Frontend built once via Vite (Node.js 18+) only if you plan to Publish
  - `cd src/frontend && npm ci && npm run build`

The content DTO JSONs are expected to exist under `output/data/<category>/*.json`. These are usually produced by the `Bfmd` extractors pipeline.

## Quick Start

1) Build the frontend (one‑time or when assets change, only for Publish)

```
cd src/frontend
npm ci
npm run build
```

2) Generate data bundles and indexes (data‑only)

```
dotnet run --project src/BfSiteGen/BfSiteGen.Cli -- [outputRoot] [distRoot]
```

- `outputRoot` (default: `output`) – where category JSONs live (input)
- `distRoot`   (default: `dist-site`) – where the static site is emitted (output)
- The CLI clears `distRoot` before writing to avoid stale artifacts.

3) Optional: Publish with SPA assets + ZIP

```
dotnet run --project src/BfSiteGen/BfSiteGen.Cli -- [outputRoot] [distRoot] --publish
```

This copies SPA assets into `distRoot/` and creates `site-bundle-<build>.zip` in the working directory.

## Step‑by‑Step Pipeline

The CLI orchestrates these steps in order. After each step you can check the corresponding output in `distRoot`.

### 1) Read & Validate Content (DTOs)

- Source: `outputRoot/data/<category>/*.json`
- Operation: `System.Text.Json` deserialisation into shared DTOs from `BfCommon.Domain`
- Validation: common requireds (`slug`, `name`, `description`, `src.abbr`, `src.name`) plus category‑specific checks
- Output: In‑memory `ContentLoadResult` with DTO lists and any validation errors (printed to console if needed)

You should see console output starting the build; if inputs are missing, the step reports errors and stops early.

### 2) Write Canonical Category Bundles (Streaming)

- Target: `distRoot/data/<category>-<hash>.json`
- Operation:
  - Stream a JSON array via `Utf8JsonWriter`, one item at a time
  - Render description HTML on demand (Markdown → HTML) per item
  - Write properties in alphabetical order for determinism
  - Compute SHA‑256 of the exact bytes written to derive `<hash>`
- Output per category (examples):
  - `data/spells-<hash>.json`
  - `data/talents-<hash>.json`
  - `data/classes-<hash>.json`
  - `data/backgrounds-<hash>.json`
  - `data/lineages-<hash>.json`

Notes:
- Bundles include `description` (HTML) but do not duplicate markdown. Structured fields are preserved.
- `sources` is a standardized array of `{ abbr, name }` entries.

### 3) Build Prebuilt Search Indexes

- Target: `distRoot/index/<category>-<indexHash>.minisearch.json`
- Operation:
  - Convert the content to a compact MiniSearch JSON index
  - Index over `name` and a simplified description (derived from markdown before or by HTML text)
  - Store key facets (e.g. `slug`, `category`, `sources`, and available category facets)
  - Compute SHA‑256 of the index bytes to derive `<indexHash>`
- Output per category (examples):
  - `index/spells-<indexHash>.minisearch.json`
  - `index/talents-<indexHash>.minisearch.json`

### 4) Generate Route Stubs for Deep Links

- Target folders:
  - `distRoot/spells/<slug>/index.html`
  - `distRoot/talents/<slug>/index.html`
  - `distRoot/classes/<slug>/index.html`
  - `distRoot/lineages/<slug>/index.html`
  - `distRoot/backgrounds/<slug>/index.html`
  - plus base category stubs (e.g. `distRoot/spells/index.html`)
- Operation: emit tiny HTML files that load `index.html` and bootstrap the SPA at the deep route. They try multiple relative locations so they work on `file://` and static hosting.

### 5) Copy Frontend Assets (Publish only)

- Source: `src/frontend/dist` (built via `npm run build`)
- Target: `distRoot/`
- Operation:
  - Copy `index.html`, `assets/`, and `sw.js` into `distRoot`
  - Rewrite absolute `/assets/...` URLs in `index.html` to `assets/...` for compatibility with `file://` and nested routes
- Output:
  - `distRoot/index.html`
  - `distRoot/assets/*`
  - `distRoot/sw.js`

### 6) Emit Site Manifest

- Target: `distRoot/site-manifest.json`
- Shape:

```
{
  "build": "2024-08-23T12:34:56.789Z",
  "categories": {
    "spells":    { "hash": "<hex>", "count": 123, "indexHash": "<hex>" },
    "talents":   { "hash": "<hex>", "count":  45, "indexHash": "<hex>" },
    "classes":   { "hash": "<hex>", "count":  12, "indexHash": "<hex>" },
    "backgrounds": { ... },
    "lineages":    { ... }
  },
  "sources": [ { "abbr": "BFRD", "name": "Black Flag Reference Document" } ]
}
```

- Usage: The frontend uses this manifest to decide what to fetch and to detect updates.

### 7) Optional ZIP Package

- Command: `--publish`
- Output: `site-bundle-<build>.zip` containing:
  - `index.html`, `assets/`, `sw.js`
  - `site-manifest.json`
  - `data/`, `index/`
  - route stub folders

## Verifying Locally

- Open `dist-site/index.html` in a browser (double‑click) – the app should load and fetch local `data/` and `index/`.
- Open a deep link stub, e.g. `dist-site/spells/<slug>/index.html` – the SPA should boot and navigate to that item.

## Determinism & Hashing

- Category bundles and indexes are written with stable property ordering; their SHA‑256 is computed over the exact bytes written.
- Given the same inputs, hashes are reproducible across runs. Changing one item only flips that category’s hash.

## Troubleshooting

- No assets copied
  - Ensure `src/frontend/dist/index.html` exists. Run `npm run build` under `src/frontend/`.
- No input content
  - Ensure `output/data/<category>/*.json` exists. Run the `Bfmd` pipeline to generate DTOs first.
- Opening deep links fails on `file://`
  - Make sure the stub tried the correct relative `index.html`. If you changed folder layout, adapt the loader.

## CLI Reference

```
dotnet run --project src/BfSiteGen/BfSiteGen.Cli -- [outputRoot] [distRoot] [--publish]
```

- `outputRoot`: Input root containing `data/` (default: `output`)
- `distRoot`:   Output folder for the static site (default: `dist-site`)
- `--publish` (or `publish`): ZIP the `distRoot` into `site-bundle-<build>.zip`

---

BfSiteGen is designed to keep memory usage low via streaming writers and to avoid duplicating markdown and HTML. All HTML is generated on demand at write‑time; data files remain compact and deterministic.
