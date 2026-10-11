# OceanForge Desktop — Requirements Traceability Matrix (TRACEABILITY.md)

| Req ID | Requirement | Spec Ref | Module / Implementation | Test Coverage | Trạng thái |
|---|---|---|---|---|---|
| REQ-001 | Zero Credential Harvesting | V4 §0, §3 | `OceanForge.Contracts` (Không chứa token/pass) | `DomainTests.RobloxTelemetryPayloadValidation` | Verified |
| REQ-002 | FNV-1a Checksum Deduplication | V4 §7, Lua script | `OceanForge.Domain.Hashing.Fnv1aHasher` | `DomainTests.Fnv1aHasher_Matches_LuaChecksum` | Verified |
| REQ-003 | Bounded Queue Telemetry Buffer | V4 §9 | `OceanForge.Telemetry.Ingestion.TelemetryIngestionService` | `ApplicationTests.IngestTelemetry_ProcessesEvent` | Verified |
| REQ-004 | Multi-Agent AI Deliberation | AI Spec | `OceanForge.AI.AiDebateService` | `ApplicationTests.RunDebate_ProducesAllFourPerspectives` | Verified |
| REQ-005 | Hardware Telemetry (CPU/RAM/Thread) | V4 §9, Infopanel | `OceanForge.Desktop.ViewModels.MainViewModel` | UI Integration | Verified |
| REQ-006 | Master Abyssal Design Tokens | UI Spec §96.4 | `OceanForge.Desktop.App.axaml`, `MainWindow.axaml` | UI Rendering | Verified |
