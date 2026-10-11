# OCEANFORGE DESKTOP — TECHNICAL ARCHITECTURE & DELIVERY MASTER SPEC
## Tài liệu kỹ thuật độc lập | C# / .NET 10 / Avalonia / Hybrid Offline Sync

**Nguồn chuẩn:** `OCEANFORGE_DESKTOP_MASTER_SPEC_V4_COMPLETE_HYBRID_PERFORMANCE.md`
**Phạm vi:** kiến trúc solution, dependency rules, C# frameworks, CPU/RAM, Domain/Application/Infrastructure, SQLite + MySQL, offline sync, API, telemetry, security, testing, GitHub CI/CD, packaging, release, rollback và maintenance.

Đặc tả UI chi tiết đã tách sang `OCEANFORGE_UI_UX_SPEC.md`. Các quyết định kỹ thuật vẫn phải được kiểm chứng bằng source code, integration tests, benchmark và release rehearsal; không coi tài liệu hoặc mockup là bằng chứng triển khai thành công.

---
# 0. ĐIỀU KHOẢN KIẾN TRÚC KHÔNG ĐƯỢC PHÁ VỠ

Đây là các quy tắc phải được coi như **architecture contract**.

```text
1. Desktop tuyệt đối không kết nối MySQL/Redis trực tiếp; mọi truy cập cloud đi qua API.
2. MySQL 8.4 là source of truth của dữ liệu nghiệp vụ trên server.
3. SQLite là local cache + outbox/cursor để hỗ trợ offline; không tự động biến thành source of truth thứ hai.
4. Redis chỉ là cache/short-lived coordination/realtime support; queue bền vững phải có durability policy rõ.
5. Domain không phụ thuộc Avalonia, ASP.NET, MySQL, SQLite, Redis hoặc OS.
6. UI không chứa SQL, DbContext query hoặc business rule.
7. Application điều phối use case; Domain giữ business rule.
8. Mọi schema change phía server và phía local đều có migration riêng.
9. Event được retry phải idempotent; không tuyên bố exactly-once delivery nếu hệ thống thực tế dùng retry.
10. Không xác nhận một thay đổi online thành công trước khi server xác nhận; offline-pending phải được hiển thị rõ.
11. Mọi release phải reproducible từ Git tag/commit; verify chữ ký/hash trước khi thực thi/cài.
12. Không commit secret; không lưu Roblox password/cookie/session secret.
13. Pull Request phải pass build/test/security checks trước khi merge.
14. Backup phải có restore drill; local SQLite backup phải xử lý WAL đúng cách.
15. Telemetry phải có retention, deduplication, rate limit và backpressure.
16. Không log password, token, cookie hoặc payload nhạy cảm.
17. Mọi thay đổi quyền phải có audit trail và yêu cầu online.
18. Không làm microservices hoặc thêm framework tràn lan trước khi cần.
19. Không làm tính năng advanced trước khi core data path, offline sync và dữ liệu thực ổn định.
20. Một feature chỉ DONE khi có test + logging + security + performance check + documentation.
```

---

# 1. MỤC TIÊU CỦA V3

V2 đã có product scope, UI language, telemetry, MySQL, realtime, analytics, backup và packaging. V3 tập trung trả lời rõ các câu hỏi mà dự án lớn thường thất bại ở đây:

```text
Code nằm ở đâu?
Project nào được phụ thuộc project nào?
Cái gì phải làm trước?
Cái gì chỉ được làm sau khi data path ổn?
GitHub làm CI/CD thế nào?
Release từ commit nào?
Ai được deploy production?
Secret để ở đâu?
Database migration ra sao?
Rollback ứng dụng và rollback database khác nhau thế nào?
Bản Windows/Linux phát hành theo format nào?
Khi app lỗi thì biết ở đâu?
Khi update lỗi thì quay lại thế nào?
Khi sửa schema thì dữ liệu cũ có còn chạy không?
Làm thế nào để 2 năm sau vẫn bảo trì được?
```

---

# 2. TECHNOLOGY BASELINE ĐÃ CHỐT

| Layer | Công nghệ | Quyết định |
|---|---|---|
| Desktop UI | Avalonia UI + XAML | `CHỐT` cho Windows + Linux; pin phiên bản đã QA |
| Runtime | .NET 10 LTS | `CHỐT`; quản lý patch theo lifecycle |
| Language | C# | Primary |
| Pattern | MVVM | Desktop presentation |
| MVVM toolkit | CommunityToolkit.Mvvm | Commands/observable state; không bắt buộc thêm ReactiveUI song song |
| Desktop hosting | Microsoft.Extensions.Hosting / DI / Options | Lifetimes, cancellation, background services |
| Local database | SQLite + provider .NET phù hợp | `CHỐT` cho cache/offline outbox/cursor; migration riêng |
| Server database | MySQL 8.4 LTS | `CHỐT` source of truth phía server |
| Server ORM | EF Core + provider MySQL đã compatibility-tested | Migrations, transactions, repository/query boundaries |
| Hot read queries | Dapper, chỉ khi benchmark chứng minh lợi ích | Không dùng hai data-access stacks mặc định cho mọi query |
| API | ASP.NET Core 10 | Central API |
| Realtime | SignalR / WebSocket | Gửi event/delta cần thiết; resync qua API |
| Cache | Redis | `OPTIONAL`; chỉ đưa vào khi workload/scale chứng minh cần |
| Durable jobs | MySQL transactional outbox + Worker lúc đầu | Redis Streams/broker chỉ sau ADR + durability/load test |
| Local event buffer | `System.Threading.Channels` bounded | Backpressure; không thay thế durable storage |
| Logging | `ILogger` + Serilog provider nếu cần sinks/routing | Structured logging |
| Validation | FluentValidation hoặc validation pipeline thống nhất | Tránh trùng hai hệ validation không lý do |
| API contract | OpenAPI | Stable contract |
| Tests | xUnit + API/DB/UI integration stack | Automated quality |
| CI/CD | GitHub Actions | Build/test/package/release |
| Windows package | Signed MSIX hoặc signed EXE installer | Chỉ chọn một primary stable path sau prototype QA |
| Linux package | `.deb` + AppImage nếu hai luồng phân phối được duy trì | Hỗ trợ đúng distro đã test |
| Container | Docker | API/Worker local + server deploy |
| Monitoring | OpenTelemetry-compatible metrics/logs/traces | API/Worker đầy đủ; desktop thu thập có chọn lọc |

`.NET 10` hiện là LTS và đang được hỗ trợ đến ngày 2028-11-14 theo chính sách hỗ trợ của Microsoft. [Microsoft .NET lifecycle policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)

Avalonia hiện hỗ trợ desktop Windows và Linux; ma trận hỗ trợ thay đổi theo major release, vì vậy release manifest của OceanForge phải ghi rõ OS/architecture được QA cho từng phiên bản. [Avalonia supported platforms](https://docs.avaloniaui.net/docs/supported-platforms)

MySQL 8.4 thuộc track LTS của MySQL, phù hợp làm đường cơ sở ổn định cho dữ liệu dài hạn. [MySQL 8.4 Reference Manual](https://dev.mysql.com/doc/refman/8.4/en/)

---

# 3. KIẾN TRÚC Ở MỨC HỆ THỐNG — V4

```text
                 ┌──────────────────────────────────┐
                 │ OceanForge Desktop (Avalonia)    │
                 │ UI + Application use cases       │
                 └───────────────┬──────────────────┘
                                 │
               ┌─────────────────┴───────────────────┐
               │                                     │
      ┌────────▼─────────┐                 ┌─────────▼────────┐
      │ Local SQLite     │                 │ Sync Engine      │
      │ Cache / Outbox   │◄── cursor ─────►│ bounded/retry    │
      │ Conflict / Draft │                 └─────────┬────────┘
      └──────────────────┘                           │ HTTPS / WSS
                                                     ▼
                                      ┌─────────────────────────────┐
                                      │ OceanForge API              │
                                      │ ASP.NET Core 10             │
                                      │ Auth / RBAC / REST / SignalR│
                                      └──────────────┬──────────────┘
                                                     │
                                           ┌─────────▼────────┐
                                           │ MySQL 8.4        │
                                           │ Server authority │
                                           │ + server outbox  │
                                           └─────────┬────────┘
                                                     │
                                           ┌─────────▼────────┐
                                           │ OceanForge Worker│
                                           │ batch / alerts / │
                                           │ analytics / jobs │
                                           └─────────┬────────┘
                                                     │
                                ┌────────────────────▼───────────┐
                                │ Redis optional                 │
                                │ cache / rate-limit / fan-out   │
                                │ only after need is measured    │
                                └────────────────────────────────┘

Authorized telemetry producer → HTTPS ingestion API → validated/idempotent storage
```

### Deployment laws

```text
Desktop → local SQLite and HTTPS API only
API → MySQL; optionally Redis by explicit architecture decision
Worker → MySQL outbox / repositories; optionally Redis/broker after durability review
Producer → authorized HTTPS ingestion endpoint

NEVER:
Desktop → MySQL directly
Desktop → Redis directly
Desktop → internal worker port
Local SQLite shared directly across multiple computers on a network share
```

`OceanForge.Worker` can be a separate deployable host but remains in the same modular-monolith codebase until scaling/team ownership proves it should be split. Redis is not a mandatory dependency for the first vertical slice.

---

# 4. KIẾN TRÚC LAYER / CLEAN ARCHITECTURE

```text
┌───────────────────────────────────────────────────────────┐
│ Presentation                                             │
│ Avalonia Views / ViewModels / Controls / Navigation     │
└──────────────────────────┬────────────────────────────────┘
                           │ depends on
                           ▼
┌───────────────────────────────────────────────────────────┐
│ Application                                              │
│ Use Cases / DTOs / Ports / Authorization / Policies     │
└──────────────────────────┬────────────────────────────────┘
                           │ depends on
                           ▼
┌───────────────────────────────────────────────────────────┐
│ Domain                                                   │
│ Entities / Value Objects / Domain Services / Rules       │
└───────────────────────────────────────────────────────────┘
                           ▲
                           │ implements ports
┌──────────────────────────┴────────────────────────────────┐
│ Infrastructure                                          │
│ MySQL / Redis / HTTP / Files / OS / Notifications       │
└───────────────────────────────────────────────────────────┘
```

## 4.1 Presentation

Chứa:

```text
Views
ViewModels
Controls
Themes
Styles
Converters
Navigation
Dialogs
Notifications
Chart adapters
Map adapters
```

Không chứa:

```text
SQL
DbContext
MySqlConnection
EF queries
Business rules
Secret handling
Database transaction logic
```

## 4.2 Application

Chứa use cases:

```text
RegisterUser
LoginUser
RefreshSession
LogoutUser

GetDashboardSummary
GetAccounts
GetAccountDetail
ArchiveAccount

ReceiveHeartbeat
ReceiveTelemetryBatch
CreateSnapshot
GetSnapshotDiff

GetAnalytics
GenerateReport

EvaluateAlertRules
AcknowledgeAlert

CreateBackup
RestoreBackup
RunDiagnostics
CheckForUpdate
```

## 4.3 Domain

Chứa:

```text
Account
CharacterState
LoadoutState
InventoryItem
MaterialBalance
AccountSession
TelemetryEvent
TelemetrySnapshot
AlertRule
AlertEvent
Report
```

Domain rule ví dụ:

```text
Level >= 0
Beli >= 0
Fragments >= 0

HeartbeatAge < onlineThreshold
    => ONLINE

HeartbeatAge < offlineThreshold
    => STALE

HeartbeatAge >= offlineThreshold
    => OFFLINE
```

## 4.4 Infrastructure

Chứa implementation:

```text
MySqlAccountRepository
MySqlTelemetryRepository
RedisCache
RedisTelemetryQueue
SignalRPublisher
FileBackupStore
EmailProvider
DesktopNotificationProvider
UpdateManifestProvider
```

---

# 5. DEPENDENCY MATRIX — PROJECT NÀO ĐƯỢC GỌI PROJECT NÀO?

| Project | Có thể reference | Không được reference |
|---|---|---|
| Domain | Shared primitives nếu thật sự cần | UI, API, EF, MySQL, Redis |
| Application | Domain, Contracts | Avalonia, EF Core concrete implementation |
| Contracts | Shared serialization types | UI, DB |
| Persistence | Application abstractions, Domain | Presentation |
| Infrastructure | Application, Domain, Contracts | Presentation |
| Security | Application, Domain | Views |
| Telemetry | Application, Domain, Contracts | Avalonia |
| Analytics | Application, Domain, Contracts | Views |
| Realtime | Application, Contracts | Views |
| Presentation | Application, Contracts | MySQL concrete implementation |
| Desktop | Presentation + DI composition root | Domain internals trực tiếp nếu không cần |
| API | Application, Contracts, Infrastructure composition | Desktop |
| Worker | Application, Infrastructure, Contracts | Avalonia |
| Tests | Project đang test + test utilities | Production secrets |

### Không được có vòng lặp

```text
Application → Presentation  X
Domain → Infrastructure     X
Infrastructure → Presentation X
Persistence → Desktop       X
API → Desktop               X
```

---

# 6. SOLUTION STRUCTURE — BẢN CHỐT

```text
OceanForge/
│
├── OceanForge.slnx
├── global.json
├── Directory.Build.props
├── Directory.Build.targets
├── Directory.Packages.props
├── .editorconfig
├── .gitignore
├── README.md
├── LICENSE
├── SECURITY.md
├── CONTRIBUTING.md
├── CODEOWNERS
│
├── src/
│   ├── OceanForge.Domain/
│   ├── OceanForge.Application/
│   ├── OceanForge.Contracts/
│   ├── OceanForge.Persistence/        # Server MySQL persistence
│   ├── OceanForge.Local.Persistence/  # SQLite local cache/outbox; migrations riêng
│   ├── OceanForge.Infrastructure/
│   ├── OceanForge.Security/
│   ├── OceanForge.Telemetry/
│   ├── OceanForge.Analytics/
│   ├── OceanForge.Realtime/
│   ├── OceanForge.Reporting/
│   ├── OceanForge.Diagnostics/
│   ├── OceanForge.Notifications/
│   ├── OceanForge.Presentation/
│   ├── OceanForge.Launcher/           # Update/download/repair UI; không thay OS package manager
│   ├── OceanForge.Desktop/
│   ├── OceanForge.Api/
│   └── OceanForge.Worker/
│
├── tests/
│   ├── OceanForge.Domain.Tests/
│   ├── OceanForge.Application.Tests/
│   ├── OceanForge.Persistence.Tests/
│   ├── OceanForge.Api.Tests/
│   ├── OceanForge.Telemetry.Tests/
│   ├── OceanForge.Integration.Tests/
│   └── OceanForge.UI.Tests/
│
├── database/
│   ├── migrations/
│   ├── seeds/
│   ├── scripts/
│   └── README.md
│
├── deploy/
│   ├── docker/
│   ├── windows/
│   ├── linux/
│   └── server/
│
├── scripts/
│   ├── build/
│   ├── test/
│   ├── package/
│   ├── release/
│   └── db/
│
├── docs/
│   ├── architecture/
│   ├── adr/
│   ├── api/
│   ├── ui/
│   ├── security/
│   ├── operations/
│   └── release/
│
└── .github/
    ├── workflows/
    ├── ISSUE_TEMPLATE/
    ├── pull_request_template.md
    ├── dependabot.yml
    └── CODEOWNERS
```

---

# 7. TRONG MỖI PROJECT PHẢI CÓ CẤU TRÚC NHẤT QUÁN

## Domain

```text
Domain/
├── Accounts/
│   ├── Account.cs
│   ├── AccountStatus.cs
│   └── AccountRules.cs
├── Sessions/
├── Telemetry/
├── Inventory/
├── Materials/
├── Alerts/
├── Reports/
└── Common/
```

## Application

```text
Application/
├── Accounts/
│   ├── Queries/
│   ├── Commands/
│   └── DTOs/
├── Dashboard/
├── Telemetry/
├── Analytics/
├── Alerts/
├── Reports/
├── Backup/
├── Diagnostics/
└── Common/
    ├── Behaviors/
    ├── Interfaces/
    ├── Validation/
    └── Authorization/
```

## Persistence

```text
Persistence/
├── DbContext/
├── Configurations/
├── Repositories/
├── Migrations/
├── QueryObjects/
└── Interceptors/
```

## Presentation

```text
Presentation/
├── Views/
├── ViewModels/
├── Controls/
├── Dialogs/
├── Navigation/
├── Themes/
├── Resources/
├── Converters/
└── Behaviors/
```

## API

```text
Api/
├── Endpoints/
├── Middleware/
├── Filters/
├── OpenApi/
├── Auth/
└── Composition/
```

## Worker

```text
Worker/
├── Jobs/
│   ├── TelemetryIngestion/
│   ├── AlertEvaluation/
│   ├── Retention/
│   ├── Reports/
│   ├── Analytics/
│   └── BackupVerification/
└── Hosting/
```

---

# 8. QUY TẮC ĐỂ DỄ BẢO TRÌ

## 8.1 Không để ViewModel thành “God Object”

Sai:

```text
AccountViewModel.cs
├── SQL
├── HTTP
├── business rule
├── telemetry
├── chart calculations
├── export
└── dialog handling
```

Đúng:

```text
AccountViewModel
    ↓
IAccountApplicationService
    ↓
Use Case
    ↓
Repository/Domain
```

## 8.2 Không để EF Entity trở thành Domain Entity

Persistence model và Domain model phải tách khi business complexity cần separation.

```text
DB Entity
    ↓ mapping
Domain Entity
    ↓ mapping
DTO
```

## 8.3 Không dùng string tự do cho mọi trạng thái

Sai:

```csharp
account.Status = "online";
```

Ưu tiên enum/value object/domain abstraction.

## 8.4 Mọi external dependency đi qua interface/adapter

```text
IClock
IAccountRepository
ICache
IQueue
INotificationSender
IUpdateProvider
IFileStore
```

## 8.5 Mọi quyết định kiến trúc lớn phải có ADR

```text
ADR-001 Cross-platform UI
ADR-002 MySQL 8.4
ADR-003 Redis queue
ADR-004 Auth model
ADR-005 Packaging strategy
ADR-006 Release channels
```

---

# 9. DATA OWNERSHIP

Mỗi dữ liệu phải có một owner rõ ràng.

| Data | Owner |
|---|---|
| User identity | Auth/Application |
| Account registry | Accounts domain |
| Current account status | Account/Telemetry pipeline |
| Historical telemetry | Telemetry subsystem |
| Snapshot | Snapshot subsystem |
| Analytics aggregate | Analytics worker |
| Alert definition | Alert subsystem |
| Audit log | Audit subsystem |
| App settings | Settings subsystem |

Không cho nhiều module cùng tự ý “sở hữu” một bảng theo cách không có contract.

---

# 10. MYSQL ARCHITECTURE

## 10.1 MySQL là source of truth

```text
Current state
Historical state
Users
Roles
Permissions
Audit
Reports metadata
Settings metadata
```

## 10.2 SQLite local store

SQLite dùng riêng trên desktop cho:

```text
Bounded read cache
SyncOutbox / SyncCursor / SyncConflict
Local preferences / drafts / saved views nếu được phép
Local schema version + migration
```

SQLite không thay MySQL làm nguồn sự thật của dữ liệu cloud. Local provider và server provider phải có migration riêng, kiểm thử riêng; không giả định schema/feature SQL hoàn toàn giống nhau.

## 10.3 Redis không phải source of truth và không bắt buộc cho MVP

Khi đã có bằng chứng cần cache/rate-limit/fan-out, Redis có thể dùng cho:

```text
Read cache có TTL/invalidation
Rate limiting counters
Short-lived coordination/locks nếu cần
Realtime fan-out / presence
Transient aggregates
```

Queue bền vững ban đầu ưu tiên MySQL transactional outbox + Worker. Redis Streams/message broker chỉ được dùng làm durable queue sau khi có ADR và đã kiểm tra persistence, consumer recovery, retry, dead-letter, backup/restore và monitoring. Không để Redis là bản duy nhất của mutation cần bảo đảm không mất.

## 10.4 Database rules

```text
UTC timestamps
utf8mb4
Foreign keys
Indexes
Parameterized queries
Transactions
Migrations
Retention jobs
Backup
Restore verification
```

## 10.5 Migration rules

Mỗi thay đổi schema:

```text
Create migration
        ↓
Review
        ↓
Integration test
        ↓
Backup/restore test nếu breaking
        ↓
Deploy staging
        ↓
Deploy production
```

### Không được

```text
Production database
↓
Manual ALTER TABLE bằng tay
↓
Không có migration
```

### Migration phải forward-compatible khi có thể

Ví dụ đổi field:

```text
Release N:
old_column + new_column
        ↓
Backfill
        ↓
App đọc new_column
        ↓
Release N+1
        ↓
Remove old_column
```

Không deploy code mới yêu cầu schema mà database chưa có.

---

# 11. DATABASE SCALE STRATEGY

Ngay từ đầu thiết kế cho tăng trưởng.

```text
Account table
    ↓ indexed
Telemetry events
    ↓ indexed by account/time
Snapshots
    ↓ hashed / deduplicated
Analytics
    ↓ pre-aggregated
```

Khi dữ liệu lớn:

```text
Partitioning / archive
Batch inserts
Cursor pagination
Read models
Aggregates
Retention
Cold storage
```

### Không làm

```text
SELECT * telemetry_events
```

cho toàn bộ fleet.

---

# 12. API ARCHITECTURE

API sử dụng `/api/v1` và response contract ổn định.

```text
/api/v1/auth
/api/v1/accounts
/api/v1/dashboard
/api/v1/telemetry
/api/v1/analytics
/api/v1/alerts
/api/v1/reports
/api/v1/admin
/api/v1/diagnostics
```

## 12.1 API layers

```text
HTTP Endpoint
    ↓
Request validation
    ↓
Authorization
    ↓
Application use case
    ↓
Domain
    ↓
Infrastructure
    ↓
Response DTO
```

## 12.2 API không trả Domain Entity trực tiếp

```text
Domain entity
    ↓ map
Response DTO
```

## 12.3 API error model

Chuẩn hóa:

```text
code
message
traceId
fieldErrors[]
```

Ví dụ:

```json
{
  "code": "ACCOUNT_NOT_FOUND",
  "message": "The tracked account was not found.",
  "traceId": "...",
  "fieldErrors": []
}
```

---

# 13. AUTHENTICATION + AUTHORIZATION

## 13.1 OceanForge authentication

```text
Register
  ↓
Email verification
  ↓
Login
  ↓
Short-lived access token
  ↓
Rotating refresh token
  ↓
Session/device record
```

## 13.2 Không dùng Roblox credential

OceanForge không lưu:

```text
Roblox password
Browser cookies
Session cookies
2FA secrets
Recovery codes
Private keys
```

## 13.3 RBAC

```text
Owner
Admin
Operator
Viewer
Auditor
```

## 13.4 Authorization phải ở server

UI hide button chỉ là UX.

Security thật nằm ở:

```text
API authorization policy
Domain permission check
Audit log
```

## 13.5 Session security

```text
Refresh token hash only
Rotation
Revocation
Expiry
Device/session list
Concurrent-session policy
Suspicious login logging
```

---

# 14. SECURITY THREAT MODEL

## Tài sản cần bảo vệ

```text
OceanForge user identity
Access tokens
Refresh token material
MySQL data
Telemetry data
Reports
Backups
Release signing credentials
GitHub credentials
Deployment credentials
```

## Threats

```text
SQL injection
Credential theft
Token theft
Replay
Privilege escalation
Malicious telemetry
Malicious import
Path traversal
Dependency compromise
Tampered installer
Tampered update
CI secret leakage
Supply-chain attack
```

## Controls

```text
TLS
RBAC
Input validation
Parameterized SQL
Rate limiting
Replay protection
Audit
Secret manager
Signed artifacts
Artifact provenance
Dependency scanning
Least privilege
Backup encryption where appropriate
```

---

# 15. SECRET MANAGEMENT

## Tuyệt đối không

```text
appsettings.json
.env committed
source code
GitHub issue
logs
screenshots
README
```

chứa production secret.

## Development

Dùng:

```text
.NET User Secrets
local environment variables
Docker local secrets
```

## CI

Ưu tiên:

```text
GitHub Environments
GitHub Secrets
OIDC to cloud provider
```

OIDC cho phép workflow nhận credential ngắn hạn thay vì phải lưu credential cloud dài hạn; GitHub yêu cầu `id-token: write` cho job cần mint OIDC token. [GitHub OIDC security hardening](https://docs.github.com/actions/security-for-github-actions/security-hardening-your-deployments/about-security-hardening-with-openid-connect)

## Production

```text
External secret manager
or
Cloud secret store
```

---

# 16. TELEMETRY ARCHITECTURE — V4 DURABILITY BOUNDARY

```text
Authorized telemetry producer
        ↓ HTTPS
Authentication + authorization
        ↓
Payload-size/schema/rate validation
        ↓
Normalize + validate source sequence/time
        ↓
Idempotency key / deduplication check
        ↓
MySQL transaction:
  persist accepted event/state + ServerOutbox record
        ↓ commit
Worker processes outbox in bounded batches
        ↓
SignalR sends notification/delta (may be repeated)
        ↓
Desktop applies delta idempotently; REST/cursor resync is the recovery path
```

Do not acknowledge durable ingestion merely because the event was put into an in-memory `Channel<T>`. If the API accepts events asynchronously before database persistence, it must use a separately configured durable broker with acknowledgement/recovery semantics. The default V4 path is persist-then-ack; optimize later after measurement.

## Do not send each frame

Send only when appropriate:

```text
heartbeat
state change
periodic snapshot
batched events
```

Actual intervals depend on source authorization, acceptable freshness, rate limit and observed workload. Do not use the desktop polling loop to pretend to infer the game's status without a valid telemetry source.

## Ingestion envelope

```text
eventId
sourceId
sourceSequence
schemaVersion
occurredAtUtc
serverReceivedAtUtc
payloadHash / idempotency key
```

Worker must handle duplicate/replayed events without duplicating business effects. Use unique constraints and integration tests to handle simultaneous duplicate requests, not just an in-memory check.

---

# 17. REALTIME ARCHITECTURE

Realtime chỉ truyền delta hoặc event cần thiết.

```text
Account 123
STATUS_CHANGED
```

không phải gửi toàn bộ account database mỗi lần.

## Desktop state flow

```text
Initial REST load
       ↓
Create SignalR connection
       ↓
Receive event
       ↓
Validate event
       ↓
Update local state
       ↓
Refresh specific query if needed
```

## Reconnect

```text
Connected
  ↓
Disconnected
  ↓
Backoff
  ↓
Reconnect
  ↓
Resync since last event/version
```

Không tạo request storm khi reconnect.

---

# 18. CACHING — MỘT CACHE CÓ OWNER VÀ QUOTA

```text
L0 Current visible UI state (small, disposable)
L1 Desktop memory cache (size/TTL bounded)
L2 Desktop SQLite local cache (persistent, explicitly stale-aware)
L3 MySQL server source of truth (via API only)
Optional Redis server cache (TTL + invalidation; not authority)
```

Every cache entry must define:

```text
key
TTL / size or byte quota
schema/version
owner
freshness timestamp
invalidation strategy
sensitivity class
```

- Desktop cache phải có giới hạn theo count/bytes; eviction không được xóa `SyncOutbox` pending.
- UI không được trình bày cache stale như LIVE; state phải bao gồm `ObservedAt`, `LastSyncedAt`, source and freshness.
- MySQL queries remain behind the API. Redis is introduced only when measured latency/load warrants the additional operational complexity.
- Do not keep sensitive data in memory/local cache indefinitely; clear per-user cache by logout policy.

---

# 19. WORKER ARCHITECTURE — DURABLE OUTBOX FIRST

`OceanForge.Worker` runs async jobs in bounded batches. Baseline durable input is a MySQL `ServerOutbox`/job table with retry metadata; in-memory channels only provide backpressure within a running process.

```text
TelemetryOutboxDispatcher
AlertEvaluationJob
RetentionJob
SnapshotJob
AnalyticsAggregationJob
ReportJob
BackupVerificationJob
CleanupJob
```

## Worker rules

```text
Idempotent handler
Transactional acknowledgement where applicable
Retry with exponential backoff + jitter
Dead-letter/permanent-failure state
Cancellation-aware shutdown
Bounded batch/concurrency
Structured logging + trace/correlation ID
Queue depth + oldest-item-age metrics
Health checks
```

Do not add Redis Streams as mandatory queue until persistence, consumer recovery, pending entries, replay, dead-letter and operations are designed/tested. Do not scale by creating an unbounded task per event.

---

# 20. ERROR HANDLING

Phân loại:

```text
ValidationError
AuthenticationError
AuthorizationError
NotFoundError
ConflictError
RateLimitError
NetworkError
DatabaseError
ExternalServiceError
UnexpectedError
```

UI không được show stack trace cho user.

User thấy:

```text
What happened
What can be done
Trace ID
```

Log server giữ chi tiết kỹ thuật.

---

# 21. LOGGING / OBSERVABILITY

Mỗi request phải có:

```text
traceId
correlationId
userId (nếu phù hợp)
route
statusCode
duration
```

Mỗi worker job:

```text
jobId
attempt
duration
result
error code
```

## Không log

```text
password
access token
refresh token
cookies
private key
secret
full sensitive payload
```

## Metrics bắt buộc

```text
api_request_count
api_error_count
api_latency_p50/p95/p99
telemetry_ingestion_rate
telemetry_duplicate_rate
queue_depth
worker_failures
active_sessions
mysql_query_latency
redis_latency
desktop_crash_count
update_failures
```

---

# 22. DIAGNOSTICS

Health checks:

```text
Desktop runtime
OS
Disk
Memory
CPU
Network
DNS
TLS
API
MySQL
Redis
SignalR
Telemetry
Updater
```

Trạng thái:

```text
PASS
WARN
FAIL
UNKNOWN
```

Diagnostic report phải có:

```text
App version
OS version
Architecture
Environment
Feature flags
Connectivity state
Recent error IDs
```

Không dump secret.

---

# 23. BACKUP / RESTORE / DISASTER RECOVERY

## Backup layers

```text
MySQL logical backup
MySQL physical/managed backup nếu hạ tầng hỗ trợ
Settings backup
Report metadata
Release metadata
```

## Backup metadata

```text
schema version
app version
timestamp
record counts
checksum
backup source
```

## Restore drill

Không chỉ kiểm tra “file tồn tại”.

```text
Create backup
   ↓
Restore into isolated DB
   ↓
Run migrations/validation
   ↓
Run smoke queries
   ↓
Run application integration tests
   ↓
Record result
```

## RPO/RTO mục tiêu phải được chốt

Ví dụ ban đầu:

```text
RPO ≤ 24h
RTO ≤ 4h
```

Sau khi vận hành thật mới giảm xuống nếu cần.

---

# 24. OFFLINE / CACHE / SYNC — V4 CONTRACT

## 24.1 Quyết định

**OceanForge dùng mô hình Hybrid/Offline-capable:**

- Server MySQL là nguồn dữ liệu chuẩn cho workspace/tài khoản/role/audit/historical telemetry đã đồng bộ.
- Desktop dùng SQLite cho bản cache được phép lưu, cursor đồng bộ, draft cục bộ và outbox bền vững.
- Khi offline, app tiếp tục mở được và hiển thị **last-known data** với thời điểm cập nhật; thao tác được cho phép offline được đánh dấu `Pending sync`.
- Thao tác quyền, quản trị, đăng nhập, thu hồi session, thay đổi role, xóa dữ liệu cloud hoặc hành động bảo mật phải online và được API xác nhận.

```text
UI / ViewModel
      ↓
Application use case
      ├── LocalStore (SQLite): cache + drafts + outbox + cursor
      └── Sync Engine ── HTTPS ── OceanForge API ── MySQL source of truth
                                  └── transactional outbox → Worker/realtime
```

## 24.2 Phân loại dữ liệu và quyền offline

| Data | Có thể đọc offline? | Có thể sửa offline? | Chính sách |
|---|---|---|---|
| Dashboard aggregates | Có, nếu đã cache | Không trực tiếp | Hiện thời điểm dữ liệu; không ghi nhãn LIVE khi stale |
| Account list/details | Có, tập dữ liệu gần nhất đã cache | Metadata/tag nếu use case cho phép | Version/ETag và conflict inbox |
| Telemetry events mới từ producer được cấp quyền | Có thể lưu tạm | Append-only | Event ID ổn định, SQLite outbox, upload idempotent |
| Inventory/material snapshots | Có, nếu cache đủ | Không sửa dữ liệu quan sát theo ý người dùng | Là dữ liệu quan sát; chỉ producer có quyền mới cập nhật |
| Saved views/filters/layout | Có | Có | Có thể local-only; cloud sync là opt-in và có version |
| User preferences/theme | Có | Có | Ưu tiên local-first; tránh conflict bằng device-scoped setting mặc định |
| Reports/export đang soạn | Có thể giữ draft | Có thể | Báo rõ report hoàn chỉnh hay đang chờ server |
| Role, permission, account ownership | Có thể xem snapshot cũ | **Không** | Yêu cầu online + authorization phía server |
| Revoke session / delete cloud data | Có thể xem trạng thái cũ | **Không** | Chỉ thao tác online, audit, confirm |
| Billing/subscription/license entitlement | Chỉ cache theo policy ngắn hạn | **Không** | Entitlement server-authoritative; không tin local clock/cache vô thời hạn |

## 24.3 Local SQLite schema tối thiểu

```text
LocalSchemaInfo
CachedWorkspace
CachedAccount
CachedAccountState
CachedInventorySnapshot
CachedAlert
LocalPreference
LocalSavedView
SyncOutbox
SyncCursor
SyncConflict
LocalReportDraft
CacheMetadata
```

Mỗi hàng outbox cần ít nhất:

```text
MutationId          // GUID/idempotency key ổn định qua retry
EntityType
EntityId
OperationType
PayloadVersion
PayloadJson
CreatedAtUtc
AttemptCount
NextAttemptAtUtc
State               // Pending, InFlight, Acknowledged, Conflict, DeadLetter
LastErrorCode
```

Không lưu password Roblox, browser cookie, Roblox session, 2FA/recovery code hoặc bí mật không cần thiết trong SQLite. Token OceanForge phải dùng cơ chế bảo vệ theo OS; không lưu refresh token dạng rõ trong local database/log. SQLite mặc định không tự mã hóa toàn bộ database; nếu phân loại dữ liệu yêu cầu encryption-at-rest thì phải chọn và QA giải pháp mã hóa riêng, đánh giá key storage/backup/package footprint.

## 24.4 Luồng đồng bộ chuẩn

```text
1. App boot: mở SQLite → kiểm tra LocalSchemaInfo → chạy local migration nếu cần.
2. UI đọc cache ngay, gắn freshness metadata.
3. Sync Engine kiểm tra kết nối/auth; một sync loop duy nhất cho mỗi workspace.
4. Pull delta theo SyncCursor; áp dụng delta + cursor trong cùng local transaction.
5. Chọn batch pending theo thứ tự an toàn; gửi qua HTTPS với MutationId/idempotency key.
6. API authenticate → authorize → validate → rate limit → xử lý transaction trên MySQL.
7. Server trả accepted/rejected/conflict theo từng mutation, không chỉ trả HTTP 200 chung chung.
8. Desktop ghi acknowledgement/conflict vào SQLite transaction; chỉ xóa payload outbox sau khi có acknowledgement bền vững.
9. Retry lỗi transient với exponential backoff + jitter; lỗi permanent đi vào Conflict Center/Dead Letter và không lặp vô hạn.
10. Kết nối lại sau thời gian dài: nếu cursor hết hạn, chạy full resync có kiểm soát.
```

**Không được:** xóa outbox trước acknowledgement; đánh dấu một thay đổi là “Synced” chỉ vì request đã gửi; gửi lại không có idempotency key; khởi chạy nhiều vòng đồng bộ cạnh tranh; phát hàng nghìn request ngay khi mạng trở lại.

## 24.5 Chính sách xung đột

| Loại | Quy tắc |
|---|---|
| Telemetry event append-only | Dedupe theo `EventId` hoặc `(SourceId, SourceSequence)`; retry không tạo record thứ hai |
| Current account state | Server chỉ chấp nhận event hợp lệ theo sequence/source timestamp policy; không tin đồng hồ desktop làm nguồn duy nhất |
| Tags/saved view chỉnh sửa bởi nhiều thiết bị | Optimistic concurrency bằng `Version`/ETag; trả conflict để merge hoặc yêu cầu người dùng chọn |
| Preferences riêng thiết bị | Device-scoped mặc định; tránh đồng bộ không cần thiết |
| Delete/archive | Dùng tombstone/version để sync; physical purge theo retention, không xóa vật lý ngay từ client |
| Roles/security/session | Không queue offline; phải online và được server authorize |
| Dữ liệu producer và người dùng sửa thủ công | Quyền ghi và ownership tách rời; người dùng không được override telemetry như dữ liệu sự thật |

Không dùng Last-Write-Wins làm quy tắc chung cho mọi bảng. Chỉ dùng khi ngữ nghĩa dữ liệu cho phép và timestamp/version được xác định đáng tin cậy.

## 24.6 SQLite vận hành an toàn

- Database ở thư mục dữ liệu app dành riêng cho user hiện tại; không đặt dưới thư mục cài đặt chỉ đọc.
- Không đặt SQLite database trên network share để nhiều máy cùng mở trực tiếp.
- Dùng transaction ngắn; có một đường ghi có kiểm soát; bật WAL chỉ sau khi kiểm tra workload, backup và checkpoint.
- Migration local độc lập với MySQL migration; không giả định cùng một schema/migration chạy như nhau trên hai provider.
- Backup khi DB đang mở phải dùng API/chiến lược backup SQLite an toàn hoặc checkpoint/close có kiểm soát; không chỉ copy mỗi file `.db` trong lúc WAL đang chứa dữ liệu chưa checkpoint.
- Có quota/retention cho cache và local telemetry queue; nếu hết disk, dừng nhận thêm với thông báo có thể hành động, không âm thầm drop event bền vững.
- Khi logout, policy xóa hoặc giữ cache phải minh bạch; tài khoản dùng chung máy cần tách cache theo user/workspace.

SQLite phù hợp dữ liệu local, nhưng có một writer tại một thời điểm và WAL không dùng được trên network filesystem; nhiều writer cạnh tranh/đa máy trực tiếp là lý do để dùng server database qua API. Nguồn chính thức: [SQLite Appropriate Uses](https://www.sqlite.org/whentouse.html), [SQLite WAL](https://www.sqlite.org/wal.html).

---

# 25. UI ARCHITECTURE

## Shell

```text
MainWindow
 ├── TitleBar
 ├── NavigationRail
 ├── ContentHost
 ├── GlobalCommandBar
 ├── NotificationHost
 └── StatusBar
```

## Feature page

```text
View
 ↓
ViewModel
 ↓
Application facade/use case
 ↓
Contracts
```

## Không để page tự gọi HTTP

Sai:

```text
DashboardViewModel → HttpClient → /dashboard
```

Ưu tiên:

```text
DashboardViewModel
       ↓
DashboardApplicationService
       ↓
IApiClient / backend use case contract
```

---

# 26. PRODUCT MODULE ORDER

Thứ tự module business:

```text
1. Authentication
2. Desktop Shell
3. Dashboard
4. Accounts / Fleet
5. Account Detail
6. Inventory / Materials
7. Sessions
8. Telemetry
9. Realtime
10. Analytics
11. Map
12. Alerts
13. Reports
14. Backup / Restore
15. Diagnostics
16. Advanced features
```

Map đẹp nhưng không được làm trước data path.

Analytics không được làm trước khi event/snapshot model ổn.

Reports không nên làm trước khi query/read model ổn định.

---

# 27. THỨ TỰ XÂY DỰNG CHÍNH XÁC

## PHASE 00 — Vision / Scope

Output:

```text
VISION.md
PRODUCT.md
NON_GOALS.md
```

Gate:

```text
[ ] Phạm vi rõ
[ ] Không scope creep
```

## PHASE 01 — Repository / GitHub Foundation

Làm:

```text
Repository
README
LICENSE
SECURITY.md
CONTRIBUTING.md
CODEOWNERS
branch protection
issue labels
PR template
```

Gate:

```text
[ ] Không secret
[ ] Main protected
[ ] Required checks bật
```

## PHASE 02 — Toolchain

```text
global.json
.editorconfig
Directory.Build.props
Directory.Packages.props
analyzers
nullable
warnings
```

Gate:

```text
[ ] dotnet restore
[ ] dotnet build
[ ] dotnet test
```

## PHASE 03 — Domain

Đầu tiên viết model và unit test.

Gate:

```text
[ ] Domain không phụ thuộc UI/DB
[ ] Unit tests xanh
```

## PHASE 04 — Server Persistence + MySQL

```text
Server DbContext
MySQL configurations/repositories
Server migrations/seeds/indexes
Transactional ServerOutbox + idempotency constraint
```

Gate:

```text
[ ] Server migration clean install + upgrade test
[ ] Transaction/idempotency integration tests
[ ] Backup/restore smoke test
```

## PHASE 04A — Local SQLite Store

```text
Separate local schema and migrations
Cached reads + LocalPreference
SyncOutbox + SyncCursor + SyncConflict
Quota/retention + logout/cache policy
```

Gate:

```text
[ ] Local clean migration + upgrade test
[ ] Crash/restart leaves outbox/cursor consistent
[ ] Disk-full / WAL / safe backup / per-user cache tests
[ ] No secret in local database
```

## PHASE 05 — Application

Use cases + validation + authorization ports.

Gate:

```text
[ ] Use case tests xanh
```

## PHASE 06 — Authentication / RBAC

Gate:

```text
[ ] Register
[ ] Login
[ ] Refresh
[ ] Logout
[ ] Revoke
[ ] Role policy
[ ] Audit
```

## PHASE 07 — API

Gate:

```text
[ ] OpenAPI
[ ] Error model
[ ] Authorization
[ ] Rate limit
[ ] Integration tests
```

## PHASE 08 — Desktop Shell

Gate:

```text
[ ] Windows
[ ] Linux
[ ] Navigation
[ ] Theme
[ ] Error host
[ ] Notification host
```

## PHASE 09 — Design System

Gate:

```text
[ ] Tokens
[ ] Button
[ ] Table
[ ] Drawer
[ ] Dialog
[ ] Chart
[ ] States
```

## PHASE 10 — Dashboard

Gate:

```text
[ ] Uses real API contract
[ ] Loading/empty/error/offline
[ ] No UI blocking
```

## PHASE 11 — Fleet + Account Detail

Gate:

```text
[ ] Search
[ ] Filter
[ ] Virtualization
[ ] Drawer
[ ] History
```

## PHASE 12 — Telemetry Ingestion

Gate:

```text
[ ] Schema validation
[ ] Auth
[ ] Rate limit
[ ] Idempotency
[ ] Queue
[ ] Worker
```

## PHASE 13 — Realtime

Gate:

```text
[ ] SignalR
[ ] Reconnect
[ ] Resync
[ ] No request storm
```

## PHASE 14 — Analytics + Snapshots

Gate:

```text
[ ] Aggregates correct
[ ] Snapshot diff tested
```

## PHASE 15 — Map

Gate:

```text
[ ] Stable coordinate model
[ ] Efficient rendering
[ ] Selection/filter integration
```

## PHASE 16 — Alerts / Reports

Gate:

```text
[ ] Rules
[ ] Notifications
[ ] Export
[ ] Scheduled jobs
```

## PHASE 17 — Backup / Diagnostics / Offline

Gate:

```text
[ ] Restore tested
[ ] Health checks
[ ] Offline UX
```

## PHASE 18 — Security hardening

Gate:

```text
[ ] Threat model
[ ] Dependency scan
[ ] Secret scan
[ ] Auth review
[ ] Permission review
```

## PHASE 19 — Performance

Gate:

```text
[ ] Benchmark
[ ] Profiling
[ ] DB query review
[ ] Fleet virtualization
```

## PHASE 20 — Packaging / Release / Update

Gate:

```text
[ ] Windows package
[ ] Linux package
[ ] Checksums
[ ] Signing
[ ] Attestation
[ ] Update
[ ] Rollback
```

## PHASE 21 — Production QA

Gate:

```text
[ ] Fresh install
[ ] Upgrade
[ ] Rollback
[ ] Network loss
[ ] DB migration
[ ] Crash recovery
```

---

# 28. CÁI GÌ LÀ QUAN TRỌNG NHẤT?

| Priority | Thành phần | Vì sao |
|---:|---|---|
| 1 | Domain model | Quyết định logic lâu dài |
| 2 | Data model / MySQL | Quyết định khả năng lưu trữ và migration |
| 3 | Application use cases | Chặn business logic khỏi UI |
| 4 | Authentication/RBAC | Chặn truy cập sai |
| 5 | Telemetry pipeline | Nền tảng toàn bộ Live/History/Analytics |
| 6 | API contract | Giúp desktop/backend độc lập |
| 7 | Tests | Cho phép sửa mà không sợ phá |
| 8 | Observability | Biết production đang hỏng ở đâu |
| 9 | CI/CD | Chặn build lỗi và release lỗi |
| 10 | UI polish | Quan trọng nhưng không được đứng trước data path |

---

# 29. NHỮNG SAI LẦM DỄ PHÁ DỰ ÁN NHẤT

## Sai 1 — Làm UI trước architecture

```text
UI đẹp
 ↓
API
 ↓
DB
```

Hậu quả:

```text
ViewModel phình
logic trùng
API không ổn
DB phải đổi nhiều lần
```

## Sai 2 — Desktop → MySQL

Hậu quả:

```text
lộ topology
permission khó kiểm soát
upgrade DB khó
security khó
```

## Sai 3 — God service

```text
OceanForgeService.cs
```

chứa tất cả.

Không chấp nhận.

## Sai 4 — God ViewModel

```text
DashboardViewModel 4000+ lines
```

Không chấp nhận.

## Sai 5 — Không migration

Production sửa tay = mất khả năng tái lập.

## Sai 6 — Không retention

Telemetry vô hạn = database phình.

## Sai 7 — Không idempotency

Retry event = dữ liệu duplicate.

## Sai 8 — Realtime reload toàn trang

Một account thay đổi mà reload 10.000 rows = chậm.

## Sai 9 — Secret trong GitHub repo

Một lần leak secret có thể buộc rotate toàn bộ credentials.

## Sai 10 — Build production khác build local

Không reproducible = khó debug.

## Sai 11 — Release không có checksum

Không xác thực artifact được.

## Sai 12 — Update không rollback

Một bản lỗi có thể làm toàn bộ user không chạy được.

## Sai 13 — Backup nhưng chưa restore thử

Backup chưa restore test = chỉ là giả định.

## Sai 14 — Không có audit

Không biết ai sửa gì.

## Sai 15 — Microservices quá sớm

Complexity cao trước khi có product-market/data need.

---

# 30. GITHUB REPOSITORY GOVERNANCE

## Branches

```text
main
feature/*
fix/*
refactor/*
security/*
release/*
chore/*
```

Không bắt buộc phải dùng `develop` nếu team nhỏ; ưu tiên trunk-based với short-lived feature branches.

## Main branch

```text
Protected
No direct push
PR required
Required status checks
Linear history hoặc squash merge
CODEOWNERS review cho vùng nhạy cảm
```

## Tags

```text
v0.1.0
v0.1.1
v0.2.0
v1.0.0
```

## Commit convention

```text
feat:
fix:
refactor:
test:
docs:
perf:
security:
ci:
build:
chore:
```

---

# 31. PULL REQUEST FLOW

```text
Developer branch
      ↓
Commit
      ↓
Push
      ↓
Pull Request
      ↓
CI
 ├── restore
 ├── build
 ├── test
 ├── lint/analyzer
 ├── dependency/security scan
 └── architecture checks
      ↓
Code review
      ↓
Approval
      ↓
Squash merge
      ↓
main
```

PR template phải hỏi:

```text
What changed?
Why?
Database migration?
Security impact?
Performance impact?
Breaking API?
Tests added?
Docs updated?
Rollback plan?
```

---

# 32. GITHUB ACTIONS — CI/CD TOÀN BỘ

## Workflow A — Pull Request CI

Mục tiêu: nhanh và không dùng production secrets.

```yaml
name: pr-ci

on:
  pull_request:

permissions:
  contents: read

jobs:
  build-test:
    strategy:
      fail-fast: false
      matrix:
        os: [windows-latest, ubuntu-latest]

    runs-on: ${{ matrix.os }}

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore --configuration Release

      - name: Unit tests
        run: dotnet test --no-build --configuration Release
```

## Workflow B — Main CI

Sau merge:

```text
main push
 ↓
Build
 ↓
Unit tests
 ↓
Integration tests
 ↓
Security scan
 ↓
Package artifacts
 ↓
Upload CI artifacts
```

## Workflow C — Nightly

```text
Dependency audit
Secret scan
Full integration test
DB migration test
DB restore drill
Performance smoke test
```

## Workflow D — Release

```text
Git tag vX.Y.Z
       ↓
Build exact commit
       ↓
Test
       ↓
Package Windows
       ↓
Package Linux
       ↓
Generate checksums
       ↓
Generate provenance attestation
       ↓
Sign artifacts
       ↓
Create GitHub Release
       ↓
Publish release notes
       ↓
Deploy update manifest
```

GitHub artifact attestations tạo build provenance có thể xác minh nguồn gốc của artifact. GitHub hiện cung cấp `actions/attest@v4`; job cần attestation phải có các quyền thích hợp như `id-token: write`, `attestations: write` và có thể cần `artifact-metadata: write` tùy flow. [GitHub artifact attestations](https://docs.github.com/en/actions/security-for-github-actions/using-artifact-attestations/using-artifact-attestations)

---

# 33. GITHUB ACTIONS SECURITY

Workflow mặc định:

```yaml
permissions:
  contents: read
```

Chỉ job nào cần quyền cao hơn mới mở rộng permission.

Ví dụ OIDC:

```yaml
permissions:
  contents: read
  id-token: write
```

Không cấp:

```text
contents: write
packages: write
id-token: write
```

toàn workflow nếu không cần.

## Release environment

Dùng GitHub Environment:

```text
development
staging
production
```

Production phải có:

```text
Required reviewers
Environment protection
Restricted secrets
Branch/tag policy
```

OIDC nên được ưu tiên khi deploy lên cloud thay cho long-lived static credentials. [GitHub OIDC security hardening](https://docs.github.com/actions/security-for-github-actions/security-hardening-your-deployments/about-security-hardening-with-openid-connect)

---

# 34. GITHUB DEPENDENCY MANAGEMENT

Bật:

```text
Dependabot
Dependency review
Secret scanning nếu plan hỗ trợ
CodeQL
```

Quy tắc:

```text
Security patch → ưu tiên cao
Minor update → batch theo tuần
Major update → ADR + test riêng
```

Không merge dependency update chỉ vì “version mới”.

Phải:

```text
Restore
Build
Test
Security scan
```

---

# 35. CI QUALITY GATES

Pull Request không merge nếu fail:

```text
Build
Unit tests
Required integration tests
Analyzer
Formatting/lint
Dependency policy
Architecture test
```

Release không publish nếu fail:

```text
All PR gates
Integration suite
Packaging
Smoke test
Signature
Checksum
Attestation
```

---

# 36. ENVIRONMENT STRATEGY

## Local

```text
Developer machine
Docker MySQL
Docker Redis
Local API
Local Desktop
```

## Development

```text
Dedicated database
Dedicated Redis
Feature flags
Debug logging
```

## Staging

```text
Production-like
Masked/synthetic data
Same release artifact
Full smoke test
```

## Production

```text
Restricted
Minimal diagnostics
Strong audit
Backup
Monitoring
Change approval
```

### Quy tắc quan trọng

**Staging và Production phải sử dụng cùng artifact đã build.**

Không:

```text
Build staging riêng
Build production lại từ source
```

Đúng:

```text
Build once
→ test artifact
→ promote artifact
```

---

# 37. VERSIONING

Dùng Semantic Versioning:

```text
MAJOR.MINOR.PATCH
```

## MAJOR

Breaking API/schema/behavior.

## MINOR

Feature backward-compatible.

## PATCH

Bug/security fix.

Ví dụ:

```text
v1.4.2
```

---

# 38. RELEASE CHANNELS

Khuyến nghị:

```text
stable
beta
nightly
```

## Stable

Chỉ build đã qua production gate.

## Beta

Feature gần hoàn tất, telemetry/feedback nhiều hơn.

## Nightly

Không đảm bảo ổn định.

Không để nightly ghi đè stable channel.

---

# 39. RELEASE ARTIFACTS

Mỗi release nên có:

```text
OceanForge-1.0.0-win-x64.msix
OceanForge-1.0.0-win-x64-setup.exe
OceanForge-1.0.0-linux-x64.AppImage
OceanForge-1.0.0-linux-x64.deb
checksums-sha256.txt
SBOM.spdx.json hoặc CycloneDX
release-notes.md
```

Artifact metadata:

```text
version
commit SHA
build timestamp
OS
architecture
runtime
installer type
```

---

# 40. CHECKSUM + ATTESTATION

Sau build:

```text
SHA-256
```

Sau đó:

```text
Attestation / provenance
```

Người dùng/admin có thể verify:

```text
artifact
 ↓
digest
 ↓
release tag
 ↓
repository commit
 ↓
build workflow provenance
```

GitHub artifact attestations được thiết kế để chứng minh artifact được tạo từ repository/workflow/commit nào. [GitHub artifact attestations](https://docs.github.com/en/actions/security-for-github-actions/using-artifact-attestations/using-artifact-attestations)

---

# 41. CODE SIGNING

## Windows

```text
Release artifact
 ↓
Code signing certificate
 ↓
Signed package
 ↓
Verify
```

Signing key không được commit vào repo.

## Linux

Tùy distribution/release policy:

```text
Checksum
Detached signature nếu dùng
Repository/package signing nếu triển khai package repo
```

---

# 42. WINDOWS PACKAGING

## Primary

Chọn một format chính cho stable distribution:

```text
MSIX
```

hoặc nếu installer workflow của sản phẩm thuận tiện hơn:

```text
Signed EXE installer
```

Không nên tạo quá nhiều format ở release đầu tiên nếu chưa có nhu cầu.

## Windows package checklist

```text
Version
Publisher
Product name
Icon
Install location
Shortcuts
Uninstall
Upgrade
Repair nếu có
Code signing
Checksum
Fresh install test
Upgrade test
Uninstall test
```

## Windows test matrix

```text
Clean Windows machine
Existing version
Upgrade from previous stable
No network
Low privilege user
Antivirus on
Insufficient disk
```

---

# 43. LINUX PACKAGING

## AppImage

Mục tiêu:

```text
Portable
Easy to distribute
```

## DEB

Mục tiêu:

```text
Debian/Ubuntu package ecosystem
Desktop entry
Dependencies
Uninstall/upgrade via package manager
```

## Linux test matrix

```text
Ubuntu target version
Debian target version
Fedora target version nếu hỗ trợ
X11
Wayland nếu package/backend được bật
Normal user
No root assumptions
```

Avalonia hiện hỗ trợ Linux desktop, nhưng mức hỗ trợ thay đổi theo distro/version; cần đóng gói test matrix rõ cho release thay vì tuyên bố “mọi Linux”. [Avalonia supported platforms](https://docs.avaloniaui.net/docs/supported-platforms)

---

# 44. UPDATER ARCHITECTURE

```text
Desktop
   ↓
Check release manifest
   ↓
Compare current version
   ↓
Download package
   ↓
Verify checksum
   ↓
Verify signature/provenance policy
   ↓
Stage
   ↓
Close app
   ↓
Install
   ↓
Health check
   ↓
Success
```

Manifest ví dụ:

```json
{
  "version": "1.0.0",
  "channel": "stable",
  "minimumVersion": "0.9.0",
  "releaseNotesUrl": "...",
  "artifacts": {
    "win-x64": {
      "url": "...",
      "sha256": "..."
    },
    "linux-x64": {
      "url": "...",
      "sha256": "..."
    }
  }
}
```

---

# 45. ROLLBACK

Có hai rollback khác nhau.

## Application rollback

```text
1. Stop broken release
2. Repoint release channel
3. Restore previous installer
4. Verify health
5. Publish incident note
```

## Database rollback

Không phải migration nào cũng nên “down migration”.

Ưu tiên:

```text
Forward-compatible migration
Data repair migration
Restore from verified backup only when necessary
```

Không tự động restore production database chỉ vì application health check fail.

---

# 46. RELEASE FLOW CHÍNH THỨC

```text
Feature complete
      ↓
PR merged
      ↓
main green
      ↓
Release candidate tag
      ↓
Build once
      ↓
Test staging
      ↓
Security gate
      ↓
Manual approval
      ↓
Sign
      ↓
Attest
      ↓
Publish GitHub Release
      ↓
Update stable manifest
      ↓
Canary / staged rollout
      ↓
Monitor
      ↓
Full rollout
```

---

# 47. STAGED ROLLOUT

Không nhất thiết phát hành cho 100% user ngay.

```text
Internal
  ↓
5%
  ↓
25%
  ↓
50%
  ↓
100%
```

Gate mỗi bước:

```text
Crash rate
Login failures
API error rate
Update failures
Telemetry errors
```

---

# 48. INCIDENT / HOTFIX FLOW

```text
Incident
 ↓
Classify severity
 ↓
Mitigate
 ↓
Create fix branch
 ↓
PR
 ↓
Fast CI
 ↓
Security review
 ↓
Patch release
 ↓
Deploy
 ↓
Verify
 ↓
Postmortem
```

Severity:

```text
P0 — service unavailable / data corruption
P1 — major feature broken
P2 — significant but workaround exists
P3 — minor issue
```

---

# 49. TESTING PYRAMID

```text
                 E2E
                /   \
           UI / Acceptance
             /       \
       Integration   Integration
            \         /
             \       /
               Unit
```

## Unit test nhiều nhất

```text
Domain
Validators
Use cases
Diff logic
Status derivation
Analytics calculations
```

## Integration

```text
MySQL
Redis
API
Auth
Telemetry
Realtime
```

## E2E

```text
Install
Register
Login
Create account
Receive telemetry
Dashboard
Account detail
Report
Update
Restart
Uninstall
```

---

# 50. ARCHITECTURE TESTS

Thêm test để ngăn dependency sai.

Ví dụ rule:

```text
Domain assembly must not reference Avalonia
Domain assembly must not reference EF Core
Application must not reference Desktop
API must not reference Desktop
```

Đây là loại test rất quan trọng cho maintainability lâu dài.

---

# 51. PERFORMANCE ENGINEERING — CPU/RAM BUDGETS ARE MEASURED, NOT PROMISED

## 51.1 Nguyên tắc phản biện

Không có framework C# nào bảo đảm CPU luôn thấp hoặc RAM không bao giờ tăng. UI framework tạo ra một phần overhead; phần lớn vấn đề thực tế thường đến từ truy vấn toàn bảng, polling dày, collection không giới hạn, event/subscription không dispose, ảnh/cache không có quota, cập nhật toàn view và công việc CPU-bound trên UI thread.

**Quy trình:** đo baseline → đặt budget trên máy tham chiếu → profile theo workload thật → sửa bottleneck lớn nhất → benchmark lại → soak test. Không tối ưu dựa trên cảm giác hoặc một con số RAM từ máy khác.

## 51.2 Bộ đo bắt buộc

| Metric | Cần xem | Vì sao |
|---|---|---|
| Process CPU / CPU time | Idle, load, reconnect storm, map/analytics | Xác định polling/loop/update thừa |
| Working Set / Private Bytes | Toàn process | Memory OS nhìn thấy; không đồng nhất với managed heap |
| .NET GC heap size / allocation rate | Sau warm-up và sau các vòng điều hướng | Phát hiện allocation churn/retention |
| GC pause/count/gen 2 | Trong live updates và import/export | Phát hiện object sống lâu/large object allocations |
| UI responsiveness | Thời gian thao tác, p95/p99, UI stalls | Ngăn giao diện treo dù CPU tổng thấp |
| Thread count / handles / socket count | Trong soak test | Phát hiện timer/task/connection không được giải phóng |
| SQLite lock/busy / query latency | Offline writes, batch sync, cache | Phát hiện tranh chấp writer |
| API/MySQL p95/p99 | Dashboard, Fleet, history, sync | UI chậm có thể do backend, không phải UI framework |
| Queue depth / oldest item age | Local outbox, DB outbox, worker | Đo sức ép và nguy cơ backlog |

Dùng `dotnet-counters`, `dotnet-gcdump`, `dotnet-dump`, profiler của IDE và metrics tương đương trên Linux. GCDump/dump có thể chứa dữ liệu nhạy cảm: hạn chế quyền, mã hóa khi cần, không upload công khai. Xem [Debug a memory leak](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/debug-memory-leak), [dotnet-gcdump](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-gcdump), [dotnet-dump](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-dump).

## 51.3 Budget ban đầu — mục tiêu kiểm chứng, KHÔNG phải cam kết marketing

Chọn một máy tham chiếu tối thiểu trước khi chốt SLA, ví dụ máy x64 4 logical cores, 8 GB RAM, SSD, không dựa vào GPU rời. Đội dự án phải ghi model CPU, RAM, OS/distro, độ phân giải, driver và version build vào báo cáo benchmark.

| Workload | Budget khởi đầu để thử nghiệm | Điều kiện |
|---|---|---|
| Desktop idle sau warm-up, không có event mới | CPU trung bình mục tiêu ≤ 2% trong cửa sổ 5 phút | Ghi rõ cách đo/OS; không có animation liên tục |
| Desktop shell + cache nhỏ | Private bytes mục tiêu ≤ 350 MB | Ngưỡng sơ bộ để thử, phải xác nhận bằng prototype thực |
| Dashboard + 1.000 account mẫu, không tải toàn lịch sử | Working/private memory mục tiêu ≤ 500 MB | Cache/chart/map assets được giới hạn; chưa phải số bảo đảm |
| Common UI action | Không thực hiện I/O/CPU-heavy trên UI thread; tác vụ nhẹ mục tiêu dưới 50 ms | Tác vụ lâu hiển thị progress/cancel |
| Search/filter | Debounce 250–350 ms, hủy truy vấn cũ | Đo p95 latency trên dữ liệu mẫu lớn |
| UI update từ live events | Gộp cập nhật 100–250 ms khi phù hợp | Persist/audit event không bị drop; chỉ coalesce render state |
| Memory soak | Sau warm-up, live managed heap không tăng đều qua các vòng lặp giống nhau | Chạy tối thiểu 8 giờ; tiêu chí sai số do workload/cache được ghi rõ |

Nếu budget không đạt, báo cáo phải xác định nguyên nhân và lựa chọn trade-off; không tăng RAM target vô hạn để che memory leak. Không kết luận memory leak chỉ dựa vào working set giữ cao, vì GC/runtime có thể giữ vùng đã cấp phát để tái sử dụng; so sánh live heap/root graph và trend sau các vòng lặp ổn định.

## 51.4 Chọn GC theo process, không theo cảm tính

- Desktop interactive: bắt đầu với cấu hình GC mặc định dành cho ứng dụng client/workstation; ưu tiên độ phản hồi.
- ASP.NET Core API/Worker: dùng hosting/runtime default thích hợp với server workload; đánh giá server GC khi throughput/heap lớn là nhu cầu thật.
- Không bật/tắt Server GC, background GC, LOH settings hoặc `GC.Collect()` thường xuyên như mẹo tối ưu chung.
- Ép GC thủ công trong code thường che vấn đề lifetime và có thể gây pause; chỉ xem xét trong workload/measurement đặc biệt có bằng chứng.

Microsoft phân biệt workstation GC cho ứng dụng client và server GC cho workload thông lượng cao; hãy benchmark đúng workload thay vì chia sẻ một cấu hình cho tất cả process. Nguồn: [Workstation and server GC](https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/workstation-server-gc).

## 51.5 Ma trận benchmark bắt buộc

```text
A. Cold start / warm start
B. Idle 5 phút không có telemetry
C. 100 / 1.000 / 10.000 account tổng; chỉ render page hiện hành
D. Burst 100 / 1.000 / 10.000 event; có duplicate và out-of-order event
E. Mạng mất 5 phút / 1 giờ / lâu hơn cursor retention
F. Mạng trở lại sau backlog lớn; kiểm tra jitter/backpressure
G. Mở/đóng 20 account detail drawer liên tiếp
H. Điều hướng 100 lần qua Map / Analytics / History
I. Import/export lớn với cancel giữa chừng
J. Sleep/resume và reconnect websocket
K. Update / repair / uninstall / restart
L. Soak test 8 giờ tối thiểu; kiểm tra memory/handle/socket trend
```

---

# 52. UI PERFORMANCE + BỘ THUẬT TOÁN GIỮ APP MƯỢT

## 52.1 Quy tắc giao diện

```text
- UI thread chỉ: cập nhật state nhẹ, điều phối navigation, render.
- I/O luôn async: HTTP, SQLite/MySQL (server-side), file, export, update.
- CPU-bound: chạy worker/background có CancellationToken.
- Không gọi .Result / .Wait() trên async task trong UI path.
- Không dùng async void ngoài event-handler bắt buộc; task nền phải bắt/log exception.
- Không tải tất cả lịch sử hoặc 100k ViewModel vào ObservableCollection.
- Mỗi page có loading / empty / error / offline / stale / permission-denied states.
```

## 52.2 Thuật toán ưu tiên

| Vấn đề | Thuật toán/cơ chế | Chính sách |
|---|---|---|
| User gõ tìm kiếm liên tục | Debounce 250–350 ms + cancel token cho truy vấn trước | Không gửi request cho từng phím nếu người dùng vẫn đang gõ |
| Fleet nhiều account | Server-side filter + cursor pagination + UI virtualization | Mỗi page giới hạn hợp lý; không offset/pull toàn bảng khi dataset lớn |
| Live event dồn dập | Bounded `Channel<T>` + backpressure + batch flush | Không dùng unbounded queue; event lịch sử phải được persist trước khi ack |
| Nhiều state update cho cùng account | Coalesce **render updates** theo account, chỉ hiển thị state mới nhất | Chỉ gộp ở UI; không xóa event lịch sử/audit |
| Refresh định kỳ | Event-driven trước; một scheduler dùng `PeriodicTimer` cho tác vụ housekeeping | Tránh một timer/poll loop cho từng account |
| Offline retry | Exponential backoff + full jitter + max cap + cancellation | Tránh request storm khi mạng trở lại |
| Snapshot không thay đổi | Canonical serialization + stable hash + skip duplicate snapshot | Hash/version phải bao phủ field có ý nghĩa và schema version |
| Dashboard metrics | Server-side aggregation + cache TTL/version + incremental invalidation | Không tính lại hàng triệu event ở ViewModel |
| Biểu đồ dài hạn | Query theo time bucket + downsampling phù hợp kích thước pixel | Không gửi 1 điểm/telemetry event cho biểu đồ triệu điểm |
| Bản đồ | Chỉ render marker/layer cần thấy; cache tile/assets có giới hạn; update theo delta | Giới hạn animation; Low Bandwidth/Performance mode giảm refresh |
| Export/report | Streaming/chunked write + progress + cancel | Không buffer toàn bộ file trong RAM |
| SQLite offline writes | Transaction ngắn, một writer pipeline, batch có giới hạn | Retry `busy/locked` với policy; không giữ transaction trong khi gọi network |

## 52.3 Bounded channel — không để hàng đợi tăng RAM vô hạn

Ví dụ cấu hình buffer trong process để điều tiết producer/consumer. Đây **không** phải durable queue; event cần bảo đảm không mất phải được ghi vào SQLite/server outbox trước khi coi là đã nhận thành công.

```csharp
using System.Threading.Channels;

var channel = Channel.CreateBounded<TelemetryEnvelope>(
    new BoundedChannelOptions(2_000)
    {
        SingleReader = true,
        SingleWriter = false,
        FullMode = BoundedChannelFullMode.Wait,
        AllowSynchronousContinuations = false
    });

// Producer: await channel.Writer.WriteAsync(item, cancellationToken);
// Consumer: drain a bounded batch, persist idempotently, then continue.
// A full channel applies backpressure instead of allocating without limit.
```

Chọn `FullMode.Wait` cho event không được phép mất. Không dùng `DropOldest`/`DropWrite` cho audit, mutation hoặc event cần lưu; chỉ cân nhắc coalescing/drop cho dữ liệu render tạm thời mà mất mẫu được chấp nhận và được đo rõ. `System.Threading.Channels` hỗ trợ bounded capacity và backpressure: [Microsoft Learn — Channels](https://learn.microsoft.com/en-us/dotnet/core/extensions/channels).

## 52.4 Batch và flush

```text
Flush khi đạt ngưỡng batch (ví dụ 100–200 item)
HOẶC khi quá hạn flush ngắn (ví dụ 100–250 ms)
HOẶC khi shutdown/cancel có thời gian graceful phù hợp.
```

Ngưỡng 100–200 và 100–250 ms là giá trị bắt đầu để benchmark, không phải hằng số cho mọi workload. Đo throughput, end-to-end latency, queue age và transaction duration rồi điều chỉnh. Batch quá lớn tăng memory/latency; batch quá nhỏ tăng overhead network/SQL.

## 52.5 Lifetime/dispose bắt buộc

- ViewModel/view đóng phải cancel request và dispose subscription/timer/resource được sở hữu.
- Dùng DI lifetime đúng: singleton chỉ cho service thật sự thread-safe/lâu dài; DbContext/connections ở scope ngắn.
- Dùng `IHttpClientFactory` hoặc typed client; không tạo/dispose `HttpClient` liên tục trong loop.
- Mọi `CancellationTokenSource`, stream, file handle, subscription SignalR, event handler và image resource có owner/dispose path.
- Cache ảnh/map/icon có giới hạn số byte + eviction; không cache vô hạn theo account ID.
- Dữ liệu history/export xử lý streaming hoặc pagination; không giữ cả dataset trong ViewModel.
- Chỉ dùng `ArrayPool<T>`/object pooling khi profiler chứng minh allocation là bottleneck; pooling sai có thể giữ object lớn và phức tạp lifetime.

## 52.6 Low-resource modes

| Profile | Hành vi |
|---|---|
| Economy | Tắt motion/glow không cần thiết, giảm tần suất chart/map refresh, hạn chế prefetch, cache nhỏ hơn, không tự mở panel nặng |
| Balanced | Default; cập nhật delta, animation trang trí vừa phải, prefetch có giới hạn |
| Full Monitoring | Tăng event visibility/history detail nhưng vẫn có backpressure, retention và memory caps |
| Low Bandwidth | Sync batch nhỏ, giảm thumbnail/asset download, exponential backoff, hiển thị freshness rõ |
| Presentation | Tắt cập nhật gây xao nhãng, giữ dữ liệu có freshness label, không giả lập LIVE |

---

# 53. ACCESSIBILITY

```text
Keyboard navigation
Focus order
Screen reader names
Contrast
Text scaling
Tooltips
Reduced motion
High DPI
```

---

# 54. MULTI-PLATFORM RULES

Không hard-code:

```text
C:\Users\...
C:\Program Files\...
Windows-only temp path
```

Thay bằng abstraction:

```text
IPathProvider
IAppDataLocation
IFileDialogService
INotificationService
IStartupService
```

Platform-specific code phải nằm ở:

```text
Infrastructure/Platforms/Windows
Infrastructure/Platforms/Linux
```

UI/business không tự kiểm tra:

```csharp
RuntimeInformation.IsOSPlatform(...)
```

trừ khi là presentation concern thực sự.

---

# 55. CONFIGURATION MANAGEMENT

## Config levels

```text
Default config
Environment config
User config
Remote feature flags
```

## Không commit secret

Config commit được:

```text
API base URL pattern
feature defaults
retention defaults
UI defaults
```

Secret runtime-only.

---

# 56. FEATURE FLAGS

Dùng để rollout an toàn:

```text
enable_time_machine
enable_new_map_renderer
enable_new_report_engine
```

Rules:

```text
Flag owner
Default
Environment overrides
Expiry date
Cleanup issue
```

Feature flag phải có ngày xóa, tránh code chết tồn tại vĩnh viễn.

---

# 57. DATA RETENTION

Mặc định ban đầu:

```text
Realtime events       7–30 days
Detailed telemetry    30–90 days
Snapshots             180–365 days
Reports               policy-based
Audit logs            long retention
```

Worker retention job:

```text
Find expired
 ↓
Batch delete/archive
 ↓
Measure
 ↓
Audit
```

Không xóa hàng triệu record trong một transaction duy nhất.

---

# 58. AUDIT LOG

Audit cho:

```text
Login
Logout
Role change
Permission change
Account create/update/archive
Alert rule change
Settings change
Backup create/restore
Report export
Release/admin actions
```

Mỗi record:

```text
who
what
when
entity
result
traceId
```

---

# 59. REPORT ENGINE

Report generation phải chạy background.

```text
User request
 ↓
Create report job
 ↓
Queue
 ↓
Worker
 ↓
Generate
 ↓
Store
 ↓
Notify user
```

Không tạo PDF nặng trên UI thread.

---

# 60. MAP ENGINE

Map phải là một adapter riêng.

```text
Map domain data
      ↓
Map projection model
      ↓
Map renderer
```

Không để map renderer biết MySQL schema.

---

# 61. ANALYTICS ENGINE

Tách:

```text
Operational queries
Analytical queries
```

Không dùng một query siêu nặng cho cả Dashboard và Analytics.

Ưu tiên:

```text
read models
aggregates
precomputed statistics
cache
```

---

# 62. ACCOUNT TIME MACHINE / SNAPSHOT

Snapshot phải immutable sau khi finalized.

```text
Snapshot A
Snapshot B
    ↓
Diff engine
    ↓
Added / Removed / Changed / Unchanged
```

Hash để deduplicate snapshot giống nhau.

---

# 63. ALERT ENGINE

```text
Telemetry event
   ↓
Normalize
   ↓
Rule evaluator
   ↓
Condition match?
   ├── No → stop
   └── Yes
         ↓
Create alert
         ↓
Notify
         ↓
Audit
```

Alert phải có cooldown để chống spam:

```text
Rule
Cooldown
Escalation
Acknowledgement
Resolution
```

---

# 64. NOTIFICATION ARCHITECTURE

Kênh:

```text
In-app
Desktop notification
Email (nếu cấu hình)
```

Mọi notification có:

```text
priority
createdAt
readAt
source
related entity
```

---

# 65. LICENSE / OPEN-SOURCE GOVERNANCE

Repository phải quản lý:

```text
LICENSE
Third-party notices
SBOM
Dependency licenses
```

Trước release:

```text
License scan
Dependency scan
SBOM generation
```

---

# 66. DOCUMENTATION STRUCTURE

```text
docs/
├── architecture/
│   ├── system.md
│   ├── dependency-rules.md
│   ├── data-flow.md
│   └── scaling.md
├── adr/
├── api/
├── security/
├── operations/
├── ui/
└── release/
```

README không được trở thành tài liệu duy nhất.

---

# 67. DEFINITION OF READY

Feature chỉ được bắt đầu khi:

```text
Requirement clear
Acceptance criteria clear
Owner clear
Data impact known
Security impact known
UX flow known
Dependencies known
Rollback idea known
Test strategy known
```

---

# 68. DEFINITION OF DONE

```text
[ ] Requirement
[ ] Acceptance criteria
[ ] UX
[ ] Domain
[ ] Application
[ ] API
[ ] Database/migration nếu cần
[ ] UI
[ ] Error handling
[ ] Logging
[ ] Security review
[ ] Unit test
[ ] Integration test
[ ] UI verification
[ ] Performance check
[ ] Documentation
[ ] PR review
[ ] CI green
```

---

# 69. DEFINITION OF PRODUCTION READY

```text
[ ] Reproducible build
[ ] Dependency lock/version policy
[ ] PR branch protection
[ ] CI green
[ ] Integration green
[ ] Security scan green
[ ] Migration tested
[ ] Backup verified
[ ] Restore drill passed
[ ] Windows package tested
[ ] Linux package tested
[ ] Installer tested
[ ] Upgrade tested
[ ] Rollback tested
[ ] Checksum generated
[ ] Artifact attestation generated
[ ] Signing verified
[ ] Monitoring active
[ ] Crash handling active
[ ] Support docs ready
[ ] Release notes ready
[ ] Incident procedure ready
```

---

# 70. VÒNG ĐỜI TOÀN BỘ SẢN PHẨM

```text
IDEA
 ↓
VISION
 ↓
REQUIREMENTS
 ↓
ARCHITECTURE
 ↓
ADR
 ↓
REPOSITORY
 ↓
DOMAIN
 ↓
DATABASE
 ↓
APPLICATION
 ↓
API
 ↓
DESKTOP SHELL
 ↓
CORE UI
 ↓
BUSINESS MODULES
 ↓
TELEMETRY
 ↓
REALTIME
 ↓
ANALYTICS
 ↓
ADVANCED FEATURES
 ↓
TESTING
 ↓
SECURITY HARDENING
 ↓
PERFORMANCE
 ↓
PACKAGING
 ↓
CI/CD
 ↓
STAGING
 ↓
RELEASE CANDIDATE
 ↓
SIGN + ATTEST
 ↓
PRODUCTION
 ↓
MONITORING
 ↓
FEEDBACK
 ↓
PATCH / MINOR / MAJOR
 ↓
MAINTENANCE
 ↓
DEPRECATION
 ↓
END OF LIFE
```

---

# 71. DAILY DEVELOPMENT LOOP

```text
Pick issue
 ↓
Read acceptance criteria
 ↓
Implement smallest vertical slice
 ↓
Unit test
 ↓
Integration test if needed
 ↓
Run local CI equivalent
 ↓
Commit
 ↓
Push
 ↓
PR
 ↓
CI
 ↓
Review
 ↓
Merge
```

Một vertical slice tốt có thể là:

```text
CreateAccount
Domain
Application
API
DB
UI
Test
```

thay vì làm xong 100% UI rồi mới làm backend.

---

# 72. SMALL VERTICAL SLICE MẪU

Feature “Create tracked account”:

```text
Domain:
  Account.Create()

Application:
  CreateTrackedAccountUseCase

Persistence:
  AccountRepository

API:
  POST /api/v1/accounts

Desktop:
  CreateAccountDialog
  CreateAccountViewModel

Tests:
  Domain
  Application
  API integration
  UI smoke
```

Feature được coi là thật sự hoàn thành chỉ khi toàn bộ slice chạy được.

---

# 73. LOCAL DEVELOPMENT STACK

```text
Developer machine
│
├── .NET 10 SDK
├── MySQL 8.4 container
├── Redis container
├── OceanForge.Api
├── OceanForge.Worker
└── OceanForge.Desktop
```

Docker Compose dev stack nên có:

```text
mysql
redis
```

Không containerize desktop UI để phát hành production trừ khi có lý do cụ thể.

---

# 74. DATABASE MIGRATION WORKFLOW

```text
Change model
 ↓
Create migration
 ↓
Inspect SQL
 ↓
Run on clean DB
 ↓
Run upgrade from previous version
 ↓
Run integration test
 ↓
Commit migration
 ↓
CI migration test
 ↓
Staging
 ↓
Production
```

Migration file phải review như source code.

---

# 75. API COMPATIBILITY

Nguyên tắc:

```text
Additive change first
Deprecate second
Remove later
```

Không xóa endpoint ngay trong minor release.

Ví dụ:

```text
v1/accounts
```

giữ tương thích nếu có thể; breaking change cần kế hoạch versioning.

---

# 76. BACKWARD COMPATIBILITY

Release N phải đọc dữ liệu do release N-1 tạo ra nếu policy yêu cầu upgrade in-place.

Test matrix:

```text
N-1 DB + N app
N DB + N app
N DB + N+1 app
```

---

# 77. MAINTENANCE MODEL

## Patch

```text
Security
Crash
Critical bug
```

## Minor

```text
Feature
Performance
UX improvement
```

## Major

```text
Breaking API
Breaking migration
Large architecture change
```

---

# 78. DEPENDENCY UPDATES

Hàng tuần:

```text
Review security advisories
Review Dependabot PRs
```

Hàng tháng:

```text
Dependency inventory
Unused package review
Major upgrade planning
```

Không để dependency trôi quá lâu không owner.

---

# 79. OBSERVABILITY AFTER RELEASE

Theo dõi:

```text
Crash-free sessions
Startup failures
Login failures
API error rate
API latency
Telemetry ingestion failures
Queue depth
Database errors
Redis errors
Update failures
```

Mọi alert production phải có owner.

---

# 80. RELEASE RETROSPECTIVE

Sau mỗi release lớn:

```text
What changed?
What failed?
What nearly failed?
What metric moved?
What should be automated?
What should be documented?
```

Tạo ADR/issue cho lesson learned quan trọng.

---

# 81. MAINTAINABILITY CHECKLIST — 6 THÁNG SAU

Nếu sau 6 tháng project vẫn có:

```text
< 1 API access pattern
< 1 persistence pattern
stable domain boundaries
automated tests
CI required
release tags
migration history
observability
```

thì architecture đang đi đúng hướng.

Nếu thấy:

```text
duplicate service
SQL trong ViewModel
random static helpers
branch sống hàng tháng
manual production changes
```

phải refactor ngay.

---

# 82. SCALE PLAN — ADD COMPLEXITY ONLY WHEN MEASURED

## Phase A — 1–100 tracked accounts (initial assumption; measure actual event rate)

```text
One ASP.NET Core API
One Worker host/codebase
MySQL as source of truth
Desktop SQLite cache + outbox/cursor
SignalR for small deltas (or polling fallback where appropriate)
Redis not mandatory unless measurements justify it
```

## Phase B — 100–1,000

```text
Review query plans/indexes/pagination
Tune connection pools and bounded worker concurrency
Add read aggregates/cache where metrics show repeated expensive reads
Introduce Redis for cache/rate-limiting/fan-out only if required
Measure outbox age, event lag and p95/p99 latency
```

## Phase C — 1,000–10,000+

```text
Batch ingestion + archive/retention strategy
Dedicated workers if CPU/IO profiles justify separate scaling
Read models/materialized aggregates
Partitioning only when query/maintenance evidence supports it
Horizontal API scaling with shared auth/rate-limit and idempotency semantics
Broker/queue only with documented durability/recovery plan
```

Scale up when measured queue age, request latency, database saturation, or operational isolation requires it—not based on account count alone. Do not scale by rewriting everything into microservices.

---

# 83. WHEN TO SPLIT SERVICES

Chỉ tách service khi có ít nhất một lý do:

```text
Scale độc lập
Deploy độc lập
Failure isolation
Team ownership
Compliance/security isolation
```

Không tách chỉ vì “microservices hiện đại”.

---

# 84. FAILURE BOUNDARIES

| Failure | App phải làm |
|---|---|
| MySQL/API down | Read stale SQLite cache; queue only explicitly approved offline mutations in durable SQLite outbox; security/role operations disabled |
| Redis down | Core API/DB must continue if Redis is configured as optional cache; degrade cache/fan-out and recover via REST/resync |
| Telemetry producer down | Show stale/offline |
| SignalR down | REST remains usable; reconnect later |
| API slow | Timeout + cancel + user feedback |
| Update server down | Keep current installed version |
| Backup failure | Alert operations; không coi job thành công |

---

# 85. SAFETY / DATA BOUNDARY

OceanForge chỉ quản lý telemetry/state mà người dùng được phép quản lý.

Không triển khai trong scope sản phẩm:

```text
Credential harvesting
Password collection
Cookie theft
Session-token theft
Exploit/injection
Bypass authentication
Phishing
```

Credential và secret của OceanForge phải được xử lý bằng secret-management đúng chuẩn.

---

# 86. FINAL MASTER ROADMAP — V4 ORDER

```text
STEP 01  Scope + non-goals + 24-screen manifest
STEP 02  GitHub governance + .NET SDK/package pinning
STEP 03  Domain + contracts + event IDs/schema versions
STEP 04  MySQL server schema + migrations + backup basics
STEP 05  Local SQLite schema + migrations + cache/outbox/cursor
STEP 06  Application use cases + repository boundaries
STEP 07  OceanForge auth / RBAC / audit
STEP 08  API contracts + authorization + idempotency
STEP 09  First vertical slice: login → cache → show stale offline state → resync
STEP 10  One approved offline mutation + conflict inbox
STEP 11  Dashboard + Fleet pagination/virtualization
STEP 12  Account detail + Inventory/Materials
STEP 13  Telemetry ingest + server outbox + Worker bounded batches
STEP 14  SignalR delta + cursor resync/reconnect tests
STEP 15  History/snapshots/retention
STEP 16  Analytics + chart downsampling
STEP 17  Map renderer + layer/marker budget
STEP 18  Alerts + reports
STEP 19  Backup/restore + diagnostics
STEP 20  Launcher download/verify/install/update/repair/uninstall flow
STEP 21  Security and performance hardening
STEP 22  Windows/Linux package and OS-specific E2E
STEP 23  GitHub CI/CD + staging + signed release artifact
STEP 24  Beta/staged rollout + observability + support/maintenance
```

---

# 87. “KHÔNG ĐƯỢC LÀM NGƯỢC” — ONE-PAGE RULE

```text
KHÔNG:
UI → DB → Security → Test → Release

CÓ:
Requirements
  ↓
Domain
  ↓
Database
  ↓
Application
  ↓
API
  ↓
UI
  ↓
Telemetry/Realtime
  ↓
Test
  ↓
Security
  ↓
Performance
  ↓
Packaging
  ↓
CI/CD
  ↓
Staging
  ↓
Release
  ↓
Monitoring
```

---

# 88. SOURCE / VERIFICATION NOTES

## User-provided architecture baseline

Tài liệu V2 đã xác định:

```text
.NET 10
Avalonia
ASP.NET Core
MySQL 8.4
Redis
MVVM
Telemetry
Analytics
Backup/Restore
GitHub Actions
Windows/Linux packaging
```

V3 mở rộng production lifecycle; V4 tiếp tục phản biện UI, bổ sung local SQLite/offline-sync contract, launcher lifecycle và CPU/RAM engineering mà không đổi visual direction cốt lõi.

## Current external verification used for V3

- .NET 10 hiện là LTS; Microsoft liệt kê bản patch hiện hành và ngày kết thúc support trong policy chính thức. [Microsoft .NET lifecycle policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)
- Avalonia hỗ trợ Windows và Linux, nhưng support tier phụ thuộc OS/version; release phải có target matrix rõ ràng. [Avalonia supported platforms](https://docs.avaloniaui.net/docs/supported-platforms)
- MySQL 8.4 thuộc LTS track. [MySQL 8.4 Reference Manual](https://dev.mysql.com/doc/refman/8.4/en/)
- GitHub cung cấp OIDC cho workflow và artifact attestations để tăng supply-chain/build provenance security. [GitHub OIDC security hardening](https://docs.github.com/actions/security-for-github-actions/security-hardening-your-deployments/about-security-hardening-with-openid-connect) and [artifact attestations](https://docs.github.com/en/actions/security-for-github-actions/using-artifact-attestations/using-artifact-attestations)

---

# 89. FINAL PRODUCTION CHECKLIST

## Product

```text
[ ] Scope locked
[ ] Non-goals documented
[ ] User flows documented
```

## Architecture

```text
[ ] Dependency rules tested
[ ] No forbidden references
[ ] ADRs current
[ ] No God services
```

## Database

```text
[ ] Migration tested
[ ] Indexes reviewed
[ ] Retention active
[ ] Backup active
[ ] Restore tested
```

## Security

```text
[ ] No secrets in Git
[ ] Auth secure
[ ] RBAC tested
[ ] Audit active
[ ] TLS verified
[ ] Dependency scan clean
[ ] Artifact signed/attested
```

## CI/CD

```text
[ ] PR required
[ ] Main protected
[ ] Build matrix green
[ ] Integration green
[ ] Security checks green
[ ] Release workflow tested
[ ] Environment protection configured
```

## Packaging

```text
[ ] Windows install
[ ] Linux install
[ ] Upgrade
[ ] Uninstall
[ ] Checksums
[ ] Signing
[ ] Attestation
```

## Operations

```text
[ ] Monitoring
[ ] Alerts
[ ] Crash reporting
[ ] Incident procedure
[ ] Rollback procedure
[ ] Support docs
```

---

# 90. FINAL DECISION — V4 BASELINE

```text
Desktop UI            = Avalonia + MVVM + CommunityToolkit.Mvvm
Runtime               = .NET 10 LTS (pin SDK and package versions)
Desktop local store   = SQLite cache + durable outbox + cursor + conflict inbox
Server API            = ASP.NET Core 10
Server source of truth= MySQL 8.4 LTS, accessed only through API
Persistence           = EF Core for normal server paths; Dapper only if profiled
Background processing = Worker + transactional outbox; bounded in-process Channel is not durable storage
Redis                 = optional scale/cache/realtime support after measured need + ADR
Realtime              = SignalR/WebSocket for deltas; REST/API resync is authoritative
Sync                  = at-least-once delivery + idempotency + explicit conflicts
Launcher              = separate download/update/repair UI; OS/package manager owns install/uninstall primitives
CI/CD                 = GitHub Actions + protected main + build/test/security gates
Packaging             = signed MSIX or signed EXE; Linux .deb and/or AppImage only for tested targets
Release               = SemVer + signed artifacts + SHA-256 + provenance/attestation
Deployment            = build once → test artifact → staging → approve → production
Security              = RBAC + OS-protected local secrets + least privilege + audit
Operations             = structured logs + metrics + traces + backup/restore drills
Performance            = budgets + reproducible benchmarks + soak tests; no unsupported “always low” promise
```

**V4 replaces V3's vague offline note with explicit local storage/sync rules.** The detailed UI concept remains the visual direction, not proof that all 24 screens are implementation-ready or that current performance targets have passed tests. Do not add another framework/database/cache unless an ADR explains the need and a prototype proves it.

---

# 4. INFORMATION ARCHITECTURE

```text
OCEANFORGE
│
├── COMMAND CENTER
│   ├── Overview
│   ├── Live
│   ├── Fleet
│   └── Map
│
├── ACCOUNTS
│   ├── All Accounts
│   ├── Groups
│   ├── Tags
│   ├── Saved Views
│   └── Account Detail
│
├── GAME DATA
│   ├── Inventory
│   ├── Fruits
│   ├── Swords
│   ├── Guns
│   ├── Fighting Styles
│   ├── Accessories
│   └── Materials
│
├── HISTORY
│   ├── Sessions
│   ├── Telemetry
│   ├── Snapshots
│   ├── Diff
│   └── Event Timeline
│
├── ANALYTICS
│   ├── Overview
│   ├── Account Growth
│   ├── Activity
│   ├── Materials
│   ├── Inventory Trends
│   ├── Uptime
│   └── Reports
│
├── AUTOMATION & ALERTS
│   ├── Alert Rules
│   ├── Notification Center
│   ├── Escalation Policies
│   └── Scheduled Reports
│
├── SYSTEM
│   ├── Users
│   ├── Roles
│   ├── Permissions
│   ├── API / Connections
│   ├── Diagnostics
│   ├── Backup / Restore
│   ├── Updates
│   ├── Settings
│   └── Audit Log
│
└── HELP
    ├── Keyboard Shortcuts
    ├── About
    └── Diagnostics Report
```

---

# 5. DESKTOP UI / VISUAL DIRECTION

## Theme

**Abyssal Ocean × Antique Pirate Chart × Brass Instrument × Treasure Command Room**

Không dùng kiểu SaaS six-card thông thường.

### Main composition

```text
┌────────────────────────────────────────────────────────────────┐
│ OCEANFORGE                            Sync ●   User   ⚙        │
├───────────────┬────────────────────────────────────────────────┤
│               │                                                │
│  NAVIGATION   │              TREASURE MAP                     │
│               │        +----------------------+                │
│  ◉ Overview   │        |   SEA / ISLANDS     |                │
│  ◈ Live       │        |    • •      •       |                │
│  ◇ Fleet      │        |         ●           |                │
│  ⌖ Map        │        |   •             •   |                │
│               │        +----------------------+                │
│  Accounts     │                                                │
│  Inventory    │   Online  Offline  Stale  Total               │
│  History      │                                                │
│               │   ┌────────┐ ┌────────┐ ┌────────┐            │
│  Analytics    │   │ ONLINE │ │  SEA   │ │ ALERTS │            │
│  Reports      │   └────────┘ └────────┘ └────────┘            │
│               │                                                │
│  System       │  Activity timeline / charts / events           │
│  Settings     │                                                │
│               │                                                │
└───────────────┴────────────────────────────────────────────────┘
```

## Color tokens

```text
Ocean-950  #06121A
Ocean-900  #0A1C27
Ocean-800  #0E2936
Ocean-700  #123847
Abyss      #02070B
Chart      #152A2B
Brass      #C8A65A
Gold       #E1BE67
Seafoam    #68C6C0
Signal     #7ED7EA
Warning    #D5A64E
Danger     #C66B63
Text       #E4ECEE
Muted      #81949A
Border     #27414A
```

### Visual rules

- Dark-first.
- Brass chỉ dùng làm accent.
- Cyan/seafoam cho telemetry.
- Red/amber chỉ dành cho trạng thái cảnh báo.
- Không gradient quá mạnh.
- Không glow toàn màn hình.
- Không card overload.
- Bảng dữ liệu phải ưu tiên readability.
- Map là visual anchor của app.

---

# 6. CÁC MÀN HÌNH CHÍNH

## 6.1 Login

- Ocean background.
- Compass / brass ornament.
- Email/username.
- Password.
- Remember device.
- Sign in.
- Register.
- Reset password.
- Server status.
- Version.

## 6.2 Registration

- Display name
- Email
- Password
- Confirm password
- Terms
- Verification status
- Anti-abuse rate limit
- Device/session registration

Không yêu cầu Roblox password.

## 6.3 Dashboard

### KPI

- Total Accounts
- Online
- Stale
- Offline
- Active Sessions
- Average Level
- Total Beli
- Total Fragments
- Alerts
- Data freshness

### Charts

- Accounts by status.
- Online sessions over time.
- Average level over time.
- Beli trend.
- Fragments trend.
- Material changes.
- Inventory changes.
- Heartbeat latency.
- Daily active tracked accounts.
- Data ingestion rate.
- API latency.
- Error rate.

## 6.4 Fleet

Table virtualization bắt buộc.

Columns:

```text
Status
Account
Alias
Level
Sea
Race
Fruit
Sword
Gun
Style
Beli
Fragments
Last Seen
Latency
Tags
Alerts
```

Filters:

```text
Search
Sea
Status
Level range
Race
Race version
Fruit
Sword
Gun
Fighting style
Accessory
Material
Last seen
Tag
Group
Saved view
```

## 6.5 Account Detail

Ưu tiên drawer bên phải thay vì modal ở giữa.

Tabs:

```text
Overview
Character
Loadout
Inventory
Materials
Session
Telemetry
History
Snapshots
Diff
Alerts
Notes
Audit
```

## 6.6 Live Monitor

Realtime event stream:

```text
17:31:02 ACCOUNT_ONLINE
17:31:05 SNAPSHOT_RECEIVED
17:31:08 LEVEL_CHANGED
17:31:15 MATERIAL_CHANGED
17:31:21 HEARTBEAT_DELAY
```

## 6.7 Map

Layers:

```text
Sea I
Sea II
Sea III
Islands
Accounts
Routes
Grid
Compass
Fog
Selection
Stale zones
Alert zones
```

Map marker:

```text
Online  = cyan center + brass ring
Offline = muted gray
Warning = amber
Critical = red
Selected = gold halo
```

## 6.8 Analytics

Có date range:

```text
24h
7d
30d
90d
Custom
```

Có compare:

```text
Account vs Account
Group vs Group
Period vs Period
Sea vs Sea
```

## 6.9 Reports

Export:

```text
CSV
JSON
Excel
PDF
```

Report types:

```text
Account report
Session report
Inventory report
Material report
Activity report
Alert report
System report
```

## 6.10 Settings

Sections:

```text
General
Appearance
Performance
Notifications
Telemetry
Map
Analytics
Data retention
Backup
Security
Accounts
Roles
Network
Proxy
Updates
Diagnostics
Keyboard shortcuts
```

---

# 7. TÍNH NĂNG KHÁC BIỆT

## 7.1 Account Time Machine

Chọn một mốc thời gian và xem account ở trạng thái gần mốc đó.

```text
Today 17:30
◀──────────────●────────────▶
```

So sánh:

```text
Level: 2400 → 2450
Beli: +12.4M
Fragments: +3,200
Fruit: unchanged
Material: +18
```

## 7.2 Account DNA

Tạo fingerprint trạng thái:

```text
Character
Loadout
Inventory
Materials
Session behavior
Heartbeat profile
```

Dùng để phát hiện thay đổi lớn bất thường.

## 7.3 Change Radar

Một màn hình chỉ hiển thị thay đổi đáng chú ý:

```text
LEVEL +1
BELI +2.5M
FRUIT CHANGED
SWORD CHANGED
RACE CHANGED
MATERIAL +10
SESSION START
SESSION END
```

## 7.4 Fleet Health Score

Score không phải đánh giá người chơi; chỉ đánh giá chất lượng telemetry:

```text
Freshness
Heartbeat stability
Error rate
Missing fields
Reconnect frequency
```

## 7.5 Stale Detector

Tự phân loại:

```text
ONLINE
STALE
OFFLINE
UNKNOWN
```

Threshold configurable.

## 7.6 Anomaly Timeline

Không tự kết luận gian lận.

Chỉ phát hiện thống kê bất thường:

```text
Spike in Beli
Sudden loadout change
Unusually frequent reconnect
Missing telemetry fields
Large state delta
```

## 7.7 Snapshot Diff Studio

Chọn:

```text
Snapshot A
Snapshot B
```

Sau đó hiển thị:

```text
Added
Removed
Changed
Unchanged
```

## 7.8 Material Vault

Tổng hợp materials của toàn bộ fleet:

```text
Material
Total
Average/account
Top accounts
Last changed
Trend
```

## 7.9 Alert Rule Designer

Ví dụ:

```text
WHEN account status changes to OFFLINE
AND offline duration > 5m
THEN notify
```

Hoặc:

```text
WHEN material count changes
THEN create event
```

## 7.10 Saved Views

Ví dụ:

```text
My Active Fleet
High Level
Sea III
Recently Offline
Needs Attention
Material Collectors
```

## 7.11 Command Palette

`Ctrl + K`

```text
Open Dashboard
Find Account
Open Fleet
Open Map
Create Snapshot
Export Report
Run Diagnostics
Switch Profile
Open Settings
```

## 7.12 Focus Mode

Ẩn navigation phụ để map + telemetry chiếm gần toàn màn hình.

## 7.13 Presentation Mode

Cho dashboard trên màn hình lớn.

## 7.14 Low Bandwidth Mode

Giảm:

- realtime frequency
- chart refresh
- map redraw
- image loading

## 7.15 Data Freshness Overlay

Mọi panel hiển thị:

```text
LIVE
12s ago
2m ago
STALE
OFFLINE
```

Đây là khác biệt lớn về khả năng đọc dữ liệu.

---



---

# 92. APPENDIX — IMPLEMENTATION ORDER SUMMARY

```text
FIRST:
Repository → Domain → DB → Application → Auth → API

THEN:
Desktop shell → Design system → Dashboard → Fleet → Account detail

THEN:
Telemetry → Worker → Redis → Realtime → History → Analytics → Map

LAST CORE:
Alerts → Reports → Backup/Restore → Diagnostics

BEFORE RELEASE:
Tests → Security → Performance → Packaging → CI/CD → Staging → RC

RELEASE:
Sign → Checksum → Attest → Publish → Stage rollout → Monitor

AFTER RELEASE:
Observe → Patch → Maintain → Deprecate → EOL
```

## Three rules to remember

```text
1. DATA FIRST
2. CONTRACTS SECOND
3. POLISH LAST
```

---

# 93. APPENDIX — FIRST 10 IMPLEMENTATION COMMITS

Khuyến nghị lịch sử Git ban đầu:

```text
chore: initialize repository and solution
chore: add dotnet toolchain and analyzers
feat(domain): add tracked account model
feat(domain): add telemetry model
feat(db): add initial mysql schema and migration
feat(application): add account use cases
feat(auth): add user authentication and session model
feat(api): expose v1 account endpoints
feat(desktop): create avalonia shell and navigation
feat(ci): add pull request build and test workflow
```

Mỗi commit nên build được hoặc, với migration/structural commit, phải giải thích rõ dependency trong PR.

---

# 94. APPENDIX — WHAT TO NEVER POST TO GITHUB

```text
passwords
refresh tokens
access tokens
cookies
private keys
certificate private keys
production connection strings
SMTP passwords
cloud access keys
MySQL root passwords
Redis passwords
backup archives containing secrets
```

Nếu một secret đã lỡ commit:

```text
1. Revoke/rotate immediately
2. Remove from current history as appropriate
3. Audit usage
4. Rebuild affected artifacts
5. Document incident
```

---

# 95. APPENDIX — MAINTAINABILITY SCORECARD

Mỗi release lớn tự chấm:

| Area | Green condition |
|---|---|
| Architecture | Không có forbidden dependency |
| Domain | Business rules nằm trong domain/application |
| Persistence | Migration đầy đủ |
| API | Versioned + error contract ổn định |
| UI | ViewModel mỏng |
| Tests | Core logic có coverage tốt |
| Security | Không secret + scan sạch |
| CI/CD | Release reproducible |
| Operations | Metrics/logging/alerts hoạt động |
| Data | Backup restore đã thử |
| Docs | ADR + release notes cập nhật |

Nếu 2 release liên tiếp một area là Red, phải tạo technical-debt issue có owner.

---

# 99. V4 — C# FRAMEWORK / LIBRARY MATRIX (CÁC LỰA CHỌN THỰC TẾ)

Không thể liệt kê mọi framework .NET từng tồn tại; bảng này so sánh các lựa chọn phù hợp nhất cho yêu cầu cụ thể: Windows + Linux desktop, giao diện nhiều bảng/map/chart, app telemetry hoạt động lâu dài, có thể bổ sung client mobile riêng. RAM/CPU dưới đây là **nhận định kiến trúc định tính**, không phải benchmark định lượng.

## 99.1 Desktop UI framework

| Framework | Windows | Linux desktop chính thức | Fit với OceanForge | Đánh đổi |
|---|---|---|---|---|
| **Avalonia UI + XAML** | Có | Có, theo OS/distro support tier | **Lựa chọn chốt** để giữ một UI codebase và custom theme | Phải QA đúng distro, rendering, accessibility, GPU fallback và native integration |
| WPF | Có | Không | Tốt nếu sản phẩm chỉ Windows và cần hệ control trưởng thành | Không đáp ứng mục tiêu Windows + Linux với một UI codebase |
| WinUI 3 | Có | Không | Rất phù hợp app Windows native/Fluent | Không phải cross-platform Linux framework |
| Windows Forms | Có | Không | Tool nội bộ đơn giản, form-centric | Không phải lựa chọn ưu tiên cho UI rich map/animated/dense data đa nền tảng |
| .NET MAUI | Windows + mobile/macOS theo support policy | Không trong danh sách desktop target chính thức hiện tại | Hữu ích nếu mục tiêu trọng tâm là Android/iOS + Windows | Không dùng làm nền desktop Linux cho V4; có thể cân nhắc **client Android riêng** |
| Uno Platform | Có và nhiều target qua platform-specific backends | Có qua các target được hỗ trợ | Phương án thay thế nếu có yêu cầu Uno/WinUI-style stack cụ thể | Tăng số lựa chọn/platform nuances; chỉ chọn sau prototype so sánh thật |
| Blazor Hybrid/WebView | Tùy host | Phụ thuộc host | Hợp UI thiên web/đội có sẵn web components | WebView có chi phí process/memory riêng; phải benchmark; không chọn mặc định cho app cần kiểm soát footprint |
| Electron-style shell + C# API | Có | Có | Dễ chia sẻ UI web nếu team web mạnh | UI không thuần C#; thường có Chromium/Node footprint lớn hơn native-oriented options; không khớp yêu cầu hiện tại |

Nguồn nền tảng chính thức: [Avalonia supported platforms](https://docs.avaloniaui.net/docs/supported-platforms), [WPF overview](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview/), [WinUI 3](https://learn.microsoft.com/en-us/windows/apps/winui), [.NET MAUI supported platforms](https://learn.microsoft.com/en-us/dotnet/maui/supported-platforms?view=net-maui-10.0). Hãy kiểm tra matrix đúng version trước release; chữ “Linux supported” không đồng nghĩa mọi distro, Wayland compositor và kiến trúc CPU được QA như nhau.

## 99.2 Foundation và data libraries

| Concern | Khuyến nghị V4 | Vì sao / lưu ý |
|---|---|---|
| MVVM | CommunityToolkit.Mvvm | Ít ceremony, source generators/commands; không đưa business logic vào ViewModel |
| DI/hosting | Microsoft.Extensions.Hosting + DI + Options | Lifetimes rõ ràng, cancellation và background services |
| HTTP | Typed clients / `IHttpClientFactory` + timeout/cancellation/resilience policy | Không tạo client/socket lặp trong loop; phải tránh retry mutation không idempotent |
| Local store | SQLite + `Microsoft.Data.Sqlite` hoặc EF Core SQLite nếu cần ORM | Cache/offline store; provider/database migration riêng |
| Server persistence | EF Core + MySQL provider đã kiểm chứng tương thích | Migrations, transactional operations; test exact provider/version |
| Hot-path query | Dapper chỉ tại các query được profile | Không chuyển mọi query sang Dapper chỉ vì nghĩ sẽ nhanh hơn |
| Realtime | SignalR client/server hoặc WebSocket abstraction | Gửi event/delta; reconnect xong resync theo cursor/version |
| Backpressure | `System.Threading.Channels` bounded | Giới hạn buffer; phân biệt render updates với event bền vững |
| Logging | `ILogger` và provider Serilog khi có nhu cầu | Tránh hai pipeline log trùng nhau; redaction bắt buộc |
| Validation | Một validation approach thống nhất | Không validate nửa ở UI, nửa khác nhau ở API; server vẫn là authority |
| Charts/Map | Chọn một lib map/chart cho prototype rồi benchmark | Không chồng nhiều chart engines; kiểm tra license, memory, performance, accessibility |
| Test | xUnit + integration test + UI/E2E test | Dữ liệu sample lớn, offline/conflict/reconnect phải test tự động |
| Diagnostics | OpenTelemetry/metrics cho API/Worker + .NET counters/dumps | Tách telemetry sản phẩm khỏi telemetry diagnostics, scrub PII |

### 99.3 Lựa chọn RAM/CPU không thể suy ra chỉ từ tên framework

- Avalonia vs WPF vs WinUI không có một con số RAM “đúng” cho mọi app. Font/rendering, số control hiện diện, ảnh, map layer, binding, allocations, chart data, data virtualization và workload mạng mới quyết định kết quả cuối.
- So sánh công bằng phải là prototype có cùng màn hình Fleet, cùng 1.000/10.000 records, cùng map assets, cùng chart window, cùng OS/scale, build Release và cùng tool đo.
- Nếu Android APK là yêu cầu thật, hãy tạo client Android project riêng (Avalonia Android hoặc .NET MAUI sau prototype), dùng chung OpenAPI/contracts và backend; không ép installer desktop vào APK, và không chọn .NET MAUI để đáp ứng Linux desktop.
- Chốt package versions qua central package management; không dùng wildcard, không để version “floating” trong stable release; nâng major framework chỉ qua ADR + CI matrix + regression/performance test.

---

# 100. V4 — LOCAL DB VS CLOUD DB VS HYBRID: PHẢN BIỆN VÀ QUYẾT ĐỊNH

## 100.1 So sánh ba mô hình

| Tiêu chí | Chỉ local | Chỉ cloud/server | **Hybrid (đề xuất)** |
|---|---|---|---|
| Hoạt động khi mất mạng | Tốt nếu mọi dữ liệu/tính năng ở local | Gần như không cho nghiệp vụ cần server | Đọc cache và ghi các thao tác offline được phép; phần server-authoritative vẫn cần mạng |
| Nhiều thiết bị/người dùng | Khó; tự xây sync/backup | Tốt | Tốt sau khi sync được thiết kế/test đúng |
| Độ trễ khi xem dữ liệu gần đây | Thấp | Phụ thuộc mạng/API | Thấp với cache; freshness phải hiện rõ |
| Nguồn dữ liệu chuẩn | Máy hiện tại | Server | MySQL server; SQLite là cache/outbox/draft có ranh giới rõ |
| Rủi ro dữ liệu | Mất máy/ổ đĩa; backup do user chịu trách nhiệm | Mất mạng/incident server; backup server bắt buộc | Cả hai loại rủi ro; cần migration, conflict handling, queue quota và restore test |
| Độ phức tạp | Thấp lúc đầu | Trung bình | **Cao nhất**: sync, conflict, idempotency, local schema, support |
| Bảo mật | Data nằm trên endpoint; cần OS permissions/encryption policy | API/auth/server hardening | Phải bảo vệ cả local cache và cloud; không lưu secret thừa |
| Fit OceanForge | Chỉ phù hợp tool cá nhân/offline đơn máy | Đơn giản hơn nếu luôn cần online | Phù hợp yêu cầu user muốn app mở được khi mất mạng và đồng bộ khi có mạng |

## 100.2 Local-only — phản biện

**Điểm mạnh:** chạy không cần internet, không có chi phí vận hành server cho nghiệp vụ cục bộ, truy cập file/database nhanh.

**Điểm yếu:** backup/migration/restore phụ thuộc máy người dùng, chia sẻ nhiều thiết bị/nhóm phức tạp, dữ liệu telemetry từ nhiều producers không tự thành nguồn sự thật thống nhất, app mất máy có thể mất dữ liệu.

**Kết luận:** không đủ nếu mục tiêu là quản lý tập trung, RBAC, audit và đồng bộ nhiều thiết bị; vẫn nên có local SQLite trong kiến trúc hybrid.

## 100.3 Cloud-only — phản biện

**Điểm mạnh:** một source of truth, RBAC/audit central, cập nhật dữ liệu nhiều thiết bị dễ hơn, backup và quan sát tập trung.

**Điểm yếu:** mất internet thì nhiều tác vụ dừng; latency mạng ảnh hưởng UX; backend/database phải có monitoring, backup/restore, protection và chi phí vận hành.

**Kết luận:** hợp nếu user chấp nhận online-required. Vì OceanForge yêu cầu offline-capable, cloud-only không đáp ứng trọn yêu cầu.

## 100.4 Hybrid — phản biện khó nhất

**Điểm mạnh:** trải nghiệm mở nhanh, có last-known state, draft/offline queue và sync lại khi mạng hồi phục.

**Chi phí/rủi ro:** duplicate sync, conflict, đồng hồ máy lệch, schema local khác schema server, local disk đầy, app bị force-close, user logout trong lúc pending, cursor hết hạn, nhiều thiết bị cùng sửa và user tưởng dữ liệu stale là LIVE.

**Kết luận:** chọn hybrid nhưng làm theo vertical slice nhỏ. Đừng xây “mọi thứ offline” ở release đầu. Bắt đầu bằng cache đọc + preferences/drafts; sau khi sync contract/test ổn mới mở offline mutation cho use case cụ thể.

## 100.5 Database ownership cuối cùng

```text
Desktop SQLite:
  - Read cache / recent account working set
  - SyncOutbox / SyncCursor / SyncConflict
  - Local preferences / saved views khi phù hợp
  - Report drafts / bounded offline events

MySQL 8.4 behind API:
  - User/workspace/account registry
  - Auth/session metadata/roles/permissions
  - Accepted telemetry/events/snapshots
  - Audit/report metadata/alert rules
  - Server sync versions and deduplication keys

Redis (optional):
  - cache, rate-limit counters, transient presence/fan-out
  - not authoritative DB; not the only copy of durable mutations
```

Không tạo MySQL local service cạnh desktop chỉ để dùng chung SQL; tăng footprint, config, privilege, upgrade và support burden. Không đặt SQLite trên network share. Không cho UI/desktop kết nối trực tiếp tới cloud MySQL.

---

# 101. V4 — SYNC ALGORITHM / DATA CONTRACT

## 101.1 Event envelope

Mọi telemetry/mutation cần sync phải có contract có version:

```json
{
  "eventId": "stable-guid",
  "sourceId": "authorized-producer-id",
  "sourceSequence": 123456,
  "schemaVersion": 1,
  "occurredAtUtc": "2026-10-09T00:00:00Z",
  "observedAtUtc": "2026-10-09T00:00:01Z",
  "accountId": "oceanforge-account-id",
  "eventType": "AccountStateChanged",
  "payload": {},
  "payloadHash": "sha256-of-canonical-payload"
}
```

Ví dụ chỉ minh họa shape; field/ID thật phải được chuẩn hóa trong `OceanForge.Contracts`. Timestamp client không thay thế server receive time. Giới hạn payload size/depth, event type allowlist và schema version; server validate lại mọi thứ.

## 101.2 Algorithm: offline write + online delivery

```text
Producer/input
  → validate local shape/permission
  → transaction: insert LocalEvent (if allowed) + insert SyncOutbox
  → commit local transaction
  → show Pending sync (not Synced)

Sync worker
  → single active loop per account/workspace
  → pick bounded batch in sequence-safe order
  → HTTPS with idempotency keys
  → server authz + rate limit + schema validation
  → MySQL transaction: dedupe + persist event + write server outbox
  → server returns per-item accepted/conflict/rejected
  → client transaction: acknowledge accepted, mark conflicts, advance safe cursor
  → update UI snapshot by delta; never reload whole fleet
```

Delivery model thực tế là **at-least-once + idempotent processing**: có thể gửi cùng event nhiều lần nhưng server chỉ lưu tác động một lần. Không quảng cáo “exactly-once” nếu implementation là retry qua mạng.

## 101.3 Retry, ordering, backpressure

- Retry network timeout, 408/429 và 5xx theo policy; tuân thủ `Retry-After` khi có, exponential backoff và full jitter, có max delay/attempt budget.
- Không retry mã lỗi validation/authorization như lỗi mạng; đưa vào conflict/error center để người dùng xử lý.
- Dùng request timeout, cancellation và concurrency limit cho uploads; không tạo task mỗi event vô hạn.
- Theo dõi `outbox_count`, `oldest_pending_age`, `retry_count`, `sync_success_rate`, `conflict_count`, `last_success_utc`.
- Nếu disk gần đầy/outbox vượt quota, hiện cảnh báo và dừng nhận tác vụ có nguy cơ mất dữ liệu; không tự drop im lặng.
- Khi reconnect, randomize retry start và sync theo batch; pull/push cursor để tránh resync toàn bộ dữ liệu mỗi lần.
- Lưu server version/cursor trong cùng transaction với delta apply để crash không gây cursor nhảy trước data.
- Server dedupe key có unique constraint; kiểm tra race nhiều request đồng thời bằng integration test.

## 101.4 API contract cho sync

Nên có API versioned tương tự:

```text
GET  /api/v1/sync/pull?cursor=...&limit=...
POST /api/v1/sync/push-batch
GET  /api/v1/sync/status
POST /api/v1/sync/conflicts/{id}/resolve   # online-only, authorized
```

Response cho từng mutation gồm `mutationId`, `status`, `serverVersion`, `errorCode` và conflict metadata tối thiểu. `limit`, byte size, số record và request duration phải bounded. Đặt cursor opaque để server có thể đổi implementation.

---

# 102. V4 — BACKEND/JOBS/REDIS: GIẢM PHỨC TẠP ĐÚNG LÚC

## 102.1 Vấn đề của “dùng mọi thứ từ ngày đầu”

MySQL + Redis cache + Redis queue + Worker + SignalR + local SQLite + analytics aggregation + installer updater là nhiều thành phần vận hành cùng lúc. Thêm hệ thống trước khi đo được nhu cầu làm tăng failure modes (queue backlog, Redis persistence, migration, reconnect, dead-letter, credential management) và thời gian debug.

## 102.2 Deployment sequence đề xuất

**MVP / 1–100 tracked accounts (giả định ban đầu):**

- Modular monolith codebase: `OceanForge.Api`, `OceanForge.Worker`, `OceanForge.Desktop`.
- MySQL là source of truth.
- Local SQLite cache/outbox có migration và sync test ngay từ đầu, nhưng chỉ mở những offline mutation đã được xác định.
- Dùng MySQL transaction/outbox pattern cho event cần publish sau commit; worker chạy bounded batch.
- SignalR có thể truyền notification/delta; REST pull/resync vẫn là đường khôi phục chính.
- Không bắt buộc Redis trước khi cache/realtime/scale metrics cho thấy cần.

**Khi đã đo được bottleneck:**

- Đưa Redis vào cho cache/rate limiting/presence/fan-out nếu invalidation/HA policy rõ.
- Chỉ dùng Redis Streams/message broker làm durable queue khi đã thiết kế persistence, consumer group recovery, pending entries, retry, dead-letter và vận hành restore.
- Tách service deploy khi cần scale độc lập, isolation hoặc team ownership; không tách microservice chỉ vì solution có nhiều project.

## 102.3 Server outbox safety

Trong cùng MySQL transaction lưu event nghiệp vụ và `ServerOutbox` record. Worker đọc outbox theo batch, publish SignalR/cache invalidation và đánh dấu processed/retry. Nếu worker crash sau publish nhưng trước khi mark, event có thể publish lại; client cần dedupe event ID hoặc cập nhật state idempotent. Outbox record cần retention/cleanup sau khi chắc chắn đã xử lý.

---

# 103. V4 — PRODUCT ROADMAP ĐÃ CẬP NHẬT

## Phase sequence

| Phase | Làm trước | Gate — chưa qua không đi tiếp |
|---:|---|---|
| 00 | Product scope/non-goals + 24-screen manifest + threat boundary | Core/later modules được phân loại; không yêu cầu Roblox credential |
| 01 | GitHub/repository/toolchain/.NET 10 pinning | Build clean từ clone mới; no secret |
| 02 | Avalonia shell prototype + design tokens + benchmark shell | Windows + Linux target matrix chạy được; typography/contrast được review |
| 03 | Domain/contracts/event ID/versioning | Unit tests cho ordering, stale state, identity, idempotency |
| 04 | Server MySQL schema + migration | Clean install + upgrade migration + backup/restore smoke |
| 05 | Local SQLite schema + local migration | Crash/reopen, full disk, WAL/backup, per-user separation test |
| 06 | Application use cases + repository ports | Domain không reference UI/ORM/OS |
| 07 | API/Auth/RBAC/authorization | No desktop→MySQL; endpoint integration/security tests |
| 08 | First vertical slice: sign-in → account list → account detail | Real API, loading/error/empty/stale/offline states |
| 09 | Sync pull/read-cache + cursor | Crash mid-pull does not advance cursor without applying data |
| 10 | Sync outbox for one allowed offline mutation only | Duplicate retry safe; conflict/rejected mutation visible |
| 11 | Telemetry ingestion + idempotency + bounded worker | Burst/backpressure/restart/replay tests |
| 12 | Dashboard/Fleet virtualization/search | 1k/10k data benchmark and cancellation tests |
| 13 | Inventory/history/snapshot diff | No full-history RAM load; paging/retention tested |
| 14 | Realtime delta + resync | disconnect/reconnect storm doesn't spam requests |
| 15 | Map/chart/analytics | visual smoothness and memory budgets measured |
| 16 | Alerts/reports/backup/diagnostics | background export, cancel, restore verified |
| 17 | Launcher install/download/verify/update/repair/uninstall | clean install/update/failure/rollback/uninstall E2E by OS |
| 18 | Security hardening and dependency review | threat model, scanning, token storage, permissions, secrets |
| 19 | Performance soak and low-resource mode | budget report and no monotonic retained-heap growth in same test loop |
| 20 | Staging/release signing/checksum/provenance | build once, same artifact promotion, signature test |
| 21 | Beta/staged rollout and operations | crashes/errors/sync backlog/update failures monitored |
| 22 | V1 stable; phase-later features only after product validation | support/runbook/restore/incident procedures complete |

## First vertical slice recommended

```text
Start app
 → login with OceanForge account
 → GET account list through API
 → cache result in SQLite
 → close and restart offline
 → show cached list with “last synced at” label
 → reconnect
 → pull changes using cursor
 → verify no duplicates and no UI freeze
```

Chỉ khi slice này thành công mới mở thêm offline mutation. Đây là cách nhỏ nhất để kiểm chứng toàn bộ kiến trúc trước khi xây đủ 24 màn hình.

---

# 104. V4 — GITHUB CI/CD GATES BỔ SUNG

Thêm kiểm thử vào CI/CD hiện có:

```text
PR CI:
  - restore/build/test on Windows + Linux
  - architecture dependency tests
  - format/analyzer/dependency scan
  - SQLite clean migration + migration upgrade test
  - server migration/integration test using isolated MySQL
  - unit tests for dedupe, cursor, conflict, retry/backoff

Nightly:
  - offline/reconnect simulation
  - duplicate/out-of-order event replay
  - full-disk/local database recovery test
  - 8h soak/performance trend on scheduled runner if available
  - DB backup + restore drill in isolated environment

Release:
  - build/package each explicitly supported OS/architecture
  - install/launch/update/rollback/uninstall smoke tests
  - verify signature/hash before apply
  - generate SBOM, checksums, provenance/attestation
  - preserve previous stable manifest/package for rollback
```

Actions version, runtime SDK, NuGet dependencies and installer tool versions must be reviewed/pinned to a known release policy. Do not allow untrusted PR workflows to access production deploy/signing secrets. Staging/production receive the exact same build artifact after gates pass.

---

# 105. V4 — DEFINITION OF DONE CHO MỌI MÀN HÌNH

Không một mockup/feature nào được gọi là hoàn tất chỉ vì “trông đẹp”. Checklist:

```text
[ ] ID/name đúng với UI manifest
[ ] Design ở kích thước độc lập và DPI cần hỗ trợ
[ ] Keyboard/focus/contrast/reduced motion được kiểm tra
[ ] Loading/empty/error/offline/stale/permission-denied states có thiết kế
[ ] API/local data source và source of truth được ghi rõ
[ ] Pagination/virtualization áp dụng nếu dữ liệu có thể lớn
[ ] ViewModel chỉ điều phối state; không chứa SQL/business rule
[ ] Cancellation/dispose/timeout/retry có policy
[ ] Security/permission/log redaction có review
[ ] Unit/integration/UI tests có IDs
[ ] CPU/RAM baseline và workload đã đo nếu trang nặng
[ ] Screenshot thật từ build chạy được, khác biệt so với mockup đã review
[ ] Help/diagnostic text có thể hành động
```

## Definition of DONE cho sync

```text
[ ] Event/mutation có ID ổn định và schema version
[ ] Retry idempotent
[ ] Conflict/permanent rejection không bị loop vô hạn
[ ] Cursor chỉ commit cùng delta đã apply
[ ] App crash/restart không mất acknowledged/unacknowledged boundary
[ ] Disk full và offline lâu có xử lý
[ ] User logout/device switch không trộn local cache
[ ] Pending/failed/conflict/synced được phân biệt trên UI
```

## Definition of DONE cho launcher

```text
[ ] Download có pause/resume/retry/cancel hoặc nêu rõ giới hạn
[ ] Size estimate và free-space validation
[ ] SHA-256 + signature/provenance policy trước install
[ ] Không execute downloaded content trước verification
[ ] Fresh install / upgrade / repair / insufficient disk / no network / app-running tests
[ ] Update failure không phá local SQLite/outbox
[ ] Uninstall không xóa cloud data và giải thích local data policy
[ ] OS/package-manager uninstall path hoạt động
[ ] Result/error states được test trên từng OS target
```

---

# 106. V4 — NGUỒN KỸ THUẬT CHÍNH THỨC

Các nguồn dưới đây hỗ trợ quyết định framework/performance/database. Tính năng, support tier và phiên bản phải được kiểm tra lại khi chốt stable release.

1. Microsoft .NET lifecycle / .NET 10 LTS: <https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core> — .NET 10 hiện thuộc LTS; ngày end-of-support theo policy đang công bố là 14-11-2028.
2. Avalonia supported platforms: <https://docs.avaloniaui.net/docs/supported-platforms> — Windows/Linux có OS/distro support tiers; không tuyên bố mọi Linux đã được QA.
3. WPF overview: <https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview/> — WPF chỉ chạy Windows.
4. WinUI 3: <https://learn.microsoft.com/en-us/windows/apps/winui> — native Windows UI framework.
5. .NET MAUI supported platforms: <https://learn.microsoft.com/en-us/dotnet/maui/supported-platforms?view=net-maui-10.0> — danh sách target chính thức gồm Android/iOS/Mac Catalyst/Windows, không bao gồm Linux desktop như target được liệt kê.
6. SQLite appropriate uses: <https://www.sqlite.org/whentouse.html> — phù hợp local embedded store; khuyến nghị server/client DB khi nhiều writer đồng thời cần thông lượng cao hoặc nhiều máy truy cập trực tiếp.
7. SQLite WAL: <https://www.sqlite.org/wal.html> — readers có thể đồng thời với writer trong WAL mode nhưng chỉ có một writer tại một thời điểm; WAL cần xử lý đúng với backup và không hoạt động qua network filesystem.
8. EF Core efficient querying: <https://learn.microsoft.com/en-us/ef/core/performance/efficient-querying> — index, projection, giới hạn kết quả và pagination là các kỹ thuật hiệu quả quan trọng.
9. .NET Channels: <https://learn.microsoft.com/en-us/dotnet/core/extensions/channels> — bounded channels áp dụng backpressure; mode drop chỉ phù hợp khi semantics chấp nhận mất item.
10. Workstation/server GC: <https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/workstation-server-gc> — chọn GC theo workload client/server và benchmark.
11. Diagnose memory leaks: <https://learn.microsoft.com/en-us/dotnet/core/diagnostics/debug-memory-leak>, <https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-gcdump>, <https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-dump> — tools để đo/điều tra managed heap/object retention.
12. MySQL 8.4 documentation: <https://dev.mysql.com/doc/refman/8.4/en/> — sử dụng version track đã chốt và kiểm tra compatibility/migrations/provider theo build cụ thể.

---

# 107. FINAL V4 ACCEPTANCE

OceanForge chỉ được tuyên bố **V4 implementation-ready** khi các quyết định sau đã được thực hiện trong source code/test chứ không chỉ nằm trong tài liệu:

```text
[ ] Avalonia + .NET 10 toolchain pinning và Windows/Linux support matrix
[ ] Desktop → API only; không có client MySQL/Redis credentials
[ ] SQLite local schema/migrations + cache quota + durable outbox/cursor/conflict inbox
[ ] MySQL schema/migrations + API authz/idempotency/audit
[ ] Sync contract versioned; no false LIVE state; pending/conflict UI
[ ] Bounded queues, cancellation, batching, backoff/jitter, retention
[ ] 24 screen IDs and launcher 16 lifecycle panels tracked individually
[ ] Main UI pages independently designed, not only a collage
[ ] Installer/updater verifies signature/hash before applying
[ ] OS-native/package-manager uninstall semantics and local-data warning
[ ] CPU/RAM budget measured on named reference hardware
[ ] 8-hour soak test, reconnection/burst/full-disk/crash tests pass
[ ] CI tests migrations, sync correctness, launcher install/update/uninstall
[ ] Backup restore drill and security review passed
[ ] Release artifact signed/hashed/attested and rollback rehearsed
```

**Luật cuối:** thiết kế hình ảnh là hợp đồng thẩm mỹ; Domain/API/schema/sync contract là hợp đồng dữ liệu; tests/benchmarks là bằng chứng kỹ thuật. Không lấy hình đẹp làm bằng chứng app chạy nhẹ, không lấy cache làm bằng chứng sync an toàn, và không lấy “build thành công” làm bằng chứng installer/rollback/uninstall đã an toàn.

---

# 108. END — OCEANFORGE DESKTOP V4

```text
Design deliberately.
Persist before acknowledging.
Bound every queue and cache.
Make stale data look stale.
Measure CPU/RAM on real workloads.
Verify before install.
Never sacrifice data integrity for a smoother demo.
```
