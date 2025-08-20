# BFMD — Markdown → JSON CLI

BFMD converts RPG content written in Markdown into normalized JSON with indexes, a manifest, and reports. It targets .NET 8 and lives under `src/Bfmd`.

## Install / Build
- Prerequisite: .NET SDK 8.0+ (`dotnet --version`).
- Build: `dotnet build BfTools.sln -c Release`
- Run help: `dotnet run --project src/Bfmd/Bfmd.Cli -- --help`

## Quick Start
1) Create folders (or reuse your own):
```
input/{classes,backgrounds,lineages}
config/
output/
```
2) Scaffold default configs:
```
dotnet run --project src/Bfmd/Bfmd.Cli -- init --config config
```
3) Convert Markdown → JSON:
```
dotnet run --project src/Bfmd/Bfmd.Cli -- convert \
  --in input --out output --config config \
  --types classes,backgrounds,lineages
```
4) Validate generated JSON:
```
dotnet run --project src/Bfmd/Bfmd.Cli -- validate --out output
```
5) Pack for release:
```
dotnet run --project src/Bfmd/Bfmd.Cli -- pack --out output --dest bfmd-release.zip
```

## Commands
- `init` (scaffold configs): creates `config/sources.yaml`, `pipeline.yaml`, and `mapping.*.yaml` if missing. Use `--force` to overwrite.
- `convert` (full pipeline): discovers Markdown in `--in`, loads YAML from `--config`, extracts, validates, writes `/output/data`, `/output/index`, `manifest.json`, and `report.*`.
- `validate` (re-check JSON): loads `/output/data/**` and runs validators again.
- `diff` (compare snapshots): `--since <oldOutputOrManifestDir>` and `--out <currentOutput>`; prints added/removed slugs.
- `pack` (zip release): zips `/data`, `/index`, and `manifest.json` to `--dest`.

Global option: `--verbosity` one of `quiet|minimal|normal|detailed` (default `normal`).

## Configuration
- `config/sources.yaml`: source list (`abbr`, `name`, `version`, `inputRoot`).
- `config/pipeline.yaml`: enabled steps with `type`, `input`, optional `mapping`, `outputData`, `outputIndex`.
- `config/mapping.<type>.yaml`: header lists, table `columnMap`, regexes, synonyms, units. If a step omits `mapping`, BFMD loads `config/mapping.<type>.yaml` by default.

## Outputs
- One JSON per entity: `output/data/<type>/<slug>.json`
- Indexes: `output/index/<type>.index.json`
- Manifest: `output/manifest.json`
- Reports: `output/report.log`, `output/report.json`

Exit codes: 0 OK; 1 validation; 2 config; 3 I/O; 4 unexpected.
