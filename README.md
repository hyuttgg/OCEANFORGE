# OceanForge Desktop

> Abyssal Tactical Command Center for Roblox Fleet Management

[![CI](https://github.com/hyuttgg/OCEANFORGE/actions/workflows/ci.yml/badge.svg)](https://github.com/hyuttgg/OCEANFORGE/actions/workflows/ci.yml)

## Tech Stack

- **Runtime:** .NET 10 LTS
- **UI:** Avalonia UI 12.1.4 (cross-platform XAML)
- **Architecture:** Clean Architecture + MVVM
- **Local Storage:** SQLite (WAL mode)
- **Auth:** Google & Discord OAuth 2.0 + PKCE
- **Targets:** Windows x64, Linux x64

## Quick Start

```bash
# Prerequisites: .NET 10 SDK
dotnet --version  # should be 10.0.x

# Build
dotnet build OceanForge.slnx

# Test
dotnet test OceanForge.slnx

# Run
dotnet run --project src/OceanForge.Desktop/OceanForge.Desktop.csproj
```

## Project Structure

```
src/
├── OceanForge.Domain/            # Entities, value objects
├── OceanForge.Contracts/         # DTOs, interfaces
├── OceanForge.Application/       # Business logic, services
├── OceanForge.AI/                # Multi-agent debate system
├── OceanForge.Telemetry/         # Hardware monitoring, HTTP bridge
├── OceanForge.Local.Persistence/ # SQLite, EF Core
└── OceanForge.Desktop/           # Avalonia UI app
tests/
├── OceanForge.Domain.Tests/
└── OceanForge.Application.Tests/
```

## License

Proprietary — All rights reserved.
