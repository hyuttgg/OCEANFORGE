# OceanForge Desktop — 24 UI Screen Manifest (UI_SCREEN_MANIFEST.md)

Tất cả 24 màn hình ứng dụng chính theo đặc tả `OCEANFORGE_UI_UX_SPEC.md` và Trình cài đặt tập trung (Launcher L01–L08) đã được xây dựng hoàn chỉnh thành các thành phần AXAML độc lập, tuân thủ bảng mã màu Abyssal Ocean và tích hợp vào hệ thống điều hướng `MainWindow.axaml` và `MainViewModel.cs`.

| ID | Screen Name | Tier | View / Implementation | Trạng Thái | Acceptance Checklist |
|---|---|---|---|---|---|
| 01 | Brand Splash / Startup | Core | `Views/SplashView.axaml` | Hoàn thành | Khởi động tức thì, logo Abyssal Ocean, non-blocking |
| 02 | Sign In | Core | `Views/SignInView.axaml` | Hoàn thành | Email/Pass nội bộ & OAuth2 (Google/Discord), Zero Roblox credentials |
| 03 | Create Account | Core | `Views/CreateAccountView.axaml` | Hoàn thành | Đăng ký tài khoản, điều khoản bảo mật, xác thực mật khẩu |
| 04 | First-run Welcome / Main Shell | Core | `Views/WelcomeShellView.axaml` | Hoàn thành | Cấu hình nhanh trạm chỉ huy, chọn thư mục dữ liệu cục bộ |
| 05 | Fleet / Account Selector | Core | `Views/FleetView.axaml` | Hoàn thành | Bảng hạm đội, bộ lọc Sea 1/2/3, tìm kiếm, level, Beli, mở ngăn kéo |
| 06 | Command Center Dashboard | Core | `Views/DashboardView.axaml` | Hoàn thành | KPI Cards, In-memory throughput, Deduplication counter, Consensus Verdict |
| 07 | Account Detail Drawer | Core | `Views/AccountDetailDrawer.axaml` | Hoàn thành | Loadout nhân vật (Trái/Kiếm/Võ/Súng), Magnet Power, Secrets, HWID |
| 08 | Inventory / Wiki Catalog | Core | `Views/InventoryView.axaml` | Hoàn thành | Kho Wiki Fandom 7 danh mục (Trái, Kiếm, Súng, Võ, Phụ kiện, NL, Tộc V4) |
| 09 | World / Sea Map | Core | `Views/WorldMapView.axaml` | Hoàn thành | Canvas hải trình 3 Vùng Biển (Sea 1, 2, 3) với ghim vị trí hạm đội |
| 10 | Analytics / Performance | Core | `Views/AnalyticsView.axaml` | Hoàn thành | Biểu đồ tăng trưởng Beli/Fragments, phân bổ hạm đội, hiệu năng farm |
| 11 | History / Event Timeline | Core | `Views/HistoryView.axaml` | Hoàn thành | Dòng thời gian sự kiện thay đổi, audit trail, lọc sự kiện |
| 12 | Alerts / Notifications | Core | `Views/AlertsView.axaml` | Hoàn thành | Cảnh báo mức độ Critical/Warning/Info, bộ lọc tab, xử lý tức thì |
| 13 | Tracked Account Metadata Editor | Core | `Views/AccountEditorView.axaml` | Hoàn thành | Chỉnh sửa ghi chú, nhãn phân loại hạm đội, bảo vệ tính toàn vẹn dữ liệu |
| 14 | Settings | Core | `Views/SettingsView.axaml` | Hoàn thành | Cấu hình cổng Bridge :5088, theme màu sắc, tần suất telemetry |
| 15 | User / Role Administration | Admin | `Views/AdminUsersView.axaml` | Hoàn thành | Quản trị người dùng, ma trận phân quyền RBAC (Admin, Operator, Viewer) |
| 16 | Backup / Restore | Core Admin | `Views/BackupRestoreView.axaml` | Hoàn thành | Sao lưu an toàn SQLite WAL cục bộ, khôi phục từ snapshot |
| 17 | Updates / Release Channel | Core | `Views/UpdatesView.axaml` | Hoàn thành | Quản lý phiên bản SemVer, chữ ký số SHA-256, kênh Stable LTS |
| 18 | System Health / Diagnostics | Core | `Views/SystemHealthView.axaml` | Hoàn thành | Giám sát CPU host, RAM GC/Working set, ThreadPool, kênh Channel đệm |
| 19 | Safe Import / QR Pairing | Later/Conditional | `Views/ImportPairingView.axaml` | Hoàn thành | Nhập/xuất JSON an toàn, ghép nối thiết bị qua mã PIN/QR |
| 20 | License / Plan | Commercial | `Views/LicensePlanView.axaml` | Hoàn thành | Bảng so sánh gói dịch vụ (Free Corsair, Captain Pro, Fleet Admiral) |
| 21 | Diagnostic Console / AI Debate | Dev/Admin | `Views/ConsoleLogsView.axaml` | Hoàn thành | Nhật ký điều hành, Hội đồng 4 Multi-AI Agent tranh biện & Consensus |
| 22 | Team Messages / Collaboration | Collaboration | `Views/TeamMessagesView.axaml` | Hoàn thành | Kênh chat nội bộ hạm đội (#tổng-quan, #leviathan, #cảnh-báo) |
| 23 | Community Statistics | Community | `Views/CommunityStatsView.axaml` | Hoàn thành | Bảng xếp hạng cộng đồng ẩn danh, tuân thủ chính sách bảo mật dữ liệu |
| 24 | About / Version / Support | Core | `Views/AboutSupportView.axaml` | Hoàn thành | Thông tin phiên bản, bản quyền mã nguồn mở, xuất gói chẩn đoán sanitized |
| -- | Launcher (L01–L08 Flow) | Installer | `Views/LauncherDownloadView.axaml` | Hoàn thành | Luồng installer L01–L08: Nguồn tin cậy, kiểm tra máy, profile, tải, xác thực, cài đặt |
