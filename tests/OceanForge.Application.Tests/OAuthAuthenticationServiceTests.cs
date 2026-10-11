using System.Linq;
using System.Threading.Tasks;
using OceanForge.Application.Auth;
using OceanForge.Contracts.Auth;
using Xunit;

namespace OceanForge.Application.Tests;

public class OAuthAuthenticationServiceTests
{
    private readonly IOAuthAuthenticationService _authService = new OAuthAuthenticationService();

    [Fact]
    public void GenerateAuthorizationUrl_ForGoogle_ContainsGoogleEndpointPkceAndScopes()
    {
        string url = _authService.GenerateAuthorizationUrl(AuthProvider.Google);
        Assert.Contains("accounts.google.com/o/oauth2/v2/auth", url);
        Assert.Contains("client_id=", url);
        Assert.Contains("scope=", url);
        Assert.Contains("code_challenge=", url);
        Assert.Contains("code_challenge_method=S256", url);
    }

    [Fact]
    public void GenerateAuthorizationUrl_ForDiscord_ContainsDiscordEndpointPkceAndScopes()
    {
        string url = _authService.GenerateAuthorizationUrl(AuthProvider.Discord);
        Assert.Contains("discord.com/oauth2/authorize", url);
        Assert.Contains("client_id=", url);
        Assert.Contains("scope=", url);
        Assert.Contains("code_challenge=", url);
        Assert.Contains("code_challenge_method=S256", url);
    }

    [Fact]
    public void ValidatePkceAndState_VerifiesS256CodeChallengeCorrectly()
    {
        var session = _authService.CreatePkceSession(AuthProvider.Google);
        Assert.True(_authService.ValidatePkceAndState(session.State, session.CodeVerifier));
        Assert.False(_authService.ValidatePkceAndState(session.State, "tampered-verifier"));
        Assert.False(_authService.ValidatePkceAndState("wrong-state", session.CodeVerifier));
    }

    [Fact]
    public async Task LoginWithGoogleAsync_CreatesNewAccountOnFirstLogin_AndQueuesWelcomeEmail()
    {
        bool eventFired = false;
        _authService.OnAuthStateChanged += _ => eventFired = true;

        // First login => New Account + Welcome Email sent
        var firstLogin = await _authService.LoginWithGoogleAsync();

        Assert.True(firstLogin.Success);
        Assert.True(firstLogin.IsNewAccount);
        Assert.NotNull(firstLogin.WelcomeEmailEvent);
        Assert.Equal(EmailDeliveryStatus.EMAIL_SENT, firstLogin.WelcomeEmailEvent.DeliveryStatus);
        Assert.Contains("#020617", firstLogin.WelcomeEmailEvent.HtmlBody);
        Assert.Contains("Welcome to OceanForge — Account Created Successfully", firstLogin.WelcomeEmailEvent.Subject);
        Assert.True(_authService.IsLoggedIn);
        Assert.NotNull(_authService.CurrentUser);
        Assert.Equal(AuthProvider.Google, _authService.CurrentUser.Provider);
        Assert.True(eventFired);

        // Second login with same email => Existing Account (no duplicate registration email)
        var secondLogin = await _authService.LoginWithGoogleAsync();
        Assert.True(secondLogin.Success);
        Assert.False(secondLogin.IsNewAccount);
        Assert.Null(secondLogin.WelcomeEmailEvent);
        Assert.Single(_authService.GetEmailEvents());

        // Sign Out
        _authService.SignOut();
        Assert.False(_authService.IsLoggedIn);
        Assert.Null(_authService.CurrentUser);
    }

    [Fact]
    public async Task LinkAndUnlinkProvider_EnforcesUnlinkProtectionWhenSingleProvider()
    {
        await _authService.LoginWithDiscordAsync();
        Assert.NotNull(_authService.CurrentUser);
        Assert.Single(_authService.CurrentUser.ConnectedIdentities);

        // Attempting to unlink the only provider when HasPassword == false must be blocked
        var blockedUnlink = await _authService.UnlinkProviderAsync(AuthProvider.Discord);
        Assert.False(blockedUnlink.Success);
        Assert.Contains("UNLINK_PROTECTION", blockedUnlink.Message);

        // Link Google as second provider
        var linkResult = await _authService.LinkProviderAsync(
            AuthProvider.Google,
            "google-999",
            "captain@oceanforge.dev",
            "Captain Khanh");
        Assert.True(linkResult.Success);
        Assert.Equal(2, _authService.CurrentUser.ConnectedIdentities.Count);

        // Now unlinking one provider is allowed because a second provider remains
        var allowedUnlink = await _authService.UnlinkProviderAsync(AuthProvider.Discord);
        Assert.True(allowedUnlink.Success);
        Assert.Single(_authService.CurrentUser.ConnectedIdentities);
        Assert.Contains(_authService.GetSecurityAuditLogs(), l => l.EventType == SecurityAuditEventType.ACCOUNT_LINKED);
    }
}
