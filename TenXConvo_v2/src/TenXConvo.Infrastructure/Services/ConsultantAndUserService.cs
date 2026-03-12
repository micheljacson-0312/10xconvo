using Microsoft.EntityFrameworkCore;
using TenXConvo.Application.Interfaces;
using TenXConvo.Domain.Entities;
using TenXConvo.Infrastructure.Data;
using Microsoft.Extensions.Configuration;

namespace TenXConvo.Infrastructure.Services;

// ═══════════════════════════════════════════════════════════════════════════
//  CONSULTANT SERVICE
// ═══════════════════════════════════════════════════════════════════════════
public class ConsultantService : IConsultantService
{
    private readonly AppDbContext _db;
    private readonly string       _uploadsPath;
    private readonly string       _baseUrl;

    public ConsultantService(AppDbContext db, IConfiguration config)
    {
        _db          = db;
        _uploadsPath = config["FileStorage:LocalPath"] ?? "wwwroot/uploads";
        _baseUrl     = config["FileStorage:BaseUrl"]   ?? "http://localhost:5000/uploads";
    }

    // ── PROFILE ───────────────────────────────────────────────────────────────

    public async Task<ConsultantProfileResult> GetMyProfileAsync(Guid userId)
    {
        var p = await _db.ConsultantProfiles
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.UserId == userId)
            ?? throw new KeyNotFoundException("Consultant profile not found.");

        return MapProfile(p);
    }

    public async Task<ConsultantProfileResult> UpdateProfileAsync(Guid userId, ConsultantProfileInput input)
    {
        var p = await _db.ConsultantProfiles.FirstOrDefaultAsync(x => x.UserId == userId);
        if (p == null)
        {
            p = new ConsultantProfile { UserId = userId, JoinedDate = DateTime.UtcNow };
            _db.ConsultantProfiles.Add(p);
        }
        p.Bio            = input.Bio;
        p.Specialization = input.Specialization;
        p.Experience     = input.Experience;
        p.HourlyRate     = input.HourlyRate;
        p.IsOnline       = input.IsOnline;
        p.IsPublic       = input.IsPublic;

        // Slug (URL-safe, lowercase, unique)
        if (!string.IsNullOrWhiteSpace(input.Slug))
        {
            var slug = System.Text.RegularExpressions.Regex.Replace(input.Slug.Trim().ToLower(), @"[^a-z0-9\-]", "-");
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-{2,}", "-").Trim('-');
            var slugTaken = await _db.ConsultantProfiles.AnyAsync(x => x.Slug == slug && x.Id != p.Id);
            if (slugTaken) throw new InvalidOperationException($"The slug '{slug}' is already taken. Choose another.");
            p.Slug = slug;
        }
        else if (string.IsNullOrEmpty(p.Slug))
        {
            // Auto-generate slug from username on first save
            var user = await _db.Users.FindAsync(userId);
            var baseSlug = System.Text.RegularExpressions.Regex.Replace((user?.UserName ?? "consultant").Trim().ToLower(), @"[^a-z0-9\-]", "-");
            baseSlug = System.Text.RegularExpressions.Regex.Replace(baseSlug, @"-{2,}", "-").Trim('-');
            var slug = baseSlug;
            var counter = 1;
            while (await _db.ConsultantProfiles.AnyAsync(x => x.Slug == slug && x.Id != p.Id))
                slug = $"{baseSlug}-{counter++}";
            p.Slug = slug;
        }

        p.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return await GetMyProfileAsync(userId);
    }

    public async Task<string> UploadAvatarAsync(Guid userId, Stream fileStream, string fileName)
    {
        var ext     = Path.GetExtension(fileName);
        var newName = $"{Guid.NewGuid()}{ext}";
        var folder  = Path.Combine(_uploadsPath, "avatars");
        Directory.CreateDirectory(folder);
        using var fs = File.Create(Path.Combine(folder, newName));
        await fileStream.CopyToAsync(fs);

        var url = $"{_baseUrl}/avatars/{newName}";
        var p   = await _db.ConsultantProfiles.FirstOrDefaultAsync(x => x.UserId == userId);
        if (p != null) { p.AvatarUrl = url; p.UpdatedAt = DateTime.UtcNow; await _db.SaveChangesAsync(); }
        return url;
    }

    public async Task SetOnlineStatusAsync(Guid userId, bool isOnline)
    {
        var p = await _db.ConsultantProfiles.FirstOrDefaultAsync(x => x.UserId == userId);
        if (p != null) { p.IsOnline = isOnline; p.UpdatedAt = DateTime.UtcNow; await _db.SaveChangesAsync(); }
    }

    // ── CLIENTS ───────────────────────────────────────────────────────────────

    public async Task<PagedResult<ClientResult>> GetMyClientsAsync(Guid consultantUserId, int page, int pageSize, string? search)
    {
        var cp = await _db.ConsultantProfiles.FirstOrDefaultAsync(x => x.UserId == consultantUserId)
            ?? throw new KeyNotFoundException("Consultant profile not found.");

        var q = _db.ClientConnections
            .Include(c => c.Customer).ThenInclude(cu => cu.User)
            .Where(c => c.ConsultantId == cp.Id && c.Status == "accepted");

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(c => c.Customer.User.UserName.Contains(search));

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(c => c.AcceptedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(c => new ClientResult(c.Customer.UserId, c.Customer.User.UserName, c.Customer.AvatarUrl, c.AcceptedAt ?? c.RequestedAt))
            .ToListAsync();

        return new PagedResult<ClientResult>(items, total, page, pageSize);
    }

    public async Task<List<ConnectionRequestResult>> GetPendingRequestsAsync(Guid consultantUserId)
    {
        var cp = await _db.ConsultantProfiles.FirstOrDefaultAsync(x => x.UserId == consultantUserId)
            ?? throw new KeyNotFoundException("Consultant profile not found.");

        return await _db.ClientConnections
            .Include(c => c.Customer).ThenInclude(cu => cu.User)
            .Where(c => c.ConsultantId == cp.Id && c.Status == "pending")
            .OrderByDescending(c => c.RequestedAt)
            .Select(c => new ConnectionRequestResult(c.Id, c.Customer.UserId, c.Customer.User.UserName, c.Customer.AvatarUrl, c.RequestedAt))
            .ToListAsync();
    }

    public async Task AcceptRequestAsync(Guid connectionId, Guid consultantUserId)
    {
        var c = await _db.ClientConnections
            .Include(x => x.Consultant)
            .FirstOrDefaultAsync(x => x.Id == connectionId && x.Consultant.UserId == consultantUserId)
            ?? throw new KeyNotFoundException("Connection request not found.");

        c.Status     = "accepted";
        c.AcceptedAt = DateTime.UtcNow;

        // Auto-create conversation
        var custProfile = await _db.CustomerProfiles.FirstOrDefaultAsync(x => x.Id == c.CustomerId);
        if (custProfile != null && !await _db.Conversations.AnyAsync(x => x.ConsultantId == c.ConsultantId && x.CustomerId == c.CustomerId))
        {
            _db.Conversations.Add(new Conversation
            {
                ConsultantId = c.ConsultantId,
                CustomerId   = c.CustomerId
            });
        }

        await _db.SaveChangesAsync();
    }

    public async Task RejectRequestAsync(Guid connectionId, Guid consultantUserId)
    {
        var c = await _db.ClientConnections
            .Include(x => x.Consultant)
            .FirstOrDefaultAsync(x => x.Id == connectionId && x.Consultant.UserId == consultantUserId)
            ?? throw new KeyNotFoundException("Connection request not found.");

        c.Status = "rejected";
        await _db.SaveChangesAsync();
    }

    // ── MESSAGING ────────────────────────────────────────────────────────────

    public async Task<PagedResult<ConversationResult>> GetConversationsAsync(Guid consultantUserId, int page, int pageSize)
    {
        var cp = await _db.ConsultantProfiles.FirstOrDefaultAsync(x => x.UserId == consultantUserId)
            ?? throw new KeyNotFoundException("Consultant profile not found.");

        var q = _db.Conversations
            .Include(c => c.Customer).ThenInclude(cu => cu.User)
            .Where(c => c.ConsultantId == cp.Id && c.IsActive);

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        var results = new List<ConversationResult>();
        foreach (var conv in items)
        {
            var lastMsg   = await _db.Messages.Where(m => m.ConversationId == conv.Id && m.DeletedAt == null).OrderByDescending(m => m.SentAt).FirstOrDefaultAsync();
            var unread    = await _db.Messages.CountAsync(m => m.ConversationId == conv.Id && !m.IsRead && m.SenderId != consultantUserId && m.DeletedAt == null);
            results.Add(new ConversationResult(conv.Id, conv.Customer.UserId, conv.Customer.User.UserName, conv.Customer.AvatarUrl, lastMsg?.Body, lastMsg?.SentAt, unread));
        }
        return new PagedResult<ConversationResult>(results, total, page, pageSize);
    }

    public async Task<PagedResult<MessageResult>> GetMessagesAsync(Guid conversationId, Guid requestingUserId, int page, int pageSize)
    {
        var total = await _db.Messages.CountAsync(m => m.ConversationId == conversationId && m.DeletedAt == null);
        var items = await _db.Messages
            .Include(m => m.Sender)
            .Where(m => m.ConversationId == conversationId && m.DeletedAt == null)
            .OrderByDescending(m => m.SentAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(m => new MessageResult(m.Id, m.SenderId, m.Sender.UserName, m.Body, m.MessageType, m.AttachmentUrl, m.IsRead, m.SentAt))
            .ToListAsync();

        return new PagedResult<MessageResult>(items, total, page, pageSize);
    }

    public async Task<MessageResult> SendMessageAsync(Guid conversationId, Guid senderUserId, string body, string messageType = "text")
    {
        var conv = await _db.Conversations.FindAsync(conversationId)
            ?? throw new KeyNotFoundException("Conversation not found.");

        var msg = new Message
        {
            ConversationId = conversationId,
            SenderId       = senderUserId,
            Body           = body,
            MessageType    = messageType
        };
        _db.Messages.Add(msg);
        conv.LastMessageAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var sender = await _db.Users.FindAsync(senderUserId);
        return new MessageResult(msg.Id, msg.SenderId, sender?.UserName ?? "", msg.Body, msg.MessageType, null, false, msg.SentAt);
    }

    public async Task MarkConversationReadAsync(Guid conversationId, Guid userId)
    {
        var unread = await _db.Messages
            .Where(m => m.ConversationId == conversationId && m.SenderId != userId && !m.IsRead)
            .ToListAsync();
        unread.ForEach(m => { m.IsRead = true; m.ReadAt = DateTime.UtcNow; });
        await _db.SaveChangesAsync();
    }

    private static ConsultantProfileResult MapProfile(ConsultantProfile p) =>
        new(p.UserId, p.User.UserName, p.Bio, p.AvatarUrl, p.IsOnline, p.IsPublic, p.JoinedDate,
            p.Specialization, p.Experience, p.HourlyRate, p.Slug,
            string.IsNullOrEmpty(p.Slug) ? null : $"/c/{p.Slug}");
}

// ═══════════════════════════════════════════════════════════════════════════
//  USER SERVICE
// ═══════════════════════════════════════════════════════════════════════════
public class UserService : IUserService
{
    private readonly AppDbContext _db;

    public UserService(AppDbContext db) => _db = db;

    // ── PUBLIC CONSULTANT DIRECTORY ───────────────────────────────────────────

    public async Task<PagedResult<ConsultantCardResult>> GetConsultantsAsync(int page, int pageSize, string? search)
    {
        var q = _db.ConsultantProfiles
            .Include(p => p.User)
            .Where(p => p.IsPublic)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(p => p.User.UserName.Contains(search) || (p.Bio != null && p.Bio.Contains(search)));

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(p => p.JoinedDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new ConsultantCardResult(p.UserId, p.User.UserName, p.Bio, p.AvatarUrl, p.IsOnline, p.JoinedDate, p.Slug, p.Specialization, p.Experience, p.HourlyRate))
            .ToListAsync();

        return new PagedResult<ConsultantCardResult>(items, total, page, pageSize);
    }

    public async Task<ConsultantCardResult> GetConsultantByIdAsync(Guid consultantUserId)
    {
        var p = await _db.ConsultantProfiles
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.UserId == consultantUserId && x.IsPublic)
            ?? throw new KeyNotFoundException("Consultant not found.");

        return new ConsultantCardResult(p.UserId, p.User.UserName, p.Bio, p.AvatarUrl, p.IsOnline, p.JoinedDate, p.Slug, p.Specialization, p.Experience, p.HourlyRate);
    }

    public async Task<ConsultantCardResult> GetConsultantBySlugAsync(string slug)
    {
        var p = await _db.ConsultantProfiles
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Slug == slug.ToLower() && x.IsPublic)
            ?? throw new KeyNotFoundException("Consultant not found.");

        return new ConsultantCardResult(p.UserId, p.User.UserName, p.Bio, p.AvatarUrl, p.IsOnline, p.JoinedDate, p.Slug, p.Specialization, p.Experience, p.HourlyRate);
    }

    // ── CONNECT BUTTON ────────────────────────────────────────────────────────

    public async Task<ConnectionResult> ConnectAsync(Guid customerUserId, Guid consultantUserId)
    {
        var cp = await _db.ConsultantProfiles.FirstOrDefaultAsync(x => x.UserId == consultantUserId)
            ?? throw new KeyNotFoundException("Consultant not found.");

        var cu = await _db.CustomerProfiles.FirstOrDefaultAsync(x => x.UserId == customerUserId)
            ?? throw new KeyNotFoundException("Customer profile not found. Please complete your profile first.");

        // Check if already connected or pending
        var existing = await _db.ClientConnections
            .FirstOrDefaultAsync(x => x.ConsultantId == cp.Id && x.CustomerId == cu.Id);

        if (existing != null)
            return new ConnectionResult(existing.Id, existing.Status);

        var conn = new ClientConnection
        {
            ConsultantId = cp.Id,
            CustomerId   = cu.Id,
            Status       = "pending"
        };
        _db.ClientConnections.Add(conn);
        await _db.SaveChangesAsync();

        return new ConnectionResult(conn.Id, conn.Status);
    }

    // ── CUSTOMER PROFILE ──────────────────────────────────────────────────────

    public async Task<CustomerProfileResult> GetMyProfileAsync(Guid userId)
    {
        var p = await _db.CustomerProfiles
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.UserId == userId)
            ?? throw new KeyNotFoundException("Customer profile not found.");

        return new CustomerProfileResult(p.UserId, p.User.UserName, p.AvatarUrl, p.Bio, p.CompanyName, p.Industry, p.JoinedDate);
    }

    public async Task<CustomerProfileResult> UpdateProfileAsync(Guid userId, CustomerProfileInput input)
    {
        var p = await _db.CustomerProfiles.FirstOrDefaultAsync(x => x.UserId == userId);
        if (p == null)
        {
            p = new CustomerProfile { UserId = userId, JoinedDate = DateTime.UtcNow };
            _db.CustomerProfiles.Add(p);
        }
        p.Bio         = input.Bio;
        p.CompanyName = input.CompanyName;
        p.Industry    = input.Industry;
        p.CityName    = input.CityName;
        p.UpdatedAt   = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return await GetMyProfileAsync(userId);
    }

    // ── MESSAGING ─────────────────────────────────────────────────────────────

    public async Task<PagedResult<ConversationResult>> GetConversationsAsync(Guid customerUserId, int page, int pageSize)
    {
        var cu = await _db.CustomerProfiles.FirstOrDefaultAsync(x => x.UserId == customerUserId)
            ?? throw new KeyNotFoundException("Customer profile not found.");

        var q = _db.Conversations
            .Include(c => c.Consultant).ThenInclude(cp => cp.User)
            .Where(c => c.CustomerId == cu.Id && c.IsActive);

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        var results = new List<ConversationResult>();
        foreach (var conv in items)
        {
            var lastMsg = await _db.Messages.Where(m => m.ConversationId == conv.Id && m.DeletedAt == null).OrderByDescending(m => m.SentAt).FirstOrDefaultAsync();
            var unread  = await _db.Messages.CountAsync(m => m.ConversationId == conv.Id && !m.IsRead && m.SenderId != customerUserId && m.DeletedAt == null);
            results.Add(new ConversationResult(conv.Id, conv.Consultant.UserId, conv.Consultant.User.UserName, conv.Consultant.AvatarUrl, lastMsg?.Body, lastMsg?.SentAt, unread));
        }
        return new PagedResult<ConversationResult>(results, total, page, pageSize);
    }

    public async Task<PagedResult<MessageResult>> GetMessagesAsync(Guid conversationId, Guid requestingUserId, int page, int pageSize)
    {
        var total = await _db.Messages.CountAsync(m => m.ConversationId == conversationId && m.DeletedAt == null);
        var items = await _db.Messages
            .Include(m => m.Sender)
            .Where(m => m.ConversationId == conversationId && m.DeletedAt == null)
            .OrderByDescending(m => m.SentAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(m => new MessageResult(m.Id, m.SenderId, m.Sender.UserName, m.Body, m.MessageType, m.AttachmentUrl, m.IsRead, m.SentAt))
            .ToListAsync();

        return new PagedResult<MessageResult>(items, total, page, pageSize);
    }

    public async Task<MessageResult> SendMessageAsync(Guid conversationId, Guid senderUserId, string body)
    {
        var conv = await _db.Conversations.FindAsync(conversationId)
            ?? throw new KeyNotFoundException("Conversation not found.");

        var msg = new Message { ConversationId = conversationId, SenderId = senderUserId, Body = body, MessageType = "text" };
        _db.Messages.Add(msg);
        conv.LastMessageAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var sender = await _db.Users.FindAsync(senderUserId);
        return new MessageResult(msg.Id, msg.SenderId, sender?.UserName ?? "", msg.Body, msg.MessageType, null, false, msg.SentAt);
    }
}
