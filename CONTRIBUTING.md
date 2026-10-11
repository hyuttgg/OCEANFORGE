# Contributing to OceanForge Desktop

1. Branch from `main` using `feature/<slug>`, `fix/<slug>`, or `security/<slug>`.
2. Run `dotnet restore OceanForge.slnx --locked-mode` and `dotnet test OceanForge.slnx -c Release` locally before opening a PR.
3. Never commit secrets, `.env`, `.pfx`, `.db`, or `.ROBLOSECURITY` tokens.
4. Keep commits atomic and follow Conventional Commits (`feat:`, `fix:`, `security:`, `ci:`, `docs:`).
