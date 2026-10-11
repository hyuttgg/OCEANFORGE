# OceanForge Desktop — Architectural Decisions (DECISIONS.md)

| ID | Quyết định | Trạng thái | Nguồn kiểm chứng / Ghi chú | Ngày |
|---|---|---|---|---|
| ADR-001 | Framework Desktop: Avalonia UI 12.1.4 + XAML | `CHỐT` | NuGet, Avalonia 12.1 release notes. Hỗ trợ .NET 10 LTS trên Windows + Linux. | 2026-10-10 |
| ADR-002 | Runtime: .NET 10 (LTS) | `CHỐT` | Microsoft .NET Support Lifecycle. Hỗ trợ dài hạn đến 2028-11-14. | 2026-10-10 |
| ADR-003 | MVVM: CommunityToolkit.Mvvm 8.4.2 | `CHỐT` | Microsoft Community Toolkit. Nhanh, source generator, không phát sinh overhead. | 2026-10-10 |
| ADR-004 | Local Cache: SQLite + EF Core / Microsoft.Data.Sqlite | `CHỐT` | V4 Specification Section 0/2. Offline outbox, cursor, cache. | 2026-10-10 |
| ADR-005 | Server DB: MySQL 8.4 LTS | `CHỐT` | Oracle MySQL LTS track. Nguồn dữ liệu chuẩn server qua API. | 2026-10-10 |
| ADR-006 | Telemetry Producer: Roblox `sender copy.lua` via HTTP Bridge | `CHỐT` | POST `/api/lua/update` và `/api/webhook/roblox`, headers `x-api-key`, `x-csharp-dedup-hash`. | 2026-10-10 |
| ADR-007 | Hardware Monitoring: Jinjinov/Hardware.Info & LibreHardwareMonitor concepts | `CHỐT` | Đo CPU Load, RAM Memory, Network, Thread Count hiển thị trên Screen 18 / Infopanel. | 2026-10-10 |
| ADR-008 | Multi-Agent AI Debate Engine (`OceanForge.AI`) | `CHỐT` | Tích hợp 4 AI Agents tranh luận và phản biện hệ thống trực tiếp trong app. | 2026-10-10 |
| ADR-009 | Bounded In-Memory Queue: `System.Threading.Channels` | `CHỐT` | Giới hạn buffer telemetry chống tràn RAM / OOM khi reconnect hoặc burst. | 2026-10-10 |
| ADR-010 | Zero Credential Harvesting: Ranh giới bảo mật tối cao | `CHỐT` | Tuyệt đối không thu thập password, Roblox cookie, 2FA. | 2026-10-10 |
