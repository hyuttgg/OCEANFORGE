# OceanForge Desktop — Next Session Plan

## 1. Trạng thái hiện tại
- Đã hoàn thành toàn bộ cốt lõi: Ingestion Telemetry Roblox Update 30, Bounded Channels, FNV-1a deduplication, Local SQLite Persistence, 4 Multi-AI Personas Deliberation, Blox Fruits Wiki Catalog 7 danh mục, Google & Discord OAuth2, Screen 07 (Account Detail Drawer), Screen 09 (World Sea Map Canvas) và Screen 18 (System Health & Hardware Telemetry).
- Build sạch 0 lỗi, 19/19 unit tests pass 100%.

## 2. Mục tiêu tiếp theo (Phase 2 & Packaging)
- Thiết lập kịch bản xuất bản (Publish & Packaging): Self-contained single-file cho Windows x64 (MSIX/InnoSetup) và Linux x64 (AppImage/deb/tar.gz) theo `docs/release/OS_MATRIX.md`.
- Triển khai Screen 10 (Analytics & Aggregate Statistics) và Screen 14 (Settings & Hardware Performance Profiles).
- Thiết lập CI/CD GitHub Actions workflow theo mục 8 của Master Spec V4.

## 3. Files trọng tâm
- `src/OceanForge.Desktop/Views/MainWindow.axaml`
- `src/OceanForge.Desktop/Views/WorldMapView.axaml`
- `src/OceanForge.Desktop/Views/AccountDetailDrawer.axaml`
- `src/OceanForge.Desktop/Views/SystemHealthView.axaml`
- `docs/release/OS_MATRIX.md`

## 4. Lệnh kiểm tra nhanh
- `dotnet build`
- `dotnet test`
- `dotnet run --project src/OceanForge.Desktop`
