# OCEANFORGE DESKTOP V3
## COMPLETE CROSS-PLATFORM Blox Fruits TELEMETRY & OPERATIONS CONSOLE
### Windows + Linux / .NET 10 LTS / Avalonia UI / ASP.NET Core 10 / MySQL 8.4 LTS

> **Status:** Production Architecture Blueprint — revised for maintainability, GitHub CI/CD, security, packaging, release engineering and full lifecycle.
> **Revision:** 2026-10-07
> **Basis:** Revised from the uploaded OCEANFORGE_DESKTOP_MASTER_SPEC_V2(1).md; core product scope and UI direction retained, architecture/delivery lifecycle expanded.

---

# 0. ĐIỀU KHOẢN KIẾN TRÚC KHÔNG ĐƯỢC PHÁ VỠ

Đây là các quy tắc phải được coi như **architecture contract**.

```text
1. Desktop không kết nối MySQL trực tiếp.
2. MySQL là source of truth của dữ liệu nghiệp vụ phía server.
3. Redis chỉ là cache / queue / realtime support, không phải source of truth.
4. Domain không phụ thuộc Avalonia, ASP.NET, MySQL, Redis hoặc OS.
5. UI không chứa SQL, EF Core query hoặc business rule.
6. Application điều phối use case; Domain giữ business rule.
7. Infrastructure hiện thực các port/interface của Application/Domain.
8. Mọi schema change phải đi qua migration.
9. Mọi release phải reproducible từ Git tag/commit.
10. Không commit secret vào repository.
11. Pull Request phải pass build/test/security checks trước khi merge.
12. Release artifact phải có checksum và provenance/attestation.
13. Update phải verify integrity trước khi cài.
14. Backup phải có restore drill, không chỉ tạo file backup.
15. Telemetry phải có retention, deduplication và rate limit.
16. Không log password, token, cookie hoặc secret.
17. Mọi thay đổi quyền phải có audit trail.
18. Không làm microservices tràn lan trước khi cần.
19. Không làm tính năng advanced trước khi core data path ổn định.
20. Một feature chỉ DONE khi có test + logging + security + documentation.
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
| Desktop | Avalonia UI + XAML | Cross-platform UI |
| Runtime | .NET 10 LTS | Chốt LTS |
| Language | C# | Primary |
| Pattern | MVVM | Desktop presentation |
| MVVM toolkit | CommunityToolkit.Mvvm | Commands/observable state |
| API | ASP.NET Core 10 | Central API |
| Realtime | SignalR / WebSocket | Event delivery |
| DB | MySQL 8.4 LTS | Primary source of truth |
| ORM | EF Core + MySQL provider | Persistence |
| Cache | Redis | Read cache / transient state |
| Queue | Redis Streams hoặc worker queue | Async ingestion |
| Logging | Serilog | Structured logging |
| Validation | FluentValidation | Input/application validation |
| API contract | OpenAPI | Stable contract |
| Tests | xUnit + integration test stack | Automated quality |
| CI/CD | GitHub Actions | Build/test/package/release |
| Windows package | Signed MSIX hoặc signed EXE | Primary distribution quyết định ở release phase |
| Linux package | AppImage + `.deb` | Desktop distribution |
| Container | Docker | API/Worker local + server deploy |
| Monitoring | OpenTelemetry-compatible metrics/logs/traces | Observability |

`.NET 10` hiện là LTS và đang được hỗ trợ đến ngày 2028-11-14 theo chính sách hỗ trợ của Microsoft. citeturn246130search3

Avalonia hiện hỗ trợ desktop Windows và Linux; ma trận hỗ trợ thay đổi theo major release, vì vậy release manifest của OceanForge phải ghi rõ OS/architecture được QA cho từng phiên bản. citeturn246130search2

MySQL 8.4 thuộc track LTS của MySQL, phù hợp làm đường cơ sở ổn định cho dữ liệu dài hạn. citeturn246130search0

---

# 3. KIẾN TRÚC Ở MỨC HỆ THỐNG

```text
                         ┌─────────────────────────────┐
                         │       OCEANFORGE USER       │
                         └──────────────┬──────────────┘
                                        │
                              ┌─────────▼─────────┐
                              │ OceanForge Desktop │
                              │ Avalonia + MVVM    │
                              └─────────┬─────────┘
                                        │
                            HTTPS / WebSocket / JSON
                                        │
                         ┌──────────────▼──────────────┐
                         │     OceanForge API          │
                         │     ASP.NET Core 10         │
                         │ Auth / RBAC / REST / WS     │
                         └───────┬───────────┬────────┘
                                 │           │
                      ┌──────────▼───┐   ┌──▼───────────┐
                      │   MySQL 8.4  │   │    Redis     │
                      │ Source Truth │   │ Cache / Queue│
                      └──────────────┘   └──────┬───────┘
                                                │
                                      ┌─────────▼─────────┐
                                      │ OceanForge Worker │
                                      │ Telemetry         │
                                      │ Alerts            │
                                      │ Retention         │
                                      │ Reports           │
                                      │ Analytics jobs    │
                                      └─────────┬─────────┘
                                                │
                                      ┌─────────▼─────────┐
                                      │ Valid telemetry   │
                                      │ producers/agents  │
                                      └───────────────────┘
```

### Luật triển khai

```text
Desktop → API
API → MySQL / Redis
Worker → MySQL / Redis
Telemetry producer → API ingestion endpoint

KHÔNG CÓ:
Desktop → MySQL
Desktop → Redis
Desktop → internal worker port
```

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
│   ├── OceanForge.Persistence/
│   ├── OceanForge.Infrastructure/
│   ├── OceanForge.Security/
│   ├── OceanForge.Telemetry/
│   ├── OceanForge.Analytics/
│   ├── OceanForge.Realtime/
│   ├── OceanForge.Reporting/
│   ├── OceanForge.Diagnostics/
│   ├── OceanForge.Notifications/
│   ├── OceanForge.Presentation/
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

## 10.2 Redis không phải source of truth

Redis dùng cho:

```text
Cache
Queue
Short-lived locks
Rate limiting
Realtime fan-out
Temporary aggregates
```

## 10.3 Database rules

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

## 10.4 Migration rules

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

OIDC cho phép workflow nhận credential ngắn hạn thay vì phải lưu credential cloud dài hạn; GitHub yêu cầu `id-token: write` cho job cần mint OIDC token. citeturn813214search5turn813214search8

## Production

```text
External secret manager
or
Cloud secret store
```

---

# 16. TELEMETRY ARCHITECTURE

```text
Telemetry producer
      ↓
HTTPS / WSS
      ↓
Auth
      ↓
Schema validation
      ↓
Rate limit
      ↓
Normalize
      ↓
Deduplicate
      ↓
Queue
      ↓
Worker
      ↓
MySQL
      ↓
Domain event
      ↓
SignalR
      ↓
Desktop
```

## Không gửi mỗi frame

Chỉ gửi:

```text
heartbeat
state change
periodic snapshot
batch event
```

## Ingestion idempotency

Mỗi event nên có:

```text
eventId
sourceId
sourceVersion
timestamp
hash/idempotency key
```

Worker phải xử lý event duplicate mà không nhân đôi dữ liệu.

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

# 18. CACHING

Cache levels:

```text
L1 Desktop memory
L2 Redis
L3 MySQL
```

### Cache rule

Mỗi cache entry phải có:

```text
key
TTL
version
owner
invalidation strategy
```

Không cache dữ liệu nhạy cảm vô thời hạn.

---

# 19. WORKER ARCHITECTURE

`OceanForge.Worker` chịu trách nhiệm tác vụ nền.

```text
TelemetryIngestionJob
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
Idempotent
Retry with backoff
Dead-letter strategy khi cần
Cancellation-aware
Structured logging
Metrics
Health checks
```

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

# 24. OFFLINE / CACHE / SYNC

Desktop phải hoạt động rõ ràng khi mất mạng.

```text
ONLINE
  ↓
NETWORK LOST
  ↓
OFFLINE MODE
  ↓
SHOW LAST KNOWN DATA + FRESHNESS
  ↓
NETWORK RESTORED
  ↓
SYNC / RESYNC
```

Không giả vờ rằng dữ liệu cũ là dữ liệu live.

Hiển thị:

```text
LIVE
12s ago
2m ago
STALE
OFFLINE
```

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

## PHASE 04 — Persistence + MySQL

```text
DbContext
Configurations
Repositories
Migrations
Seeds
Indexes
```

Gate:

```text
[ ] Migration clean install
[ ] Migration upgrade test
[ ] Backup/restore smoke test
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

GitHub artifact attestations tạo build provenance có thể xác minh nguồn gốc của artifact. GitHub hiện cung cấp `actions/attest@v4`; job cần attestation phải có các quyền thích hợp như `id-token: write`, `attestations: write` và có thể cần `artifact-metadata: write` tùy flow. citeturn137391search0turn137391search3

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

OIDC nên được ưu tiên khi deploy lên cloud thay cho long-lived static credentials. citeturn813214search5turn813214search8

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

GitHub artifact attestations được thiết kế để chứng minh artifact được tạo từ repository/workflow/commit nào. citeturn246130search1turn137391search0

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

Avalonia hiện hỗ trợ Linux desktop, nhưng mức hỗ trợ thay đổi theo distro/version; cần đóng gói test matrix rõ cho release thay vì tuyên bố “mọi Linux”. citeturn246130search2

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

# 51. PERFORMANCE ENGINEERING

Đo trước khi tối ưu.

Metrics:

```text
Startup time
Memory
CPU
UI responsiveness
DB latency
API p95/p99
Queue depth
Telemetry throughput
Realtime delivery latency
```

## Fleet

Bắt buộc:

```text
Virtualization
Pagination
Debounce search
Incremental loading
Partial updates
```

---

# 52. UI PERFORMANCE

Không làm:

```text
Observable collection 100k rows
+ full redraw
```

Ưu tiên:

```text
Virtualized DataGrid
Incremental updates
Memoized aggregates
Background processing
UI-thread minimal work
```

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

# 82. SCALE PLAN

## Phase A — 1–100 accounts

```text
Single API
Single Worker
MySQL
Redis
```

## Phase B — 100–1,000

```text
Connection pool tuning
Indexes
Read aggregates
Worker concurrency
Cache tuning
```

## Phase C — 1,000–10,000+

```text
Queue partitioning if needed
Dedicated workers
Read models
Archive
Partitioning
Horizontal API scaling
```

Không scale bằng cách viết lại toàn bộ hệ thống.

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
| MySQL down | Read cached data + warn; write queue/disable write tùy policy |
| Redis down | Degrade cache/realtime; API core vẫn hoạt động nếu có đường fallback |
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

# 86. FINAL MASTER ROADMAP

```text
STEP 01  Repository + GitHub rules
STEP 02  Toolchain lock
STEP 03  Domain
STEP 04  MySQL schema + migration
STEP 05  Application
STEP 06  Auth + RBAC
STEP 07  API
STEP 08  Desktop shell
STEP 09  Design system
STEP 10  Dashboard
STEP 11  Fleet
STEP 12  Account detail
STEP 13  Telemetry
STEP 14  Worker + Redis
STEP 15  Realtime
STEP 16  Snapshots + history
STEP 17  Analytics
STEP 18  Map
STEP 19  Alerts
STEP 20  Reports
STEP 21  Backup/restore
STEP 22  Diagnostics
STEP 23  Testing hardening
STEP 24  Security hardening
STEP 25  Performance
STEP 26  Windows packaging
STEP 27  Linux packaging
STEP 28  CI/CD
STEP 29  Staging
STEP 30  Release candidate
STEP 31  Signing + checksums + attestation
STEP 32  Production
STEP 33  Monitoring
STEP 34  Maintenance
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

V3 mở rộng các phần còn thiếu cho production lifecycle nhưng không đổi product direction chính.

## Current external verification used for V3

- .NET 10 hiện là LTS; Microsoft liệt kê bản patch hiện hành và ngày kết thúc support trong policy chính thức. citeturn246130search3
- Avalonia hỗ trợ Windows và Linux, nhưng support tier phụ thuộc OS/version; release phải có target matrix rõ ràng. citeturn246130search2
- MySQL 8.4 thuộc LTS track. citeturn246130search0
- GitHub cung cấp OIDC cho workflow và artifact attestations để tăng supply-chain/build provenance security. citeturn813214search5turn137391search0

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

# 90. FINAL DECISION

OceanForge V3 được coi là **đã chốt kiến trúc** khi:

```text
Desktop = Avalonia + MVVM
Backend = ASP.NET Core 10
DB = MySQL 8.4 LTS
Cache/Queue = Redis
Realtime = SignalR/WebSocket
CI/CD = GitHub Actions
Packaging = Windows MSIX/signed EXE + Linux AppImage/DEB
Source control = GitHub protected main + PR
Release = SemVer + signed artifacts + checksums + attestation
Deployment = build once → staging → approve → production
Security = RBAC + secret management + least privilege + audit
Operations = logs + metrics + traces + backup + restore drill
Maintenance = migrations + tests + ADR + dependency governance
```

**Đây là baseline để bắt đầu code. Không thêm framework/platform mới nếu chưa có ADR ghi rõ lý do.**

---

# 91. APPENDIX — RETAINED V2 PRODUCT/UI SPECIFICATION

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

# 96. END

**OceanForge Desktop V3 — Master Engineering Specification**

```text
Build correctly.
Release reproducibly.
Secure by default.
Observe everything that matters.
Keep the domain stable.
Change infrastructure without rewriting the product.
```
