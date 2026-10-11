# OCEANFORGE DESKTOP — GITHUB CI/CD, ĐÓNG GÓI VÀ PHÁT HÀNH

**Tài liệu:** Engineering / DevOps Runbook  
**Phiên bản tài liệu:** 1.0  
**Cơ sở kiến trúc:** OceanForge Desktop V4 — Hybrid & Performance Specification  
**UI:** Avalonia UI + XAML + MVVM  
**Runtime:** .NET 10 LTS  
**Backend:** ASP.NET Core 10  
**Database nghiệp vụ:** MySQL 8.4 LTS  
**Desktop local store:** SQLite  
**CI/CD:** GitHub Actions  
**Nền tảng đích dự kiến:** Windows x64; Linux x64 theo distro/support matrix đã QA  
**Trạng thái:** Quy trình chuẩn và mẫu cấu hình; cần thay các đường dẫn placeholder theo repository thật và chạy QA trước khi bật phát hành production.

---

## 0. MỤC TIÊU VÀ QUY TẮC ĐỌC TÀI LIỆU

Tài liệu này là runbook từng bước cho toàn bộ vòng đời mã nguồn đến bản phát hành OceanForge:

```text
Product/Architecture decisions
  -> GitHub repository & branch rules
  -> Local build/test
  -> Pull request CI
  -> Merge to main
  -> Nightly/security checks
  -> Release tag
  -> Build exact commit
  -> Test package
  -> Package by OS/architecture
  -> Code signing / integrity metadata / SBOM / provenance
  -> Draft release
  -> Manual release approval
  -> Staging / production deployment where applicable
  -> Update manifest and distribution
  -> Monitor / rollback / hotfix
```

### 0.1 Các nguyên tắc không được phá vỡ

1. **Build một lần, kiểm thử artifact đó, rồi promote chính artifact ấy.** Không build một bộ cho staging và build lại từ source cho production.
2. **PR CI không có production secret.** Không cho mã chưa review truy cập credentials ký mã, API production, cơ sở dữ liệu hoặc cloud deploy.
3. **Default-deny permissions** trong GitHub Actions. Mỗi job chỉ có đúng quyền cần thiết.
4. **Không phát hành stable nếu chưa ký/xác minh gói theo policy đã chốt.** Checksum không thay thế chữ ký; attestation không thay thế chữ ký Authenticode/MSIX.
5. **Không tự động migrate production database trong mỗi startup của Desktop.** Migration server phải là một job triển khai riêng có backup, review và kiểm tra.
6. **Không xóa local SQLite/outbox chỉ vì ứng dụng được cập nhật/gỡ cài đặt.** Quy tắc dữ liệu khi uninstall phải được người dùng thông báo và xác nhận.
7. **Không tự động rollback database chỉ vì app health check fail.** App rollback và database recovery là hai thủ tục khác nhau.
8. **Không tuyên bố hỗ trợ Windows/Linux nào chưa chạy smoke test trên hệ đó.**
9. **Không coi thành công của `dotnet build` là bằng chứng installer, updater, repair, rollback hay uninstall hoạt động.**
10. **Không đưa Roblox password, cookie, session token, API secret hoặc khóa ký vào repository, package client hay log.**

### 0.2 Tình trạng repository cần xác minh

Bộ module Get Script được cung cấp trước đây hiện gồm các file UI/UX, ViewModel và tài liệu; bản module đó chưa tự nó chứng minh đã có solution hoàn chỉnh, project `.csproj`, API, database migrations hoặc pipeline có thể chạy. Vì vậy:

- Trước khi copy workflow vào repository, phải kiểm tra `git ls-files`, `find . -name '*.sln' -o -name '*.csproj'` và chạy `dotnet --info`.
- Những đường dẫn như `OceanForge.sln`, `src/OceanForge.Desktop/OceanForge.Desktop.csproj`, `src/OceanForge.Api/OceanForge.Api.csproj` trong tài liệu là **giá trị mẫu**. Hãy thay bằng đường dẫn thực tế.
- Workflow dưới đây là mẫu khởi điểm đầy đủ về luồng. Chưa thể gọi là “đã chạy thành công” cho repo của bạn cho đến khi file được commit, CI thực thi và các gate đều xanh.

---

## 1. MÔ HÌNH PHÁT HÀNH CHO OCEANFORGE

Tách ít nhất ba mặt hàng triển khai để không nhầm trách nhiệm:

| Phần | Artifact / deploy unit | Phát hành khi nào? | Điều cần bảo vệ |
|---|---|---|---|
| Desktop | `OceanForge.Desktop` | Theo tag desktop release | Package, ký mã, config, cache SQLite |
| Launcher / updater | `OceanForge.Launcher` hoặc OS-native installer flow | Khi launcher/updater thay đổi | Trust root, manifest, checksum, chữ ký, rollback |
| Backend API + Worker | OCI container hoặc deployment artifact | Backend release độc lập | Database credentials, cloud secrets, migrations, authorization |
| Database schema | Migration bundle | Cùng backend release khi cần | Backup, expand/contract, kiểm soát thay đổi phá vỡ |
| Get Script package | Gói `account-manager-telemetry` được ký | Theo policy module riêng | Không chạy mã tải về tùy ý; không nhúng khóa riêng vào client |

### 1.1 Quyết định package nên ghi vào ADR

Trước khi ra bản stable đầu tiên, tạo `docs/adr/ADR-XXXX-release-package-formats.md` và chọn:

- **Windows primary:** MSIX **hoặc** signed EXE installer. V4 yêu cầu chọn một đường phát hành stable chính sau prototype/QA; đừng phát hành nhiều installer cạnh tranh khi chưa có lý do.
- **Linux primary:** `.deb` cho các phiên bản Debian/Ubuntu được kiểm thử. Chỉ thêm AppImage khi build, native dependencies, desktop entry và smoke test đã tự động hóa.
- **Portable ZIP/TAR:** có thể dùng cho CI artifact và beta nội bộ; không tự coi đây là installer production, và không dùng checksum đơn lẻ thay cho xác minh nguồn gốc.
- **RID (Runtime Identifier):** tối thiểu `win-x64`, `linux-x64` nếu hai target đã được chọn. Chỉ thêm `win-arm64`/`linux-arm64` sau khi dependencies, package và máy test phù hợp.

Self-contained làm package lớn hơn nhưng không yêu cầu .NET runtime đã cài; framework-dependent nhỏ hơn nhưng cần runtime phù hợp. Mỗi RID là một build target riêng. Single-file, trimming và Native AOT **không bật mặc định** cho đến khi mọi dependency Avalonia/reflection/native library được test. Tham khảo tài liệu publish .NET: <https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview>.

### 1.2 Release channel

| Channel | Mục tiêu | Nguồn phát hành | Tự cập nhật? |
|---|---|---|---|
| `nightly` | Phát hiện lỗi sớm | Build theo lịch, không cam kết ổn định | Không được ghi đè `stable` |
| `beta` | QA theo nhóm nhỏ | Release candidate đã qua CI | Có thể yêu cầu opt-in rõ |
| `stable` | Người dùng chính thức | Tag được review, signed package, smoke-test, rollback plan | Chỉ theo manifest đã ký/xác thực |

Không quảng bá Nightly/Beta là Stable; không để manifest Nightly trỏ sang package Stable theo kiểu ghi đè mơ hồ.

---

## 2. GIAI ĐOẠN 0 — KIỂM KÊ REPOSITORY VÀ CHỐT PHẠM VI

### Bước 0.1 — Xác minh môi trường local

Chạy từ root repository:

```bash
 git status --short
 git branch --show-current
 git remote -v
 dotnet --info
 dotnet --list-sdks
 dotnet --list-runtimes
 find . -maxdepth 4 \( -name '*.sln' -o -name '*.slnx' -o -name '*.csproj' \) -print
 find .github -maxdepth 3 -type f -print 2>/dev/null
```

Trên Windows PowerShell có thể dùng:

```powershell
git status --short
git branch --show-current
git remote -v
dotnet --info
dotnet --list-sdks
Get-ChildItem -Recurse -Include *.sln,*.slnx,*.csproj | Select-Object -ExpandProperty FullName
Get-ChildItem .github -Recurse -File -ErrorAction SilentlyContinue
```

### Bước 0.2 — Không đoán đường dẫn project

Ghi các giá trị thực tế vào `docs/devops/REPOSITORY_PATHS.md`:

```text
Solution path: <đường dẫn solution thật>
Desktop project: <đường dẫn .csproj thật>
API project: <đường dẫn .csproj thật hoặc NOT CREATED>
Worker project: <đường dẫn .csproj thật hoặc NOT CREATED>
Unit test project(s): <đường dẫn thật>
Integration test project(s): <đường dẫn thật hoặc NOT CREATED>
Windows package script: <đường dẫn thật hoặc NOT CREATED>
Linux package script: <đường dẫn thật hoặc NOT CREATED>
Updater manifest schema: <đường dẫn thật hoặc NOT CREATED>
```

Không tạo release workflow hoạt động như thể package scripts đã tồn tại nếu repository chưa có chúng.

### Bước 0.3 — Tạo tài liệu DevOps

Tạo ít nhất:

```text
docs/devops/REPOSITORY_PATHS.md
docs/devops/BRANCH_AND_PR_POLICY.md
docs/devops/ENVIRONMENT_MATRIX.md
docs/devops/SECRETS_INVENTORY.md
docs/devops/RELEASE_CHECKLIST.md
docs/devops/ROLLBACK_RUNBOOK.md
docs/devops/DATABASE_MIGRATION_RUNBOOK.md
docs/devops/INCIDENT_RUNBOOK.md
docs/devops/ACTION_PINS.md
docs/devops/RELEASE_SUPPORT_MATRIX.md
```

---

## 3. GIAI ĐOẠN 1 — TẠO VÀ CẤU HÌNH GITHUB REPOSITORY

### Bước 1.1 — Tạo repository

1. Tạo repository GitHub thuộc owner/organization đúng của dự án.
2. Ban đầu đặt **Private** nếu source chưa sẵn sàng công khai hoặc còn cấu hình nhạy cảm cần rà soát.
3. Đặt default branch là `main`.
4. Không upload file `.env`, file credential, certificate `.pfx`, database dump thật, session token, key Lua hay log chứa dữ liệu cá nhân.
5. Tạo `README.md`, `LICENSE` nếu đã chọn giấy phép, `SECURITY.md`, `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md` nếu phù hợp, `CHANGELOG.md` và `.gitignore`.

### Bước 1.2 — Đẩy source ban đầu

Nếu source chưa có Git repository:

```bash
git init
git branch -M main
git add .
git status --short
# Dừng lại kiểm tra danh sách file đã stage trước khi commit.
git commit -m "chore: initialize OceanForge repository"
git remote add origin https://github.com/<OWNER>/<REPO>.git
git push -u origin main
```

Nếu repository đã được clone thì không chạy `git init` lại; hãy kiểm tra `git remote -v` rồi push branch phù hợp.

### Bước 1.3 — Bật Settings cơ bản

Trong GitHub repository, rà soát:

- **Settings → General:** default branch `main`, chỉ bật tính năng repository cần dùng.
- **Settings → Code security and analysis:** bật Dependency graph, Dependabot alerts và secret scanning/push protection nếu plan cho phép.
- **Settings → Actions → General:** chỉ cho phép GitHub Actions và action bên thứ ba đã được phê duyệt; giới hạn quyền `GITHUB_TOKEN` mặc định ở read-only; cân nhắc yêu cầu action pin full-length commit SHA.
- **Settings → Branches / Rules:** tạo ruleset bảo vệ `main` (mục 2.1 bên dưới).
- **Settings → Environments:** tạo `development`, `staging`, `production` hoặc `production-release` tùy workflow.

### Bước 1.4 — Quy tắc nhánh

Khuyến nghị trunk-based cho nhóm nhỏ:

```text
main                         luôn phải xanh, có thể release
feature/<short-description>  tính năng
fix/<short-description>      bug fix
security/<short-description> sửa bảo mật
refactor/<short-description> refactor
release/<version>            chỉ dùng nếu release process thực sự cần nhánh riêng
```

Giữ feature branch ngắn hạn; không bắt buộc dùng `develop` nếu không có quy trình phát hành theo nhánh dài hơi.

### Bước 1.5 — Commit convention

Dùng quy ước nhất quán:

```text
feat: add account detail view
fix: prevent duplicate sync batch
perf: virtualize fleet rows
security: reject unsigned package
ci: add Linux integration job
build: pin .NET SDK
refactor: isolate package verification
docs: describe production rollback
chore: update dependency lock files
```

Không nhét nhiều thay đổi không liên quan vào một commit.

---

## 4. GIAI ĐOẠN 2 — BẢO VỆ MAIN, PR VÀ CODEOWNERS

### Bước 2.1 — Ruleset cho `main`

Trong **Settings → Rules → Rulesets** (hoặc Branch protection rules nếu giao diện repository đang dùng cách cũ), cấu hình:

- Require a pull request before merging.
- Require ít nhất 1 approval; tăng lên 2 khi có maintainer/reviewer phù hợp.
- Dismiss stale approvals khi có commit mới, nếu phù hợp với quy trình.
- Require status checks: CI build/test Windows + Linux, integration checks, security/dependency checks và architecture checks đã được bật.
- Require branch up to date nếu đội chấp nhận việc cập nhật branch trước merge.
- Block force pushes và branch deletion.
- Không cho direct push lên `main`, ngoại trừ break-glass theo chính sách có ghi lại.
- Bắt buộc CODEOWNER review cho khu vực security, release, CI workflows, DB migrations và updater.

Status check tên phải trùng chính xác với job/check xuất hiện trên PR; cấu hình khi workflow đã chạy ít nhất một lần để chọn đúng tên.

### Bước 2.2 — CODEOWNERS

Tạo `.github/CODEOWNERS` và sửa owner/team thật:

```text
# Thay @OWNER hoặc @ORG/TEAM bằng GitHub owner thật.
/.github/                         @OWNER
/.github/workflows/               @OWNER
/src/OceanForge.Security/         @OWNER
/src/OceanForge.Infrastructure/   @OWNER
/src/OceanForge.Api/              @OWNER
/src/OceanForge.Worker/           @OWNER
/database/                        @OWNER
/packaging/                       @OWNER
/docs/devops/                     @OWNER
```

Nếu path hiện tại khác, sửa lại. File CODEOWNERS không có tác dụng nếu owner/team không tồn tại hoặc không có quyền review.

### Bước 2.3 — Pull Request template

Tạo `.github/pull_request_template.md`:

```markdown
## Thay đổi
- [ ] Mô tả phần thay đổi và lý do.

## Ảnh hưởng
- [ ] API / API contract
- [ ] MySQL migration
- [ ] SQLite migration hoặc dữ liệu offline
- [ ] Authentication / authorization / secrets
- [ ] Performance / memory / network
- [ ] Launcher / updater / installer / uninstall
- [ ] Backward compatibility

## Kiểm thử
- [ ] Unit tests
- [ ] Integration tests
- [ ] UI/E2E tests nếu liên quan
- [ ] Migration/restore test nếu liên quan
- [ ] Windows/Linux target đã kiểm tra nếu liên quan

## Phát hành và khôi phục
- [ ] Documentation cập nhật
- [ ] Rollback/forward-fix plan
- [ ] Không có secret hoặc dữ liệu người dùng trong diff/log
```

---

## 5. GIAI ĐOẠN 3 — CHUẨN HÓA TOOLCHAIN VÀ DEPENDENCY

### Bước 3.1 — Pin .NET SDK

1. Trên máy build chuẩn, cài SDK .NET 10 được Microsoft hỗ trợ.
2. Chạy `dotnet --list-sdks` và chọn phiên bản SDK thật đã dùng kiểm thử.
3. Tạo `global.json` bằng phiên bản vừa chọn; không copy một số phiên bản ví dụ mà không xác minh rằng nó tồn tại trong runner.
4. Có lịch cập nhật SDK patch/service release, tối thiểu mỗi tháng hoặc theo security advisory.
5. Workflow chạy `dotnet --info` và lưu thông tin SDK khi cần chẩn đoán.

Ví dụ cấu trúc `global.json` (thay `10.0.xxx` bằng SDK thật đã cài):

```json
{
  "sdk": {
    "version": "10.0.xxx",
    "rollForward": "latestPatch",
    "allowPrerelease": false
  }
}
```

Nếu chọn exact SDK version, phải đảm bảo tất cả máy dev và runner có thể cài phiên bản đó. Cần cập nhật version bằng PR có CI, không sửa âm thầm.

### Bước 3.2 — Central Package Management

Với repository có nhiều project, cân nhắc `Directory.Packages.props` để quản lý package version tập trung. Không để mỗi project tự nâng một package nền tảng khác nhau nếu không có lý do.

Quy tắc:

- Version package phải cụ thể, không wildcard/floating cho stable release.
- Nâng package major qua PR/ADR và chạy regression tests.
- Đánh giá NuGet audit advisory, license và nguồn package.
- Không dùng package lạ chỉ vì nó làm build qua tạm thời.

### Bước 3.3 — Lock dependency resolution

Bật lock file trong `Directory.Build.props` hoặc `.csproj` phù hợp:

```xml
<PropertyGroup>
  <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
</PropertyGroup>
```

Sau đó chạy restore để sinh `packages.lock.json` trong project phù hợp rồi commit các lock file này.

Khi lock files đã được tạo và cập nhật đúng, CI dùng:

```bash
dotnet restore OceanForge.sln --locked-mode
```

Nếu lock files chưa có, **không bật `--locked-mode` ngay**; hãy tạo/chốt lock files trước rồi mới đưa tùy chọn đó vào required CI.

### Bước 3.4 — Directory.Build.props

Ví dụ baseline; merge vào cấu hình thật và kiểm thử trên tất cả project, không ghi đè các thiết lập riêng cần thiết:

```xml
<Project>
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <Deterministic>true</Deterministic>
    <ContinuousIntegrationBuild Condition="'$(CI)' == 'true'">true</ContinuousIntegrationBuild>
    <NuGetAudit>true</NuGetAudit>
    <WarningsAsErrors>$(WarningsAsErrors);NU1901;NU1902;NU1903;NU1904</WarningsAsErrors>
  </PropertyGroup>
</Project>
```

Không bật `TreatWarningsAsErrors` cho toàn bộ warning trong một bước nếu source cũ còn nhiều warning chưa xử lý; có thể chuyển dần sang error theo quy trình đã thống nhất. Các warning bảo mật dependency nghiêm trọng cần policy chặn rõ ràng.

### Bước 3.5 — NuGet.Config

Nếu sử dụng `NuGet.Config`, chỉ khai báo nguồn package được duyệt. Không bật ignore-failed-sources để che lỗi restore. Không commit credential feed vào file; dùng GitHub Packages token/secret chỉ trong job cần thiết và chỉ cấp quyền đọc.

---

## 6. GIAI ĐOẠN 4 — CẤU TRÚC REPOSITORY ĐỀ XUẤT

Điều chỉnh theo source hiện tại, nhưng nên có cấu trúc trách nhiệm rõ ràng:

```text
/
├── OceanForge.sln
├── global.json
├── Directory.Build.props
├── Directory.Packages.props
├── NuGet.Config
├── .editorconfig
├── .gitignore
├── .github/
│   ├── CODEOWNERS
│   ├── pull_request_template.md
│   ├── dependabot.yml
│   └── workflows/
│       ├── ci.yml
│       ├── security.yml
│       ├── nightly.yml
│       ├── desktop-release.yml
│       ├── backend-staging.yml
│       └── backend-production.yml
├── src/
│   ├── OceanForge.Domain/
│   ├── OceanForge.Application/
│   ├── OceanForge.Contracts/
│   ├── OceanForge.Infrastructure/
│   ├── OceanForge.Storage/
│   ├── OceanForge.Security/
│   ├── OceanForge.Telemetry/
│   ├── OceanForge.Realtime/
│   ├── OceanForge.Analytics/
│   ├── OceanForge.Desktop/
│   ├── OceanForge.Launcher/
│   ├── OceanForge.Api/
│   └── OceanForge.Worker/
├── tests/
│   ├── OceanForge.Tests.Unit/
│   ├── OceanForge.Tests.Integration/
│   └── OceanForge.Tests.UI/
├── database/
│   ├── migrations/
│   └── scripts/
├── packaging/
│   ├── windows/
│   └── linux/
├── deployment/
│   ├── containers/
│   └── manifests/
└── docs/
    ├── adr/
    ├── devops/
    └── release/
```

Không bắt buộc chia tất cả thành microservices deploy độc lập. Theo V4, bắt đầu modular monolith với API/Worker/Desktop riêng và chỉ tách dịch vụ khi metrics, reliability hoặc team ownership chứng minh cần.

---

## 7. GIAI ĐOẠN 5 — QUY ƯỚC VERSION VÀ TAG RELEASE

### Bước 5.1 — Semantic Versioning

Dùng `MAJOR.MINOR.PATCH`:

- MAJOR: thay đổi tương thích phá vỡ (API/schema/behavior) có migration plan.
- MINOR: tính năng tương thích ngược.
- PATCH: bug/security fix tương thích ngược.

Ví dụ:

```text
v0.1.0
v0.2.0
v0.2.1
v1.0.0
v1.0.1
v1.1.0-beta.1
v1.1.0-rc.1
```

### Bước 5.2 — Không lấy version từ biến tự do

Release phải lấy version từ tag đã kiểm tra, hoặc nguồn version duy nhất đã ghi trong ADR. Kiểm tra:

- tag là SemVer hợp lệ;
- tag trỏ tới commit đã qua CI/được phép phát hành;
- working tree không được sửa sau khi tag đã tạo;
- commit SHA, version, OS, architecture, SDK và build timestamp được ghi trong release metadata.

### Bước 5.3 — Ví dụ tạo tag bằng annotated tag

```bash
git checkout main
git pull --ff-only
git status --short
# Chỉ tiếp tục nếu working tree sạch và CI main xanh.
git tag -a v1.0.0 -m "OceanForge Desktop v1.0.0"
git push origin v1.0.0
```

Nếu tag sai, **không âm thầm force-push tag đã công bố**. Theo chính sách release, hủy draft hoặc phát hành patch tag mới; ghi lại lý do trong changelog/incident log.

---

## 8. GIAI ĐOẠN 6 — GITHUB ACTIONS SECURITY, SECRETS VÀ ENVIRONMENTS

### Bước 6.1 — Permissions tối thiểu

Đặt ở đầu workflow:

```yaml
permissions:
  contents: read
```

Chỉ mở rộng ở job thật sự cần:

- tạo GitHub Release: `contents: write`;
- provenance attestation: `id-token: write`, `attestations: write` và quyền phụ trợ theo action/policy;
- deploy cloud bằng OIDC: `id-token: write` ở job deploy;
- CodeQL upload: permission security events theo yêu cầu của workflow.

Không cấp `contents: write`, `packages: write`, `id-token: write` cho toàn bộ workflow mặc định.

### Bước 6.2 — Pin GitHub Actions

GitHub khuyến nghị pin action bên ngoài bằng **full-length commit SHA** để tham chiếu không bị đổi nội dung theo tag. Các ví dụ YAML trong runbook dùng version tag để dễ đọc; trước khi đánh dấu production-ready, hãy thay `@vX` bằng full SHA đã xác minh từ repository chính thức và ghi lại action/version/SHA trong `docs/devops/ACTION_PINS.md`.

Quy trình cập nhật pin:

1. Kiểm tra action thuộc publisher/repository chính thức.
2. Đọc release notes, permissions, source và security advisories.
3. Lấy commit SHA đúng với release dự định dùng.
4. Ghi version và SHA vào ledger.
5. Nâng pin trong PR, chạy CI/security test.
6. Không tự cập nhật tag action ở production mà không review.

### Bước 6.3 — Danh mục secrets và variables

Chỉ tạo secret cần thật sự cho môi trường đã chọn. Dùng GitHub Environment secrets thay vì repository-wide secret nếu chỉ một environment cần nó.

| Tên đề xuất | Loại | Môi trường | Ghi chú |
|---|---|---|---|
| `STAGING_API_BASE_URL` | Variable | Staging | URL không bí mật |
| `PRODUCTION_API_BASE_URL` | Variable | Production | URL không bí mật |
| `WINDOWS_SIGNING_*` | Secret hoặc OIDC metadata | Release | Chỉ nếu mô hình ký đã chốt; ưu tiên managed signing/OIDC |
| `CLOUD_OIDC_*` | Variable/secret theo provider | Staging/Production | Ưu tiên identity federation, tránh credential dài hạn |
| `STAGING_DEPLOY_ROLE` | Variable | Staging | Tên/ID role; trust policy phía cloud phải giới hạn repo, branch và environment |
| `PRODUCTION_DEPLOY_ROLE` | Variable | Production | Tách biệt staging, chỉ job đã review được dùng |
| `MYSQL_*` | Environment secret | API deployment/migration | Chỉ ở backend deploy job; Desktop không nhận DB credential |
| `PACKAGE_FEED_TOKEN` | Secret | Package job | Chỉ cần nếu đọc private feed; read-only |

Không tạo secret cho local URL/giá trị công khai. Không đưa secret vào command-line argument nếu runner có thể ghi log; ưu tiên biến môi trường/method của tool, che log nếu buộc phải thao tác. Không echo secrets, không xuất `env` tràn lan, không upload file `.env`, `appsettings.Production.json` hay dump chứa secret.

### Bước 6.4 — Environments

Tạo ít nhất:

- `staging`: dữ liệu test/synthetic, không phải production credentials;
- `production`: required reviewer, branch/tag policy, chỉ maintainer release được phép deploy;
- `production-release`: nếu muốn bước duyệt phát hành Desktop tách riêng khỏi backend deploy.

Required reviewers và deployment protection rules phải được bật trong GitHub Settings nếu gói plan/repository hỗ trợ. Không dựa vào một dòng YAML duy nhất để thay thế cấu hình bảo vệ environment.

### Bước 6.5 — OIDC

Ưu tiên GitHub OIDC để lấy credential ngắn hạn từ cloud provider khi provider hỗ trợ. Trust policy của cloud phải kiểm tra các claim cần thiết (repo/organization, branch/tag, workflow/environment) và chỉ cho đúng workflow nhận role. `id-token: write` chỉ cho phép job yêu cầu token OIDC; nó không thay thế policy ở cloud và không nên cấp cho PR CI.

---

## 9. GIAI ĐOẠN 7 — WORKFLOW CI CHO PULL REQUEST VÀ MAIN

Tạo `.github/workflows/ci.yml`. **Trước khi commit:** thay `OceanForge.sln` bằng đường dẫn solution thật. Nếu lock files đã có, bật `--locked-mode`; nếu chưa, hoàn tất mục 3.3 trước.

> Ghi chú bảo mật: ví dụ dưới đây dùng `actions/checkout@v4`, `actions/setup-dotnet@v4` và `actions/upload-artifact@v4` làm tên phiên bản dễ đọc. Hãy pin sang full commit SHA đã xác minh trước khi production.

```yaml
name: OceanForge CI

on:
  pull_request:
    branches: [main]
  push:
    branches: [main]
  workflow_dispatch:

permissions:
  contents: read

concurrency:
  group: ci-${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: true

env:
  SOLUTION_PATH: OceanForge.sln # TODO: thay bằng solution thật

jobs:
  build-test:
    name: Build and unit test (${{ matrix.os }})
    strategy:
      fail-fast: false
      matrix:
        os: [windows-latest, ubuntu-latest]
    runs-on: ${{ matrix.os }}
    timeout-minutes: 30

    steps:
      - name: Checkout
        uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - name: Setup .NET SDK
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x

      - name: Record toolchain
        shell: pwsh
        run: |
          dotnet --info
          dotnet --list-sdks

      - name: Restore
        shell: pwsh
        run: dotnet restore "$env:SOLUTION_PATH" --locked-mode

      - name: Verify formatting
        shell: pwsh
        run: dotnet format "$env:SOLUTION_PATH" --verify-no-changes --no-restore

      - name: Build Release
        shell: pwsh
        run: dotnet build "$env:SOLUTION_PATH" --configuration Release --no-restore

      - name: Run unit tests
        shell: pwsh
        run: |
          $results = Join-Path $env:RUNNER_TEMP "TestResults"
          New-Item -ItemType Directory -Force -Path $results | Out-Null
          dotnet test "$env:SOLUTION_PATH" `
            --configuration Release `
            --no-build `
            --filter "Category!=Integration" `
            --logger "trx" `
            --results-directory $results

      - name: Upload test results
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: test-results-${{ matrix.os }}
          path: ${{ runner.temp }}/TestResults/**/*.trx
          if-no-files-found: warn
          retention-days: 14
```

### 9.1 Quy tắc cho `Category=Integration`

Đánh dấu integration tests bằng trait/category đã thống nhất, ví dụ xUnit:

```csharp
[Trait("Category", "Integration")]
public sealed class TelemetryApiIntegrationTests { }
```

Đừng dùng filter ở workflow nếu test suite chưa tag đúng; nếu filter sai, CI có thể chạy 0 integration tests mà team không nhận ra. Hãy thêm bước kiểm tra số test được discover khi bắt đầu dùng integration gate.

### 9.2 Integration test có MySQL riêng

Khi API/Worker và test project đã tồn tại, thêm job riêng. Database bên dưới chỉ là test DB dùng một lần, không dùng production connection string.

```yaml
  integration-tests:
    name: MySQL integration tests
    runs-on: ubuntu-latest
    timeout-minutes: 30
    services:
      mysql:
        image: mysql:8.4
        env:
          MYSQL_ROOT_PASSWORD: test-root-password
          MYSQL_DATABASE: oceanforge_test
          MYSQL_USER: oceanforge_test_user
          MYSQL_PASSWORD: test-only-password
        ports:
          - 3306:3306
        options: >-
          --health-cmd="mysqladmin ping -h 127.0.0.1 -ptest-root-password"
          --health-interval=10s
          --health-timeout=5s
          --health-retries=12
    env:
      ConnectionStrings__OceanForge: "Server=127.0.0.1;Port=3306;Database=oceanforge_test;User=oceanforge_test_user;Password=test-only-password;SslMode=None;"
      SOLUTION_PATH: OceanForge.sln # TODO: thay bằng đường dẫn thật
      INTEGRATION_TEST_PROJECT: tests/OceanForge.Tests.Integration/OceanForge.Tests.Integration.csproj
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x
      - name: Restore integration tests
        run: dotnet restore "$env:INTEGRATION_TEST_PROJECT" --locked-mode
        shell: pwsh
      - name: Apply test database migrations
        shell: pwsh
        run: |
          # Gọi migration runner/command đã được tạo cho repository.
          # Không thêm lệnh production migration tại đây.
          dotnet --info
          # TODO: thay dòng dưới bằng command migration test chính thức.
      - name: Run integration tests
        shell: pwsh
        run: dotnet test "$env:INTEGRATION_TEST_PROJECT" -c Release --filter "Category=Integration" --logger "trx"
```

**Không merge nguyên mẫu integration job mà chưa điền migration runner.** Một comment `TODO` không phải migration test. Khi API/test project chưa tồn tại, đánh dấu bước này `NOT IMPLEMENTED` trong status thay vì bật một gate giả.

### 9.3 Các acceptance gate sau khi CI xanh

- `dotnet restore` không tải dependency ngoài lock policy.
- `dotnet format` không phát hiện format drift.
- Build Release thành công trên Windows và Linux.
- Unit tests không bị skip một cách âm thầm.
- Không có secret scan finding chưa được xử lý.
- Integration tests kết nối vào DB test cô lập.
- Tên required checks được cấu hình trong branch ruleset.

---

## 10. GIAI ĐOẠN 8 — CODEQL, DEPENDENCY REVIEW, SECRET SCANNING

### Bước 8.1 — Security workflow

Tạo `.github/workflows/security.yml`, điều chỉnh language/project theo source thực tế:

```yaml
name: OceanForge Security

on:
  pull_request:
    branches: [main]
  push:
    branches: [main]
  schedule:
    - cron: "23 3 * * 1"
  workflow_dispatch:

permissions:
  contents: read
  security-events: write
  pull-requests: read

jobs:
  codeql:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4
      - name: Initialize CodeQL
        uses: github/codeql-action/init@v3
        with:
          languages: csharp
      - name: Build for CodeQL
        uses: github/codeql-action/autobuild@v3
      - name: Analyze
        uses: github/codeql-action/analyze@v3

  dependency-review:
    if: github.event_name == 'pull_request'
    runs-on: ubuntu-latest
    permissions:
      contents: read
      pull-requests: write
    steps:
      - uses: actions/checkout@v4
      - uses: actions/dependency-review-action@v4
        with:
          fail-on-severity: high
```

Trước khi áp dụng, xác minh tên/version/action SHA theo nguồn chính thức; pin từng action. Nếu CodeQL không build được do solution/project chưa cấu hình, sửa project hoặc điều chỉnh build mode có giải thích; không tắt scan chỉ để PR xanh.

### Bước 8.2 — NuGet audit

Trong CI, chạy audit report:

```bash
dotnet list OceanForge.sln package --vulnerable --include-transitive
```

Nếu dùng tên solution khác, thay đúng. Lệnh audit report cần kết hợp với policy restore/warnings để severity đã chọn có thể chặn build; đừng giả định mọi bản `dotnet list package` tự động trả exit code khác 0 khi phát hiện advisory.

### Bước 8.3 — Secret scanning

- Bật GitHub secret scanning/push protection nếu plan hỗ trợ.
- Có `.gitignore` bao gồm `.env*`, `*.pfx`, `*.p12`, file credential local, log/dump, build outputs và SQLite production data.
- Nếu secret từng bị commit: thu hồi/rotate trước; xóa khỏi history không làm secret cũ mất hiệu lực.
- Không cho phép exception secret scanning mà không có owner, lý do, expiry và rotation plan.

### Bước 8.4 — Dependency update

Tạo `.github/dependabot.yml`:

```yaml
version: 2
updates:
  - package-ecosystem: nuget
    directory: "/"
    schedule:
      interval: weekly
    open-pull-requests-limit: 10
    groups:
      dotnet-minor-patch:
        update-types:
          - minor
          - patch
  - package-ecosystem: github-actions
    directory: "/"
    schedule:
      interval: weekly
    open-pull-requests-limit: 10
```

Nếu repo không dùng package manifest ở root hoặc có nhiều `Directory.Packages.props`, cần kiểm tra Dependabot config thực tế. Major version không được auto-merge; phải review release notes và chạy full CI.

---

## 11. GIAI ĐOẠN 9 — NIGHTLY CI VÀ KIỂM THỬ BỀN

Tạo `.github/workflows/nightly.yml` khi các project/test target tồn tại:

```yaml
name: OceanForge Nightly

on:
  schedule:
    - cron: "37 2 * * *" # UTC
  workflow_dispatch:

permissions:
  contents: read

concurrency:
  group: nightly-${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: true

env:
  SOLUTION_PATH: OceanForge.sln # TODO: thay bằng đường dẫn thật

jobs:
  full-validation:
    runs-on: ubuntu-latest
    timeout-minutes: 90
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x
      - run: dotnet restore "$env:SOLUTION_PATH" --locked-mode
        shell: pwsh
      - run: dotnet build "$env:SOLUTION_PATH" -c Release --no-restore
        shell: pwsh
      - run: dotnet test "$env:SOLUTION_PATH" -c Release --no-build --logger trx
        shell: pwsh
      - name: Package audit report
        shell: pwsh
        run: dotnet list "$env:SOLUTION_PATH" package --vulnerable --include-transitive
      - name: Upload test output
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: nightly-test-results
          path: "**/TestResults/**/*.trx"
          if-no-files-found: warn
          retention-days: 14
```

Nightly là nền tảng, chưa tự động chứng minh các bài test chuyên sâu đã tồn tại. Khi từng module được triển khai, bổ sung:

- MySQL clean migration + upgrade migration;
- SQLite migration, crash/reopen, queue persistence, disk-full behavior;
- offline/reconnect, duplicate/out-of-order event replay, cursor expiry, conflict;
- package verification failure, partial download, cancellation, corrupted archive;
- launcher install/update/repair/uninstall E2E theo từng hệ;
- benchmark với 1k/10k account records;
- memory soak test tối thiểu 8 giờ theo V4 nếu runner có độ ổn định đủ và kết quả có thể so sánh;
- backup + restore drill cho staging/test data;
- performance trend báo cáo chứ không so sánh runner khác cấu hình mà thiếu kiểm soát.

Không chạy bài test 8 giờ trên mỗi PR nếu làm mất phản hồi nhanh; đưa vào nightly/scheduled runner, còn PR chạy smoke/short version.

---

## 12. GIAI ĐOẠN 10 — BUILD CONFIGURATION VÀ PUBLISH PROFILE

### Bước 10.1 — Baseline publish

Đối với Avalonia, bắt đầu bằng publish folder thông thường theo RID. Không bật trimming/AOT/single-file cho đến khi QA native libraries, reflection, resource loading, WebView và launcher flows.

Nếu dùng `packages.lock.json` và publish nhiều RID, hãy bảo đảm lock files đã được tạo/commit cho từng RID (ví dụ khai báo `<RuntimeIdentifiers>win-x64;linux-x64</RuntimeIdentifiers>` trong project desktop, nếu phù hợp) rồi mới dùng `dotnet restore -r <RID> --locked-mode` trong CI. Cách tạo lock file phải được thử trên clean clone; không tắt locked mode ở release chỉ để vượt qua lỗi không khớp lock.

Ví dụ Windows x64:

```powershell
dotnet restore src/OceanForge.Desktop/OceanForge.Desktop.csproj -r win-x64 --locked-mode
dotnet publish src/OceanForge.Desktop/OceanForge.Desktop.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  --no-restore `
  -p:PublishTrimmed=false `
  -p:PublishSingleFile=false `
  -o artifacts/publish/win-x64
```

Ví dụ Linux x64:

```bash
dotnet restore src/OceanForge.Desktop/OceanForge.Desktop.csproj -r linux-x64 --locked-mode
dotnet publish src/OceanForge.Desktop/OceanForge.Desktop.csproj \
  --configuration Release \
  --runtime linux-x64 \
  --self-contained true \
  --no-restore \
  -p:PublishTrimmed=false \
  -p:PublishSingleFile=false \
  -o artifacts/publish/linux-x64
```

Đổi đường dẫn `.csproj` thực tế. `--no-restore` chỉ dùng sau restore đúng project, RID và lock mode. Không mặc định rằng publish `linux-x64` trên Windows có thể tạo/test package cuối cùng; package final cần runner/target environment phù hợp.

### Bước 10.2 — Build metadata

Mỗi bản build release phải ghi các giá trị:

```text
Product: OceanForge Desktop
SemVer: <tag version>
Git commit SHA: <full commit SHA>
Build run URL: <GitHub Actions run>
SDK version: <dotnet --info>
Target RID: <win-x64/linux-x64>
Publish mode: <self-contained/framework-dependent>
Installer format: <MSIX/EXE/.deb/AppImage/portable>
Build timestamp UTC: <timestamp>
Database migration range: <if backend release>
```

Không chèn secret hay biến môi trường nhạy cảm vào `build-info.json`.

### Bước 10.3 — Không bật Native AOT/trimming theo cảm tính

Chỉ thử sau khi có benchmark:

1. Tạo branch thử riêng.
2. Bật tùy chọn cho một RID.
3. Build và chạy mọi UI route/resource/load/reflection path.
4. Chạy package install/upgrade/uninstall.
5. So sánh size, startup và memory bằng cùng hardware/workload.
6. Merge chỉ khi không có regression và output đạt mức cải thiện có ý nghĩa.

---

## 13. GIAI ĐOẠN 11 — ĐÓNG GÓI WINDOWS

### 13.1 Chọn một đường chính

#### Lựa chọn A — MSIX

Chọn nếu muốn lifecycle install/update/uninstall tích hợp với Windows/App Installer và package identity rõ.

Việc cần làm:

1. Tạo package manifest với Name/Publisher/Identity/Version/architecture phù hợp.
2. Publisher phải khớp danh tính certificate ký gói.
3. Thêm logo/icon/resource theo yêu cầu package.
4. Xác định install/update policy và dữ liệu user không nằm trong thư mục cài đặt.
5. Dùng công cụ đóng gói chính thức đã được pin và phiên bản ghi nhận.
6. Ký MSIX bằng certificate/managed signing production hợp lệ.
7. Xác minh chữ ký sau khi ký.
8. Test clean install, upgrade từ bản cũ, launch, uninstall và xử lý local data.

Tài liệu Microsoft về ký MSIX: <https://learn.microsoft.com/en-us/windows/msix/package/sign-app-package-using-signtool> và hướng dẫn end-to-end: <https://learn.microsoft.com/en-us/windows/msix/package/sign-msix-package-guide>.

#### Lựa chọn B — Signed EXE installer

Chọn nếu app cần quy trình cài đặt/repair phức tạp hoặc MSIX không phù hợp. Chốt đúng installer engine, version, script installer và quyền chạy.

Checklist:

- Installer signed bằng danh tính publisher đã chốt.
- Không chạy với quyền admin nếu không cần.
- Chỉ tạo shortcut/uninstall registration hợp lệ.
- Không tự xóa file user ngoài vùng ứng dụng.
- Không tải và execute payload chưa xác minh.
- Không xóa hoặc ghi đè SQLite/outbox trong upgrade.
- Hỗ trợ kiểm tra disk space, cancel ở safe checkpoint và rollback ứng dụng.
- Test Defender/SmartScreen behavior trên máy sạch; không tắt antivirus để “làm installer chạy”.

### 13.2 Ký mã trên CI

Khuyến nghị dùng managed signing/identity federation nếu tổ chức và provider hỗ trợ. Nếu phải dùng certificate file:

- certificate private key và password chỉ ở release environment secrets/secret manager;
- không để `.pfx` trong repository hoặc build artifact;
- chỉ release job protected mới có quyền lấy chứng chỉ;
- không cấp signing secret cho PR, fork, nightly untrusted hoặc local debug;
- chỉ ký artifact đã qua test;
- xác minh signature và timestamp sau ký;
- ghi certificate identity/thumbprint và thời hạn vào release record, không log private material.

Lệnh `signtool` cần chạy với toolchain/certificate đúng và policy hash/timestamp đã thống nhất. Cú pháp minh họa từ Microsoft docs chỉ là hướng dẫn tool; đừng copy password vào workflow literal. Thông tin chính thức: <https://learn.microsoft.com/en-us/windows/msix/package/sign-app-package-using-signtool>.

### 13.3 Windows test matrix

Ít nhất:

- clean Windows 10/11 target versions đã công bố;
- máy chưa cài .NET runtime nếu publish self-contained;
- update từ previous stable;
- thiếu dung lượng;
- mất mạng khi download;
- package digest/signature mismatch;
- app đang chạy trong lúc update;
- standard user không có quyền admin;
- uninstall trong khi có dữ liệu local pending;
- reinstall sau uninstall;
- high-DPI/scaling cho installer/launcher;
- launch desktop sau update và rollback smoke.

Không ghi “hỗ trợ Windows 10/11” chung chung nếu chỉ thử trên đúng một máy không đại diện cho support matrix.

---

## 14. GIAI ĐOẠN 12 — ĐÓNG GÓI LINUX (.DEB VÀ APPIMAGE)

### 14.1 Chốt OS matrix

Ghi chính xác distro/version/desktop backend được hỗ trợ trong `docs/devops/RELEASE_SUPPORT_MATRIX.md`. Avalonia có hướng dẫn deployment Linux, nhưng việc build thành công không đồng nghĩa chạy được trên mọi distro. Tài liệu: <https://docs.avaloniaui.net/docs/deployment/linux>.

### 14.2 `.deb` — cấu trúc cơ bản

Ví dụ package root:

```text
packaging/linux/deb-root/
├── DEBIAN/
│   ├── control
│   └── postinst                 # chỉ khi thực sự cần
├── usr/
│   ├── bin/oceanforge           # launcher wrapper
│   ├── lib/oceanforge/          # publish output
│   └── share/
│       ├── applications/oceanforge.desktop
│       └── icons/hicolor/...    # icon sizes
```

Quy tắc:

- `.deb` control metadata có package name, version, architecture, depends và description.
- Không chạy `sudo`/root command tuỳ tiện trong `postinst`.
- Wrapper `usr/bin/oceanforge` phải trỏ đúng vị trí package; quote biến đường dẫn.
- Local settings/cache/database phải ở vị trí user phù hợp, không nằm trong `/usr/lib`.
- Xác định dependencies native của build bằng test VM/container phù hợp.
- Dùng version package đúng với SemVer mapping; version package Debian phải hợp lệ theo quy ước đã ghi.
- Build `.deb`, chạy `dpkg-deb --info` / `--contents`, cài trong clean Ubuntu/Debian image đã chốt, launch, update và uninstall.

### 14.3 AppImage — checklist

AppImage cần AppDir, executable/launcher, `.desktop` entry, icon, runtime/native libraries phù hợp và test môi trường sạch. Chỉ dùng `linuxdeploy`/AppImage tooling khi phiên bản tool đã pin và được review.

- Không tuyên bố universal compatibility sau khi test một distro duy nhất.
- Test mount/launch, no `HOME`, path spaces, permissions, desktop entry và update flow.
- Kiểm tra native dependencies (Avalonia/SkiaSharp/WebView nếu có).
- Xác minh checksum/signature của file phát hành.
- Đảm bảo hướng dẫn uninstall giải thích AppImage file và data user là hai thứ riêng.

Tài liệu AppImage packaging: <https://docs.appimage.org/>.

### 14.4 Linux package release gate

Chưa được phát hành `.deb`/AppImage stable nếu chưa có script trong `packaging/linux/`, runner đã tạo đúng artifact và smoke tests cho các distro/version được công bố. Không được đổi tên một `tar.gz` thành `.deb` để giả vờ đã package.

---

## 15. GIAI ĐOẠN 13 — WORKFLOW RELEASE CHO DESKTOP

Workflow này tạo gói portable candidate theo RID, checksum, SBOM và provenance, rồi tạo **GitHub Draft Release**. Đây là mẫu cuối pipeline cơ bản, không tự biến portable archive thành installer đã ký. Trước stable public release, cần nối package/signing scripts cho MSIX/EXE và `.deb`/AppImage theo lựa chọn đã duyệt.

Tạo `.github/workflows/desktop-release.yml` sau khi solution/project paths tồn tại. Thay hai giá trị `SOLUTION_PATH` và `DESKTOP_PROJECT_PATH`.

```yaml
name: OceanForge Desktop Release

on:
  push:
    tags:
      - "v*.*.*"
  workflow_dispatch:

permissions:
  contents: read

concurrency:
  group: desktop-release-${{ github.ref }}
  cancel-in-progress: false

env:
  SOLUTION_PATH: OceanForge.sln # TODO: thay bằng solution thật
  DESKTOP_PROJECT_PATH: src/OceanForge.Desktop/OceanForge.Desktop.csproj # TODO: thay path thật

jobs:
  validate-tag:
    name: Validate release tag
    runs-on: ubuntu-latest
    permissions:
      contents: read
    outputs:
      version: ${{ steps.version.outputs.version }}
      is_prerelease: ${{ steps.version.outputs.is_prerelease }}
    steps:
      - name: Checkout full history
        uses: actions/checkout@v4
        with:
          fetch-depth: 0
      - id: version
        name: Check SemVer tag and ancestry
        shell: bash
        run: |
          set -euo pipefail
          tag="${GITHUB_REF_NAME}"
          if [[ ! "$tag" =~ ^v(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(-[0-9A-Za-z.-]+)?(\+[0-9A-Za-z.-]+)?$ ]]; then
            echo "Invalid release tag: $tag" >&2
            exit 1
          fi
          version="${tag#v}"
          echo "version=$version" >> "$GITHUB_OUTPUT"
          if [[ "$version" == *-* ]]; then
            echo "is_prerelease=true" >> "$GITHUB_OUTPUT"
          else
            echo "is_prerelease=false" >> "$GITHUB_OUTPUT"
          fi
          git fetch origin +refs/heads/main:refs/remotes/origin/main
          commit="${GITHUB_SHA}"
          if ! git merge-base --is-ancestor "$commit" origin/main; then
            echo "Tag commit is not reachable from origin/main." >&2
            exit 1
          fi
          # Stable policy khuyến nghị annotated tags. Nếu bắt buộc annotated tag,
          # hãy thêm kiểm tra object type theo policy của repository.

  build-portable:
    name: Build portable candidate (${{ matrix.rid }})
    needs: validate-tag
    strategy:
      fail-fast: false
      matrix:
        include:
          - os: windows-latest
            rid: win-x64
            archive_extension: zip
          - os: ubuntu-latest
            rid: linux-x64
            archive_extension: tar.gz
    runs-on: ${{ matrix.os }}
    timeout-minutes: 45
    permissions:
      contents: read
    env:
      RID: ${{ matrix.rid }}
      APP_VERSION: ${{ needs.validate-tag.outputs.version }}
    steps:
      - name: Checkout exact tag commit
        uses: actions/checkout@v4
        with:
          ref: ${{ github.sha }}
          fetch-depth: 0
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x
      - name: Confirm source commit and toolchain
        shell: pwsh
        run: |
          git rev-parse HEAD
          dotnet --info
      - name: Restore solution (locked mode)
        shell: pwsh
        run: dotnet restore "$env:SOLUTION_PATH" --locked-mode
      - name: Build Release
        shell: pwsh
        run: dotnet build "$env:SOLUTION_PATH" -c Release --no-restore
      - name: Unit tests
        shell: pwsh
        run: dotnet test "$env:SOLUTION_PATH" -c Release --no-build --filter "Category!=Integration" --logger trx
      - name: Restore desktop project for target RID
        shell: pwsh
        run: dotnet restore "$env:DESKTOP_PROJECT_PATH" -r "$env:RID" --locked-mode
      - name: Publish self-contained folder
        shell: pwsh
        run: |
          $out = Join-Path $env:RUNNER_TEMP "publish/$env:RID"
          New-Item -ItemType Directory -Force -Path $out | Out-Null
          dotnet publish "$env:DESKTOP_PROJECT_PATH" `
            --configuration Release `
            --runtime "$env:RID" `
            --self-contained true `
            --no-restore `
            -p:Version="$env:APP_VERSION" `
            -p:ContinuousIntegrationBuild=true `
            -p:PublishTrimmed=false `
            -p:PublishSingleFile=false `
            -o "$out"
      - name: Create Windows portable candidate archive
        if: matrix.rid == 'win-x64'
        shell: pwsh
        run: |
          $out = Join-Path $env:RUNNER_TEMP "publish/$env:RID"
          $pkg = Join-Path $env:RUNNER_TEMP "OceanForge-$env:APP_VERSION-$env:RID-portable.zip"
          Compress-Archive -Path (Join-Path $out '*') -DestinationPath $pkg
      - name: Create Linux portable candidate archive
        if: matrix.rid == 'linux-x64'
        shell: bash
        run: |
          set -euo pipefail
          out="$RUNNER_TEMP/publish/$RID"
          pkg="$RUNNER_TEMP/OceanForge-$APP_VERSION-$RID-portable.tar.gz"
          tar -czf "$pkg" -C "$out" .
      - name: Upload candidate package
        uses: actions/upload-artifact@v4
        with:
          name: package-${{ matrix.rid }}
          path: ${{ runner.temp }}/OceanForge-${{ needs.validate-tag.outputs.version }}-${{ matrix.rid }}-portable.${{ matrix.archive_extension }}
          if-no-files-found: error
          retention-days: 14

  draft-release:
    name: Create draft release with integrity metadata
    needs: [validate-tag, build-portable]
    runs-on: ubuntu-latest
    timeout-minutes: 20
    permissions:
      contents: write
      id-token: write
      attestations: write
    env:
      TAG_NAME: ${{ github.ref_name }}
      APP_VERSION: ${{ needs.validate-tag.outputs.version }}
      GH_TOKEN: ${{ github.token }}
    steps:
      - name: Checkout tagged source
        uses: actions/checkout@v4
        with:
          ref: ${{ github.sha }}
          fetch-depth: 0
      - name: Download package artifacts
        uses: actions/download-artifact@v4
        with:
          pattern: package-*
          path: incoming
          merge-multiple: false
      - name: Assemble release assets
        shell: bash
        run: |
          set -euo pipefail
          mkdir -p release-assets
          find incoming -maxdepth 4 -type f -print -exec cp {} release-assets/ \;
          test -n "$(find release-assets -maxdepth 1 -type f -print -quit)"
          ls -lah release-assets
      - name: Generate SBOM
        uses: anchore/sbom-action@v0
        with:
          path: .
          format: spdx-json
          output-file: release-assets/SBOM.spdx.json
      - name: Generate SHA-256 checksums
        shell: bash
        run: |
          set -euo pipefail
          cd release-assets
          find . -maxdepth 1 -type f ! -name 'checksums-sha256.txt' -print0 \
            | sort -z \
            | xargs -0 sha256sum > checksums-sha256.txt
          cat checksums-sha256.txt
      - name: Attest build provenance
        uses: actions/attest-build-provenance@v2
        with:
          subject-path: "release-assets/*"
      - name: Create GitHub draft release
        shell: bash
        run: |
          set -euo pipefail
          args=("$TAG_NAME" release-assets/* --title "OceanForge $TAG_NAME" --generate-notes --draft --verify-tag)
          if [[ "$TAG_NAME" == *-* ]]; then
            args+=(--prerelease)
          fi
          gh release create "${args[@]}"
```

### 15.1 Những điểm bắt buộc trước khi dùng workflow

- Thay `SOLUTION_PATH` và `DESKTOP_PROJECT_PATH` bằng path thật.
- `--locked-mode` yêu cầu lock files đã commit.
- CodeQL/SBOM/attestation action cần pin full SHA trong production.
- Kiểm tra `anchore/sbom-action` version/input theo repo chính thức trước khi dùng; nếu plan/repo không hỗ trợ artifact attestations thì workflow attestation sẽ fail — không được bỏ qua failure để publish stable.
- Workflow trên đóng gói portable candidates, chưa ký MSIX/EXE/.deb/AppImage. Hãy nối `packaging/windows` và `packaging/linux` sau khi package format đã chốt.
- Không công bố draft thành stable cho đến khi người duyệt xác nhận package installer cuối cùng, signature, hash, provenance, release notes, install/upgrade/uninstall tests và rollback rehearsal đều đạt.
- `workflow_dispatch` chạy trên tag/branch cần được kiểm soát; không cho user tự chọn ref bất kỳ để tạo stable release. Với thiết kế chặt hơn, tạo workflow release tag-only và workflow manual beta tách riêng.

### 15.2 Thao tác phát hành thủ công sau khi draft xuất hiện

1. Mở **Releases → Draft**.
2. Đối chiếu tag, commit SHA và release notes.
3. Thay portable candidates bằng installer chính thức đã package/ký nếu release policy yêu cầu.
4. Kiểm tra checksum/SBOM/provenance của **chính file phát hành cuối cùng**, sau khi ký và đóng gói cuối cùng.
5. Kiểm tra asset names, version, architecture, support matrix, changelog và known issues.
6. Xác minh chữ ký của installer/package trên một máy sạch.
7. Chạy smoke test từ chính artifact đã tải về từ draft/staging.
8. Chỉ sau khi checklist hoàn tất, nhấn Publish release.

Nếu gói được ký sau khi workflow tạo checksum, **phải tạo lại checksum và attestation sau thay đổi cuối**. Checksum trước khi ký không đại diện cho binary sau ký.

---

## 16. GIAI ĐOẠN 14 — SBOM, CHECKSUM, SIGNATURE VÀ PROVENANCE

Đây là bốn mục khác nhau; không dùng lẫn thuật ngữ:

| Cơ chế | Trả lời câu hỏi | Không thay thế |
|---|---|---|
| SHA-256 checksum | File tải về có trùng bytes với hash công bố không? | Không chứng minh người phát hành là ai |
| Code signing (Authenticode/MSIX/package signing) | Ai ký artifact và artifact có bị đổi sau ký không? | Không chứng minh source code không có bug |
| SBOM | Artifact/source phụ thuộc vào thành phần nào? | Không tự động chứng minh dependencies an toàn |
| Provenance/attestation | Artifact được build từ repository/commit/workflow nào? | Không thay thế code signature theo platform |

### Bước 14.1 — Hash

- Tính SHA-256 sau khi hoàn tất package và signing.
- Phát hành `checksums-sha256.txt` cùng asset.
- Lưu bản copy/checksum trong release record.
- Verify trên một máy khác sau khi tải từ GitHub Release.

Linux:

```bash
sha256sum -c checksums-sha256.txt
```

Windows PowerShell:

```powershell
Get-FileHash .\OceanForge-<version>-win-x64.msix -Algorithm SHA256
```

So sánh với hash của **đúng tên file/version** trong checksum list. Không đối chiếu hash của file khác tên hoặc artifact trước khi ký.

### Bước 14.2 — Signature

- MSIX: dùng certificate/publisher identity phù hợp; verify signature theo Microsoft tooling.
- EXE: dùng Authenticode và timestamp service theo policy đã chọn.
- Linux `.deb`/AppImage: checksum + detached signature hoặc package repository signing theo distribution model; ghi rõ người dùng phải xác minh bằng lệnh nào.
- Private signing key không được đưa vào artifact hoặc source.

### Bước 14.3 — SBOM

Mỗi release stable nên có `SBOM.spdx.json` hoặc CycloneDX tương ứng với source/dependency build đã tạo artifact. Ghi rõ file SBOM được sinh từ source hay artifact; không tự gọi nó là SBOM “của binary” nếu tool chỉ quét source tree.

### Bước 14.4 — GitHub artifact attestations

Artifact attestations giúp cung cấp bằng chứng provenance/integrity về repository, commit và workflow build. Nếu sử dụng:

- cấp permissions tối thiểu cần thiết cho job;
- không tạo attestation cho artifact đã bị chỉnh sửa ngoài luồng;
- kiểm tra repository plan/visibility hỗ trợ use case; tài liệu GitHub hiện ghi khả năng sử dụng có điều kiện theo plan và public/private repository;
- hướng dẫn người nhận cách verify attestation theo tài liệu GitHub.

Nguồn: <https://docs.github.com/en/actions/how-tos/secure-your-work/use-artifact-attestations/use-artifact-attestations>.

---

## 17. GIAI ĐOẠN 15 — LAUNCHER, UPDATE MANIFEST VÀ ROLLBACK ỨNG DỤNG

### 17.1 Manifest là contract có version

Launcher không được đoán URL hoặc cài bản mới theo tên file gần giống. Manifest phải chứa tối thiểu:

```json
{
  "schemaVersion": 1,
  "product": "OceanForge Desktop",
  "channel": "stable",
  "version": "1.0.0",
  "minimumUpdaterVersion": "1.0.0",
  "publishedAtUtc": "<real UTC timestamp>",
  "releaseNotesUrl": "<HTTPS release notes URL>",
  "artifacts": [
    {
      "rid": "win-x64",
      "packageType": "msix",
      "url": "<HTTPS URL of final signed package>",
      "sizeBytes": 0,
      "sha256": "<64 hex chars of actual package>",
      "signaturePolicy": "windows-msix-trusted-publisher"
    }
  ]
}
```

Đây chỉ là schema minh họa, **không phải manifest phát hành thật**. `sizeBytes`/hash/URL phải được sinh từ artifact thực; không dùng số 0/example placeholder trong production.

### 17.2 Update flow an toàn

```text
Check signed/validated manifest
  -> compare current/latest version and channel
  -> download to temporary/staging location
  -> verify HTTPS policy + digest + required signature/provenance
  -> verify platform/RID/version/size/manifest schema
  -> wait for app safe close/checkpoint
  -> apply using platform-supported installation mechanism
  -> launch smoke/health check
  -> record update success
  -> if failed, restore previous app version where platform supports it
```

- Không cho app chạy gói ngay khi tải xong nhưng chưa verify.
- Không cho manifest redirect sang arbitrary HTTP URL.
- Không bỏ TLS certificate validation.
- Không cho release channel Beta ghi đè Stable bằng version thấp/khác track.
- Không xóa SQLite/outbox/local user data trong app update.
- Dùng staged download path và atomic replace/OS package manager phù hợp; không sửa trực tiếp file đang được OS quản lý.

### 17.3 Rollback ứng dụng

1. Dừng rollout/tắt version lỗi trong manifest hoặc distribution policy.
2. Giữ artifact lỗi và logs để điều tra, không âm thầm xóa evidence.
3. Chuyển stable manifest về **artifact cũ đã ký và vẫn hỗ trợ dữ liệu hiện tại**.
4. Verify hash/signature của artifact rollback.
5. Test clean install và upgrade/rollback từ version lỗi trên staging.
6. Công bố incident note hoặc release notice nếu người dùng bị ảnh hưởng.
7. Phát hành patch version đã sửa; không tái sử dụng tag/overwrite tag đã công bố.

Rollback application không làm DB schema tự quay ngược. Nếu phiên bản cũ không tương thích schema mới, phải có API/schema compatibility policy trước khi deploy.

---

## 18. GIAI ĐOẠN 16 — BACKEND CI/CD: API, WORKER VÀ MYSQL

Desktop release và backend release phải có luồng riêng nhưng cùng version/contract policy. Không để Desktop có MySQL credentials; Desktop chỉ gọi API được bảo vệ.

### 18.1 Pipeline backend

```text
PR
  -> build/test/security
  -> container build candidate
  -> integration tests against disposable MySQL 8.4
  -> migration validation
  -> artifact/SBOM/provenance
  -> deploy to staging using OIDC
  -> smoke tests and query/error metrics
  -> backup/restore readiness check
  -> required manual approval
  -> deploy the same container digest to production
  -> apply approved migration job
  -> health check
  -> monitor / rollback application if needed
```

### 18.2 Image identity

- Tag image bằng commit SHA/immutable digest, không dùng `latest` làm deployment identity.
- Sau build, ghi OCI image digest vào release record.
- Staging và production promote đúng digest đã test.
- Nếu cần rebuild image cho environment khác, đánh dấu đó là artifact mới và test lại; không tuyên bố “build once” khi bytes thay đổi.

### 18.3 Database migration gate

Trước migration production:

1. Migration file được review qua PR.
2. Test clean database từ đầu.
3. Test upgrade từ schema trước đó có dữ liệu synthetic đại diện.
4. Kiểm tra thời gian migration, lock, index changes và dung lượng.
5. Xác định backward compatibility với API/Worker/Desktop version đang chạy.
6. Có backup đã hoàn thành và có dấu hiệu xác minh; với thay đổi rủi ro cao, chạy restore drill trong môi trường cô lập.
7. Deploy theo expand/contract: thêm cột/bảng nullable hoặc schema mới; app mới có thể đọc schema cũ/mới; backfill; chuyển đọc/ghi; sau thời gian an toàn mới drop field cũ ở release sau.
8. Chạy migration qua job riêng có quyền DB tối thiểu, không chạy cùng quyền runtime API nếu có thể tránh.
9. Smoke test API queries/indexes và error rates.
10. Ghi migration version, deploy ID, backup ID và kết quả.

### 18.4 Backup/restore

- Thiết lập lịch backup theo RPO/RTO được sản phẩm chốt.
- Lưu backup ở vị trí hạn chế quyền, mã hóa và tách biệt khỏi database runtime.
- Có retention policy, alert khi backup thất bại và kiểm tra dung lượng.
- Restore drill phải được chạy; file backup tồn tại chưa đủ chứng minh phục hồi được.
- Trước nâng MySQL, kiểm tra upgrade path/compatibility, backup và test trên clone/staging. Không giả định downgrade server luôn được hỗ trợ.
- Không chạy backup của user data thật trên CI công khai hoặc upload vào Actions artifact.

Nguồn MySQL chính thức về upgrade/backup: <https://dev.mysql.com/doc/refman/8.4/en/upgrade-best-practices.html> và <https://dev.mysql.com/doc/refman/8.4/en/backup-and-recovery.html>.

### 18.5 Schema backward compatibility

Không deploy Desktop/API mới đòi cột DB chưa được deploy. Không xóa cột/bảng cùng release mà client cũ còn dùng. Mỗi migration phải ghi:

```text
Migration ID:
Owner/reviewer:
Schema change:
Backfill required:
Expected lock/duration:
Compatible app/API versions:
Backup/restore plan:
Forward fix plan:
Dangerous operation?:
Verification queries:
```

---

## 19. GIAI ĐOẠN 17 — STAGING, APPROVAL VÀ PRODUCTION

### 19.1 Staging phải gần production

Staging cần:

- cùng build artifact/container digest;
- cùng kiểu database/connection pattern nhưng dữ liệu synthetic/masked;
- cùng API route/version và package distribution policy;
- config riêng, secret riêng, telemetry/logging phù hợp;
- không cho user thật hoặc external clients dùng nhầm staging.

### 19.2 Smoke tests trước production

Desktop/Launcher:

- download đúng artifact;
- verify hash/signature;
- install/launch;
- login bằng staging account;
- navigation cơ bản và API health;
- local cache/SQLite migration;
- offline/online state không báo nhầm Live;
- update từ bản trước;
- rollback ở staging;
- uninstall có đúng thông báo dữ liệu local.

Backend:

- `/health/live` và `/health/ready` theo thiết kế;
- authn/authz, API contracts;
- DB migration applied đúng phiên bản;
- queue/outbox depth, retry, error rate;
- telemetry ingestion validates schema/idempotency;
- logs không chứa secrets.

### 19.3 Manual gate production

Production release/production deploy chỉ tiếp tục khi reviewer kiểm tra:

- PR/release tag và commit SHA;
- status checks xanh;
- package formats và support matrix;
- signature/verify result;
- SHA-256, SBOM và attestation/provenance nếu được cấu hình;
- database migration/backup plan;
- release notes/known issues;
- rollback plan và người chịu trách nhiệm theo dõi.

Không tự động dùng `workflow_dispatch` từ bất kỳ branch nào để deploy production. Workflow production chỉ nhận ref được phép và chỉ environment protected được cấp quyền.

---

## 20. GIAI ĐOẠN 18 — PUBLISH GITHUB RELEASE VÀ RELEASE NOTES

### Bước 20.1 — Nội dung Release

Mỗi release cần:

- tiêu đề `OceanForge vX.Y.Z`;
- release notes với Added/Changed/Fixed/Security/Known Issues;
- yêu cầu hệ điều hành/RID cụ thể;
- links đến privacy/support/changelog;
- package installer chính thức và định dạng rõ;
- `checksums-sha256.txt`;
- `SBOM.spdx.json` hoặc CycloneDX;
- signature/provenance instructions;
- migration notes nếu backend schema/API đổi;
- upgrade/rollback warnings;
- không có file debug log/config chứa secret.

### Bước 20.2 — Tạo release notes từ PR

Dùng labels nhất quán, ví dụ:

```text
release-note:feature
release-note:fix
release-note:security
release-note:breaking
release-note:none
```

Các PR có `release-note:none` chỉ khi không ảnh hưởng người dùng. Tự tạo notes cần review tay để không lộ security details, internal endpoint hoặc thông tin chưa công bố.

### Bước 20.3 — Upload assets

- Asset names nhất quán: `OceanForge-1.0.0-win-x64.msix`, `OceanForge-1.0.0-linux-x64.deb`, v.v.
- Không upload `.pfx`, `.env`, source map/log nhạy cảm, DB dump hay artifact chưa kiểm tra.
- Không ghi đè asset sau khi published; phát hành patch mới nếu artifact sai.
- Xác minh download asset về máy QA và check hash lại.

GitHub Release tự cung cấp source ZIP/tarball của repository tại tag; không nhầm archive source ấy với binary installer. Xem: <https://docs.github.com/en/repositories/releasing-projects-on-github/about-releases>.

---

## 21. GIAI ĐOẠN 19 — RELEASE CHECKLIST CHUẨN

### 21.1 Trước khi tạo tag

- [ ] Feature freeze/release scope được ghi.
- [ ] Tất cả PR merged, không có sửa tay ngoài Git.
- [ ] `main` đã xanh trên required checks.
- [ ] Unit + integration tests pass.
- [ ] Security/dependency/secret scans xử lý xong.
- [ ] UI smoke tests trên supported OS/RID đạt.
- [ ] API compatibility và DB migration đã review.
- [ ] README/CHANGELOG/known issues cập nhật.
- [ ] Version SemVer được chốt.
- [ ] Installer package script, app icon/identity/version đã kiểm tra.

### 21.2 Sau khi tag/build

- [ ] Release workflow build exact tag commit.
- [ ] Build/Test log và SDK/RID metadata lưu giữ.
- [ ] Windows/Linux package tạo từ đúng RID.
- [ ] Package contents không chứa dev settings/secrets/test keys.
- [ ] Signature verify pass.
- [ ] SHA-256 được tạo sau bước sign/package cuối cùng.
- [ ] SBOM được tạo cho release commit/artifacts theo policy.
- [ ] Provenance/attestation thành công nếu cấu hình.
- [ ] File tải về từ draft được verify trên máy khác.
- [ ] Installation/update/repair/uninstall E2E pass.
- [ ] Không có `TODO`, placeholder URL, `example.com`, dummy hash hoặc test secret trong manifest thực.

### 21.3 Trước Publish stable

- [ ] Reviewer release đã approval.
- [ ] Installer/format chính thức đúng ADR.
- [ ] Release notes phản ánh thay đổi thật.
- [ ] Support matrix và known limitations hiển thị.
- [ ] Previous stable artifact/manifest được giữ để rollback.
- [ ] Update manifest được validate và phát hành bằng quy trình kiểm soát.
- [ ] Monitoring/alert/on-call owner sẵn sàng.

### 21.4 Sau phát hành

- [ ] Cài bản phát hành từ link công khai trên clean machine.
- [ ] Verify signature/hash lại.
- [ ] Kiểm tra startup/login/API errors và crash rate.
- [ ] Kiểm tra update failure rate, sync queue/conflicts và telemetry error rate.
- [ ] Kiểm tra dung lượng/CPU/RAM của reference workload.
- [ ] Ghi nhận vấn đề trong `KNOWN_ISSUES.md`.
- [ ] Sau 1–3 ngày tùy quy mô, tổ chức release retrospective.

---

## 22. ROLLBACK, HOTFIX VÀ INCIDENT RESPONSE

### 22.1 Khi desktop release lỗi

1. Xác định ảnh hưởng: install failure, startup crash, auth, sync, data loss, signature, updater.
2. Dừng rollout/update manifest mới; không tiếp tục phát rộng khi lỗi nghiêm trọng chưa rõ.
3. Giữ evidence: workflow run, commit SHA, package hash/signature, logs đã redacted.
4. Chọn rollback app version tương thích API/schema hiện tại.
5. Test rollback từ đúng version bị lỗi trên staging.
6. Publish rollback manifest đã được ký/xác minh; phát incident notice khi cần.
7. Sửa bằng patch release mới; không tái sử dụng tag.
8. Kiểm tra local SQLite/outbox và pending sync không bị xóa hoặc gửi lặp nguy hiểm.

### 22.2 Khi backend release lỗi

1. Dừng traffic promotion/deployment.
2. Nếu code rollback tương thích schema, deploy lại container digest cũ.
3. Nếu migration đã chạy, **không tự chạy destructive down migration**.
4. Đánh giá forward fix hoặc restore từ backup đã xác minh theo incident commander/DB owner.
5. Kiểm tra idempotency/outbox/queue/realtime resync sau rollback.
6. Kiểm tra health, errors, latency, database locks, queue backlog.
7. Ghi timeline, root cause, customer impact và prevention tasks.

### 22.3 Mức độ incident gợi ý

| Severity | Ví dụ | Phản ứng |
|---|---|---|
| SEV-1 | Lộ secret, phá hủy/mất dữ liệu diện rộng, updater có thể cài gói không hợp lệ | Dừng release, revoke/rotate, isolate, incident lead ngay |
| SEV-2 | Login/sync/API lỗi diện rộng, crash tăng cao | Dừng rollout, rollback/forward fix, cập nhật status |
| SEV-3 | Một màn hình/feature lỗi có workaround | Ticket + patch theo ưu tiên |
| SEV-4 | UI nhỏ/typo không ảnh hưởng chức năng | Gom release kế tiếp |

### 22.4 Hotfix flow

```text
Create fix branch from production tag or main according to policy
  -> fix minimal scope
  -> add regression test
  -> CI/security gate
  -> build once
  -> staging smoke
  -> review/approve
  -> patch tag (e.g. v1.0.1)
  -> sign/hash/SBOM/attest
  -> publish
  -> monitor
  -> merge fix back to main if hotfix branched from a release
```

---

## 23. KIỂM SOÁT DỮ LIỆU, SQLITE VÀ MYSQL KHI UPDATE/UNINSTALL

### 23.1 Desktop update

- Không cài app update đè vào file user data.
- Trước local schema migration: kiểm tra free disk, tạo recovery backup phù hợp, apply migration transactionally nếu provider hỗ trợ và test crash recovery.
- Outbox item chỉ được đánh dấu synced sau server acknowledgement bền vững.
- Không xóa pending outbox nếu user đăng xuất/chuyển account; tách storage theo account/workspace và có UI cảnh báo nếu cần xóa.
- App rollback có thể yêu cầu compatibility với schema; nếu không, cần forward-compatible migration hoặc dữ liệu local migration strategy đã test.

### 23.2 Uninstall

Uninstaller phải phân biệt:

1. file chương trình;
2. shortcut/cache tạm;
3. user preferences;
4. SQLite database/cache/outbox;
5. local logs/diagnostics;
6. dữ liệu server/cloud.

Mặc định nên giữ dữ liệu người dùng hoặc hỏi rõ trước khi xóa local data. Uninstall không được xóa cloud data ngầm. Nếu pending outbox còn tồn tại, thông báo rằng dữ liệu chưa đồng bộ có thể mất nếu người dùng yêu cầu xóa.

### 23.3 Server DB

- Desktop installer không chứa MySQL migration/admin credential.
- API deployment mới kiểm tra schema version và trả lỗi có thể xử lý khi schema chưa tương thích.
- DB migration dùng service account quyền tối thiểu.
- Backup trước thay đổi rủi ro cao; restore test định kỳ.
- Không bao giờ upload production database dump vào GitHub Actions artifact.

---

## 24. PERFORMANCE VÀ OBSERVABILITY SAU PHÁT HÀNH

Theo V4, hiệu năng phải được đo thay vì hứa “CPU/RAM luôn thấp”.

### 24.1 Desktop metrics

- startup time;
- idle CPU;
- private bytes/working set và allocation/GC trend;
- API latency/error rate;
- SQLite query latency, DB file size, outbox depth;
- UI freeze/exception/crash rate;
- sync lag, oldest pending mutation age, conflict count;
- update/install/verification failure count.

Không log raw token, raw key, credential hay nội dung nhạy cảm. Diagnostic export cần redaction và phải được kiểm thử.

### 24.2 Backend metrics

- API latency p50/p95/p99;
- 4xx/5xx rate và auth failure rates;
- MySQL connection saturation, slow queries, lock waits;
- queue/outbox pending count/age;
- worker retry/dead-letter count;
- realtime publish failures;
- backup success/age;
- migration version/health.

### 24.3 Canary / staged rollout

Nếu có cơ chế update manifest hỗ trợ staged rollout:

```text
Internal QA -> small opt-in cohort -> wider cohort -> all stable users
```

Mỗi giai đoạn cần quan sát crash, login failure, update failure, API errors và sync regressions; ngưỡng rollout/rollback phải được quyết định trước. Không giả lập percentage rollout nếu server manifest không thực thi nó.

---

## 25. TROUBLESHOOTING — LỖI THƯỜNG GẶP

| Lỗi | Nguyên nhân thường gặp | Cách xử lý an toàn |
|---|---|---|
| `dotnet restore --locked-mode` fail | lock file chưa có hoặc thay đổi không được commit | Chạy restore local theo policy, review dependency diff, commit lock file; không tắt locked mode lâu dài |
| CI không tìm thấy solution | `SOLUTION_PATH` sai | Kiểm tra `find`/`Get-ChildItem`, sửa env path |
| `dotnet format` fail nhưng local pass | SDK/editorconfig/version khác | So sánh `dotnet --info`, chuẩn hóa `.editorconfig`, chạy lại cùng command |
| CI chạy 0 integration tests | Filter/category sai hoặc không có test | Kiểm tra test discovery và tag; tạo assertion/gate cho số test tối thiểu |
| MySQL service không healthy | health command/password/port sai | Xem service logs, kiểm tra port và test-only connection string |
| Release tag không hợp lệ | Tag không theo SemVer hoặc tag sai commit | Dừng release; tạo tag đúng sau khi xác nhận commit, không force-rewrite đã công bố |
| `dotnet publish` thiếu native library trên Linux | RID/package/dependency không đúng | Publish đúng RID, kiểm tra output/native libs trên target distro và thêm smoke test |
| MSIX signature không hợp lệ | Publisher không khớp certificate, cert hết hạn hoặc package đổi sau ký | Sửa manifest/cert, đóng gói/ký lại, verify trước upload; tạo lại hash/provenance |
| Checksums mismatch | Artifact bị thay, lấy nhầm version hoặc hash sinh trước bước cuối | Dừng phát hành, xác định nguồn; không bảo user bỏ qua cảnh báo |
| Attestation job fail | permissions/plan/subject path/action pin không hợp lệ | Kiểm tra docs/permission/repo plan; không bỏ gate rồi vẫn gọi release trusted |
| GitHub Release draft bị thiếu asset | download-artifact pattern/path mismatch | Kiểm tra artifact names, upload step logs và list file trước `gh release create` |
| Staging pass nhưng production fail | config/schema/secrets/traffic khác | Giữ đúng artifact; so sánh environment config đã redacted, schema, feature flags và rollout conditions |
| Migration fail giữa chừng | DDL/transaction semantics, lock hoặc dữ liệu không hợp lệ | Dừng deploy, giữ evidence, theo migration recovery plan; không chạy migration phá hủy tùy tiện |
| Update thất bại và app không mở | package incompatible hoặc rollback mechanism thiếu | Dừng rollout, phát manifest về artifact cũ tương thích, lưu diagnostics redacted, tái hiện trên staging |
| RAM tăng theo thời gian | retained references/subscriptions/cache không giới hạn | Soak test, dump/gcdump, phân tích object retention; không chỉ ép GC như một “fix” |

---

## 26. RELEASE SUPPORT MATRIX — MẪU CẦN ĐIỀN

Điền phiên bản thật sau QA. Không giữ dấu `TBD` trong release notes stable.

| OS/Version | Architecture | Package | Clean install | Upgrade | Uninstall | Status |
|---|---|---|---|---|---|---|
| Windows <phiên bản đã chốt> | x64 | MSIX hoặc EXE | NOT RUN | NOT RUN | NOT RUN | TBD |
| Ubuntu <LTS đã chốt> | x64 | `.deb` | NOT RUN | NOT RUN | NOT RUN | TBD |
| Debian <version đã chốt, nếu hỗ trợ> | x64 | `.deb` | NOT RUN | NOT RUN | NOT RUN | TBD |
| Linux AppImage targets | x64 | AppImage | NOT RUN | NOT RUN | n/a/package-specific | TBD |

`PASS` chỉ được dùng khi có test report/run URL; `NOT RUN` không được đổi thành PASS theo suy đoán.

---

## 27. THỨ TỰ TRIỂN KHAI KHUYẾN NGHỊ (GATE-BY-GATE)

| Phase | Việc làm | Gate bắt buộc để qua phase |
|---:|---|---|
| 0 | Inventory repo, quyết định package format, support matrix | Paths/project status đã ghi, không suy đoán |
| 1 | GitHub repo, `main`, PR rules, CODEOWNERS, secret protections | Không direct push, PR required, secret policy hoạt động |
| 2 | Pin SDK/dependencies, lock files, `.editorconfig` | Clean clone restore/build pass |
| 3 | CI Windows + Linux | Required status checks hiện đúng, tests chạy thực |
| 4 | CodeQL/dependency review/secret scanning | Findings được triage, false-positive có exception có hạn |
| 5 | Integration MySQL và migration tests | Test DB cô lập, clean+upgrade migrations pass |
| 6 | Nightly, offline/reconnect, performance, restore drill | Jobs kết thúc đúng, report có thể đọc |
| 7 | Publish baseline theo RID | Output chạy được trên target smoke environment |
| 8 | Windows installer và Linux package scripts | Clean install/upgrade/uninstall pass từng OS |
| 9 | Ký gói, hash, SBOM, provenance | Verify thành công từ artifact cuối cùng |
| 10 | Draft release và approval | Checklist toàn bộ được reviewer ký nhận |
| 11 | Staging/backend/DB promotion | Cùng artifact tested, migration/backup gate pass |
| 12 | Stable release/update manifest | Monitor/alert/rollback có sẵn |
| 13 | Release retrospective, dependency/SDK maintenance | Bugs và action items có owner/deadline |

Không nhảy sang phase sau chỉ vì muốn sớm có installer. Nếu phase bị chặn, ghi vào `KNOWN_ISSUES.md` và `NEXT_SESSION.md` với lý do và bước tháo gỡ.

---

## 28. DEFINITION OF DONE — DEVOPS

Một quy trình phát hành chỉ được coi là hoàn chỉnh khi:

- [ ] Một clean clone có thể restore/build/test theo docs.
- [ ] SDK/tool versions và dependency graph có thể tái tạo.
- [ ] `main` được bảo vệ và merge qua PR.
- [ ] PR CI không dùng production secrets.
- [ ] Windows + Linux build matrix đã chạy.
- [ ] Unit/integration/security/migration tests có báo cáo.
- [ ] Package scripts đã tạo artifact phù hợp platform; không đổi đuôi file để giả dạng installer.
- [ ] Installer/package đã qua clean install, upgrade, failure, rollback và uninstall tests.
- [ ] Signing credentials bị cách ly khỏi build/test jobs không tin cậy.
- [ ] Signature, checksum, SBOM, provenance được phân biệt và verify.
- [ ] Release asset khớp với tag/commit/version.
- [ ] Staging dùng đúng artifact sẽ promote; config/secrets tách biệt.
- [ ] Production DB migration có backup, compatibility, approval và recovery plan.
- [ ] Updater manifest được validate, không thực thi package chưa xác minh.
- [ ] Rollback application đã rehearsal và tách riêng khỏi database recovery.
- [ ] Logs/diagnostics không làm lộ secret hoặc dữ liệu không cần thiết.
- [ ] Monitoring/alert và incident owner đã xác định.
- [ ] `RELEASE_CHECKLIST.md`, `ROLLBACK_RUNBOOK.md`, `RELEASE_SUPPORT_MATRIX.md` cập nhật.

---

## 29. NGUỒN TÀI LIỆU CHÍNH THỨC

Kiểm tra lại tài liệu khi thay đổi action/tooling hoặc trước release lớn; các version/action interface có thể thay đổi.

1. Microsoft — .NET releases, patches and support: <https://learn.microsoft.com/dotnet/core/releases-and-support> (tài liệu đang công bố .NET 10 LTS được hỗ trợ đến tháng 11/2028).
2. GitHub — Secure use reference for GitHub Actions: <https://docs.github.com/en/actions/reference/security/secure-use> (pin action bên ngoài bằng full-length commit SHA).
3. GitHub — Workflow syntax: <https://docs.github.com/en/actions/writing-workflows/workflow-syntax-for-github-actions>.
4. GitHub — Security hardening / OIDC: <https://docs.github.com/en/actions/how-tos/secure-your-work/security-harden-deployments>.
5. GitHub — Artifact attestations: <https://docs.github.com/en/actions/how-tos/secure-your-work/use-artifact-attestations/use-artifact-attestations>.
6. GitHub — Workflow artifacts: <https://docs.github.com/en/actions/concepts/workflows-and-actions/workflow-artifacts>.
7. GitHub — Releases: <https://docs.github.com/en/repositories/releasing-projects-on-github/about-releases>.
8. Microsoft — Sign an MSIX package: <https://learn.microsoft.com/en-us/windows/msix/package/sign-app-package-using-signtool>.
9. Microsoft — End-to-end MSIX signing: <https://learn.microsoft.com/en-us/windows/msix/package/sign-msix-package-guide>.
10. Microsoft — .NET single-file deployment: <https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview>.
11. Avalonia — Linux deployment: <https://docs.avaloniaui.net/docs/deployment/linux>.
12. AppImage — Documentation: <https://docs.appimage.org/>.
13. MySQL 8.4 — Upgrade best practices: <https://dev.mysql.com/doc/refman/8.4/en/upgrade-best-practices.html>.
14. MySQL 8.4 — Backup and recovery: <https://dev.mysql.com/doc/refman/8.4/en/backup-and-recovery.html>.

### Điều tài liệu không khẳng định

- File runbook không chứng minh repository hiện có build thành công.
- YAML mẫu không chứng minh action pins/package scripts/signing đã được xác thực trên repository của bạn.
- Có checksum không đồng nghĩa artifact đã được ký.
- Có SBOM không đồng nghĩa không có vulnerability.
- Có attestation không đồng nghĩa installer đã vượt E2E test.
- Có staging xanh không chứng minh mọi distro/Windows configuration đều được hỗ trợ.
- Có backup không chứng minh có thể restore cho đến khi đã thử khôi phục.

---

## 30. CÔNG VIỆC TIẾP THEO CHÍNH XÁC

Thực hiện theo thứ tự; không làm tất cả cùng một lúc:

1. Kiểm kê repo và điền `docs/devops/REPOSITORY_PATHS.md`.
2. Chốt Windows primary package (MSIX hoặc signed EXE) và Linux support/package matrix bằng ADR.
3. Hoàn thiện solution/project structure và lock files.
4. Thêm `ci.yml`, pin action SHA đã review, chạy PR test trên Windows/Linux.
5. Bảo vệ `main`, thiết lập required checks và CODEOWNERS.
6. Thêm CodeQL/Dependabot/secret scanning.
7. Tạo integration test MySQL và migration runner thật; bỏ placeholder trước khi gọi gate này là xong.
8. Thêm publish baseline và test executable trên supported platforms.
9. Tạo package scripts thật: Windows installer + `.deb`; AppImage chỉ khi có test matrix.
10. Cấu hình signing identity/managed signing, tạo verify job và cách ly secret trong release environment.
11. Hoàn thành release workflow, SBOM/checksum/attestation và tạo draft.
12. Rehearse install/upgrade/rollback/uninstall và DB restore trên staging.
13. Chỉ sau khi checklist đạt mới publish stable; theo dõi và retrospective.

**Quy tắc cuối cùng:** CI phải kiểm tra code, release workflow phải tạo đúng artifact, QA phải kiểm tra artifact, signing phải xác thực publisher/integrity, migration phải bảo vệ dữ liệu và rollback phải được diễn tập. Không thay bằng các cam kết chung như “build đã xanh”, “đã mã hóa” hoặc “đã có backup”.
