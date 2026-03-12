using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenXConvo.Application.Interfaces;

namespace TenXConvo.API.Controllers.Auth;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("login/step1")] [AllowAnonymous] [EnableRateLimiting("login-step1")]
    public async Task<IActionResult> LoginStep1([FromBody] Step1Request req)
    {
        try   { return Ok(new { success = true, data = await _auth.LoginStep1Async(req.Email) }); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { success = false, message = ex.Message }); }
    }

    [HttpPost("login/step2")] [AllowAnonymous] [EnableRateLimiting("login-step2")]
    public async Task<IActionResult> LoginStep2([FromBody] Step2Request req)
    {
        try
        {
            var result = await _auth.LoginStep2Async(new LoginStep2Input(req.Email, req.Password, req.LocationId, req.Connection, req.FiscalYearId, req.RememberMe));
            return Ok(new { success = true, data = result });
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { success = false, message = ex.Message }); }
        catch (InvalidOperationException   ex) { return BadRequest(new  { success = false, message = ex.Message }); }
    }

    [HttpPost("refresh")] [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest req)
    {
        try   { return Ok(new { success = true, data = await _auth.RefreshTokenAsync(req.RefreshToken) }); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { success = false, message = ex.Message }); }
    }

    [HttpPost("logout")] [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest req)
    {
        await _auth.LogoutAsync(req.RefreshToken);
        return Ok(new { success = true, message = "Logged out." });
    }

    [HttpGet("me")] [Authorize]
    public async Task<IActionResult> Me()
    {
        var userId = Guid.Parse(User.FindFirst("sub")?.Value!);
        return Ok(new { success = true, data = await _auth.GetMeAsync(userId) });
    }

    [HttpPut("change-password")] [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("sub")?.Value!);
            await _auth.ChangePasswordAsync(userId, req.CurrentPassword, req.NewPassword);
            return Ok(new { success = true, message = "Password changed." });
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { success = false, message = ex.Message }); }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  OAUTH / EXTERNAL LOGIN
    //  Frontend handles the OAuth popup/redirect → gets id_token → sends claims here
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// External login via Google / Microsoft OAuth.
    /// Frontend gets id_token from the OAuth provider, extracts claims, sends them here.
    /// Server finds-or-creates user + issues JWT.
    /// </summary>
    [HttpPost("external-login")] [AllowAnonymous]
    public async Task<IActionResult> ExternalLogin([FromBody] ExternalLoginRequest req)
    {
        try
        {
            var input = new ExternalLoginInput(req.Provider, req.ProviderKey, req.Email, req.DisplayName, req.AvatarUrl, req.LocationId, req.Connection, req.FiscalYearId);
            var result = await _auth.ExternalLoginAsync(input);
            return Ok(new { success = true, data = result });
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { success = false, message = ex.Message }); }
        catch (InvalidOperationException   ex) { return BadRequest(new  { success = false, message = ex.Message }); }
    }

    /// <summary>Get all linked OAuth providers for the current user.</summary>
    [HttpGet("external-logins")] [Authorize]
    public async Task<IActionResult> GetLinkedProviders()
    {
        var userId = Guid.Parse(User.FindFirst("sub")?.Value!);
        return Ok(new { success = true, data = await _auth.GetLinkedProvidersAsync(userId) });
    }

    /// <summary>Link a new OAuth provider to the current account.</summary>
    [HttpPost("external-logins/link")] [Authorize]
    public async Task<IActionResult> LinkProvider([FromBody] ExternalLoginRequest req)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("sub")?.Value!);
            var input  = new ExternalLoginInput(req.Provider, req.ProviderKey, req.Email, req.DisplayName, req.AvatarUrl, null, null, null);
            await _auth.LinkExternalLoginAsync(userId, input);
            return Ok(new { success = true, message = $"{req.Provider} account linked." });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { success = false, message = ex.Message }); }
    }

    /// <summary>Unlink an OAuth provider from the current account.</summary>
    [HttpDelete("external-logins/{provider}")] [Authorize]
    public async Task<IActionResult> UnlinkProvider(string provider)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("sub")?.Value!);
            await _auth.UnlinkExternalLoginAsync(userId, provider);
            return Ok(new { success = true, message = $"{provider} account unlinked." });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { success = false, message = ex.Message }); }
    }
}

public record Step1Request(string Email);
public record Step2Request(string Email, string Password, Guid LocationId, string Connection, Guid FiscalYearId, bool RememberMe = false);
public record RefreshRequest(string RefreshToken);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record ExternalLoginRequest(string Provider, string ProviderKey, string Email, string? DisplayName = null, string? AvatarUrl = null, Guid? LocationId = null, string? Connection = null, Guid? FiscalYearId = null);
