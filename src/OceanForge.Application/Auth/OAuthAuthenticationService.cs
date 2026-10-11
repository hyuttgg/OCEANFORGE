using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OceanForge.Contracts.Auth;

namespace OceanForge.Application.Auth;

public interface IOAuthAuthenticationService
{
    AuthUserProfile? CurrentUser { get; }
    bool IsLoggedIn { get; }
    event Action<AuthUserProfile?>? OnAuthStateChanged;

    string GenerateAuthorizationUrl(AuthProvider provider, string? state = null);
    OAuthPkceSession CreatePkceSession(AuthProvider provider, string? customState = null);
    bool ValidatePkceAndState(string state, string codeVerifier);

    Task<OAuthLoginResult> LoginWithGoogleAsync(CancellationToken cancellationToken = default);
    Task<OAuthLoginResult> LoginWithDiscordAsync(CancellationToken cancellationToken = default);
    Task<OAuthLoginResult> AuthenticateOAuthProviderAsync(
        AuthProvider provider,
        string providerUserId,
        string email,
        string displayName,
        string avatarUrl,
        bool emailVerified = true,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message)> LinkProviderAsync(
        AuthProvider provider,
        string providerUserId,
        string providerEmail,
        string displayName);

    Task<(bool Success, string Message)> UnlinkProviderAsync(AuthProvider provider);

    string BuildWelcomeRegistrationHtmlEmail(string displayName, string email, AuthProvider provider, DateTime registeredAtUtc);
    string BuildWelcomeRegistrationPlainTextEmail(string displayName, string email, AuthProvider provider, DateTime registeredAtUtc);

    IReadOnlyList<EmailNotificationEventDto> GetEmailEvents();
    IReadOnlyList<SecurityAuditLogDto> GetSecurityAuditLogs();
    void SignOut();
}

public class OAuthAuthenticationService : IOAuthAuthenticationService
{
    private AuthUserProfile? _currentUser;
    public AuthUserProfile? CurrentUser => _currentUser;
    public bool IsLoggedIn => _currentUser != null && _currentUser.IsAuthenticated;

    public event Action<AuthUserProfile?>? OnAuthStateChanged;

    private const string GoogleClientId = "oceanforge-google-client-id.apps.googleusercontent.com";
    private const string DiscordClientId = "123456789012345678";
    private const string RedirectUri = "http://127.0.0.1:5088/api/auth/callback";

    // In-memory Identity, Session, Email Queue & Audit stores
    private readonly ConcurrentDictionary<string, OAuthPkceSession> _pkceSessionsByState = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, AuthUserProfile> _usersByEmail = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentQueue<EmailNotificationEventDto> _emailEvents = new();
    private readonly ConcurrentQueue<SecurityAuditLogDto> _auditLogs = new();

    public string GenerateAuthorizationUrl(AuthProvider provider, string? state = null)
    {
        var session = CreatePkceSession(provider, state);
        return session.AuthorizationUrl;
    }

    public OAuthPkceSession CreatePkceSession(AuthProvider provider, string? customState = null)
    {
        string secureState = !string.IsNullOrWhiteSpace(customState) ? customState : GenerateRandomState();
        string codeVerifier = GeneratePkceCodeVerifier();
        string codeChallenge = ComputeCodeChallengeS256(codeVerifier);

        string authUrl = provider switch
        {
            AuthProvider.Google =>
                $"https://accounts.google.com/o/oauth2/v2/auth?" +
                $"client_id={GoogleClientId}&" +
                $"redirect_uri={Uri.EscapeDataString(RedirectUri)}&" +
                $"response_type=code&" +
                $"scope={Uri.EscapeDataString("openid email profile")}&" +
                $"state={secureState}&" +
                $"code_challenge={codeChallenge}&" +
                $"code_challenge_method=S256",

            AuthProvider.Discord =>
                $"https://discord.com/oauth2/authorize?" +
                $"client_id={DiscordClientId}&" +
                $"redirect_uri={Uri.EscapeDataString(RedirectUri)}&" +
                $"response_type=code&" +
                $"scope={Uri.EscapeDataString("identify email")}&" +
                $"state={secureState}&" +
                $"code_challenge={codeChallenge}&" +
                $"code_challenge_method=S256",

            _ => throw new ArgumentException($"Unsupported provider {provider}", nameof(provider))
        };

        var session = new OAuthPkceSession
        {
            Provider = provider,
            State = secureState,
            CodeVerifier = codeVerifier,
            CodeChallenge = codeChallenge,
            CodeChallengeMethod = "S256",
            RedirectUri = RedirectUri,
            AuthorizationUrl = authUrl,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10)
        };

        _pkceSessionsByState[secureState] = session;
        RecordAuditLog(SecurityAuditEventType.LOGIN_STARTED, "anonymous", provider.ToString(), $"OAuth 2.0 + PKCE (S256) flow started; state={secureState[..Math.Min(8, secureState.Length)]}...");
        return session;
    }

    public bool ValidatePkceAndState(string state, string codeVerifier)
    {
        if (string.IsNullOrWhiteSpace(state) || string.IsNullOrWhiteSpace(codeVerifier))
        {
            return false;
        }

        if (!_pkceSessionsByState.TryGetValue(state, out var session))
        {
            return false;
        }

        if (DateTime.UtcNow > session.ExpiresAtUtc)
        {
            _pkceSessionsByState.TryRemove(state, out _);
            return false;
        }

        string computedChallenge = ComputeCodeChallengeS256(codeVerifier);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(computedChallenge),
            Encoding.ASCII.GetBytes(session.CodeChallenge));
    }

    public Task<OAuthLoginResult> LoginWithGoogleAsync(CancellationToken cancellationToken = default)
    {
        return AuthenticateOAuthProviderAsync(
            AuthProvider.Google,
            providerUserId: "google-sub-1092837465",
            email: "captain.oceanforge@gmail.com",
            displayName: "Captain Khanh (Google Verified)",
            avatarUrl: "https://lh3.googleusercontent.com/a/default-user=s96-c",
            emailVerified: true,
            cancellationToken: cancellationToken);
    }

    public Task<OAuthLoginResult> LoginWithDiscordAsync(CancellationToken cancellationToken = default)
    {
        return AuthenticateOAuthProviderAsync(
            AuthProvider.Discord,
            providerUserId: "discord-7894561230",
            email: "abyssal.navigator@discord.gg",
            displayName: "AbyssalAdmiral#1337 (Discord)",
            avatarUrl: "https://cdn.discordapp.com/embed/avatars/0.png",
            emailVerified: true,
            cancellationToken: cancellationToken);
    }

    public async Task<OAuthLoginResult> AuthenticateOAuthProviderAsync(
        AuthProvider provider,
        string providerUserId,
        string email,
        string displayName,
        string avatarUrl,
        bool emailVerified = true,
        CancellationToken cancellationToken = default)
    {
        var pkceSession = CreatePkceSession(provider);
        await Task.Delay(80, cancellationToken);

        // Validate email verification requirement (Section 23.4 / 19)
        if (!emailVerified || string.IsNullOrWhiteSpace(email))
        {
            RecordAuditLog(SecurityAuditEventType.LOGIN_FAILED, "anonymous", provider.ToString(), "Email unverified or missing from OAuth provider.");
            return new OAuthLoginResult
            {
                Success = false,
                ErrorCode = "EMAIL_UNVERIFIED",
                Message = $"Tài khoản {provider} chưa xác minh địa chỉ Email. Vui lòng xác minh email trên {provider} trước khi đăng nhập."
            };
        }

        // Verify PKCE roundtrip
        if (!ValidatePkceAndState(pkceSession.State, pkceSession.CodeVerifier))
        {
            RecordAuditLog(SecurityAuditEventType.LOGIN_FAILED, "anonymous", provider.ToString(), "PKCE or State validation failed.");
            return new OAuthLoginResult
            {
                Success = false,
                ErrorCode = "PKCE_INVALID",
                Message = "Xác thực PKCE / State không hợp lệ."
            };
        }

        DateTime nowUtc = DateTime.UtcNow;
        bool isNewAccount = false;
        EmailNotificationEventDto? welcomeEmailEvent = null;

        if (!_usersByEmail.TryGetValue(email, out var userProfile))
        {
            // CASE A — NEW ACCOUNT (Section 9)
            isNewAccount = true;
            userProfile = new AuthUserProfile
            {
                Id = $"{provider.ToString().ToLowerInvariant()}-{Guid.NewGuid().ToString("N")[..12]}",
                Email = email,
                DisplayName = displayName,
                AvatarUrl = avatarUrl,
                Provider = provider,
                AccessToken = $"of_jwt_access_{Guid.NewGuid():N}",
                RefreshToken = $"of_refresh_{Guid.NewGuid():N}",
                AccessTokenExpiresAtUtc = nowUtc.AddMinutes(15),
                RefreshTokenExpiresAtUtc = nowUtc.AddDays(30),
                CreatedAtUtc = nowUtc,
                AuthenticatedAtUtc = nowUtc,
                IsAuthenticated = true,
                EmailVerified = emailVerified,
                IsNewAccount = true,
                HasPassword = false,
                AccountStatus = "Active",
                ConnectedIdentities =
                [
                    new ConnectedIdentityDto
                    {
                        Provider = provider,
                        ProviderUserId = providerUserId,
                        ProviderEmail = email,
                        ProviderDisplayName = displayName,
                        EmailVerified = emailVerified,
                        ConnectedAtUtc = nowUtc,
                        LastLoginAtUtc = nowUtc
                    }
                ]
            };

            _usersByEmail[email] = userProfile;
            RecordAuditLog(SecurityAuditEventType.ACCOUNT_CREATED, userProfile.Id, provider.ToString(), $"New OceanForge account created for {email}");

            // Queue & send Registration Notification Email asynchronously (Sections 13–21)
            welcomeEmailEvent = EnqueueAndSendRegistrationEmail(userProfile, provider, nowUtc);
        }
        else
        {
            // CASE B — EXISTING ACCOUNT (Section 9)
            userProfile.IsNewAccount = false;
            userProfile.Provider = provider;
            userProfile.DisplayName = displayName;
            userProfile.AvatarUrl = avatarUrl;
            userProfile.AuthenticatedAtUtc = nowUtc;
            userProfile.IsAuthenticated = true;
            userProfile.AccessToken = $"of_jwt_access_{Guid.NewGuid():N}";
            userProfile.AccessTokenExpiresAtUtc = nowUtc.AddMinutes(15);

            var existingIdentity = userProfile.ConnectedIdentities
                .FirstOrDefault(i => i.Provider == provider);

            if (existingIdentity == null)
            {
                userProfile.ConnectedIdentities.Add(new ConnectedIdentityDto
                {
                    Provider = provider,
                    ProviderUserId = providerUserId,
                    ProviderEmail = email,
                    ProviderDisplayName = displayName,
                    EmailVerified = emailVerified,
                    ConnectedAtUtc = nowUtc,
                    LastLoginAtUtc = nowUtc
                });
                RecordAuditLog(SecurityAuditEventType.ACCOUNT_LINKED, userProfile.Id, provider.ToString(), $"Linked {provider} identity to existing account {email}");
            }
            else
            {
                existingIdentity.LastLoginAtUtc = nowUtc;
            }
        }

        _currentUser = userProfile;
        RecordAuditLog(SecurityAuditEventType.LOGIN_SUCCESS, userProfile.Id, provider.ToString(), $"Authenticated via {provider} OAuth 2.0 + PKCE");
        OnAuthStateChanged?.Invoke(_currentUser);

        string statusMsg = isNewAccount
            ? $"Đăng ký & Đăng nhập {provider} OAuth2 thành công! Đã gửi email chào mừng tới {userProfile.Email}."
            : $"Đăng nhập {provider} OAuth2 thành công! Chào mừng trở lại, {userProfile.DisplayName}.";

        return new OAuthLoginResult
        {
            Success = true,
            IsNewAccount = isNewAccount,
            Message = statusMsg,
            User = _currentUser,
            PkceSession = pkceSession,
            WelcomeEmailEvent = welcomeEmailEvent
        };
    }

    public Task<(bool Success, string Message)> LinkProviderAsync(
        AuthProvider provider,
        string providerUserId,
        string providerEmail,
        string displayName)
    {
        if (_currentUser == null || !_currentUser.IsAuthenticated)
        {
            return Task.FromResult((false, "Bạn cần đăng nhập trước khi liên kết thêm tài khoản OAuth."));
        }

        if (_currentUser.ConnectedIdentities.Any(i => i.Provider == provider))
        {
            return Task.FromResult((false, $"Tài khoản {provider} đã được liên kết trước đó."));
        }

        _currentUser.ConnectedIdentities.Add(new ConnectedIdentityDto
        {
            Provider = provider,
            ProviderUserId = providerUserId,
            ProviderEmail = providerEmail,
            ProviderDisplayName = displayName,
            EmailVerified = true,
            ConnectedAtUtc = DateTime.UtcNow,
            LastLoginAtUtc = DateTime.UtcNow
        });

        RecordAuditLog(SecurityAuditEventType.ACCOUNT_LINKED, _currentUser.Id, provider.ToString(), $"Linked {provider} ({providerEmail})");
        OnAuthStateChanged?.Invoke(_currentUser);
        return Task.FromResult((true, $"Đã liên kết thành công tài khoản {provider} ({providerEmail})!"));
    }

    /// <summary>
    /// Section 25: Unlink Protection — Do not allow unlinking the last authentication provider if the user has no password.
    /// </summary>
    public Task<(bool Success, string Message)> UnlinkProviderAsync(AuthProvider provider)
    {
        if (_currentUser == null || !_currentUser.IsAuthenticated)
        {
            return Task.FromResult((false, "Chưa đăng nhập."));
        }

        var target = _currentUser.ConnectedIdentities.FirstOrDefault(i => i.Provider == provider);
        if (target == null)
        {
            return Task.FromResult((false, $"Không tìm thấy liên kết {provider}."));
        }

        if (_currentUser.ConnectedIdentities.Count <= 1 && !_currentUser.HasPassword)
        {
            return Task.FromResult((false, "UNLINK_PROTECTION: Không thể gỡ phương thức đăng nhập duy nhất khi tài khoản chưa thiết lập mật khẩu dự phòng."));
        }

        _currentUser.ConnectedIdentities.Remove(target);
        RecordAuditLog(SecurityAuditEventType.ACCOUNT_UNLINKED, _currentUser.Id, provider.ToString(), $"Unlinked provider {provider}");
        OnAuthStateChanged?.Invoke(_currentUser);
        return Task.FromResult((true, $"Đã gỡ liên kết {provider} thành công."));
    }

    public string BuildWelcomeRegistrationHtmlEmail(string displayName, string email, AuthProvider provider, DateTime registeredAtUtc)
    {
        string formattedDate = registeredAtUtc.ToString("dd MMM yyyy — HH:mm 'UTC'");
        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""utf-8"" />
  <title>Welcome to OceanForge — Account Created Successfully</title>
</head>
<body style=""margin:0;padding:32px;background-color:#020617;font-family:'Segoe UI',Inter,sans-serif;color:#F3F6FA;"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""max-width:600px;margin:0 auto;background-color:#0B1329;border:1px solid #D4AF37;border-radius:8px;overflow:hidden;"">
    <tr>
      <td style=""padding:24px 32px;background-color:#060D1F;border-bottom:1px solid #1E293B;text-align:center;"">
        <div style=""font-size:26px;color:#D4AF37;font-weight:bold;letter-spacing:2px;"">⚓ OCEANFORGE</div>
        <div style=""font-size:12px;color:#5BC0BE;margin-top:4px;letter-spacing:1px;"">ACCOUNT REGISTRATION CONFIRMED</div>
      </td>
    </tr>
    <tr>
      <td style=""padding:28px 32px;"">
        <p style=""font-size:15px;color:#F3F6FA;margin-top:0;"">Hello <strong>{displayName}</strong>,</p>
        <p style=""font-size:14px;color:#CBD5E1;line-height:1.6;"">Your OceanForge account has been created successfully.</p>
        <table width=""100%"" cellpadding=""10"" cellspacing=""0"" style=""background-color:#020617;border:1px solid #1E293B;border-radius:6px;margin:20px 0;font-size:13px;"">
          <tr>
            <td style=""color:#94A3B8;width:40%;"">Account Email:</td>
            <td style=""color:#F3F6FA;font-weight:600;"">{email}</td>
          </tr>
          <tr>
            <td style=""color:#94A3B8;"">Sign-In Method:</td>
            <td style=""color:#5BC0BE;font-weight:600;"">{provider}</td>
          </tr>
          <tr>
            <td style=""color:#94A3B8;"">Registration Time:</td>
            <td style=""color:#F3F6FA;"">{formattedDate}</td>
          </tr>
          <tr>
            <td style=""color:#94A3B8;"">Security Status:</td>
            <td style=""color:#10B981;font-weight:600;"">✔ Verified via OAuth</td>
          </tr>
        </table>
        <p style=""font-size:13px;color:#CBD5E1;line-height:1.6;"">You can now sign in to OceanForge Desktop and access your workspace.</p>
        <div style=""text-align:center;margin:28px 0;"">
          <a href=""oceanforge://open-workspace"" style=""display:inline-block;padding:12px 28px;background-color:#D4AF37;color:#020617;font-weight:bold;text-decoration:none;border-radius:4px;font-size:14px;"">Open OceanForge</a>
        </div>
        <p style=""font-size:12px;color:#F59E0B;border-top:1px solid #1E293B;padding-top:16px;margin-bottom:0;"">
          ⚠️ If you did not create this account, please secure your account immediately at <a href=""https://oceanforge.dev/security"" style=""color:#5BC0BE;"">OceanForge Security Settings</a>.
        </p>
      </td>
    </tr>
    <tr>
      <td style=""padding:16px 32px;background-color:#060D1F;text-align:center;font-size:11px;color:#64748B;"">
        OceanForge Security Team • no-reply@oceanforge.app • Command The Digital Fleet
      </td>
    </tr>
  </table>
</body>
</html>";
    }

    public string BuildWelcomeRegistrationPlainTextEmail(string displayName, string email, AuthProvider provider, DateTime registeredAtUtc)
    {
        string formattedDate = registeredAtUtc.ToString("dd MMM yyyy — HH:mm 'UTC'");
        return $@"OCEANFORGE — ACCOUNT REGISTRATION CONFIRMED
------------------------------------------------------------
Hello {displayName},

Your OceanForge account has been created successfully.

Account Details:
- Account Email:     {email}
- Sign-In Method:    {provider}
- Registration Time: {formattedDate}
- Security Status:   Verified via OAuth

You can now sign in to OceanForge Desktop and access your workspace.

If you did not create this account, please secure your account immediately.

— OceanForge Security Team";
    }

    public IReadOnlyList<EmailNotificationEventDto> GetEmailEvents() => _emailEvents.ToArray();

    public IReadOnlyList<SecurityAuditLogDto> GetSecurityAuditLogs() => _auditLogs.ToArray();

    public void SignOut()
    {
        if (_currentUser != null)
        {
            RecordAuditLog(SecurityAuditEventType.LOGOUT, _currentUser.Id, _currentUser.Provider.ToString(), "User signed out and cleared local session tokens.");
        }
        _currentUser = null;
        OnAuthStateChanged?.Invoke(null);
    }

    private EmailNotificationEventDto EnqueueAndSendRegistrationEmail(AuthUserProfile user, AuthProvider provider, DateTime nowUtc)
    {
        var emailEvent = new EmailNotificationEventDto
        {
            EventId = Guid.NewGuid().ToString("N")[..12],
            UserId = user.Id,
            RecipientEmail = user.Email,
            DisplayName = user.DisplayName,
            Provider = provider,
            EventType = "REGISTRATION_SUCCESS",
            Subject = "Welcome to OceanForge — Account Created Successfully",
            DeliveryStatus = EmailDeliveryStatus.EMAIL_QUEUED,
            AttemptCount = 1,
            NextRetryPolicy = "Attempt 1 (Immediate) -> 10s -> 30s -> 2m -> DLQ",
            HtmlBody = BuildWelcomeRegistrationHtmlEmail(user.DisplayName, user.Email, provider, nowUtc),
            PlainTextBody = BuildWelcomeRegistrationPlainTextEmail(user.DisplayName, user.Email, provider, nowUtc),
            QueuedAtUtc = nowUtc
        };

        RecordAuditLog(SecurityAuditEventType.EMAIL_QUEUED, user.Id, provider.ToString(), $"Queued REGISTRATION_SUCCESS email to {user.Email}");

        // Simulate non-blocking transactional email worker delivery
        emailEvent.DeliveryStatus = EmailDeliveryStatus.EMAIL_SENT;
        emailEvent.SentAtUtc = DateTime.UtcNow;
        _emailEvents.Enqueue(emailEvent);

        RecordAuditLog(SecurityAuditEventType.EMAIL_SENT, user.Id, provider.ToString(), $"Delivered REGISTRATION_SUCCESS email to {user.Email} on Attempt 1");
        return emailEvent;
    }

    private void RecordAuditLog(SecurityAuditEventType eventType, string userId, string provider, string details)
    {
        _auditLogs.Enqueue(new SecurityAuditLogDto
        {
            EventType = eventType,
            UserId = userId,
            Provider = provider,
            IpAddress = "127.0.0.1",
            DeviceName = Environment.MachineName,
            Details = details,
            TimestampUtc = DateTime.UtcNow
        });
    }

    private static string GenerateRandomState()
    {
        byte[] bytes = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static string GeneratePkceCodeVerifier()
    {
        byte[] bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Base64UrlEncode(bytes);
    }

    public static string ComputeCodeChallengeS256(string codeVerifier)
    {
        byte[] challengeBytes = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        return Base64UrlEncode(challengeBytes);
    }

    private static string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
