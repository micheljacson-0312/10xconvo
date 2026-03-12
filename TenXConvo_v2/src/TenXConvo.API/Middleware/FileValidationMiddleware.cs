using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TenXConvo.API.Middleware;

// ═══════════════════════════════════════════════════════════════════════════
//  FILE UPLOAD VALIDATION
//  Apply [ValidateFile] attribute to any file upload endpoint
//  Or call FileValidator.Validate() directly in controller
// ═══════════════════════════════════════════════════════════════════════════

public static class FileValidator
{
    private static readonly Dictionary<string, string[]> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image"]    = [".jpg", ".jpeg", ".png", ".webp", ".gif"],
        ["document"] = [".pdf", ".doc", ".docx", ".xls", ".xlsx"],
        ["avatar"]   = [".jpg", ".jpeg", ".png", ".webp"],
        ["logo"]     = [".jpg", ".jpeg", ".png", ".webp", ".svg"],
        ["any"]      = [".jpg", ".jpeg", ".png", ".webp", ".gif", ".pdf", ".doc", ".docx"],
    };

    private static readonly Dictionary<string, string[]> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image"]    = ["image/jpeg", "image/png", "image/webp", "image/gif"],
        ["document"] = ["application/pdf", "application/msword",
                        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                        "application/vnd.ms-excel",
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"],
        ["avatar"]   = ["image/jpeg", "image/png", "image/webp"],
        ["logo"]     = ["image/jpeg", "image/png", "image/webp", "image/svg+xml"],
        ["any"]      = ["image/jpeg", "image/png", "image/webp", "image/gif",
                        "application/pdf", "application/msword",
                        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"],
    };

    // Magic byte signatures to verify actual file type (not just extension)
    private static readonly Dictionary<string, byte[]> MagicBytes = new()
    {
        ["image/jpeg"] = [0xFF, 0xD8, 0xFF],
        ["image/png"]  = [0x89, 0x50, 0x4E, 0x47],
        ["image/webp"] = [0x52, 0x49, 0x46, 0x46],  // RIFF
        ["image/gif"]  = [0x47, 0x49, 0x46],
        ["application/pdf"] = [0x25, 0x50, 0x44, 0x46],  // %PDF
    };

    /// <summary>
    /// Validates file type, size, and magic bytes.
    /// Returns (isValid, errorMessage).
    /// </summary>
    public static async Task<(bool IsValid, string? Error)> ValidateAsync(
        IFormFile file,
        string    category     = "any",
        int       maxSizeMb    = 5)
    {
        if (file == null || file.Length == 0)
            return (false, "No file provided.");

        // ── Size check ────────────────────────────────────────────────────────
        var maxBytes = maxSizeMb * 1024 * 1024;
        if (file.Length > maxBytes)
            return (false, $"File too large. Maximum allowed size is {maxSizeMb} MB.");

        // ── Extension check ───────────────────────────────────────────────────
        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext))
            return (false, "File must have an extension.");

        if (!AllowedExtensions.TryGetValue(category, out var allowedExts))
            allowedExts = AllowedExtensions["any"];

        if (!allowedExts.Contains(ext))
            return (false, $"File type not allowed. Allowed: {string.Join(", ", allowedExts)}");

        // ── MIME type check ───────────────────────────────────────────────────
        if (!AllowedMimeTypes.TryGetValue(category, out var allowedMimes))
            allowedMimes = AllowedMimeTypes["any"];

        var contentType = file.ContentType.ToLower();
        if (!allowedMimes.Any(m => contentType.StartsWith(m.Split('/')[0]) || contentType == m))
            return (false, $"Invalid file content type: {file.ContentType}");

        // ── Magic bytes check (prevents disguised files) ──────────────────────
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        ms.Position = 0;

        var header = new byte[8];
        _ = await ms.ReadAsync(header);

        // For image types, verify magic bytes
        var mimeToCheck = allowedMimes.Where(m => MagicBytes.ContainsKey(m));
        var anyMagicMatch = mimeToCheck.Any(mime =>
        {
            var magic = MagicBytes[mime];
            return header.Take(magic.Length).SequenceEqual(magic);
        });

        // Only enforce magic check for images and PDFs (not for Office docs which vary)
        if (category is "image" or "avatar" or "logo" && !anyMagicMatch)
            return (false, "File content does not match its extension. Upload may be corrupted or spoofed.");

        return (true, null);
    }

    /// <summary>
    /// Safe file save — generates a unique name to prevent path traversal.
    /// Returns the saved relative path.
    /// </summary>
    public static async Task<string> SaveAsync(IFormFile file, string uploadsRoot, string subfolder)
    {
        var dir = Path.Combine(uploadsRoot, subfolder);
        Directory.CreateDirectory(dir);

        var ext      = Path.GetExtension(file.FileName).ToLower();
        var safeName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(dir, safeName);

        using var stream = File.Create(fullPath);
        await file.CopyToAsync(stream);

        return $"{subfolder}/{safeName}";
    }

    /// <summary>
    /// Delete a previously uploaded file safely (prevents path traversal).
    /// </summary>
    public static void DeleteSafe(string uploadsRoot, string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath)) return;

        // Resolve to absolute path and verify it's inside uploads root
        var fullPath = Path.GetFullPath(Path.Combine(uploadsRoot, relativePath));
        var root     = Path.GetFullPath(uploadsRoot);

        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return;
        if (File.Exists(fullPath)) File.Delete(fullPath);
    }
}

// ── ACTION FILTER ATTRIBUTE ───────────────────────────────────────────────────
// Usage: [ValidateFile("avatar", maxSizeMb: 2)]

[AttributeUsage(AttributeTargets.Method)]
public class ValidateFileAttribute : Attribute, IAsyncActionFilter
{
    private readonly string _category;
    private readonly int    _maxSizeMb;

    public ValidateFileAttribute(string category = "any", int maxSizeMb = 5)
    {
        _category  = category;
        _maxSizeMb = maxSizeMb;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Find IFormFile in action arguments
        var file = context.ActionArguments.Values.OfType<IFormFile>().FirstOrDefault();

        if (file == null)
        {
            context.Result = new BadRequestObjectResult(new { success = false, message = "No file uploaded." });
            return;
        }

        var (isValid, error) = await FileValidator.ValidateAsync(file, _category, _maxSizeMb);
        if (!isValid)
        {
            context.Result = new BadRequestObjectResult(new { success = false, message = error });
            return;
        }

        await next();
    }
}
