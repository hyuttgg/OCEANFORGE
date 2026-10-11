using System;
using System.Collections.Generic;

namespace OceanForge.Contracts.Auth;

public enum AuthProvider
{
    Local,
    Google,
    Discord
}

public enum EmailDeliveryStatus
{
    EMAIL_QUEUED,
    EMAIL_SENT,
    RETRY_PENDING,
    DEAD_LETTER_QUEUE,
    EMAIL_FAILED
}

public enum SecurityAuditEventType
{
    LOGIN_STARTED,
    LOGIN_SUCCESS,
    LOGIN_FAILED,
    ACCOUNT_CREATED,
    ACCOUNT_LINKED,
    ACCOUNT_UNLINKED,
    EMAIL_QUEUED,
    EMAIL_SENT,
    EMAIL_FAILED,
    LOGOUT
}

public class ConnectedIdentityDto
{
    public string IdentityId { get; set; } = Guid.NewGuid().ToString("N");
    public AuthProvider Provider { get; set; }
    public string ProviderUserId { get; set; } = string.Empty;
    public string ProviderEmail { get; set; } = string.Empty;
    public string ProviderDisplayName { get; set; } = string.Empty;
    public bool EmailVerified { get; set; } = true;
    public DateTime ConnectedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastLoginAtUtc { get; set; } = DateTime.UtcNow;
}

public class AuthUserProfile
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public AuthProvider Provider { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAtUtc { get; set; } = DateTime.UtcNow.AddMinutes(15);
    public DateTime RefreshTokenExpiresAtUtc { get; set; } = DateTime.UtcNow.AddDays(30);
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime AuthenticatedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsAuthenticated { get; set; }
    public bool EmailVerified { get; set; } = true;
    public bool IsNewAccount { get; set; }
    public bool HasPassword { get; set; }
    public string AccountStatus { get; set; } = "Active";
    public List<ConnectedIdentityDto> ConnectedIdentities { get; set; } = [];
}

public class OAuthPkceSession
{
    public AuthProvider Provider { get; set; }
    public string State { get; set; } = string.Empty;
    public string CodeVerifier { get; set; } = string.Empty;
    public string CodeChallenge { get; set; } = string.Empty;
    public string CodeChallengeMethod { get; set; } = "S256";
    public string RedirectUri { get; set; } = "http://127.0.0.1:5088/api/auth/callback";
    public string AuthorizationUrl { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; } = DateTime.UtcNow.AddMinutes(10);
}

public class OAuthLoginRequest
{
    public AuthProvider Provider { get; set; }
    public string RedirectUri { get; set; } = "http://127.0.0.1:5088/api/auth/callback";
    public string State { get; set; } = string.Empty;
    public string CodeChallenge { get; set; } = string.Empty;
    public string CodeChallengeMethod { get; set; } = "S256";
}

public class EmailNotificationEventDto
{
    public string EventId { get; set; } = Guid.NewGuid().ToString("N");
    public string UserId { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public AuthProvider Provider { get; set; }
    public string EventType { get; set; } = "REGISTRATION_SUCCESS";
    public string Subject { get; set; } = "Welcome to OceanForge — Account Created Successfully";
    public EmailDeliveryStatus DeliveryStatus { get; set; } = EmailDeliveryStatus.EMAIL_QUEUED;
    public int AttemptCount { get; set; } = 1;
    public string NextRetryPolicy { get; set; } = "Immediate -> 10s -> 30s -> 2m -> DLQ";
    public string HtmlBody { get; set; } = string.Empty;
    public string PlainTextBody { get; set; } = string.Empty;
    public DateTime QueuedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? SentAtUtc { get; set; }
}

public class SecurityAuditLogDto
{
    public string EventId { get; set; } = Guid.NewGuid().ToString("N")[..12];
    public SecurityAuditEventType EventType { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string IpAddress { get; set; } = "127.0.0.1";
    public string DeviceName { get; set; } = Environment.MachineName;
    public string Details { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}

public class OAuthLoginResult
{
    public bool Success { get; set; }
    public bool IsNewAccount { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public AuthUserProfile? User { get; set; }
    public OAuthPkceSession? PkceSession { get; set; }
    public EmailNotificationEventDto? WelcomeEmailEvent { get; set; }
}
