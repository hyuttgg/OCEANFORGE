# OceanForge Desktop — Project Context

## 1. Product Summary
- **Product Name:** OceanForge Desktop
- **Aesthetic Direction:** Abyssal Ocean × Ancient Treasure Map × Captain's Command Room (Premium Dark Navy `#06121A`, Brass/Gold `#C8A65A`, Seafoam `#68C6C0`).
- **Core Role:** Cross-platform telemetry, state management, operations console, and Multi-Agent AI deliberation system for tracked Roblox game accounts (Blox Fruits telemetry via `sender copy.lua`).
- **Target OS:** Windows 10/11 (x64) and Linux Desktop (Ubuntu 22.04/24.04, Debian 12, Fedora on X11 / XWayland).
- **Core Stack:** .NET 10 (LTS), C# 13, Avalonia UI 12.1.4 (XAML/MVVM), CommunityToolkit.Mvvm 8.4.2, ASP.NET Core 10 Kestrel bridge, Local SQLite for offline caching/outbox, MySQL 8.4 LTS for cloud source of truth.

## 2. Telemetry Producer Integration
- **Producer Script:** `sender copy.lua` (Roblox client executor script).
- **Ingestion Protocol:** HTTP POST to `/api/lua/update` and `/api/webhook/roblox`.
- **Custom Headers:**
  - `x-api-key`: Workspace / account security token.
  - `x-csharp-accelerated`: "true"
  - `x-csharp-dedup-hash`: 32-bit FNV-1a checksum of account state.
  - `x-csharp-state-unchanged`: "true" / "false" deduplication indicator.
- **Payload Coverage:** 34+ attributes (Account level, Beli, Fragments, Race, Sea, equipped Fruit/Sword/Gun/Fighting Style, Inventory, Magnet Power, Secrets progress, Device HWID, Playtime).

## 3. Multi-Agent AI Deliberation Architecture
- **In-App AI Debate Engine:** Multi-Agent debate system (`OceanForge.AI`) with 4 distinct personas:
  1. *Lead Architect Agent*: Verifies system invariants, bounded memory, and clean boundaries.
  2. *Security & Compliance Agent*: Enforces strict anti-harvesting rules (no credentials/cookies), rate limits, and token redaction.
  3. *Avalonia UI/UX Specialist*: Ensures 60 FPS responsiveness, non-blocking UI thread, and compliance with the 24-screen inventory.
  4. *Performance & Hardware Monitor Agent*: Evaluates CPU/RAM budgets based on `LibreHardwareMonitor` & `Hardware.Info` principles.

## 4. Architecture Constraints (Non-Negotiable)
1. Desktop does NOT connect directly to MySQL/Redis; all cloud calls route via API.
2. Zero credential harvesting: no Roblox passwords, browser cookies, session tokens, or 2FA secrets.
3. Local SQLite is purely for offline cache, cursors, and transactional outbox.
4. UI thread performs zero I/O or blocking operations; bounded `System.Threading.Channels` for telemetry ingestion.
