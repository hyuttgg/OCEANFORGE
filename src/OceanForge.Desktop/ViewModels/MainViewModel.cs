using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OceanForge.AI.Services;
using OceanForge.Application.Auth;
using OceanForge.Application.Catalog;
using OceanForge.Application.DTOs;
using OceanForge.Application.ScriptKey;
using OceanForge.Application.Security;
using OceanForge.Contracts.AI;
using OceanForge.Contracts.Auth;
using OceanForge.Contracts.Catalog;
using OceanForge.Contracts.Hardware;
using OceanForge.Contracts.ScriptKey;
using OceanForge.Contracts.Telemetry;
using OceanForge.Local.Persistence.Context;
using OceanForge.Local.Persistence.Entities;
using OceanForge.Local.Persistence.Repositories;
using OceanForge.Telemetry.HttpBridge;
using OceanForge.Telemetry.Services;

namespace OceanForge.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ITelemetryIngestionService _telemetryService;
    private readonly IAiDebateService _aiDebateService;
    private readonly LocalTelemetryBridgeServer _bridgeServer;
    private readonly ILocalAccountRepository _localRepo;
    private readonly IBloxFruitsCatalogService _catalogService;
    private readonly IOAuthAuthenticationService _authService;
    private readonly IHardwareMonitoringService _hardwareService;
    private readonly IScriptKeyManagerService _scriptKeyService;
    private readonly ISystemSecurityHardeningService _securityHardeningService;
    private readonly ISystemResourceOptimizerService _resourceOptimizerService;
    private readonly Process _currentProcess;

    private string _rawStandardClipboardScript = string.Empty;
    private string _rawEncryptedClipboardScript = string.Empty;
    private string _activeKeyCode = string.Empty;
    private string? _overrideOsName;

    [ObservableProperty]
    private string _windowTitle = "OceanForge Desktop — Abyssal Command Center v4.0";

    [ObservableProperty]
    private string _engineStatus = "ONLINE | 60 FPS";

    [ObservableProperty]
    private string _bridgeStatus = "LISTENING :5088";

    [ObservableProperty]
    private string _cpuUsage = "0.8% CPU";

    [ObservableProperty]
    private string _ramUsage = "78 MB RAM";

    [ObservableProperty]
    private string _diskUsage = "120.0 GB Trống";

    [ObservableProperty]
    private string _diskDetails = "Ổ C: • WAL: 0.0 MB";

    [ObservableProperty]
    private string _ringBufferCursorDisplay = "Con trỏ: 0/120";

    [ObservableProperty]
    private double _packetsPerSecond = 0.0;

    [ObservableProperty]
    private long _totalPacketsCount = 0;

    [ObservableProperty]
    private long _dedupedPacketsCount = 0;

    [ObservableProperty]
    private int _activeAccountsCount = 0;

    [ObservableProperty]
    private int _currentTabIndex = 0;

    [ObservableProperty]
    private string _currentScreenId = "06"; // Default to Screen 06 Dashboard

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private string _selectedSeaFilter = "All";

    [ObservableProperty]
    private string _selectedCatalogCategoryName = "All";

    [ObservableProperty]
    private string _statusNotice = string.Empty;

    [ObservableProperty]
    private string _consensusVerdict = "HỆ THỐNG SẴN SÀNG: Chưa có cảnh báo bất thường. FNV-1a Checksum và Channel Buffer đang hoạt động tối ưu.";

    [ObservableProperty]
    private string _selectedDebateTopic = "Tối ưu hóa Ingestion Telemetry & FNV-1a Checksum khi Hạm đội tăng lên 50+ Accounts";

    [ObservableProperty]
    private string _selectedMapSea = "All";

    // Drawer state (Screen 07)
    [ObservableProperty]
    private AccountSummaryDto? _selectedAccount;

    [ObservableProperty]
    private bool _isAccountDrawerOpen = false;

    // Hardware Telemetry Snapshot (Screen 18)
    [ObservableProperty]
    private HardwareMetricsSnapshot _hardwareSnapshot = new();

    // Authentication & OAuth PKCE / Email States
    [ObservableProperty]
    private bool _isUserLoggedIn = false;

    [ObservableProperty]
    private string _userDisplayName = "Khách (Chưa đăng nhập)";

    [ObservableProperty]
    private string _userEmail = string.Empty;

    [ObservableProperty]
    private string _authProviderBadge = "GUEST";

    [ObservableProperty]
    private string _userAvatarUrl = string.Empty;

    [ObservableProperty]
    private string _pkceChallengePreview = "PKCE Method: S256 | State: Ready for Google / Discord OAuth 2.0";

    [ObservableProperty]
    private string _lastEmailDeliveryStatus = "READY (QUEUE IDLE)";

    [ObservableProperty]
    private string _lastWelcomeEmailSummary = "Khi tài khoản mới đăng nhập bằng Google hoặc Discord lần đầu, hệ thống sẽ tự động xếp hàng (EMAIL_QUEUED) và gửi email chào mừng qua Transactional Email Worker.";

    // Get Script Lua & Key Manager States (00_header.png .. 09_tom_tat_loi_ich.png)
    [ObservableProperty]
    private string _officialLuaLoaderLine = ScriptKeyManagerService.DefaultLuaLoader;

    [ObservableProperty]
    private string _detectedOsDisplay = "Windows 11 (Operating System)";

    [ObservableProperty]
    private string _detectedHardwareIdsDisplay = "HWID-DETECTING • CPU-X64";

    [ObservableProperty]
    private string _currentGeneratedKeyDisplay = "Key: OF-8F3K-7D2M (Tự động tạo)";

    [ObservableProperty]
    private string _currentKeyStateText = "ACTIVE";

    [ObservableProperty]
    private string _scriptKeyStatusBadge = "SẴN SÀNG LẤY SCRIPT";

    [ObservableProperty]
    private string _scriptClipboardStatusTitle = "✔ Sẵn sàng lấy script";

    [ObservableProperty]
    private string _scriptClipboardStatusDetail = "Hệ thống sẽ tự động tạo key và dán vào clipboard khi bạn bấm Get Script.";

    [ObservableProperty]
    private string _currentKeyJsonPayload = string.Empty;

    [ObservableProperty]
    private string _activeClipboardLuaContent = string.Empty;

    [ObservableProperty]
    private bool _isLuaSourceEncrypted = false;

    [ObservableProperty]
    private bool _isKeyRateLimited = false;

    [ObservableProperty]
    private string _rateLimitJsonPreview = string.Empty;

    [ObservableProperty]
    private string _antiSpamIpUsageDisplay = "1 / 10 request / giờ (IP: 127.0.0.1)";

    [ObservableProperty]
    private string _antiSpamCooldownDisplay = "Trạng thái: Hợp lệ (Sẵn sàng)";

    // System Resource Optimizer & Security Hardening States
    [ObservableProperty]
    private string _optimizationStatusBadge = "CPU ULTRA 60 FPS • RAM COMPACT • WAL OPTIMAL";

    [ObservableProperty]
    private string _lastOptimizationSummary = "Thuật toán điều phối CPU Adaptive Governor (16ms/frame), nén LOH + Trim Working Set RAM và Checkpoint SQLite WAL đang hoạt động tự động.";

    [ObservableProperty]
    private string _securityPostureBadge = "A+ ZERO-TRUST (100%)";

    [ObservableProperty]
    private string _securityPostureDetails = "Span<char> Zero-Alloc Scrubber • HMAC-SHA256 Replay Guard • CryptographicOperations.ZeroMemory • Loopback 127.0.0.1";

    public bool IsDashboardTab => CurrentTabIndex == 0;
    public bool IsFleetTab => CurrentTabIndex == 1;
    public bool IsWorldMapTab => CurrentTabIndex == 2;
    public bool IsWikiTab => CurrentTabIndex == 3;
    public bool IsAiDebateTab => CurrentTabIndex == 4;
    public bool IsSystemHealthTab => CurrentTabIndex == 5;

    // Full 24 Screens + Launcher + Script Key Manager Visibility
    public bool IsScreen01 => CurrentScreenId == "01";
    public bool IsScreen02 => CurrentScreenId == "02";
    public bool IsScreen03 => CurrentScreenId == "03";
    public bool IsScreen04 => CurrentScreenId == "04";
    public bool IsScreen05 => CurrentScreenId == "05";
    public bool IsScreen06 => CurrentScreenId == "06";
    public bool IsScreen07 => CurrentScreenId == "07";
    public bool IsScreen08 => CurrentScreenId == "08";
    public bool IsScreen09 => CurrentScreenId == "09";
    public bool IsScreen10 => CurrentScreenId == "10";
    public bool IsScreen11 => CurrentScreenId == "11";
    public bool IsScreen12 => CurrentScreenId == "12";
    public bool IsScreen13 => CurrentScreenId == "13";
    public bool IsScreen14 => CurrentScreenId == "14";
    public bool IsScreen15 => CurrentScreenId == "15";
    public bool IsScreen16 => CurrentScreenId == "16";
    public bool IsScreen17 => CurrentScreenId == "17";
    public bool IsScreen18 => CurrentScreenId == "18";
    public bool IsScreen19 => CurrentScreenId == "19";
    public bool IsScreen20 => CurrentScreenId == "20";
    public bool IsScreen21 => CurrentScreenId == "21";
    public bool IsScreen22 => CurrentScreenId == "22";
    public bool IsScreen23 => CurrentScreenId == "23";
    public bool IsScreen24 => CurrentScreenId == "24";
    public bool IsScreenLauncher => CurrentScreenId == "launcher";
    public bool IsScreenScript => CurrentScreenId == "script";

    public ObservableCollection<AiDebateMessage> Debates { get; } = [];
    public ObservableCollection<AccountSummaryDto> Accounts { get; } = [];
    public ObservableCollection<AccountSummaryDto> FilteredAccounts { get; } = [];
    public ObservableCollection<BloxCatalogItem> CatalogItems { get; } = [];
    public ObservableCollection<ScriptKeyRecordDto> GeneratedKeysHistory { get; } = [];
    public ObservableCollection<ConnectedIdentityDto> ConnectedIdentities { get; } = [];
    public ObservableCollection<SecurityAuditLogDto> SecurityAuditLogs { get; } = [];

    public MainViewModel()
    {
        _telemetryService = new TelemetryIngestionService();
        _aiDebateService = new AiDebateService();
        _localRepo = new LocalAccountRepository(new OceanForgeLocalDbContext());
        _catalogService = new BloxFruitsWikiCatalogService();
        _authService = new OAuthAuthenticationService();
        _hardwareService = new HardwareMonitoringService();
        _scriptKeyService = new ScriptKeyManagerService();
        _securityHardeningService = new SystemSecurityHardeningService();
        _resourceOptimizerService = new SystemResourceOptimizerService();
        _currentProcess = Process.GetCurrentProcess();

        _authService.OnAuthStateChanged += OnAuthStateChangedHandler;

        // Start local HTTP Bridge receiver on port 5088
        _bridgeServer = new LocalTelemetryBridgeServer(_telemetryService, 5088);
        _bridgeServer.Start();
        BridgeStatus = _bridgeServer.IsRunning ? "LISTENING :5088" : "BRIDGE STANDBY";

        _telemetryService.OnAccountUpdated += OnAccountUpdatedHandler;

        // Initialize Catalog
        LoadCatalogItems("All");

        // Initialize OS detection & initial sample Key + Lua script
        _ = InitializeScriptKeyStateAsync();

        // Seed initial telemetry sample data matching Roblox Blox Fruits
        _ = SeedInitialDataAsync();
    }

    partial void OnIsLuaSourceEncryptedChanged(bool value)
    {
        if (!string.IsNullOrWhiteSpace(_rawStandardClipboardScript))
        {
            ActiveClipboardLuaContent = value ? _rawEncryptedClipboardScript : _rawStandardClipboardScript;
            _ = TrySetSystemClipboardAsync(ActiveClipboardLuaContent);
            StatusNotice = value
                ? $"Đã chuyển sang Chế độ Mã hóa nguồn Lua (XOR ByteTable) và copy vào Clipboard!"
                : $"Đã chuyển sang Chế độ Script Lua + JSON chuẩn và copy vào Clipboard!";
        }
    }

    partial void OnCurrentTabIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsDashboardTab));
        OnPropertyChanged(nameof(IsFleetTab));
        OnPropertyChanged(nameof(IsWorldMapTab));
        OnPropertyChanged(nameof(IsWikiTab));
        OnPropertyChanged(nameof(IsAiDebateTab));
        OnPropertyChanged(nameof(IsSystemHealthTab));
    }

    partial void OnCurrentScreenIdChanged(string value)
    {
        OnPropertyChanged(nameof(IsScreen01));
        OnPropertyChanged(nameof(IsScreen02));
        OnPropertyChanged(nameof(IsScreen03));
        OnPropertyChanged(nameof(IsScreen04));
        OnPropertyChanged(nameof(IsScreen05));
        OnPropertyChanged(nameof(IsScreen06));
        OnPropertyChanged(nameof(IsScreen07));
        OnPropertyChanged(nameof(IsScreen08));
        OnPropertyChanged(nameof(IsScreen09));
        OnPropertyChanged(nameof(IsScreen10));
        OnPropertyChanged(nameof(IsScreen11));
        OnPropertyChanged(nameof(IsScreen12));
        OnPropertyChanged(nameof(IsScreen13));
        OnPropertyChanged(nameof(IsScreen14));
        OnPropertyChanged(nameof(IsScreen15));
        OnPropertyChanged(nameof(IsScreen16));
        OnPropertyChanged(nameof(IsScreen17));
        OnPropertyChanged(nameof(IsScreen18));
        OnPropertyChanged(nameof(IsScreen19));
        OnPropertyChanged(nameof(IsScreen20));
        OnPropertyChanged(nameof(IsScreen21));
        OnPropertyChanged(nameof(IsScreen22));
        OnPropertyChanged(nameof(IsScreen23));
        OnPropertyChanged(nameof(IsScreen24));
        OnPropertyChanged(nameof(IsScreenLauncher));
        OnPropertyChanged(nameof(IsScreenScript));

        // Sync legacy tab index where applicable
        if (value == "06") CurrentTabIndex = 0;
        else if (value == "05") CurrentTabIndex = 1;
        else if (value == "09") CurrentTabIndex = 2;
        else if (value == "08") CurrentTabIndex = 3;
        else if (value == "21") CurrentTabIndex = 4;
        else if (value == "18") CurrentTabIndex = 5;
    }

    private async Task InitializeScriptKeyStateAsync()
    {
        var osInfo = _scriptKeyService.DetectCurrentOs();
        DetectedOsDisplay = $"{osInfo.OsName} (Operating System)";
        DetectedHardwareIdsDisplay = $"{osInfo.Hwid} • {osInfo.CpuId} • {osInfo.Architecture}";

        // Generate initial ready key bypassing cooldown so the user immediately sees a valid key & JSON preview
        var initResult = await _scriptKeyService.GenerateScriptAndKeyAsync(forceBypassCooldown: true);
        ApplyKeyGenerationResultToUi(initResult, isInitialSeed: true);
    }

    [RelayCommand]
    public async Task GetScriptAndCopyAsync()
    {
        var result = await _scriptKeyService.GenerateScriptAndKeyAsync(overrideOsName: _overrideOsName);
        ApplyKeyGenerationResultToUi(result, isInitialSeed: false);

        if (result.Success)
        {
            await TrySetSystemClipboardAsync(ActiveClipboardLuaContent);
            StatusNotice = $"Hoàn tất! Đã tạo Key {result.Payload?.Key} cho {result.Payload?.Os} và dán JSON + Script Lua vào Clipboard!";
        }
        else
        {
            StatusNotice = $"[CHỐNG SPAM KEY] {result.Message} (Thử lại sau {result.RetryAfterSeconds}s)";
        }
    }

    [RelayCommand]
    public async Task CopyGeneratedScriptOnlyAsync()
    {
        if (string.IsNullOrWhiteSpace(ActiveClipboardLuaContent))
        {
            await GetScriptAndCopyAsync();
            return;
        }

        await TrySetSystemClipboardAsync(ActiveClipboardLuaContent);
        ScriptKeyStatusBadge = "ĐÃ COPY CLIPBOARD";
        ScriptClipboardStatusTitle = "✔ Hoàn tất!";
        ScriptClipboardStatusDetail = "Script đã được dán vào clipboard. Bạn có thể mở Roblox và paste để sử dụng.";
        StatusNotice = $"Đã sao chép Script Lua + JSON ({ActiveClipboardLuaContent.Length} ký tự) vào Clipboard!";
    }

    [RelayCommand]
    public async Task RevokeCurrentKeyAsync()
    {
        if (string.IsNullOrWhiteSpace(_activeKeyCode))
        {
            StatusNotice = "Không có Key đang hoạt động để hủy.";
            return;
        }

        var revokeResult = await _scriptKeyService.RevokeKeyAsync(_activeKeyCode, "Người dùng bấm Hủy Key trực tiếp trên giao diện");
        if (revokeResult.Success)
        {
            CurrentKeyStateText = "REVOKED";
            CurrentGeneratedKeyDisplay = $"Key: {_activeKeyCode} (Đã hủy / Revoked)";
            ScriptKeyStatusBadge = "KEY ĐÃ BỊ HỦY";
            ScriptClipboardStatusTitle = "🗑️ Đã hủy Key thành công";
            ScriptClipboardStatusDetail = $"Kết quả trả về: {revokeResult.RawResponseJson} — Key đã bị xóa khỏi Cache.";
            StatusNotice = $"Đã hủy Key {_activeKeyCode} thành công ({revokeResult.RawResponseJson}).";
            RefreshKeyHistoryAndAntiSpamUi();
        }
    }

    [RelayCommand]
    public async Task SelectSimulatedOsAsync(string osName)
    {
        if (string.IsNullOrWhiteSpace(osName)) return;
        _overrideOsName = osName;
        var classified = _scriptKeyService.ClassifyOsDescription(osName);
        DetectedOsDisplay = $"{classified.OsName} ({classified.OsFamily} Operating System)";

        // Bypass cooldown when switching OS in simulator so user can immediately inspect the updated JSON + Key
        var result = await _scriptKeyService.GenerateScriptAndKeyAsync(overrideOsName: classified.OsName, forceBypassCooldown: true);
        ApplyKeyGenerationResultToUi(result, isInitialSeed: false);
        await TrySetSystemClipboardAsync(ActiveClipboardLuaContent);
        StatusNotice = $"Đã chuyển hệ điều hành sang {classified.OsName} và tự động cập nhật JSON + Key ({result.Payload?.Key}) trong Clipboard!";
    }

    [RelayCommand]
    public async Task SimulateKeySpamRateLimitAsync()
    {
        // Trigger two rapid requests without bypass to demonstrate the 5-minute cooldown & RATE_LIMIT JSON response
        await _scriptKeyService.GenerateScriptAndKeyAsync(overrideOsName: _overrideOsName);
        var rateLimitedResult = await _scriptKeyService.GenerateScriptAndKeyAsync(overrideOsName: _overrideOsName);
        ApplyKeyGenerationResultToUi(rateLimitedResult, isInitialSeed: false);
        StatusNotice = $"[TEST ANTI-SPAM] Đã kích hoạt bộ chặn Spam Key: {rateLimitedResult.Message}";
    }

    [RelayCommand]
    public void ResetKeyCooldown()
    {
        _scriptKeyService.ResetRateLimitsForTesting();
        IsKeyRateLimited = false;
        RateLimitJsonPreview = string.Empty;
        ScriptKeyStatusBadge = "SẴN SÀNG LẤY SCRIPT";
        ScriptClipboardStatusTitle = "✔ Đã đặt lại bộ đếm Cooldown Anti-Spam";
        ScriptClipboardStatusDetail = "Bạn có thể tạo Key mới ngay lập tức mà không cần chờ 5 phút.";
        RefreshKeyHistoryAndAntiSpamUi();
        StatusNotice = "Đã reset bộ đếm Anti-Spam Cooldown.";
    }

    private void ApplyKeyGenerationResultToUi(KeyGenerationResult result, bool isInitialSeed)
    {
        if (result.Success && result.Payload != null)
        {
            _activeKeyCode = result.Payload.Key;
            _rawStandardClipboardScript = result.CombinedClipboardScript;
            _rawEncryptedClipboardScript = result.EncryptedLuaScript;

            IsKeyRateLimited = false;
            RateLimitJsonPreview = string.Empty;
            DetectedOsDisplay = $"{result.Payload.Os} (Operating System)";
            CurrentGeneratedKeyDisplay = $"Key: {result.Payload.Key} (Tự động tạo)";
            CurrentKeyStateText = "ACTIVE";
            CurrentKeyJsonPayload = result.RawJsonPayload;
            ActiveClipboardLuaContent = IsLuaSourceEncrypted ? _rawEncryptedClipboardScript : _rawStandardClipboardScript;

            if (isInitialSeed)
            {
                ScriptKeyStatusBadge = "SẴN SÀNG LẤY SCRIPT";
                ScriptClipboardStatusTitle = "✔ Sẵn sàng lấy script";
                ScriptClipboardStatusDetail = "Hệ thống sẽ tự động tạo key và dán vào clipboard.";
            }
            else
            {
                ScriptKeyStatusBadge = "ĐÃ DÁN VÀO CLIPBOARD";
                ScriptClipboardStatusTitle = "✔ Hoàn tất!";
                ScriptClipboardStatusDetail = result.Message;
            }
        }
        else
        {
            IsKeyRateLimited = true;
            ScriptKeyStatusBadge = "BỊ CHẶN RATE_LIMIT";
            ScriptClipboardStatusTitle = "⛔ Bị giới hạn tốc độ tạo Key (Anti-Spam)";
            ScriptClipboardStatusDetail = result.Message;
            RateLimitJsonPreview = result.RateLimitJsonResponse;
        }

        RefreshKeyHistoryAndAntiSpamUi();
    }

    private void RefreshKeyHistoryAndAntiSpamUi()
    {
        var keys = _scriptKeyService.GetAllKeys();
        GeneratedKeysHistory.Clear();
        foreach (var k in keys.Take(8))
        {
            GeneratedKeysHistory.Add(k);
        }

        var spamStatus = _scriptKeyService.GetAntiSpamStatus(osName: _overrideOsName);
        AntiSpamIpUsageDisplay = $"{spamStatus.RequestsInLastHour} / {spamStatus.MaxRequestsPerHour} request / giờ (IP: {spamStatus.ClientIp})";
        AntiSpamCooldownDisplay = spamStatus.IsTemporarilyLocked
            ? $"ĐANG KHÓA TẠM THỜI 30 PHÚT (Còn {spamStatus.RemainingCooldownSeconds}s)"
            : spamStatus.RemainingCooldownSeconds > 0
                ? $"Cooldown thiết bị: còn {spamStatus.RemainingCooldownSeconds}s / 300s"
                : "Trạng thái: Hợp lệ (Sẵn sàng tạo key)";
    }

    private static async Task TrySetSystemClipboardAsync(string text)
    {
        try
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow?.Clipboard is { } clipboard)
            {
                await clipboard.SetTextAsync(text);
            }
        }
        catch
        {
            // Headless / test environment fallback
        }
    }

    private void LoadCatalogItems(string categoryName)
    {
        CatalogItems.Clear();
        var all = _catalogService.GetAllItems();

        if (categoryName == "All")
        {
            foreach (var item in all) CatalogItems.Add(item);
        }
        else if (Enum.TryParse<BloxItemCategory>(categoryName, true, out var cat))
        {
            foreach (var item in all.Where(i => i.Category == cat)) CatalogItems.Add(item);
        }
    }

    [RelayCommand]
    public void FilterCatalog(string categoryName)
    {
        SelectedCatalogCategoryName = categoryName;
        LoadCatalogItems(categoryName);
    }

    [RelayCommand]
    public async Task LoginGoogleAsync()
    {
        var result = await _authService.LoginWithGoogleAsync();
        UpdateOAuthUiDetails(result);
        if (result.Success && result.User != null)
        {
            StatusNotice = result.Message;
        }
    }

    [RelayCommand]
    public async Task LoginDiscordAsync()
    {
        var result = await _authService.LoginWithDiscordAsync();
        UpdateOAuthUiDetails(result);
        if (result.Success && result.User != null)
        {
            StatusNotice = result.Message;
        }
    }

    [RelayCommand]
    public async Task LinkSecondaryGoogleAsync()
    {
        var (success, message) = await _authService.LinkProviderAsync(
            AuthProvider.Google,
            "google-linked-998877",
            "captain.oceanforge@gmail.com",
            "Captain Khanh (Google)");
        RefreshAuthAuditAndIdentities();
        StatusNotice = message;
    }

    [RelayCommand]
    public async Task LinkSecondaryDiscordAsync()
    {
        var (success, message) = await _authService.LinkProviderAsync(
            AuthProvider.Discord,
            "discord-linked-789456",
            "abyssal.navigator@discord.gg",
            "AbyssalAdmiral#1337");
        RefreshAuthAuditAndIdentities();
        StatusNotice = message;
    }

    [RelayCommand]
    public async Task TestUnlinkProviderAsync()
    {
        if (_authService.CurrentUser == null)
        {
            StatusNotice = "Hãy đăng nhập Google hoặc Discord trước để kiểm thử tính năng Unlink Protection.";
            return;
        }

        var firstProvider = _authService.CurrentUser.ConnectedIdentities.FirstOrDefault()?.Provider ?? _authService.CurrentUser.Provider;
        var (_, message) = await _authService.UnlinkProviderAsync(firstProvider);
        RefreshAuthAuditAndIdentities();
        StatusNotice = message;
    }

    [RelayCommand]
    public void SignOut()
    {
        _authService.SignOut();
        RefreshAuthAuditAndIdentities();
        StatusNotice = "Đã đăng xuất phiên làm việc và xóa session token.";
    }

    private void UpdateOAuthUiDetails(OAuthLoginResult result)
    {
        if (result.PkceSession != null)
        {
            PkceChallengePreview = $"PKCE S256 Challenge: {result.PkceSession.CodeChallenge[..Math.Min(24, result.PkceSession.CodeChallenge.Length)]}... | State: {result.PkceSession.State[..12]}...";
        }

        if (result.WelcomeEmailEvent != null)
        {
            LastEmailDeliveryStatus = $"{result.WelcomeEmailEvent.DeliveryStatus} (Attempt {result.WelcomeEmailEvent.AttemptCount})";
            LastWelcomeEmailSummary = $"To: {result.WelcomeEmailEvent.RecipientEmail} | Method: {result.WelcomeEmailEvent.Provider} | Registered: {result.WelcomeEmailEvent.QueuedAtUtc:dd MMM yyyy HH:mm} UTC | Status: Verified via OAuth";
        }
        else if (result.Success && result.User != null)
        {
            LastEmailDeliveryStatus = "EXISTING ACCOUNT (NO DUPLICATE EMAIL)";
            LastWelcomeEmailSummary = $"Tài khoản {result.User.Email} đã tồn tại trong OceanForge. Hệ thống đăng nhập trực tiếp (LOGIN_SUCCESS) và không gửi lặp lại email đăng ký.";
        }

        RefreshAuthAuditAndIdentities();
    }

    private void RefreshAuthAuditAndIdentities()
    {
        ConnectedIdentities.Clear();
        if (_authService.CurrentUser != null)
        {
            foreach (var id in _authService.CurrentUser.ConnectedIdentities)
            {
                ConnectedIdentities.Add(id);
            }
        }

        SecurityAuditLogs.Clear();
        foreach (var log in _authService.GetSecurityAuditLogs().Reverse().Take(8))
        {
            SecurityAuditLogs.Add(log);
        }
    }

    private void OnAuthStateChangedHandler(AuthUserProfile? user)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (user != null && user.IsAuthenticated)
            {
                IsUserLoggedIn = true;
                UserDisplayName = user.DisplayName;
                UserEmail = user.Email;
                AuthProviderBadge = user.Provider.ToString().ToUpperInvariant();
                UserAvatarUrl = user.AvatarUrl;
            }
            else
            {
                IsUserLoggedIn = false;
                UserDisplayName = "Khách (Chưa đăng nhập)";
                UserEmail = string.Empty;
                AuthProviderBadge = "GUEST";
                UserAvatarUrl = string.Empty;
            }
            RefreshAuthAuditAndIdentities();
        });
    }

    private async Task SeedInitialDataAsync()
    {
        var sample1 = new RobloxTelemetryPayload
        {
            RobloxUsername = "CaptainKha_77",
            Level = 2600,
            Beli = 145000000,
            Fragments = 75200,
            Race = "Cyborg V4",
            Sea = "Sea 3",
            Fruit = "Kitsune (Mastery 600)",
            Sword = "Cursed Dual Katana",
            FightingStyle = "Godhuman",
            Status = "Leviathan Hunting 🐉",
            Location = "Tiki Outpost",
            Playtime = 142000,
            Hwid = "HWID-X889-WINDOWS",
            Executor = "Wave v3.2",
            CsharpAccelerated = true,
            MagnetTokens = 1250,
            MagnetPower = new MagnetPowerDto { Stage = 3, Power = 85000, TargetPower = 100000 },
            Secrets = new SecretsProgressDto { Completed = 8, Total = 10, Percentage = 80.0 }
        };

        var sample2 = new RobloxTelemetryPayload
        {
            RobloxUsername = "AbyssalRider_VN",
            Level = 2550,
            Beli = 88200000,
            Fragments = 42100,
            Race = "Ghoul V4",
            Sea = "Sea 3",
            Fruit = "Dragon (Mastery 580)",
            Sword = "Shark Anchor",
            FightingStyle = "Sanguine Art",
            Status = "Kitsune Shrine Trial ⛩️",
            Location = "Sea of Treats",
            Playtime = 98000,
            Hwid = "HWID-Z102-WINDOWS",
            Executor = "Synapse Z",
            CsharpAccelerated = true,
            MagnetTokens = 840,
            MagnetPower = new MagnetPowerDto { Stage = 2, Power = 42000, TargetPower = 60000 },
            Secrets = new SecretsProgressDto { Completed = 5, Total = 10, Percentage = 50.0 }
        };

        var sample3 = new RobloxTelemetryPayload
        {
            RobloxUsername = "Corsair_Ghost",
            Level = 2200,
            Beli = 32000000,
            Fragments = 15800,
            Race = "Mink V3",
            Sea = "Sea 2",
            Fruit = "Portal (Mastery 400)",
            Sword = "True Triple Katana",
            FightingStyle = "Electric Claw",
            Status = "Chest Farming ⚡",
            Location = "Green Zone",
            Playtime = 45000,
            Hwid = "HWID-M455-LINUX",
            Executor = "Krampus Engine",
            CsharpAccelerated = true,
            MagnetTokens = 310,
            MagnetPower = new MagnetPowerDto { Stage = 1, Power = 12000, TargetPower = 30000 },
            Secrets = new SecretsProgressDto { Completed = 2, Total = 10, Percentage = 20.0 }
        };

        await _telemetryService.IngestAsync(sample1);
        await _telemetryService.IngestAsync(sample2);
        await _telemetryService.IngestAsync(sample3);

        await RunAiDebateAsync();
        await RefreshMetricsAsync();

        if (Accounts.Count > 0)
        {
            SelectedAccount = Accounts[0];
        }
    }

    [RelayCommand]
    public void SelectTab(object? tabParam)
    {
        if (tabParam != null && int.TryParse(tabParam.ToString(), out int tabIndex))
        {
            CurrentTabIndex = tabIndex;
            CurrentScreenId = tabIndex switch
            {
                0 => "06",
                1 => "05",
                2 => "09",
                3 => "08",
                4 => "21",
                5 => "18",
                _ => "06"
            };
        }
    }

    [RelayCommand]
    public void SelectScreen(string? screenId)
    {
        if (!string.IsNullOrWhiteSpace(screenId))
        {
            if (screenId.Equals("launcher", StringComparison.OrdinalIgnoreCase))
            {
                CurrentScreenId = "launcher";
            }
            else if (screenId.Equals("script", StringComparison.OrdinalIgnoreCase))
            {
                CurrentScreenId = "script";
            }
            else
            {
                CurrentScreenId = screenId.PadLeft(2, '0');
            }
        }
    }

    [RelayCommand]
    public void OpenAccountDetail(AccountSummaryDto? account)
    {
        if (account != null)
        {
            SelectedAccount = account;
            IsAccountDrawerOpen = true;
        }
    }

    [RelayCommand]
    public void CloseAccountDetail()
    {
        IsAccountDrawerOpen = false;
    }

    [RelayCommand]
    public void SelectMapSea(string sea)
    {
        SelectedMapSea = sea;
    }

    [RelayCommand]
    public void SetSeaFilter(string sea)
    {
        SelectedSeaFilter = sea;
        ApplyFilters();
    }

    partial void OnSearchQueryChanged(string value)
    {
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        FilteredAccounts.Clear();
        var query = Accounts.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            query = query.Where(a =>
                a.RobloxUsername.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                a.Fruit.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                a.Location.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedSeaFilter != "All")
        {
            query = query.Where(a => a.Sea.Equals(SelectedSeaFilter, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var acc in query)
        {
            FilteredAccounts.Add(acc);
        }
    }

    [RelayCommand]
    public async Task RefreshHardwareMetricsAsync()
    {
        HardwareSnapshot = _hardwareService.GetCurrentSnapshot();
        RamUsage = $"{HardwareSnapshot.ProcessWorkingSetBytes / (1024 * 1024)} MB RAM";
        CpuUsage = $"{HardwareSnapshot.HostCpuUsagePercentage:F1}% CPU";
        DiskUsage = $"{HardwareSnapshot.DiskFreeSpaceGb:F1} GB Trống";
        double walMb = HardwareSnapshot.SqliteWalSizeBytes / (1024.0 * 1024.0);
        DiskDetails = $"{HardwareSnapshot.DiskDriveName} ({HardwareSnapshot.DiskFileSystem}) • WAL: {walMb:F2} MB";
        RingBufferCursorDisplay = $"Con trỏ: {HardwareSnapshot.RingBufferWriteCursor}/{HardwareSnapshot.RingBufferCapacity} ({HardwareSnapshot.RingBufferTotalSamples} mẫu)";
        await Task.CompletedTask;
    }

    [RelayCommand]
    public async Task OptimizeEntireSystemAsync()
    {
        var result = await _resourceOptimizerService.OptimizeEntireSystemAsync(
            HardwareSnapshot.HostCpuUsagePercentage,
            ct => _localRepo.OptimizeSqliteWalAndVacuumAsync(ct));

        await RefreshHardwareMetricsAsync();

        OptimizationStatusBadge = $"{result.ActiveCpuMode} ({result.TargetFrameBudgetMs}ms) • -{result.TotalRamFreedMb:F2} MB RAM";
        LastOptimizationSummary = result.SummaryMessage;
        StatusNotice = $"🚀 {result.SummaryMessage}";
    }

    [RelayCommand]
    public async Task RunSecuritySelfTestAsync()
    {
        // 1. Inspect clean payload
        var cleanCheck = _securityHardeningService.InspectPayloadZeroAlloc("{\"username\":\"CaptainKha_77\",\"level\":2600}");

        // 2. Inspect simulated forbidden credential leak (.ROBLOSECURITY) to verify zero-allocation block
        var blockedLeak = _securityHardeningService.InspectPayloadZeroAlloc("{\"cookie\":\".ROBLOSECURITY=_|WARNING:-DO-NOT-SHARE-THIS\"}");

        // 3. Verify HMAC-SHA256 & Replay Attack protection
        long nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nonce = Guid.NewGuid().ToString("N")[..12];
        string sig = _securityHardeningService.SignPayloadHmac("telemetry-packet", nonce, nowUnix);
        _ = _securityHardeningService.VerifyPayloadHmacAndReplay("telemetry-packet", nonce, nowUnix, sig);
        // Replay attempt with same nonce (blocked)
        _ = _securityHardeningService.VerifyPayloadHmacAndReplay("telemetry-packet", nonce, nowUnix, sig);

        var report = _securityHardeningService.GetSecurityPostureReport();
        SecurityPostureBadge = $"{report.SecurityGrade} ({report.SecurityScorePercent:F0}%)";
        SecurityPostureDetails = $"Đã quét {report.TotalPayloadsInspected} gói tin ({cleanCheck.InspectionMicroseconds:F2} µs/gói) • Chặn {report.BlockedCredentialLeaksCount} rò rỉ Credential ({blockedLeak.ThreatCode}) • Chặn {report.BlockedReplayOrTamperCount} Replay/Tamper • Xóa trắng {report.MemoryBuffersZeroizedCount} vùng nhớ RAM.";
        StatusNotice = $"🛡️ Kiểm định Bảo mật hoàn tất: {SecurityPostureDetails}";
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task RefreshMetricsAsync()
    {
        await RefreshHardwareMetricsAsync();

        PacketsPerSecond = _telemetryService.CurrentPacketsPerSecond;
        TotalPacketsCount = _telemetryService.TotalPacketsIngested;
        DedupedPacketsCount = _telemetryService.DeduplicatedPacketsCount;

        var active = _telemetryService.GetActiveAccounts();
        ActiveAccountsCount = active.Count;

        Accounts.Clear();
        foreach (var acc in active.OrderByDescending(a => a.Level))
        {
            Accounts.Add(new AccountSummaryDto
            {
                Id = acc.Id,
                RobloxUsername = acc.RobloxUsername,
                Level = acc.Level,
                Beli = acc.Beli,
                Fragments = acc.Fragments,
                Race = acc.Race,
                Sea = acc.Sea,
                Fruit = acc.Fruit,
                Sword = acc.Sword,
                FightingStyle = acc.FightingStyle,
                Status = acc.Status,
                Location = acc.Location,
                ChecksumHash = acc.LastChecksumHash,
                IsOnline = acc.IsOnline,
                LastSeenUtc = acc.LastSeenUtc,
                MagnetStage = acc.MagnetStage,
                MagnetTokens = acc.MagnetTokens,
                SecretsCompleted = acc.SecretsCompleted,
                SecretsPercentage = acc.SecretsCompleted * 10.0,
                DeviceHwid = acc.DeviceHwid,
                Executor = acc.Executor,
                PlaytimeHours = Math.Round(acc.PlaytimeSeconds / 3600.0, 1)
            });
        }

        ApplyFilters();
    }

    [RelayCommand]
    public async Task TriggerDebateWithTopicAsync(string topic)
    {
        SelectedDebateTopic = topic;
        var result = await _aiDebateService.RunDeliberationAsync(topic);

        ConsensusVerdict = result.ConsensusVerdict;
        Debates.Clear();
        foreach (var debate in result.Debates)
        {
            Debates.Add(debate);
        }
    }

    [RelayCommand]
    private async Task RunAiDebateAsync()
    {
        await TriggerDebateWithTopicAsync(SelectedDebateTopic);
    }

    [RelayCommand]
    private async Task SimulateRobloxTelemetryAsync()
    {
        var random = new Random();
        var accounts = new[] { "CaptainKha_77", "AbyssalRider_VN", "Corsair_Ghost" };
        string targetUser = accounts[random.Next(accounts.Length)];

        var simulated = new RobloxTelemetryPayload
        {
            RobloxUsername = targetUser,
            Level = 2600,
            Beli = 145000000 + random.Next(1000, 50000),
            Fragments = 75200 + random.Next(10, 500),
            Race = "Cyborg V4",
            Sea = "Sea 3",
            Fruit = "Kitsune (Mastery 600)",
            Sword = "Cursed Dual Katana",
            FightingStyle = "Godhuman",
            Status = random.Next(2) == 0 ? "Sea Beast Defeated! 🌊" : "Farming Elite Pirate ⚔️",
            Location = "Tiki Outpost",
            Playtime = 142500,
            Hwid = "HWID-X889-WINDOWS",
            Executor = "Wave v3.2",
            CsharpAccelerated = true,
            MagnetTokens = 1300,
            MagnetPower = new MagnetPowerDto { Stage = 3, Power = 86000, TargetPower = 100000 },
            Secrets = new SecretsProgressDto { Completed = 8, Total = 10, Percentage = 80.0 }
        };

        await _telemetryService.IngestAsync(simulated);
        await RefreshMetricsAsync();
    }

    [RelayCommand]
    public async Task CopyRobloxLoaderScriptAsync()
    {
        // Navigate to Get Script Lua view & copy the auto-generated JSON + Lua Script
        CurrentScreenId = "script";
        await GetScriptAndCopyAsync();
    }

    private void OnAccountUpdatedHandler(Domain.Entities.TrackedAccount account)
    {
        // Persist to local SQLite storage
        _ = _localRepo.UpsertAccountAsync(new LocalAccountRecord
        {
            Id = account.Id,
            RobloxUsername = account.RobloxUsername,
            Level = account.Level,
            Beli = account.Beli,
            Fragments = account.Fragments,
            Race = account.Race,
            Sea = account.Sea,
            Fruit = account.Fruit,
            Sword = account.Sword,
            FightingStyle = account.FightingStyle,
            Status = account.Status,
            Location = account.Location,
            PlaytimeSeconds = account.PlaytimeSeconds,
            Hwid = account.DeviceHwid,
            Executor = account.Executor,
            LastChecksumHash = account.LastChecksumHash,
            MagnetStage = account.MagnetStage,
            SecretsCompleted = account.SecretsCompleted,
            MagnetTokens = account.MagnetTokens,
            LastSeenUtc = account.LastSeenUtc,
            IsOnline = account.IsOnline
        });

        // Notification on thread pool; update UI via dispatcher in Avalonia
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var existing = Accounts.FirstOrDefault(a => a.RobloxUsername.Equals(account.RobloxUsername, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.Level = account.Level;
                existing.Beli = account.Beli;
                existing.Fragments = account.Fragments;
                existing.Status = account.Status;
                existing.Location = account.Location;
                existing.ChecksumHash = account.LastChecksumHash;
                existing.LastSeenUtc = account.LastSeenUtc;
                existing.MagnetStage = account.MagnetStage;
                existing.MagnetTokens = account.MagnetTokens;
                existing.SecretsCompleted = account.SecretsCompleted;
                existing.SecretsPercentage = account.SecretsCompleted * 10.0;
            }
            ApplyFilters();
        });
    }
}
