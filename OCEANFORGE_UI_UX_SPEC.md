# OCEANFORGE DESKTOP — UI/UX MASTER SPECIFICATION
## Tài liệu giao diện độc lập | Windows + Linux | Avalonia UI + XAML

**Nguồn chuẩn:** `OCEANFORGE_DESKTOP_MASTER_SPEC_V4_COMPLETE_HYBRID_PERFORMANCE.md`
**Phạm vi:** visual direction, information architecture, screen inventory, lifecycle UI, launcher/download/install/update/uninstall.

## Quy tắc bàn giao
- Mỗi giao diện là **một ảnh riêng**, không ghép collage/storyboard.
- Mỗi ảnh phải có file Markdown mô tả mục tiêu, vai trò, viewport/DPI, dữ liệu, hành động, quyền, keyboard/focus và trạng thái loading/empty/error/offline/stale.
- Phong cách: Abyssal Ocean × Antique Treasure Map × Brass Instrument; ưu tiên độ đọc, hierarchy và độ tương phản, không lạm dụng neon.
- UI không phải nguồn xác thực quyền hay dữ liệu; trạng thái live phải phản ánh freshness thực.
- Launcher, installer, updater, uninstaller và ứng dụng chính là các luồng lifecycle riêng biệt.

## Luồng đầu-cuối
Official release page → launcher/bootstrapper → system check → download → signature/hash verification → install → first run → login/register → main shell → operational modules → update/repair/rollback → backup/restore → uninstall/data cleanup.

---
# 91. APPENDIX — RETAINED V2 PRODUCT/UI SPECIFICATION

# 96. V4 — PHẢN BIỆN THIẾT KẾ 24 PANEL ĐÃ CHỐT

## 96.1 Kết luận trước

Hình tổng hợp được chốt là **visual direction/reference board**: nó giúp thống nhất thương hiệu, màu tối đại dương, cảm giác phòng chỉ huy và phạm vi chức năng. Nhưng đó không phải 24 file giao diện production-ready. Chữ trong nhiều panel quá nhỏ để review UX chính xác; text sinh ra trong ảnh có lỗi/không ổn định; một số module xuất hiện nhưng chưa có quyết định nghiệp vụ hoặc API tương ứng. Mỗi màn hình cuối cùng phải được dựng thành mockup độc lập, kiểm tra ở kích thước thật và sau đó mới implementation.

## 96.2 Điều tốt cần giữ

- **Nhận diện mạnh:** logo, màu navy, brass/gold và hình hải đồ làm sản phẩm dễ nhận biết.
- **Định hướng thao tác:** Fleet, detail, inventory, map, analytics, history, alert center và settings tạo ra một console vận hành, không chỉ là dashboard quảng cáo.
- **Bố cục tối:** phù hợp app dùng lâu nếu tương phản và cỡ chữ được sửa đúng.
- **Tách nghiệp vụ:** bảng tài khoản, chi tiết, thống kê, lịch sử và cấu hình là những vùng đúng để tiếp tục thiết kế.
- **Có tính hệ thống:** các panel dường như dùng cùng một sidebar/palette; cần biến sự nhất quán này thành component/token thật trong design system.

## 96.3 Phản biện thẳng — các lỗi cần sửa trước khi triển khai

| Vấn đề trong board | Tại sao nguy hiểm | Quyết định V4 |
|---|---|---|
| Một ảnh ghép chứa 24 panel rất nhỏ | Không thể đánh giá font, khoảng cách, trạng thái focus hoặc khả năng đọc ở 100% scale | Xuất 24 ảnh độc lập; mỗi ảnh có ID, tên, kích thước, trạng thái và acceptance checklist |
| Số thứ tự hiển thị không nhất quán; nhãn `24` có vẻ lặp và các số `18/19` không rõ | Không thể trace thiết kế với ticket/code | Dùng manifest 24 ID cố định bên dưới; không đặt tên file dựa vào thứ tự suy đoán từ collage |
| Gọi login/register/dashboard là “Launcher” dễ nhầm | Cài app, update app và đăng nhập vào app là ba vòng đời khác nhau | Tách **Installer/Bootstrapper**, **Launcher/Updater**, và **Main Desktop App** |
| Chưa có màn hình tải bộ cài, kiểm tra checksum/chữ ký, tiến trình cài, repair, rollback và gỡ cài đặt | Người dùng không biết tải gì, app có an toàn không, gỡ đi có mất dữ liệu không | Launcher lifecycle bắt buộc ở mục 98; không coi là một panel trang trí |
| Nhiều panel dùng xanh điện/neon và glow dày | Sai tinh thần “quiet/premium”; hiệu ứng làm mỏi mắt và tăng chi phí render | Navy làm nền; seafoam/cyan chỉ cho telemetry/action phụ; brass/gold là accent/selection, không phủ toàn màn hình |
| Hình tàu/đảo lớn xuất hiện sau nội dung nghiệp vụ | Hình nền giảm độ đọc, gây khó cho bảng và chữ nhỏ | Artwork toàn màn hình chỉ cho splash/installer/login; nội dung bên trong dùng panel nền đục/độ tương phản ổn định |
| Font và dữ liệu ở panel quá nhỏ | Fleet là giao diện làm việc, không phải poster; 10–12 px trên nhiều panel sẽ không thể đọc ở scale khác | Desktop content mặc định 14–16 px; metadata phụ không dưới mức dễ đọc; kiểm tra DPI 100/125/150/200% |
| Chưa thể hiện rõ loading/empty/error/offline/stale/permission-denied | UI đẹp ở trạng thái có dữ liệu nhưng thất bại khi server/mạng hỏng | Mỗi trang phải có đủ state design; badge freshness phải dựa timestamp thật |
| Marketplace/premium, chat, ranking cộng đồng, QR và admin xuất hiện ngang hàng core telemetry | Scope creep, thêm thanh toán, quyền riêng tư, abuse/moderation và hỗ trợ sản phẩm trước khi lõi ổn | Core V1 = auth, shell, fleet, detail, inventory, telemetry/history, dashboard, settings, backup/diagnostics; Marketplace/chat/community để phase sau nếu có yêu cầu sản phẩm đã xác thực |
| QR “thêm tài khoản” không nêu chính xác dữ liệu được quét | Dễ bị hiểu thành thu thập credential/session token | QR chỉ import mã cấu hình/ID hoặc pairing nonce do OceanForge phát hành; tuyệt đối không đọc/lưu Roblox password/cookie/session |
| Không chỉ ra nguồn thật của trạng thái Online/Offline | Có thể hiển thị “online” sai vì event đã cũ | Trạng thái được suy ra từ heartbeat/source sequence + freshness; hiện “Last seen” và nguồn; không tuyên bố live từ snapshot cũ |
| Cùng một style card cho mọi trang | Bảng dữ liệu, map, console và settings có nhu cầu tương tác khác nhau | Dùng component system chung nhưng layout theo nhiệm vụ: data grid cho Fleet, canvas cho Map, timeline cho History, form group cho Settings |
| Chưa có bàn phím/focus/reduced motion/contrast | Dễ gây khó sử dụng và có lỗi thao tác | Accessibility checklist là acceptance criteria, không phải polishing cuối dự án |

## 96.4 Hệ màu và layout V4

Duy trì các token hiện có ở phần 91; áp dụng quy tắc nghiêm hơn:

- `Ocean-950/#06121A`, `Ocean-900/#0A1C27`: shell/nền.
- `Seafoam/#68C6C0`, `Signal/#7ED7EA`: trạng thái live, liên kết và telemetry.
- `Brass/#C8A65A`, `Gold/#E1BE67`: selected state, điểm nhấn thương hiệu và thao tác nổi bật có chủ đích.
- `Warning/#D5A64E`, `Danger/#C66B63`: chỉ cho cảnh báo/lỗi; không dùng làm trang trí.
- Text chính `#E4ECEE`, muted `#81949A`, border `#27414A`; kiểm tra tương phản thực bằng tool trên cặp màu foreground/background.

Grid/layout khuyến nghị:

```text
Spacing: 4 / 8 / 12 / 16 / 24 / 32 px
Sidebar: có expanded/collapsed state, chiều rộng không đổi tùy ý giữa trang
Content: giới hạn max-width khi là form; full width cho data grid/map
Drawer: dùng cho account detail, tránh modal trung tâm cho thông tin dài
Primary action: một nút nổi bật theo màn hình; destructive actions dùng confirm riêng
Motion: ngắn, có purpose; reduce-motion preference phải được tôn trọng
```

Không chốt pixel nào nếu chưa thử ở cửa sổ 1280×720, 1366×768, 1920×1080 và scale DPI. Map/graph không được buộc bảng dữ liệu xuống cỡ chữ nhỏ.

---

# 97. V4 — MANIFEST 24 MÀN HÌNH APP (ID CHÍNH THỨC)

Manifest này chuẩn hóa phạm vi panel được thể hiện trong concept; 24 ID dưới đây không có nghĩa tất cả đều nằm trong MVP. Nhóm `Core` xây trước; nhóm `Later` phải qua quyết định sản phẩm riêng.

| ID | Màn hình chuẩn | Ưu tiên | Nội dung/acceptance chính |
|---:|---|---|---|
| 01 | Brand Splash / startup | Core | Logo, version, khởi động; không giữ splash lâu giả tạo |
| 02 | Sign In | Core | Email, password OceanForge, server status, show/hide password, reset; không hỏi Roblox password |
| 03 | Create Account | Core | Validation, terms, verification, rate-limit feedback, privacy notice |
| 04 | First-run Welcome / Main Shell | Core | API status, data freshness, navigation; tách khỏi installer |
| 05 | Fleet / Account Selector | Core | Search, filter, status, Sea, tags, saved views, pagination/virtualization |
| 06 | Command Center Dashboard | Core | KPI, recent changes, health, telemetry freshness, drill-down links |
| 07 | Account Detail Drawer | Core | Character, loadout, inventory, materials, sessions, history, snapshot; không chứa secrets |
| 08 | Inventory / Materials | Core | Fruits/Swords/Guns/Styles/Accessories/Materials, search/filter, data source/time |
| 09 | World / Sea Map | Core | Sea/island/layer toggles, marker status/freshness, selected route; renderer riêng |
| 10 | Analytics | Core | Time range, server-side aggregates, charts có empty/error/loading state |
| 11 | History / Event Timeline | Core | Cursor paging, filter, source, event detail, snapshot diffs |
| 12 | Alerts / Notifications | Core | Severity, acknowledge, mute/snooze, rule source, audit |
| 13 | Tracked Account Metadata Editor | Core | Thay đổi metadata được phép; version/conflict handling; không thu credential |
| 14 | Settings | Core | Appearance, performance profile, bandwidth, privacy/cache, startup preference, logs |
| 15 | User / Role Administration | Admin phase | Server-side RBAC, audit, online-only mutations, confirmation |
| 16 | Backup / Restore | Core admin | Destination, schema/version, checksum, preview/confirmation, restore verification |
| 17 | Updates / Release Channel | Core | Current/latest version, release notes, signature/status, defer/retry/rollback status |
| 18 | System Health / Diagnostics | Core | OS/runtime/API/local DB/disk/memory/sync state; export scrubbed report |
| 19 | Safe Import / QR Pairing | Later/core only if specified | QR payload type is explicit; reject passwords/cookies/tokens; preview before commit |
| 20 | License / Plan | Later, conditional | Only if license/subscription product model exists; legal/payment/security review |
| 21 | Diagnostic Console / Logs | Developer/admin phase | Structured filters, redaction, export; no raw secret/payload dump |
| 22 | Team Messages / Collaboration | Later | Requires threat model, permissions, retention and moderation policy |
| 23 | Community Statistics / Leaderboard | Later | Requires privacy, anti-abuse and clear data-sharing consent; not required for core telemetry |
| 24 | About / Version / Support | Core | App version, platform, release channel, license notices, help/diagnostic links |

## 97.1 Quy cách bàn giao từng ảnh

Mỗi ảnh độc lập phải có:

```text
UI-<ID>-<screen-name>.png
UI-<ID>-<screen-name>.md
```

Markdown đi kèm ghi: mục tiêu, user role, viewport/DPI, data contract, interactions, keyboard/focus behavior, loading/empty/error/offline states, permissions, source-of-truth, test ID và việc gì còn placeholder. Không dùng text AI sinh trong mockup làm nội dung final; thay bằng sample data có schema rõ ràng và nhất quán.

---

# 98. V4 — LAUNCHER / DOWNLOADER / INSTALLER / UPDATER / UNINSTALLER

## 98.1 Tách ba sản phẩm con về mặt lifecycle

```text
Website / Official release page
      ↓ download signed bootstrapper/package
OceanForge Launcher (download, check, verify, update, repair entry points)
      ↓ install/update verified package using platform-supported mechanism
OceanForge Desktop (login + all operational modules)
      ↓ uninstall via OS/package mechanism + optional local-data cleanup
```

**Installer ≠ Launcher ≠ Main App.** Login/register thuộc Main App. Không bắt người dùng đăng nhập OceanForge chỉ để tải bộ cài. Installer không chứa cloud/database secret và không thực thi script tùy ý nhận từ server.

## 98.2 Các màn hình launcher bắt buộc

| ID | Màn hình | Acceptance criteria |
|---|---|---|
| L01 | Welcome / Official Source | Tên app, publisher, version/channel, link privacy/release notes; không nút tải từ mirror chưa xác thực |
| L02 | System Check | OS/architecture/version support, disk space, permission, existing install, process đang chạy; báo điều kiện không đạt |
| L03 | Install Type | Minimal/Standard/Full; mô tả dung lượng dự kiến và gói tài nguyên sẽ tải |
| L04 | Install Location | Path hợp lệ, permission, disk free space; không mặc định cài vào thư mục user có thể bị xóa nhầm |
| L05 | Download Center | Tổng dung lượng, downloaded/total, tốc độ, tiến độ, pause/resume/cancel, retry, lỗi mạng, resume Range khi server hỗ trợ |
| L06 | Integrity Verification | Verify SHA-256 và chữ ký artifact/manifest theo trust policy; không cài/chạy nếu mismatch |
| L07 | Install Progress | Phase rõ: prepare, unpack, register, shortcut, migration (nếu cần), verify; cancel chỉ ở checkpoint an toàn |
| L08 | Finish / Launch | Phiên bản đã cài, channel, launch app, view release notes; không hiện “success” trước health check cơ bản |
| L09 | Update Available | Current/latest version, size, release notes, security update flag, Later/Update; không tải không giới hạn ở nền |
| L10 | Apply Update | Stage → verify → close app gracefully → install atomically/through package manager → smoke check |
| L11 | Update Recovery / Rollback | Nếu health check thất bại, khôi phục bản app trước nếu package model hỗ trợ; giữ database/local outbox |
| L12 | Repair / Verify Files | Kiểm tra manifest, tải lại file hỏng, không đè user data, ghi diagnostic ID |
| L13 | Uninstall Confirmation | Nêu chính xác app files nào bị xóa và dữ liệu nào sẽ giữ; local cache/config là lựa chọn tách biệt |
| L14 | Uninstall Progress | Đóng app, xử lý lock, xóa app theo cơ chế OS/package, báo file không xóa được; không xóa tùy tiện ngoài install root |
| L15 | Uninstall Result | Kết quả thật; lựa chọn riêng để xóa local cache/settings; cảnh báo thao tác không thể hoàn tác |
| L16 | Uninstall/Install Failure | Error code, nguyên nhân người dùng hiểu được, retry/log bundle đã scrub; không để lại trạng thái nửa cài đặt nếu có thể phục hồi |

Nếu dung lượng installer ~200 MB và footprints Minimal 450–600 MB / Standard 900 MB–1.1 GB / Full ~1.3–1.35 GB vẫn là mục tiêu sản phẩm, hãy ghi đó là **target cần đo trên artifact thật**; manifest phải có `downloadBytes`, `installedBytesEstimate`, profile và asset optional. Không hiển thị size ảo. Tách asset packs để Minimal không tải toàn bộ bản đồ/artwork nếu không cần.

## 98.3 Cách uninstall đúng theo nền tảng

- **Windows MSIX:** để Windows/App Installer quản lý lifecycle uninstall/update; launcher đưa người dùng tới đường quản lý app của OS nếu cần. Không tự xóa registry/file hệ thống tùy ý.
- **Windows signed EXE:** installer framework đã chọn phải tạo uninstaller đăng ký đúng với OS; test repair/upgrade/uninstall và file đang bị khóa.
- **Linux `.deb`:** package manager chịu trách nhiệm gỡ package; user config/cache phải tách khỏi package files.
- **Linux AppImage:** package có thể là một file tự chứa; xóa file AppImage không đồng nghĩa xóa data trong thư mục user. Cần cleanup guide/wizard riêng, không tự đòi root.
- **Cloud data:** gỡ ứng dụng **không** tự xóa dữ liệu cloud/workspace. Xóa cloud account là thao tác riêng, cần đăng nhập, xác nhận rõ và audit.
- **Local data:** mặc định giữ Settings/Reports/Cache theo policy đã thông báo; cho lựa chọn xóa cache riêng. Outbox chưa sync phải cảnh báo trước khi xóa và có export/backup nếu phù hợp.

## 98.4 Download/update algorithm

```text
Fetch signed/approved release manifest over HTTPS
  → validate channel/platform/architecture/minimum version
  → check free disk + existing process
  → download to temporary staging path (resume only if ETag/range validation passes)
  → verify byte size + SHA-256 + trusted signature/provenance policy
  → stage package atomically
  → invoke platform-supported installation flow
  → run lightweight health check
  → mark success only after check
  → if failed, preserve logs and invoke rollback policy where supported
```

- Không tin checksum tải từ cùng nguồn nếu trust model không xác minh được manifest/signature.
- Không bỏ qua TLS certificate validation, không chạy nội dung tải về trước verify, không elevate privileges trước khi cần.
- Ghi rõ channel stable/beta/nightly; không tự nâng stable lên beta/nightly.
- Update database schema tách khỏi updater client. App update lỗi không được tự restore MySQL production.
- Download retry có exponential backoff + jitter; resume phải xác nhận ETag/hash và không ghép nhầm file version.

---

# UI DELIVERY CHECKLIST — PHẢI NGHIỆM THU RIÊNG

1. Official release / launcher home
2. System compatibility check
3. Install type / resource selection
4. Install location
5. Download manager (pause/resume/retry)
6. Signature/hash verification
7. Install progress and success
8. First-run setup
9. Login/register/email verification/recovery
10. Dashboard / Command Center
11. Fleet/accounts / account detail
12. Live Monitor / Map
13. Inventory / Material Vault
14. Analytics / Sessions / History
15. Snapshot / Diff / Time Machine
16. Alerts / Notifications / Telemetry Quality
17. Reports / Backup / Restore
18. Diagnostics / Settings / Users & Roles / API connections
19. Update check/download/verify/apply
20. Update failure/rollback / repair
21. Uninstall preflight / data selection / progress / result
22. About / Support / Crash recovery / Safe mode

Mỗi giao diện cần có: `UI-<ID>-<screen-name>.png`, `UI-<ID>-<screen-name>.md`, implementation View/ViewModel và test IDs. Không dùng chữ nhỏ không đọc được hoặc dữ liệu giả trình bày như live.

