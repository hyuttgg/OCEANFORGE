# OCEANFORGE_UI_UX_SPEC.md

## OceanForge Desktop — UI/UX Master Specification

**Phiên bản tài liệu:** 1.0  
**Ngày:** 2026-10-09  
**Nền tảng:** Windows + Linux desktop  
**UI framework:** Avalonia UI + XAML / MVVM  
**Phong cách đã chốt:** Abyssal Ocean × Ancient Treasure Map × Captain’s Command Room  
**Phạm vi:** 24 màn hình app độc lập + 16 màn hình/luồng launcher, installer, downloader, updater, repair và uninstaller.

> **Quy tắc quan trọng:** bảng ghép 24 panel chỉ là overview để duyệt phong cách. Không được dùng bảng ghép làm ảnh bàn giao cuối. Mỗi màn hình trong manifest phải có PNG riêng, tỷ lệ khung hình nhất quán, vùng chữ đủ đọc, và bản mô tả tương tác trong tài liệu này. 24 màn hình app và 16 màn hình launcher là hai nhóm khác nhau.

---

# 1. Mục tiêu và phạm vi

Tài liệu này là hợp đồng giao diện cho OceanForge Desktop. Nó chuyển concept đã chốt thành những yêu cầu có thể thiết kế, xây dựng bằng Avalonia/XAML và nghiệm thu được. Tài liệu không khẳng định các màn hình đã được lập trình; nó xác định thiết kế cần làm và hành vi cần kiểm thử.

## 1.1 Sản phẩm được chia thành ba phần

1. **Official Download / Bootstrapper:** điểm tải chính thức và gói bootstrap ban đầu. Người dùng không cần đăng nhập OceanForge để tải ứng dụng.
2. **OceanForge Launcher:** kiểm tra máy, chọn gói, tải, xác minh, cài đặt, kiểm tra update, repair, rollback khi được hỗ trợ và gỡ cài đặt qua cơ chế phù hợp của hệ điều hành.
3. **OceanForge Desktop:** đăng nhập OceanForge và dùng dashboard, Fleet, telemetry, inventory, analytics, alerts, settings và các module vận hành khác.

`Installer ≠ Launcher ≠ Main App`. Đăng nhập/đăng ký tài khoản OceanForge thuộc ứng dụng chính, không phải cổng bắt buộc để tải installer. Gỡ app không đồng nghĩa xóa dữ liệu Cloud.

## 1.2 Quy tắc về 24 màn hình

- Giữ đúng ID `01–24` trong toàn bộ tên file, ticket, View, ViewModel và screenshot test.
- `Core` được xây trước. `Later/Conditional` phải qua xác nhận phạm vi trước khi triển khai.
- Mỗi ảnh chỉ mô tả **một cửa sổ/một trạng thái UI**, không ghép nhiều trang vào một ảnh.
- Ảnh mockup dùng sample data nhất quán; thay placeholder bằng API/local state khi triển khai.
- Không đưa password, token, cookie, secret hoặc thông tin nhạy cảm thật vào mockup/log.
- Mỗi trang phải định nghĩa loading, empty, error, stale/offline, permission-denied và success khi các trạng thái tương ứng có ý nghĩa.

---

# 2. Thiết kế hình ảnh đã chốt

## 2.1 Ngôn ngữ thị giác

**Từ khóa:** premium maritime, calm operational UI, deep ocean, antique chart, brass instrument, treasure navigation, disciplined information density.

App phải gợi cảm giác phòng điều khiển của thuyền trưởng ở vùng biển sâu. Bản đồ thế giới là điểm nhấn trực quan của một số màn hình, nhưng bảng dữ liệu phải rõ ràng và nhanh. Không biến mọi trang thành tranh nền; artwork chỉ xuất hiện ở splash, đăng nhập/đăng ký, welcome, launcher, update/error recovery và About. Các trang có dữ liệu phải dùng nền phẳng hoặc bề mặt rất nhẹ để đảm bảo độ tương phản.

## 2.2 Bảng màu token

| Token | Hex | Mục đích |
|---|---|---|
| `Abyss` | `#02070B` | nền sâu, vùng viền ngoài |
| `Ocean950` | `#06121A` | nền cửa sổ chính |
| `Ocean900` | `#0A1C27` | sidebar / surface cấp 1 |
| `Ocean800` | `#0E2936` | panel cấp 2 |
| `Ocean700` | `#123847` | panel hover / selected subdued |
| `Chart` | `#152A2B` | surface bản đồ/biểu đồ đậm |
| `Brass` | `#C8A65A` | accent có chừng mực |
| `Gold` | `#E1BE67` | CTA phụ / selected halo / key marker |
| `Seafoam` | `#68C6C0` | dữ liệu telemetry bình thường |
| `Signal` | `#7ED7EA` | live indicator / link / focus support |
| `Warning` | `#D5A64E` | stale / warning / cần xem lại |
| `Danger` | `#C66B63` | destructive / error nghiêm trọng |
| `TextPrimary` | `#E4ECEE` | chữ chính |
| `TextMuted` | `#81949A` | mô tả / metadata |
| `Border` | `#27414A` | đường phân cách |
| `SurfaceHover` | `#143240` | hover nhẹ; phải được QA độ tương phản |

### Luật dùng màu

- Navy/teal là nền chính; gold chỉ làm accent, không phủ lên toàn bộ nút và viền.
- Cyan/seafoam biểu thị thông tin/liveness, không dùng làm màu cảnh báo nguy hiểm.
- Amber là Warning/Stale; đỏ chỉ dành cho lỗi hoặc hành động destructive.
- Trạng thái không được phân biệt chỉ bằng màu. Mỗi status cần icon + label.
- Không dùng glow/blur toàn màn hình; chỉ dùng highlight mỏng cho selected marker, focus và CTA chính.
- Text trên hình nền phải có scrim/overlay đủ tối; không đặt bảng dữ liệu trực tiếp trên artwork phức tạp.

## 2.3 Typography

Ưu tiên font sans dễ đọc có sẵn trên nền tảng, fallback ổn định. Không đóng gói font có điều kiện cấp phép chưa kiểm tra.

| Style | Kích cỡ tham khảo | Cách dùng |
|---|---:|---|
| Display | 30–36 px | Splash / tiêu đề quảng bá ngắn |
| Page title | 24–28 px | tiêu đề trang |
| Section title | 16–18 px | tiêu đề panel |
| Body | 14–16 px | nội dung thông thường |
| Table | 13–14 px | bảng có mật độ cao, không nhỏ hơn nếu không cần |
| Caption | 12–13 px | timestamp, helper text |
| Numeric KPI | 24–32 px | chỉ số tổng quan |

Không dùng chữ hoa toàn bộ cho paragraph dài. Numeric columns nên dùng tabular numerals và căn phải. Text dài trong bảng có tooltip hoặc drawer chi tiết; không rút gọn những giá trị cần ra quyết định.

## 2.4 Spacing, shape và elevation

- Grid cơ sở 4 px; khoảng cách phổ biến 8, 12, 16, 20, 24, 32 px.
- Panel radius 8–12 px; field/button radius 6–8 px; tránh viên thuốc ở mọi nơi.
- Viền 1 px, màu `Border`; elevation thấp, không dùng shadow đen quá mạnh.
- Khoảng cách giữa các card phải nhìn rõ trên nền tối; card không dính nhau.
- Một trang không có quá nhiều lớp panel lồng nhau. Phân nhóm bằng heading, spacing và divider trước khi thêm card mới.

## 2.5 Kích thước và responsive

- Master design viewport: **1440 × 900** logical pixels.
- Review viewport phụ: **1280 × 800** và **1920 × 1080**.
- Mức co nhỏ cần kiểm tra: **1024 × 680**; ưu tiên cuộn vùng nội dung, thu gọn sidebar và xếp panel lại thay vì làm chữ bé đi.
- Không cho cửa sổ dữ liệu chính có layout tràn ngang ở 1280 × 800. Nếu không đủ không gian, toolbar chuyển thành menu phụ.
- Kiểm tra Windows scaling 100%, 125%, 150% và DPI Linux mục tiêu. Không dùng pixel positioning cứng cho toàn trang.
- Ghi rõ kích cỡ ảnh mockup là pixel ảnh, không nhầm với logical pixel của UI.

## 2.6 Motion và hiệu ứng

- Animation dùng để phản hồi trạng thái, không dùng để trang trí liên tục.
- Tốc độ tham khảo: hover 100–150 ms; drawer/dialog 160–220 ms; loading không nhấp nháy.
- Reduced Motion tắt pulse/particle/transition không thiết yếu.
- Online indicator không pulse quá mạnh; không animate mọi marker của map.
- Không tải video/particle background trong các trang Fleet, History, Analytics hoặc Settings.

---

# 3. Shell và design system

## 3.1 Shell chung

```text
┌──────────────────────────────────────────────────────────────────────────────┐
│ Brand / Breadcrumb / Global Search / Sync State / Notifications / User Menu │
├─────────────────┬────────────────────────────────────────────────────────────┤
│ Navigation      │ Page title + context + main actions                        │
│                 ├────────────────────────────────────────────────────────────┤
│ Command Center  │ Page body: tables, map, charts, forms, drawer               │
│ Accounts        │                                                            │
│ Game Data       │                                                            │
│ History         │                                                            │
│ Analytics       │                                                            │
│ Alerts          │                                                            │
│ System          │                                                            │
│ Help            │                                                            │
├─────────────────┴────────────────────────────────────────────────────────────┤
│ Connectivity / Last sync / Pending changes / Version                         │
└──────────────────────────────────────────────────────────────────────────────┘
```

### Thanh đầu trang

- Logo nhỏ và tên sản phẩm; tránh artwork chiếm quá nhiều chiều cao.
- Search/Command Palette `Ctrl+K`.
- Sync status có text: `Live`, `Đã cập nhật 12 giây trước`, `Đang đồng bộ`, `Đang chờ đồng bộ`, `Offline`.
- Notification center và menu user.
- Không hiển thị tên tài khoản game thay cho danh tính người dùng OceanForge.

### Sidebar

- Nhóm menu: Command Center; Accounts; Game Data; History; Analytics; Alerts; System; Help.
- Active item dùng bar cyan/gold nhẹ cùng icon/label, không chỉ đổi màu chữ.
- Sidebar có chế độ compact; nhớ tuỳ chọn cục bộ.
- Không render toàn bộ trang khác chỉ để đổi selected item. Navigation phải hủy tác vụ cũ khi phù hợp.

### Status bar

Hiển thị trạng thái kết nối API, sync/queue, thời điểm dữ liệu gần nhất và version. Nếu offline, phải báo stale/pending một cách dễ hiểu. Click vào trạng thái mở trang Diagnostics hoặc Sync Center phù hợp.

## 3.2 Thành phần dùng lại

- `PrimaryButton`, `SecondaryButton`, `DangerButton`, `IconButton` có disabled/busy states.
- `StatusBadge` với icon + text.
- `FreshnessLabel` gồm nguồn, thời điểm nhận, thời điểm sync.
- `EmptyState` có giải thích + hành động tiếp theo.
- `InlineError` có mã lỗi/Retry khi hợp lệ.
- `SkeletonRow`/`SkeletonCard` nhẹ, tránh shimmer quá mạnh.
- `ConfirmDialog` thể hiện rõ đối tượng và hậu quả; hành động phá hủy yêu cầu confirm cụ thể.
- `RightDrawer` cho chi tiết tài khoản và quick inspect; không dùng centered modal lớn cho mọi thao tác.
- `VirtualizedDataGrid` cho tập dữ liệu có thể lớn.
- `ChartFrame` có title, time range, unit, data freshness, loading/no data/error.
- `SyncChip` cho synced/pending/conflict/failed.
- `PermissionGate` chỉ ở UI để giải thích; API vẫn là nơi quyết định quyền thật.
- `CopyableValue` chỉ cho giá trị được phép copy; không hiển thị secret.

## 3.3 Quy chuẩn icon

Dùng một hệ icon thống nhất, line icon dễ nhìn; không trộn quá nhiều bộ emoji/3D. Icon có tooltip khi label không hiện. Chỉ dùng hình đầu lâu/hải tặc ở thương hiệu, splash và artwork giới hạn; không dùng icon này cho trạng thái lỗi hoặc nút gỡ cài đặt.

---

# 4. Trạng thái tương tác bắt buộc cho app

Mọi trang cần triển khai các trạng thái áp dụng được bên dưới. Không phải mỗi trang cần tất cả, nhưng nếu state có ý nghĩa thì phải thiết kế và test.

| State ID | Tên | Hành vi và copy tham khảo |
|---|---|---|
| `ST-LOAD` | Loading | Skeleton đơn giản; giữ bố cục; không khóa toàn cửa sổ nếu chỉ một panel đang tải |
| `ST-EMPTY` | Empty | Giải thích vì sao chưa có dữ liệu và nút tạo/thêm/làm mới phù hợp |
| `ST-ERROR` | Error | Nêu vấn đề và hành động tiếp theo; hiển thị Trace ID, không show stack trace |
| `ST-OFFLINE` | Offline | Hiển thị dữ liệu cache nếu có; không gắn nhãn LIVE; thao tác offline chỉ mở khi policy cho phép |
| `ST-STALE` | Stale | Hiển thị `Cập nhật lần cuối ...`; có thể làm mới khi online |
| `ST-PENDING` | Pending sync | Hiện số mục chờ; cho xem hàng đợi; không tuyên bố server đã lưu |
| `ST-CONFLICT` | Conflict | Hiện khác biệt, phiên bản/local/server và lựa chọn giải quyết nếu thao tác hỗ trợ merge |
| `ST-PERMISSION` | Permission denied | Giải thích quyền còn thiếu và nơi liên hệ; không tiết lộ dữ liệu bị chặn |
| `ST-BUSY` | Busy | Ngăn thao tác trùng; nút busy có progress; không block UI thread |
| `ST-SUCCESS` | Success | Chỉ hiện khi server/local operation xác nhận hoàn tất thật |
| `ST-RETRY` | Retry scheduled | Hiển thị thử lại sau bao lâu; backoff không tạo bão request |
| `ST-DISK` | Disk low/full | Chỉ rõ việc nào tạm dừng, dung lượng cần giải phóng; không âm thầm xóa pending outbox |

### Copywriting

- Viết ngắn, hành động cụ thể, không đổ lỗi cho người dùng.
- Lỗi mạng: `Không kết nối được máy chủ. Dữ liệu đang hiển thị là dữ liệu đã lưu lúc ...`.
- Pending: `Thay đổi đang chờ đồng bộ. Chưa được máy chủ xác nhận.`
- Conflict: `Dữ liệu đã thay đổi ở nơi khác. Kiểm tra phiên bản và chọn cách xử lý.`
- Gỡ app: nêu rõ app file, cache/config local, outbox chưa đồng bộ và cloud data là các nhóm khác nhau.
- Không dùng thông điệp `Thành công` trước khi operation/health-check xác nhận.

---

# 5. Manifest 24 màn hình app

`Core` là phạm vi ưu tiên; `Admin` phụ thuộc quyền; `Later`/`Conditional` không cần làm trong MVP nếu chưa xác nhận tính năng.

| ID | Tên chuẩn | Ưu tiên | File ảnh riêng |
|---:|---|---|---|
| 01 | Brand Splash / Startup | Core | `UI-01-Splash.png` |
| 02 | Sign In | Core | `UI-02-SignIn.png` |
| 03 | Create Account | Core | `UI-03-CreateAccount.png` |
| 04 | First-run Welcome / Main Shell | Core | `UI-04-WelcomeShell.png` |
| 05 | Fleet / Account Selector | Core | `UI-05-Fleet.png` |
| 06 | Command Center Dashboard | Core | `UI-06-Dashboard.png` |
| 07 | Account Detail Drawer | Core | `UI-07-AccountDetail.png` |
| 08 | Inventory / Materials | Core | `UI-08-Inventory.png` |
| 09 | World / Sea Map | Core | `UI-09-WorldMap.png` |
| 10 | Analytics | Core | `UI-10-Analytics.png` |
| 11 | History / Event Timeline | Core | `UI-11-History.png` |
| 12 | Alerts / Notification Center | Core | `UI-12-Alerts.png` |
| 13 | Tracked Account Metadata Editor | Core | `UI-13-AccountEditor.png` |
| 14 | Settings | Core | `UI-14-Settings.png` |
| 15 | User / Role Administration | Admin | `UI-15-AdminUsers.png` |
| 16 | Backup / Restore | Core admin | `UI-16-BackupRestore.png` |
| 17 | Updates / Release Channel | Core | `UI-17-Updates.png` |
| 18 | System Health / Diagnostics | Core | `UI-18-Diagnostics.png` |
| 19 | Safe Import / QR Pairing | Conditional | `UI-19-ImportPairing.png` |
| 20 | License / Plan | Conditional | `UI-20-LicensePlan.png` |
| 21 | Diagnostic Console / Logs | Developer/Admin | `UI-21-ConsoleLogs.png` |
| 22 | Team Messages / Collaboration | Later | `UI-22-TeamMessages.png` |
| 23 | Community Statistics / Leaderboard | Later | `UI-23-CommunityStats.png` |
| 24 | About / Version / Support | Core | `UI-24-AboutSupport.png` |

## UI-01 — Brand Splash / Startup

**Mục tiêu:** đưa thương hiệu lên màn hình trong thời gian khởi động thật, sau đó chuyển sang trạng thái tiếp theo mà không giữ splash giả tạo.

**Bố cục:** hình tàu/biển làm artwork trung tâm hoặc lệch trái; logo OceanForge và tên app; progress line nhỏ ở đáy; version/channel ở góc dưới. Không dùng nhiều nút. Khu vực branding chiếm trọng tâm nhưng progress phải dễ nhìn.

**Tương tác:** không cần nút nếu khởi động bình thường; khi khởi tạo quá lâu có thể hiện `Đang kiểm tra kết nối...` và nút `Xem chẩn đoán`. Nếu có lỗi runtime, chuyển sang error recovery thay vì đứng vô hạn.

**States:** startup, loading dependency, local database migration, offline-start, startup failure. Splash không mô phỏng % giả nếu không có tiến độ có thể đo.

**Acceptance:** không block UI sau khởi tạo; app khởi động offline được nếu cache hợp lệ; version hiển thị khớp build metadata.

**Prompt ảnh độc lập:** `Single desktop app splash screen, 16:10, cinematic pirate ship in deep navy and cyan ocean at dusk, restrained brass OceanForge emblem, centered wordmark, thin honest progress indicator at bottom, version and channel in small readable text, premium operational software, no collage, one screen only.`

## UI-02 — Sign In

**Mục tiêu:** đăng nhập vào tài khoản OceanForge, không phải tài khoản Roblox.

**Bố cục:** thương hiệu bên trái hoặc ở nền tối có overlay; form rõ ràng ở một panel độc lập; email/username, password, show/hide, remember device nếu policy cho phép, nút Sign in, Register, Reset password, server status và privacy/help link.

**Tương tác:** Enter gửi form; Caps Lock indicator nếu khả thi; password visibility toggle không làm mất nội dung; nút submit có busy state; validation field-level; rate limit/verification message không tiết lộ có/không tài khoản theo cách gây enumeration.

**States:** pristine, invalid input, submitting, wrong credentials, rate limited, server offline, expired session, success. Khi API offline có thể mở app limited/offline workspace nếu auth cache/session policy cho phép; không bỏ qua authentication chỉ vì mất mạng.

**Acceptance:** password không log; không lưu password; focus order đúng; lỗi có nội dung cụ thể; không hỏi Roblox cookie/token.

**Prompt ảnh độc lập:** `Single OceanForge sign-in window, dark abyss navy background and subtle ocean ship artwork, centered compact login panel, email field, password with eye toggle, remember-device checkbox, primary blue/cyan sign-in button, secondary create-account and reset links, server status and version, high readability, 16:10, no collage.`

## UI-03 — Create Account

**Mục tiêu:** tạo tài khoản OceanForge mới.

**Bố cục:** form 2/3 chiều rộng, logo nhỏ, tên hiển thị, email, password, confirm password, điều khoản/privacy checkbox, trạng thái email verification. Không để artwork lấn át form.

**Tương tác:** password rules hiển thị trước submit; confirm mismatch chỉ ra field; submit disabled khi invalid; rate limit có retry time; chuyển sang màn hình xác minh email; có link quay lại Sign In.

**States:** form pristine, validation errors, submitting, verification pending, email already used (copy chống account enumeration theo security policy), network loss, rate limit, registration success.

**Acceptance:** không yêu cầu Roblox credential; không gắn UI thành công khi server chưa tạo tài khoản; điều khoản có thể mở đọc.

**Prompt ảnh độc lập:** `Single OceanForge create-account desktop screen, dark navy maritime theme, clean form with display name, email, password, confirm password, accept terms, password requirements helper, verification step hint, restrained brass logo and cyan action button, legible labels, 16:10, no collage.`

## UI-04 — First-run Welcome / Main Shell

**Mục tiêu:** giúp người mới biết bước kế tiếp và giới thiệu điều hướng thật của app.

**Bố cục:** shell chuẩn, sidebar đầy đủ, vùng welcome rộng vừa phải với artwork hải đảo trong panel giới hạn; lời chào, trạng thái API/sync, các action: `Mở Fleet`, `Thiết lập nguồn telemetry`, `Xem Diagnostics`. Khu vực Recent Activity có empty state khi workspace mới.

**Tương tác:** mỗi quick action điều hướng tới trang cụ thể; dismiss welcome nhớ theo user; có thể quay lại qua Help.

**States:** workspace trống, đã có dữ liệu, server offline, cache có dữ liệu cũ, cần chọn workspace, permission limited.

**Acceptance:** welcome không thay Dashboard; sidebar không mất; status bar có thời điểm sync/freshness.

**Prompt ảnh độc lập:** `Single OceanForge first-run welcome page within a polished desktop shell, left compact navigation rail, upper status bar, central limited pirate harbor artwork, three clear action tiles for Fleet, Data Source, Diagnostics, recent activity empty state, navy/teal/gold premium interface, 16:10, no collage.`

## UI-05 — Fleet / Account Selector

**Mục tiêu:** tìm, lọc và chọn các account được phép theo dõi.

**Bố cục:** page title + số kết quả; search; filter bar (Sea, status, level, race, fruit, tags, group, last seen); saved views; virtualized table/list. Hàng có status icon + text, display name/alias, level, Sea, Race, fruit/loadout tóm tắt, Beli/Fragments, last seen/freshness, alert badge. Có bulk action menu nhưng chỉ cho thao tác được ủy quyền.

**Tương tác:** debounce search, hủy request cũ, sort server/local theo contract, pagination/cursor, saved view, chọn hàng mở `UI-07` drawer, empty-filter reset. Không load toàn bộ fleet một lúc.

**States:** empty workspace, no search result, loading first page, append next page, offline cache, stale rows, sync pending, partial API error, unauthorized bulk action.

**Acceptance:** virtualization đo với 1k/10k row sample; chọn account không reset scroll khi chỉ có delta; freshness hiển thị trên row hoặc toolbar.

**Prompt ảnh độc lập:** `Single dense but readable fleet management desktop page, OceanForge dark ocean theme, compact sidebar, toolbar with search and filters, saved views tabs, virtualized data grid of sample tracked game account telemetry, status icons and labels, last-seen column, right detail affordance, restrained cyan/gold accents, 16:10, no collage.`

## UI-06 — Command Center Dashboard

**Mục tiêu:** cho người vận hành biết toàn fleet đang khỏe ra sao.

**Bố cục:** KPI strip: Total, Online, Stale, Offline, Active Sessions, Alerts; main central map/world summary hoặc status distribution; recent changes; charts nhỏ cho online trend và telemetry freshness. Không lạm dụng card 6 ô giống SaaS tiêu chuẩn. KPI có drill-down.

**Tương tác:** time range selector; click KPI áp filter Fleet; click event mở account detail; refresh không reload toàn trang; chart tooltip keyboard-accessible.

**States:** loading placeholders, no tracked accounts, stale data/offline banner, partial chart failure, API error with retry. KPI phải nêu `as of` khi dữ liệu không live.

**Acceptance:** dashboard query là aggregate từ API; không tính aggregate lớn trên UI thread; delta update chỉ thay panel cần thiết.

**Prompt ảnh độc lập:** `Single OceanForge command-center dashboard for a cross-platform desktop app, deep navy captain's room design, narrow left navigation, clear KPI strip for tracked accounts online stale offline alerts, large central ocean-map/status region, compact activity timeline and two restrained charts, precise data hierarchy, cyan telemetry and gold highlight, 16:10, no collage.`

## UI-07 — Account Detail Drawer

**Mục tiêu:** xem chi tiết một account mà không mất ngữ cảnh Fleet.

**Bố cục:** right drawer trên Fleet/dashboard; header avatar placeholder, display name/alias, online/stale/offline chip, level/Sea; tabs `Overview`, `Character`, `Loadout`, `Inventory`, `Materials`, `Sessions`, `History`, `Snapshots`; khối thông tin phân hai cột phù hợp. Không hiển thị credential.

**Tương tác:** drawer có close/escape, link đến full history, compare snapshots, copy allowed public identifier, refresh riêng; tab data tải theo nhu cầu. Khi account đổi selection, hủy request trước đó.

**States:** loading, account removed, cached snapshot, access denied, offline/stale, inventory absent, snapshot diff empty, fetch error.

**Acceptance:** selected account ID là key; drawer không giữ dữ liệu nhầm từ account trước; tab chuyển không tải tất cả dữ liệu cùng lúc.

**Prompt ảnh độc lập:** `Single right-side account detail drawer overlaying a blurred/dimmed OceanForge fleet grid, profile header and status, tabs Overview Character Loadout Inventory Materials Sessions History Snapshots, concise telemetry values, small loadout illustration area, dark navy panels and gold selected accent, clear typography, 16:10, no collage.`

## UI-08 — Inventory / Materials

**Mục tiêu:** duyệt inventory quan sát được theo loại vật phẩm và material.

**Bố cục:** tabs Fruits, Swords, Guns, Fighting Styles, Accessories, Materials; search, Sea/owned/source/time filters; grid/list toggle; item tile gồm icon/art, tên chuẩn, rarity/type nếu có nguồn dữ liệu hợp lệ, quantity và last updated. Panel details bên phải hoặc bottom.

**Tương tác:** filter/sort; chọn item mở details; export dữ liệu theo quyền; không có thao tác game mutation/cheat. Grid virtualization hoặc paging với danh mục lớn.

**States:** no inventory, unsupported item icon fallback, stale snapshot, source not authorized, partial image load, local cache only, permission denied.

**Acceptance:** icon không là nguồn dữ liệu duy nhất; item ID/enum ổn định; quantity và timestamp luôn gắn account/source.

**Prompt ảnh độc lập:** `Single OceanForge inventory manager screen, dark ocean UI, category tabs for Fruits Swords Guns Fighting Styles Accessories Materials, search and filter toolbar, tidy grid of vivid but restrained fantasy item icons with item names and quantities, selected-item details pane, data freshness label, navy teal gold style, 16:10, no collage.`

## UI-09 — World / Sea Map

**Mục tiêu:** cho thấy vị trí tương đối/nhóm Sea và marker telemetry nếu data source được phép cung cấp.

**Bố cục:** map là anchor; Sea selector, layers (islands, account markers, route, grid, labels), compass, minimap nếu hữu ích, legend online/stale/offline, marker details side panel. Fog/ornament ở mức nhẹ để không che dữ liệu.

**Tương tác:** zoom/pan, layer toggle, select marker, center selected, filter by status/group; keyboard focus alternative cho marker; disable route nếu không có tọa độ hợp lệ.

**States:** map asset not installed, data unavailable, no markers, stale marker, map tile/image loading, low-bandwidth mode (static/less detail), renderer error.

**Acceptance:** renderer là adapter riêng; marker freshness và legend thống nhất; không render marker sai coordinate; không yêu cầu background animation liên tục.

**Prompt ảnh độc lập:** `Single OceanForge world/sea map screen, ornate but readable colorful archipelago map in a broad central canvas, layers sidebar or toolbar, account status markers with restrained cyan/gold/amber/red legend and clear labels, right selected-marker details, dark navy application shell, 16:10, one screen, no collage.`

## UI-10 — Analytics

**Mục tiêu:** phân tích xu hướng từ telemetry đã lưu, không giả dữ liệu thiếu.

**Bố cục:** time range/date range, account/group selector, tabs Overview/Level/Beli/Fragments/Materials/Activity/Uptime; 1–3 chart chính; KPI summary và note data coverage/gaps. Tránh 5 biểu đồ nhỏ không đọc được.

**Tương tác:** đổi range debounce/cancel previous query; cross-filter; export report chạy background; tooltip có unit/timestamp; zoom/selection không làm thay đổi dữ liệu gốc.

**States:** no data for range, gaps, stale aggregate, processing, export queued, query failure, offline only cached range.

**Acceptance:** aggregate nặng ở server/worker; chart có units; cache key chứa range/filter/version; không tính toàn bộ history trên UI thread.

**Prompt ảnh độc lập:** `Single high-quality OceanForge analytics page, deep navy chart workspace, time range and account filters across top, one large clear line chart and two supporting charts for level/resources/activity, compact summary metrics and data coverage note, consistent axes and units, restrained teal/cyan with gold highlight, 16:10, no collage.`

## UI-11 — History / Event Timeline

**Mục tiêu:** kiểm tra chuỗi sự kiện, session, telemetry, snapshots và diff.

**Bố cục:** filter chips, tab Events/Sessions/Telemetry/Snapshots/Diff; cursor-paged timeline or table; timestamp, event type, account/source, summary, result; details drawer. Có badge event append-only và data source.

**Tương tác:** time filters, export async, open snapshot diff, copy event ID, sort theo contract. Không tải toàn bộ history; `Load more` hoặc paging.

**States:** no event, gaps in history, retention expired, offline cache only, cursor expired/full resync required, malformed source payload flagged.

**Acceptance:** UTC internal, local display locale/timezone; event identity/dedup visible for support; paging ổn định khi có event mới.

**Prompt ảnh độc lập:** `Single OceanForge History and Event Timeline page, dark desktop shell, tabs for Events Sessions Telemetry Snapshots Diff, searchable cursor-paged table with timestamps, accounts, event type, concise detail drawer and filters, crisp text hierarchy, subtle cyan selected event, 16:10, no collage.`

## UI-12 — Alerts / Notification Center

**Mục tiêu:** phân loại, xác nhận và quản lý cảnh báo.

**Bố cục:** severity tabs All/Critical/Warning/Info; active alert list; right panel detail gồm rule source, target/account, first/last seen, evidence summary, actions acknowledge/snooze/mute/open account. Tách alert event khỏi notification delivery status.

**Tương tác:** acknowledge có audit; snooze duration rõ; bulk acknowledge có confirmation; link sang rule nếu permission; không ẩn critical alert tự động.

**States:** no alerts, new alert arrives, stale alert, muted/snoozed, delivery failed, restricted action, offline unable to acknowledge.

**Acceptance:** severity có icon + label; action kết quả phải confirmed; rule edits online only.

**Prompt ảnh độc lập:** `Single OceanForge alerts center, dark navy table of critical warning and info events with icon and readable severity labels, selected alert details panel showing source rule and timestamps, clear acknowledge snooze mute actions, gold amber warning, restrained red only critical, 16:10, no collage.`

## UI-13 — Tracked Account Metadata Editor

**Mục tiêu:** chỉnh metadata OceanForge được phép như alias, tag, group và note; không chỉnh dữ liệu telemetry như thể đó là sự thật.

**Bố cục:** form chia Identity/Organization/Tags/Notes; nguồn dữ liệu read-only nằm riêng; footer Save/Cancel; hiển thị Version/Last modified. Không có trường password/cookie/token Roblox.

**Tương tác:** dirty-state warning khi đóng; validation; online/offline save policy; offline draft có label Pending; optimistic concurrency; conflict preview nếu version thay đổi.

**States:** pristine, dirty, validation, saving, saved, pending offline, conflict, permission denied, server rejection.

**Acceptance:** chỉ gửi field allowlist; server authorization; conflict không tự ghi đè nếu policy chưa cho phép.

**Prompt ảnh độc lập:** `Single OceanForge tracked account metadata editor form, clean dark maritime UI, alias group tags notes editable in the foreground, source telemetry values in a visibly read-only section, version and last modified text, Save and Cancel footer, clear pending/conflict indicators, 16:10, no collage.`

## UI-14 — Settings

**Mục tiêu:** quản lý sở thích, hiệu năng, mạng, privacy/cache và hành vi khởi động.

**Bố cục:** section navigation: General, Appearance, Performance, Network, Offline & Cache, Notifications, Privacy, Startup, Logs/Diagnostics, Advanced. Main panel chỉ hiển thị settings của section đang chọn.

**Tương tác:** setting thay đổi áp dụng immediate khi an toàn hoặc có Apply/Cancel; reset có confirmation; profile `Default`, `Monitoring`, `Performance`, `Low Bandwidth`, `Presentation`, `Developer`. Cấu hình không chứa production secret.

**States:** saving, unsaved changes, invalid range, restart required, settings locked by policy, offline settings local-only.

**Acceptance:** giải thích impact của low-bandwidth/performance; không hứa giảm CPU/RAM tuyệt đối; profile thay đổi batching/render/caching đã được định nghĩa/test.

**Prompt ảnh độc lập:** `Single OceanForge settings page in premium dark navy theme, left settings subsection menu, right clean controls for appearance performance network offline cache privacy notifications startup, segmented performance profile choices, toggles and select fields spaced clearly, restrained gold highlight, 16:10, no collage.`

## UI-15 — User / Role Administration

**Mục tiêu:** admin quản lý thành viên, vai trò và quyền server-side.

**Bố cục:** user table (display/email masked as policy requires, role, status, last active); role editor with permission matrix; audit panel; invite/remove/change role actions tách biệt.

**Tương tác:** mọi quyền thay đổi online; confirm cho quyền cao; bulk changes phải preview; save chỉ khi server xác nhận; audit record hiển thị sau commit.

**States:** no users, loading, permission denied, API offline (read cached view-only), concurrent version conflict, save rejected, admin self-lockout protection.

**Acceptance:** UI không phải ranh giới security; API RBAC và audit là authoritative; hạn chế xóa role/admin cuối cùng.

**Prompt ảnh độc lập:** `Single OceanForge admin user and role administration screen, dense but legible table of members with roles and status, separate permission matrix panel, audit history strip, confirmation affordance for privileged changes, dark navy teal and restrained brass, 16:10, no collage.`

## UI-16 — Backup / Restore

**Mục tiêu:** tạo/kiểm tra/khôi phục bản sao cho đúng phạm vi dữ liệu.

**Bố cục:** tabs Server Backup / Local Export / Restore / History; destination, backup scope, schema version, timestamp, size, checksum, validation status; restore preview cho biết dữ liệu sẽ thay đổi. Không gộp backup cloud và local SQLite thành cùng một nút không phân biệt.

**Tương tác:** create backup là background job; cancel chỉ trước commit point; restore có preview + xác nhận nhập cụm từ/checkbox explicit; backup hiện tại trước restore; sau restore smoke validation. Local outbox pending được cảnh báo.

**States:** queued, progress, success verified, checksum mismatch, destination unavailable, insufficient disk, restore failed, restore completed with warnings, online-only permission.

**Acceptance:** file tồn tại không có nghĩa backup hợp lệ; restore drill chạy ngoài UI; không xóa dữ liệu cloud khi uninstall.

**Prompt ảnh độc lập:** `Single OceanForge backup and restore management screen, dark nautical workstation style, backup scope selector, destination and retention controls, table of dated backup records with size checksum status, right-side restore preview and explicit destructive confirmation area, clear progress and verification labels, 16:10, no collage.`

## UI-17 — Updates / Release Channel

**Mục tiêu:** xem phiên bản và cập nhật của app từ giao diện chính; installation thực thi bởi launcher/platform package flow.

**Bố cục:** current version/channel, latest approved version, release notes, signature/integrity status, download size, maintenance notice, buttons Check Updates/Update via Launcher/Later. Không chạy bộ cài bên trong ViewModel.

**Tương tác:** check update có busy/cancel; click update chuyển handoff sang launcher hoặc OS package manager; kênh stable/beta/nightly có cảnh báo; rollback status và update history rõ.

**States:** up-to-date, update available, offline, manifest unavailable, unsupported OS, signature validation failure, update staged, restart required.

**Acceptance:** stable không tự chuyển sang beta/nightly; không báo update success trước health check; data local/outbox không bị xóa khi update.

**Prompt ảnh độc lập:** `Single OceanForge app Updates page, version comparison panel for current and latest approved stable release, release notes text, package size, verified-signature status, clear Check Updates and Open Launcher controls, dark navy with cyan progress and restrained gold, 16:10, no collage.`

## UI-18 — System Health / Diagnostics

**Mục tiêu:** cho người dùng/support biết lớp nào hoạt động: runtime, OS, disk, memory, CPU, local DB, API, sync, signal/realtime và update.

**Bố cục:** status rows/cards with PASS/WARN/FAIL/UNKNOWN, connectivity path, local DB schema version, pending outbox count/oldest age, disk free, app version/OS. Có Generate Diagnostic Report và Copy Trace ID.

**Tương tác:** run checks async/cancel; report export redact by default; retry riêng từng check; click failed subsystem mở troubleshooting. Không dump secrets.

**States:** test running, partial unknown, endpoint unavailable, local database integrity warning, disk full, update service error.

**Acceptance:** health check không block UI; metrics không đọc hoặc upload nội dung riêng tư không cần thiết.

**Prompt ảnh độc lập:** `Single OceanForge system health diagnostics page, deep navy clean dashboard with rows for runtime operating system disk memory CPU local SQLite API sync realtime update, PASS WARN FAIL UNKNOWN labels with icons, pending sync and last successful connection, export sanitized report button, 16:10, no collage.`

## UI-19 — Safe Import / QR Pairing (Conditional)

**Mục tiêu:** import dữ liệu đã được định dạng hoặc ghép một thiết bị/nguồn hợp lệ; QR không phải đường nhập credential.

**Bố cục:** source picker, file type/schema preview, record count, validation issues, duplicate/conflict preview, mapping section, QR pairing step với payload purpose rõ ràng và expiry. Không dùng QR để chuyển password/cookie/session token.

**Tương tác:** preview trước commit; import chạy async; invalid rows có report; cancel an toàn; QR expire/revoke; user xác nhận workspace đích.

**States:** unsupported type, malformed file, oversized file, duplicate records, expired QR, pairing rejected, offline, import pending/server rejected.

**Acceptance:** reject secrets in imports; file parser có giới hạn kích thước/path traversal protection; không auto-execute nội dung file.

**Prompt ảnh độc lập:** `Single OceanForge safe import and QR pairing page, dark maritime UI, import-source wizard with schema validation preview and duplicate count on one side, explicit temporary QR pairing panel on the other, clear expiry and security notice that passwords and session tokens are not accepted, 16:10, no collage.`

## UI-20 — License / Plan (Conditional)

**Mục tiêu:** hiển thị edition/quota/entitlement nếu sản phẩm thực sự có mô hình license/subscription.

**Bố cục:** current plan, included limits/features, renewal/expiry state when relevant, usage and support; plans so sánh bằng bảng rõ ràng thay vì quảng cáo quá nhiều. Đây là trang conditional, không chặn MVP nếu chưa có billing policy.

**Tương tác:** refresh entitlement from server; manage plan mở trang chính thức; offline hiển thị last verified time và policy grace period đã phê duyệt; không có nút “unlock” cục bộ để vượt entitlement.

**States:** active, trial, expiry approaching, expired, server unavailable, payment provider unavailable, user lacks billing permission.

**Acceptance:** server-authoritative entitlement; không lưu payment secret; text pháp lý cần review.

**Prompt ảnh độc lập:** `Single OceanForge license and plan page, premium dark navy tables comparing a few clear plans, current entitlement and usage meter, renewal status and official manage-plan link, restrained gold highlights, no aggressive neon or misleading pricing, 16:10, no collage.`

## UI-21 — Diagnostic Console / Logs

**Mục tiêu:** developer/admin xem structured logs đã redact.

**Bố cục:** filter bar (time, severity, category, trace ID), virtualized log list, selected event detail với properties đã redact, export controls và retention notice. Console không phải shell thực thi lệnh hệ thống tự do.

**Tương tác:** query/cursor paging; copy trace ID; export có redact; live tail có pause và rate limit; không block UI bằng log flood.

**States:** no logs, high volume throttled, export queued, permission denied, offline local logs only, redaction warning.

**Acceptance:** không show password/token/cookie/private keys/full sensitive payload; log flood được batch/coalesce; filter có thể hủy.

**Prompt ảnh độc lập:** `Single OceanForge diagnostic logs console, dark navy technical panel, time severity category trace filters, compact structured log table and selected sanitized JSON-like property viewer, paused live-tail control and export button, no shell command prompt suggesting arbitrary execution, 16:10, no collage.`

## UI-22 — Team Messages / Collaboration (Later)

**Mục tiêu:** trao đổi trong workspace nếu sản phẩm có collaboration.

**Bố cục:** channel/team list, message timeline, composer, author/time, unread indicators, attachment policy. Không dùng hình game quá nhiều; giữ readability.

**Tương tác:** send online; offline draft chỉ khi được yêu cầu và label rõ; message edit/delete policy; report/moderation; no-secret warning nếu user paste credential-like string.

**States:** empty channel, sending, failed send/retry, offline draft, permission denied, deleted/retained message.

**Acceptance:** access and retention policy exist before release; sanitize links/attachments; no secret in notifications.

**Prompt ảnh độc lập:** `Single OceanForge team collaboration page, dark navy professional chat with channel rail, readable message timeline, modest avatars, composer, unread markers, secure workspace tone, cyan selected channel and gold subtle accents, 16:10, no collage.`

## UI-23 — Community Statistics / Leaderboard (Later)

**Mục tiêu:** public/community aggregate nếu thực sự được phê duyệt; không tự chia sẻ dữ liệu của workspace private.

**Bố cục:** ranking table, timeframe, metric definition, opt-in/data-sharing status, aggregate charts. Tránh expose account identifiers hoặc user activity không được đồng ý.

**Tương tác:** filter period, explanation of ranking, export only if allowed; privacy opt-in separate from local settings.

**States:** not enrolled, insufficient aggregate, loading, privacy restricted, server unavailable, anti-abuse rate limited.

**Acceptance:** explicit consent, data minimization, abuse review; module mặc định không bắt buộc và có thể loại khỏi MVP.

**Prompt ảnh độc lập:** `Single OceanForge community statistics page, dark navy dashboard with privacy-aware aggregate leaderboard, ranked rows using fictional names and coarse metrics, timeframe filter, explicit opt-in data sharing label, tasteful chart and clear metric definitions, 16:10, no collage.`

## UI-24 — About / Version / Support

**Mục tiêu:** xem version, channel, platform, notices, help và support.

**Bố cục:** brand emblem nhỏ, OceanForge Desktop version/build/commit, platform/runtime, license notices, release channel, links Help/Release Notes/Diagnostics/Privacy. Artwork đậm nhưng giới hạn khu vực.

**Tương tác:** copy version/diagnostic ID; open links bằng handler tin cậy; export sanitized diagnostics; check update chuyển `UI-17`.

**States:** links unavailable offline, newer version available, license list loading, diagnostic bundle ready/failed.

**Acceptance:** build version khớp release artifact; không lộ file path/user PII trong about.

**Prompt ảnh độc lập:** `Single OceanForge About and Support page, elegant deep ocean pirate emblem and small ship artwork balanced with version build platform runtime release channel license notices support links and diagnostics export, dark navy and restrained brass, readable text hierarchy, 16:10, no collage.`

---

# 6. Manifest 16 màn hình Launcher / Installer / Downloader / Updater / Uninstaller

Các màn hình này **tách khỏi 24 màn hình app**. Tùy nền tảng, một số bước có thể được thực thi bởi OS/package manager thay vì custom window. Mục tiêu là có hành vi nhất quán, không giả vờ rằng launcher có quyền thay thế cơ chế an toàn của hệ điều hành.

| ID | Màn hình/luồng | Khi nào |
|---|---|---|
| L01 | Welcome / Official Source | Bắt đầu installer/launcher |
| L02 | System Check | Kiểm tra OS, architecture, permission, dung lượng |
| L03 | Install Profile | Chọn Minimal/Standard/Full |
| L04 | Install Location | Chọn đường dẫn và kiểm tra quyền/dung lượng |
| L05 | Download Center | Tải package/assets |
| L06 | Integrity Verification | Verify manifest/hash/signature |
| L07 | Install Progress | Cài đặt theo checkpoint |
| L08 | Finish / Launch | Hoàn tất và khởi chạy |
| L09 | Update Available | Phát hiện bản mới |
| L10 | Apply Update | Stage/cài/update health check |
| L11 | Update Recovery / Rollback | Update lỗi |
| L12 | Repair / Verify Files | File hỏng/thiếu |
| L13 | Uninstall Confirmation | Xác nhận phạm vi gỡ |
| L14 | Uninstall Progress | Gỡ qua OS/package mechanism |
| L15 | Uninstall Result | Kết quả thực tế và local data choice |
| L16 | Install/Update/Uninstall Failure | Lỗi phục hồi được hoặc không |

## Quy tắc chung cho launcher

- Không yêu cầu đăng nhập OceanForge để tải/cài đặt.
- Không chứa secret cloud/database; không chạy script tùy ý tải từ server.
- Release manifest phải được xác minh theo trust policy. Checksum tải cùng nguồn đơn thuần không đủ nếu chính manifest chưa được tin cậy.
- Không cài/chạy package trước khi xác minh byte size, hash và chữ ký theo chính sách.
- Có pause/resume/retry/cancel khi cơ chế download hỗ trợ; resume phải xác minh ETag/range/version, không nối nhầm file.
- Hiển thị dung lượng thực từ manifest: `downloadBytes`, `installedBytesEstimate`, profile, asset pack. Không dùng số dung lượng giả.
- Không yêu cầu quyền admin sớm nếu chưa cần; giải thích lý do khi nâng quyền.
- Không xóa `SQLite`, setting hoặc pending outbox khi update/repair.
- Uninstall không tự xóa cloud data. Outbox chưa sync phải được nêu rõ trước khi user chọn xóa local data.
- Mỗi bước có error code/trace ID và đường phục hồi; không để người dùng ở spinner vô hạn.

## L01 — Welcome / Official Source

**Layout:** logo và tên OceanForge; Publisher; version/channel; thông báo rõ gói được tải từ nguồn chính thức; Privacy, License, Release Notes; CTA `Kiểm tra máy`/`Tiếp tục`; không yêu cầu sign-in.

**Interaction:** link được allowlist; hiển thị nguồn publisher; Cancel/Exit; mở release notes không làm mất tiến trình.

**States:** first open, offline, unknown/unsupported manifest, publisher metadata invalid, source untrusted.

**Accept:** không có nút mirror không xác thực; không download khi chưa xác minh nguồn metadata.

**Image prompt:** `Single OceanForge installer welcome window, cinematic restrained ocean-ship hero artwork, clear publisher and release channel, privacy/license links, one primary Continue to system check button and Cancel, no login form, 16:10, no collage.`

## L02 — System Check

**Layout:** checklist OS version, CPU architecture, disk free, permission, installed version, running process, connectivity; status icon + label + details.

**Interaction:** recheck; show reason for blocked item; close app if required only after user agrees; offer link to free disk/help.

**States:** passed, warning, blocked unsupported OS, insufficient disk, app is running, offline but package is already available.

**Accept:** check is real; unsupported target is not reported as ready; no blanket requirement to run as admin.

**Image prompt:** `Single OceanForge system compatibility check, structured rows for OS architecture disk permissions existing install running process and network, PASS/WARN/BLOCKED labels plus a clear next-step footer, dark navy maritime styling, 16:10, no collage.`

## L03 — Install Profile

**Layout:** Minimal / Standard / Full profile cards with installed size estimate, download size, feature/asset differences and disk budget. Mark recommended profile without misleading badges.

**Interaction:** selecting profile updates all sizes; accessible detail for optional packs; Continue; back preserves selection.

**States:** profile loading, profile unsupported, not enough disk, optional pack unavailable.

**Accept:** bytes from signed manifest, not hard-coded claims; Minimal does not fetch optional map/art assets not required.

**Image prompt:** `Single OceanForge install profile selection window with Minimal Standard Full choices, exact download and installed size estimates, included components list, current disk free line, subtle recommended profile outline, 16:10, no collage.`

## L04 — Install Location

**Layout:** path field, Browse button, disk selector/free-space, install scope where platform permits, permission check, notes about user data being stored separately.

**Interaction:** Browse native file picker; validate path; block protected/system-sensitive paths as appropriate; recheck free space before download/install.

**States:** valid path, invalid path, read-only, not writable, insufficient space, removable drive disconnected.

**Accept:** installer uses platform conventions; does not place writable database in read-only install directory.

**Image prompt:** `Single OceanForge install location screen with editable destination path, Browse control, disk free/required space comparison and permission status, note that user cache/settings are stored separately, dark premium UI, 16:10, no collage.`

## L05 — Download Center

**Layout:** package/asset rows, bytes downloaded/total, percent derived from bytes, speed, elapsed/ETA only if reliable, pause/resume/cancel, retry, per-file integrity pending status, event log foldout.

**Interaction:** pause/resume, cancel confirmation if partial work will be discarded, retry with backoff, switch to low-bandwidth mode, keep UI responsive. Resume only with matching ETag/range/version.

**States:** queued, downloading, paused, reconnecting, rate limited, server unreachable, disk full, cancel pending, complete-awaiting-verify.

**Accept:** progress never moves past total; no fake percent; cancel cleans temporary files safely; bounded concurrency and disk quota.

**Image prompt:** `Single OceanForge download center, prominent download progress bar, downloaded/total bytes, current speed, a list of app package and optional resource packs, Pause Resume Cancel controls, network retry message and dark ocean-themed frame, 16:10, no collage.`

## L06 — Integrity Verification

**Layout:** steps for manifest trust, downloaded size, SHA-256 digest, package signature/provenance policy, verified/rejected result; do not display secret key material.

**Interaction:** verification runs async; Cancel where safe; on mismatch provide `Tải lại` and report code; never offer an unsafe `Ignore and run` bypass.

**States:** verifying, verified, hash mismatch, signature missing/invalid, manifest expired/untrusted, platform verification unavailable.

**Accept:** installation hard-blocked unless trust policy passes; failed artifact quarantined/deleted according to safe policy.

**Image prompt:** `Single OceanForge integrity verification window, a vertical checklist for trusted release manifest, byte-size check, SHA-256, package signature and provenance status, verified result in seafoam, failed result in muted red with only safe retry options, 16:10, no collage.`

## L07 — Install Progress

**Layout:** phase sequence Prepare → Unpack/Stage → Register/Install → Shortcuts/Integrations → Local migration if applicable → Health check; one progress area plus current phase.

**Interaction:** close button disabled during non-interruptible commit checkpoint; cancel allowed only at safe checkpoints; show elapsed/log details toggle.

**States:** preparing, installing, waiting for app close, permission prompt, migrating local schema, verifying, cancel requested, failed/recovery.

**Accept:** not display success before verification; migration is versioned and backup-safe; never modify cloud DB schema from desktop installer.

**Image prompt:** `Single OceanForge installation progress screen, phase checklist and clear percentage/bar with current step, optional technical details drawer, controlled cancel policy, navy/cyan/gold accents and subtle ship image, 16:10, no collage.`

## L08 — Finish / Launch

**Layout:** version installed, channel, components installed, health-check status, checkbox `Mở OceanForge`, release notes link, Finish.

**Interaction:** launch only when install verified; Finish returns/ closes installer; if launch fails open diagnostics. No auto-launch without disclosed setting.

**States:** success, installed but launch failed, restart required, optional component skipped.

**Accept:** version/build from actual manifest; success message only after installed file checks and lightweight launch health check pass.

**Image prompt:** `Single OceanForge installer finish screen showing verified installed version and channel, health check passed, open app checkbox, release notes link, clear Finish button and subdued maritime artwork, 16:10, no collage.`

## L09 — Update Available

**Layout:** current vs available version, stable/beta/nightly channel label, package size, release notes summary, severity/security update note, `Update now`, `Later`, change channel only in separate settings action.

**Interaction:** details expanded before approval; Later sets reminder according to policy; limited background download follows Settings, not unlimited hidden work.

**States:** update available, mandatory critical security update policy if genuinely applicable, offline, unsupported platform, manifest unavailable.

**Accept:** stable never silently switches to beta/nightly; user sees exact update source and size.

**Image prompt:** `Single OceanForge update available window comparing installed and newest approved version, stable channel, package size, concise release notes and security notice, Update now and Later controls, dark premium nautical styling, 16:10, no collage.`

## L10 — Apply Update

**Layout:** stage/verify/close app/apply/health-check sequence; note data preservation; progress and current phase. Separate UI from OS-native package manager action where appropriate.

**Interaction:** ask user to save work; request graceful app close; do not terminate abruptly without an explicit policy; use atomic/staged replacement when supported; restart app only after confirmation/policy.

**States:** preparing, waiting for app close, applying, OS package manager handoff, health check, success, failed.

**Accept:** preserves local SQLite/settings/outbox; no unverified package execution; records rollback-compatible previous version where packaging supports it.

**Image prompt:** `Single OceanForge apply update screen with sequential phase indicator Stage Verify Close App Apply Health Check, clear data-preservation note, current progress, dark navy UI with cyan progress and restrained gold highlights, 16:10, no collage.`

## L11 — Update Recovery / Rollback

**Layout:** problem summary, update version that failed, previous known-good version, rollback capability/status, local-data preservation status, trace ID, options `Khôi phục bản trước`, `Xem chẩn đoán`, `Thử lại` only when safe.

**Interaction:** rollback app files only when package model supports; never automatically restore production DB; do not silently downgrade local schema if migration is not backward-compatible.

**States:** rollback available, rollback in progress, rollback unavailable, old version restored, manual recovery required.

**Accept:** accurate rollback claim; clear whether restart required; preserve user data/outbox.

**Image prompt:** `Single OceanForge update recovery screen, calm non-alarming error explanation, previous known-good version card, app rollback progress/status, explicit preserve-local-data note, Retry Diagnostics and Restore Previous Version controls where supported, 16:10, no collage.`

## L12 — Repair / Verify Files

**Layout:** quick/full verification selector, detected missing/modified files, download size needed, manifest status, expected impact, start repair.

**Interaction:** repair only app-owned files; verify replacement before apply; preserve user settings, reports and SQLite; ask before closing running app.

**States:** scan, healthy, files missing, files altered, download needed, disk insufficient, repair completed, repair failed.

**Accept:** never overwrite user data indiscriminately; output diagnostic code and exact list of repaired items.

**Image prompt:** `Single OceanForge repair and file verification window, checklist of app files healthy/missing/modified, verification scope, repair download estimate, Start Repair button, clear guarantee that settings/local user data are preserved, 16:10, no collage.`

## L13 — Uninstall Confirmation

**Layout:** application name/version; list of files/package that will be removed; separate unchecked option for local cache/settings/report deletion if supported; prominent warning for pending unsynced outbox; cloud data explicitly retained.

**Interaction:** require deliberate confirm; if outbox pending, offer export/return to app/cancel before delete; do not combine `Uninstall app` and `Delete cloud workspace`.

**States:** no pending local data, pending sync exists, cannot access cache, platform manager handoff, cancel.

**Accept:** cloud data never deleted by app uninstall; destructive local cleanup opt-in and clearly described.

**Image prompt:** `Single OceanForge uninstall confirmation dialog, clear list of app files to be removed, separate unchecked delete-local-cache-and-settings option, amber warning for pending unsynced changes, explicit text that cloud workspace is not deleted, Cancel and Uninstall buttons, 16:10, no collage.`

## L14 — Uninstall Progress

**Layout:** phases close app, wait for locks, call OS-native uninstaller/package manager, cleanup only app-owned files, report leftovers. Minimal brand artwork.

**Interaction:** progress/cancel only if underlying package system safely permits; handle locked files with Retry on restart/help; never claim complete until verified.

**States:** removing, app still running, package manager handoff, permission denied, partial cleanup, done.

**Accept:** no arbitrary registry/system-file deletion; no root assumption on Linux; app-owned path boundary.

**Image prompt:** `Single OceanForge uninstall progress window with steps close app, remove package, clean owned files, verify removal, transparent progress and status for locked files, restrained dark nautical UI, 16:10, no collage.`

## L15 — Uninstall Result

**Layout:** Removed/Partially removed/Failed result; version; package manager result; local data policy result; buttons `Close`, `Open cleanup guide`, `Export diagnostic`. If app removed but data retained, say where and why at a safe, non-sensitive level.

**Interaction:** optional local cleanup separate from cloud deletion; cleanup action explicit and irreversible warning; diagnostic bundle scrubbed.

**States:** fully removed, app removed/local data retained, partial leftovers, pending restart, cleanup failed.

**Accept:** use verified result, not assumed success; offer a clear next action.

**Image prompt:** `Single OceanForge uninstall result screen with accurate removed/partial/failed outcome, separate local data retained summary, optional cleanup guide and sanitized diagnostics, concise Finish button, dark navy minimal style, 16:10, no collage.`

## L16 — Install / Update / Uninstall Failure

**Layout:** operation name, understandable reason, short error code, safe recovery action, retry/re-download/help and export sanitized diagnostic. Show current version/install state.

**Interaction:** retry only after transient errors or after user resolves cause; permanent signature mismatch must not offer bypass; rollback/repair actions route to their flow.

**States:** network error, disk full, permission denial, signature failure, corrupt package, locked file, unsupported OS, rollback failure.

**Accept:** no stack trace or secrets; no endless retry; final status must tell whether app is installed/updated/removed.

**Image prompt:** `Single OceanForge launcher failure and recovery window, understandable operation-specific error, error code, install-state summary, safe Retry Repair and Export Sanitized Diagnostics actions, signature trust errors clearly blocked, dark navy maritime style, 16:10, no collage.`

## 6.1 Hành vi theo hệ điều hành khi gỡ

- **Windows MSIX:** để Windows/App Installer quản lý install/update/uninstall; launcher chỉ chuyển giao hoặc mở trang quản lý phù hợp.
- **Windows signed EXE:** installer framework phải đăng ký uninstaller và test upgrade/repair/uninstall, file đang bị khóa và low-privilege user.
- **Linux `.deb`:** package manager quản lý package; user data/cache không nằm trong package-owned file set.
- **Linux AppImage:** xóa AppImage không đồng nghĩa xóa config/cache ở home; có hướng dẫn/wizard cleanup riêng, không tự đòi root.
- **Cloud:** uninstall không xóa workspace hoặc dữ liệu cloud. Xóa workspace là tính năng riêng, online-only, có xác nhận và audit.
- **Local:** giữ settings/reports/cache theo policy đã thông báo. Pending outbox phải được cảnh báo; cung cấp export/backup nếu phù hợp.

---

# 7. Luồng điều hướng chính

## 7.1 Main app

```text
UI-01 Splash
  ├── startup success + no session → UI-02 Sign In
  ├── first-time registered/verified → UI-04 Welcome Shell
  ├── session valid → UI-06 Dashboard
  └── startup error → UI-18 Diagnostics / recovery state

UI-02 Sign In ↔ UI-03 Create Account
UI-04 Welcome → UI-05 Fleet / UI-06 Dashboard / UI-18 Diagnostics
UI-05 Fleet → UI-07 Account Detail
UI-06 Dashboard → UI-05 Fleet / UI-09 Map / UI-10 Analytics / UI-12 Alerts
UI-07 Account Detail → UI-08 Inventory / UI-11 History / UI-13 Metadata Editor
UI-14 Settings → UI-17 Updates / UI-18 Diagnostics / UI-21 Logs
UI-16 Backup Restore → confirmation → background job → result/diagnostics
UI-24 About → release notes / support / diagnostics
```

## 7.2 Launcher

```text
L01 Welcome
 → L02 System Check
 → L03 Install Profile
 → L04 Install Location
 → L05 Download Center
 → L06 Integrity Verification
 → L07 Install Progress
 → L08 Finish / Launch

installed app:
 → L09 Update Available → L10 Apply Update → success
                              └ failure → L11 Recovery / Rollback
 → L12 Repair / Verify Files
 → L13 Uninstall Confirmation → L14 Progress → L15 Result
Any blocking lifecycle operation → L16 Failure/Recovery when needed
```

Không bắt buộc hiển thị cả 16 bước tuần tự trong một lần cài; manifest xác định các trạng thái/cửa sổ cần có khi điều kiện tương ứng xảy ra.

---

# 8. Quy chuẩn tạo ảnh độc lập

## 8.1 Thông số ảnh

- Mỗi mockup giao diện chính: PNG độc lập, gợi ý 1600 × 1000 px (16:10). Có thể xuất 1440 × 900 nếu workflow thiết kế yêu cầu; không thay tỷ lệ giữa các màn hình trong cùng một nhóm.
- Launcher có thể dùng cửa sổ nhỏ hơn nhưng ảnh vẫn đặt trên canvas nhất quán, ví dụ 1440 × 900, để review typography.
- Mỗi ảnh chứa duy nhất một cửa sổ/viewport; không có contact sheet, bảng ghép, collage, thiết bị giả lập bên ngoài hoặc nhiều biến thể trên một canvas.
- UI label trong ảnh phải ngắn, nhất quán tiếng Việt; text mang tính dữ liệu phải đủ đọc. Nếu image generator tạo chữ sai, coi ảnh là concept art, không phải spec cuối: thay chữ trong Figma/vector/design tool trước khi duyệt.
- Không yêu cầu image generator tạo code XAML thật; ảnh chỉ duyệt thị giác và bố cục.

## 8.2 Tên file bắt buộc

```text
ui-ux/
├── app/
│   ├── UI-01-Splash.png
│   ├── UI-02-SignIn.png
│   ├── UI-03-CreateAccount.png
│   ├── UI-04-WelcomeShell.png
│   ├── UI-05-Fleet.png
│   ├── UI-06-Dashboard.png
│   ├── UI-07-AccountDetail.png
│   ├── UI-08-Inventory.png
│   ├── UI-09-WorldMap.png
│   ├── UI-10-Analytics.png
│   ├── UI-11-History.png
│   ├── UI-12-Alerts.png
│   ├── UI-13-AccountEditor.png
│   ├── UI-14-Settings.png
│   ├── UI-15-AdminUsers.png
│   ├── UI-16-BackupRestore.png
│   ├── UI-17-Updates.png
│   ├── UI-18-Diagnostics.png
│   ├── UI-19-ImportPairing.png
│   ├── UI-20-LicensePlan.png
│   ├── UI-21-ConsoleLogs.png
│   ├── UI-22-TeamMessages.png
│   ├── UI-23-CommunityStats.png
│   └── UI-24-AboutSupport.png
└── launcher/
    ├── L01-Welcome.png
    ├── L02-SystemCheck.png
    ├── L03-InstallProfile.png
    ├── L04-InstallLocation.png
    ├── L05-DownloadCenter.png
    ├── L06-IntegrityVerification.png
    ├── L07-InstallProgress.png
    ├── L08-FinishLaunch.png
    ├── L09-UpdateAvailable.png
    ├── L10-ApplyUpdate.png
    ├── L11-UpdateRecovery.png
    ├── L12-RepairFiles.png
    ├── L13-UninstallConfirmation.png
    ├── L14-UninstallProgress.png
    ├── L15-UninstallResult.png
    └── L16-OperationFailure.png
```

## 8.3 Prompt mẫu dùng cho từng ảnh

Dùng prompt riêng đã ghi tại mỗi màn hình và nối thêm chuẩn hình ảnh chung này:

> `Premium production desktop software UI mockup, OceanForge brand, Abyssal Ocean / Ancient Treasure Map / Captain’s Command Room visual language, deep navy background, restrained teal and brass accents, strong typography hierarchy, clear spacing, realistic Avalonia desktop layout, high contrast, one independent screen only, clean straight-on screenshot, no collage, no contact sheet, no phone frame, no unreadable decorative paragraphs, use short accurate Vietnamese labels.`

Negative constraints: `no 24-panel collage, no multiple app screens in one image, no tiny illegible text, no random brand variations, no excessive glow, no unrelated logos, no fake OS dialogs unless specified, no secrets or real account credentials.`

---

# 9. Hợp đồng triển khai UI trong C# / Avalonia

## 9.1 Naming

| Artifact | Quy tắc |
|---|---|
| View | `FleetView.axaml`, `DashboardView.axaml`, `LauncherDownloadView.axaml` |
| ViewModel | `FleetViewModel.cs`, `DashboardViewModel.cs`, `LauncherDownloadViewModel.cs` |
| State | `ScreenLoadState`, `SyncPresentationState`, `LauncherOperationState` hoặc enum tương ứng |
| Command | `LoadCommand`, `RetryCommand`, `SaveCommand`, `CancelCommand`, `OpenDetailsCommand` |
| Test ID | `UI-05-Fleet-Search`, `L05-Download-Pause` v.v. |
| Asset | `Assets/Brand`, `Assets/Maps`, `Assets/Items`, `Assets/Icons` |

## 9.2 Trách nhiệm ViewModel

ViewModel chỉ điều phối presentation state và gọi Application service/use case. Không chứa SQL, business rule, update package execution hay thuật toán analytics nặng. Mọi I/O có cancellation/timeout; mọi event subscription có lifecycle/dispose rõ.

## 9.3 Trạng thái mẫu

```csharp
public enum ScreenLoadState
{
    Idle,
    Loading,
    Loaded,
    Empty,
    Stale,
    Offline,
    Error
}

public enum SyncPresentationState
{
    Synced,
    Pending,
    Syncing,
    Conflict,
    Failed,
    Offline
}

public enum LauncherOperationState
{
    Idle,
    Checking,
    Downloading,
    Paused,
    Verifying,
    Installing,
    HealthChecking,
    Completed,
    Failed,
    RollingBack,
    Repairing,
    Uninstalling
}
```

Đây là gợi ý model state; cần mapping rõ với business state và không được lấy riêng `ScreenLoadState` thay thế error/security/sync state.

## 9.4 Không treo UI

- Không chạy network, file hashing, ZIP extraction, report export hoặc full analytics trên UI thread.
- Dùng progress reporting throttled/coalesced, không push thousands of UI updates per second.
- Search có debounce + cancellation token; VirtualizedDataGrid; cursor pagination.
- Hủy tác vụ cũ khi thay filter/account; kiểm tra cancellation trước khi apply kết quả.
- Dùng bounded queues và bounded cache; không tạo task vô hạn cho mỗi event.
- Dispose timer/subscriptions/connections khi ViewModel/page rời khỏi lifecycle.
- Update/uninstall là worker/launcher operation với explicit state; Main App không sửa app files trực tiếp.

---

# 10. Quy chuẩn dữ liệu và sample content

- Tên sample account thống nhất giữa các màn hình: `DragonKing`, `BlueMaster`, `FruitHunter`, `White444`, `Shadow999`. Chỉ là sample giả.
- Dùng Level/Beli/Fragments/Sea/Race/Fruit/Last seen với đơn vị và schema nhất quán; không thay đổi số vô lý giữa Dashboard, Fleet và Detail nếu cùng snapshot.
- Sample timestamp dùng thời điểm cố định của prototype, có timezone rõ; production chuyển UTC lưu trữ, locale/timezone của user chỉ áp dụng khi hiển thị.
- `Online`, `Stale`, `Offline` phải tương ứng với ngưỡng heartbeat được cấu hình và freshness; không gán status chỉ dựa vào màu/avatar.
- `Last synced` khác `Last observed`. `Last observed` là lúc nguồn telemetry báo; `Last synced` là lúc máy chủ hoặc local state được đồng bộ.
- Inventory/material state là telemetry quan sát, không phải công cụ thay đổi inventory trong game.
- Không đưa Roblox password, cookie, session token, 2FA/recovery code vào sample, QR payload, logs hay screenshot.

---

# 11. Accessibility, keyboard và desktop behavior

- Keyboard navigation đầy đủ; focus ring hiển thị trên nền tối; tab order theo logic thị giác.
- `Esc` đóng drawer/dialog không phá dữ liệu chưa lưu mà không có cảnh báo.
- `Ctrl+K` mở Command Palette; `Ctrl+F` focus search trang; `F5` chỉ refresh trang nếu không xung đột; các phím có shortcut phải được ghi trong Help.
- Screen reader label cho icon-only controls; chart có summary/text alternative.
- Text scale 100–200% không làm mất chức năng quan trọng; kiểm tra high DPI.
- Reduced Motion tắt animation không cần thiết; không dùng màu làm tín hiệu duy nhất.
- Dialog destructive có nút secondary/cancel rõ, default focus không nằm trên nút xóa/gỡ.
- Table row actions có thể truy cập bằng keyboard; marker trên map có alternative list.

---

# 12. Tiêu chí nghiệm thu cho mỗi PNG và implementation

## Design review checklist

```text
[ ] File có đúng ID và đúng một màn hình duy nhất
[ ] Phong cách màu/typography tương thích design token
[ ] Nội dung chính và primary action nhận biết trong 3 giây
[ ] Không có card/button/section dính vào nhau
[ ] Labels đọc được ở tỷ lệ xem 100%
[ ] Không sử dụng text AI sai chính tả làm bản final
[ ] Không có secret hoặc dữ liệu người dùng thật
[ ] Active, disabled, hover, focus và destructive state có thiết kế khi cần
[ ] Có note về loading/empty/error/offline/stale/pending/conflict
[ ] Core và Later được phân biệt rõ
```

## Implementation acceptance checklist

```text
[ ] View có ViewModel và Application use case phù hợp
[ ] Real data contract đã xác định; không hard-code sample làm production data
[ ] Loading, empty, error và stale/offline xử lý đúng
[ ] Sync state được trình bày trung thực
[ ] Permission UI khớp policy API; server vẫn kiểm tra quyền
[ ] Keyboard, DPI, contrast, reduced motion đã được QA
[ ] List/grid lớn dùng virtualization/paging
[ ] Mọi tác vụ I/O có cancellation/timeout và error path
[ ] Không chạy công việc nặng trên UI thread
[ ] Event/timer/cache/subscription có lifetime/disposal rõ
[ ] Test ID liên kết UI automation/screenshot test
[ ] CPU/RAM được đo khi trang có workload nặng
[ ] Screenshot chụp từ build thật được so với mockup đã duyệt
```

## Launcher acceptance checklist

```text
[ ] Có system check và kiểm tra dung lượng thật
[ ] Download progress dựa trên bytes thật; pause/resume/retry/cancel theo hỗ trợ thực tế
[ ] Verify manifest/hash/signature trước cài/chạy
[ ] Không có bypass “ignore invalid signature”
[ ] Install/upgrade không phá SQLite/settings/outbox
[ ] Repair chỉ đụng file do ứng dụng quản lý
[ ] Có update recovery/rollback khi package model hỗ trợ
[ ] Uninstall theo cơ chế OS/package manager
[ ] Local data cleanup là lựa chọn riêng; pending sync được cảnh báo
[ ] Uninstall app không xóa cloud workspace
[ ] Lỗi báo state cuối chính xác và có diagnostic code
[ ] E2E test Windows và từng distro Linux được tuyên bố hỗ trợ
```

---

# 13. Trình tự thiết kế và phê duyệt

## Giai đoạn A — Khóa design system

1. Duyệt palette, typography, sizing, shell, buttons, tables, badges và forms.
2. Tạo một ảnh reference độc lập cho shell và một ảnh launcher reference.
3. Duyệt xong mới tạo 24 ảnh app; không đổi brand token lẻ tẻ giữa panel.

## Giai đoạn B — Ảnh app theo thứ tự

1. UI-01 Splash
2. UI-02 Sign In
3. UI-03 Create Account
4. UI-04 Welcome/Shell
5. UI-05 Fleet
6. UI-06 Dashboard
7. UI-07 Account Detail
8. UI-08 Inventory
9. UI-09 World Map
10. UI-10 Analytics
11. UI-11 History
12. UI-12 Alerts
13. UI-13 Metadata Editor
14. UI-14 Settings
15. UI-16 Backup/Restore
16. UI-17 Updates
17. UI-18 Diagnostics
18. UI-24 About/Support
19. UI-15 Admin Users
20. UI-19 Import/Pairing nếu đã duyệt scope
21. UI-21 Console/Logs
22. UI-20 License/Plan nếu có mô hình thương mại
23. UI-22 Team Messages nếu có collaboration
24. UI-23 Community Statistics nếu có privacy/consent policy

Thứ tự này ưu tiên luồng Core trước; ID không thay đổi dù thứ tự sản xuất ảnh thay đổi.

## Giai đoạn C — Launcher

Tạo L01–L08 trước để hoàn thành fresh install; sau đó L09–L12 cho update/repair; cuối cùng L13–L16 cho uninstall/failure/recovery. Cần có ít nhất một trạng thái lỗi thiết kế cho mỗi operation quan trọng.

## Giai đoạn D — Từ mockup sang code

Duyệt PNG độc lập → kiểm tra accessibility/responsive → xây component → wire API/local state → test interactive states → screenshot từ build thật → so sánh và sửa. Không xem mockup tĩnh là bằng chứng logic đã hoạt động.

---

# 14. Quyết định cuối cùng

- Ngôn ngữ hình ảnh OceanForge đã chốt: abyssal navy, teal/cyan telemetry, brass/gold accent và artwork hàng hải có tiết chế.
- Bộ app có **24 màn hình độc lập** theo ID UI-01…UI-24; không phải một ảnh ghép.
- Launcher có **16 màn hình/luồng độc lập** L01…L16, bao gồm download, verify, install, update, rollback, repair, uninstall và failure.
- Core pages ưu tiên khả năng đọc, freshness và trạng thái thật hơn hiệu ứng trang trí.
- Offline/pending/conflict phải được thể hiện trung thực; installer/update không được làm mất dữ liệu người dùng.
- Các tính năng conditional/later như billing, collaboration và community không tự động trở thành MVP chỉ vì có mặt trong concept.
- UI/UX được nghiệm thu bằng PNG riêng, behavior spec, tests và screenshot từ build thật.

**Design deliberately. Keep data readable. Make every state honest. Verify before install.**
