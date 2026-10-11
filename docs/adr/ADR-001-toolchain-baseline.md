# ADR-001: Toolchain Baseline, Avalonia 12, and .NET 10 LTS

## Context
OceanForge Desktop yêu cầu một nền tảng desktop cross-platform (Windows + Linux) hiệu năng cao, render 60 FPS mượt mà, hỗ trợ giao diện tối màu phức tạp (Abyssal Ocean), xử lý telemetry dồn dập từ hàng chục tài khoản Roblox chạy song song mà không nghẽn UI thread.

## Decision
1. **Runtime:** .NET 10 LTS (`net10.0`).
2. **UI Framework:** Avalonia UI 12.1.4 với XAML và CommunityToolkit.Mvvm 8.4.2.
3. **Architecture:** Clean Architecture + Modular Monolith:
   - `OceanForge.Contracts`: Request/Response DTOs, schemas.
   - `OceanForge.Domain`: Pure domain rules, FNV-1a checksum calculation, invariant validation.
   - `OceanForge.Application`: Use cases, ports.
   - `OceanForge.Telemetry`: Ingestion pipeline, bounded Channels, deduplication.
   - `OceanForge.AI`: Multi-Agent Deliberation Engine (Lead Architect, Security, UI/UX, Hardware & Performance).
   - `OceanForge.Local.Persistence`: SQLite offline cache.
   - `OceanForge.Desktop`: Composition root, Avalonia UI, Themes, ViewModels.

## Consequences
- Single codebase chạy mượt mà trên cả Windows và Linux.
- Đảm bảo tính nhất quán qua các phiên làm việc với các file context trong `docs/`.
