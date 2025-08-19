# Repository Guidelines

## Project Structure & Module Organization
- `src/Bfmd/Bfmd.Cli`: Console entry point (`Program.cs`).
- `src/Bfmd/Bfmd.Core`: Core domain logic and public APIs.
- `src/Bfmd/Bfmd.Extractors`: Pluggable extractors and adapters.
- `tests/Bfmd/Bfmd.UnitTests`: Unit tests (xUnit).
- `tests/Bfmd/Bfmd.IntegrationTests`: Integration tests (xUnit).
- `docs/`: Project docs and design notes.

Use namespaces and project names under `Bfmd.*`. Keep one public type per file named after the type.

## Build, Test, and Development Commands
- Restore/build: `dotnet build BfTools.sln -c Debug`
- Run CLI: `dotnet run --project src/Bfmd/Bfmd.Cli`
- All tests: `dotnet test BfTools.sln -c Debug`
- Tests with coverage: `dotnet test --collect:"XPlat Code Coverage"`
- Format (if installed): `dotnet format` (run at repo root)

Target framework is `net8.0` with nullable reference types and implicit usings enabled.

## Coding Style & Naming Conventions
- C#: 4‑space indentation, UTF‑8, LF line endings.
- Braces on new lines; prefer expression-bodied members when clear.
- Naming: `PascalCase` for types/methods, `camelCase` for locals/params, `_camelCase` for private fields, `I*` for interfaces.
- Folders mirror namespaces (e.g., `Bfmd.Core.Parsing` → `src/Bfmd/Bfmd.Core/Parsing`).
- Keep CLI concerns in `Bfmd.Cli`; shared logic lives in `Bfmd.Core`; concrete extractors in `Bfmd.Extractors`.

## Testing Guidelines
- Framework: xUnit with `Microsoft.NET.Test.Sdk` and `coverlet.collector`.
- Naming: `MethodName_ShouldExpected_WhenCondition` for tests; file/class mirrors SUT.
- Place unit tests under a matching namespace path in `Bfmd.UnitTests`; broader end‑to‑end flows in `Bfmd.IntegrationTests`.
- Run: `dotnet test` from repo root. Coverage artifacts are written under `TestResults/`.

## Commit & Pull Request Guidelines
- Commits: prefer Conventional Commits (e.g., `feat(core): add parser`), small, and scoped.
- PRs: include a clear description, linked issues, how-to-test steps, and notes on risks/rollout. Add tests for new behavior and update docs under `docs/` when relevant.
- Ensure CI/build and `dotnet test` pass locally before requesting review.

## Security & Configuration Tips
- Do not commit secrets or local config; honor `.gitignore`.
- Keep public surface area in `Bfmd.Core` minimal and documented. Validate untrusted inputs in extractors.
