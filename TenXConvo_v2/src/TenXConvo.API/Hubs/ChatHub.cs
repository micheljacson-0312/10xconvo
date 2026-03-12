using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TenXConvo.Domain.Entities;
using TenXConvo.Infrastructure.Data;
using TenXConvo.Infrastructure.Services;

namespace TenXConvo.API.Hubs;

// ═══════════════════════════════════════════════════════════════════════════
//  CHAT HUB — Real-time messaging via SignalR
//  URL: wss://api.10xdigitalventures.com/hubs/chat
//
//  How it works:
//  1. Frontend connects: new HubConnectionBuilder().withUrl("/hubs/chat", { accessTokenFactory: () => jwt }).build()
//  2. User joins their conversation room: connection.invoke("JoinConversation", conversationId)
//  3. User sends message: connection.invoke("SendMessage", conversationId, body)
//  4. All members in that room receive: connection.on("ReceiveMessage", (message) => ...)
//  5. Online presence tracked via OnConnected/OnDisconnected
//
//  Client Events (Frontend listens to these):
//    ReceiveMessage      → new message arrived
//    MessageRead         → other user read the messages
//    UserOnline          → consultant came online
//    UserOffline         → consultant went offline
//    ConnectionAccepted  → consultant accepted connect request
//    NewConnectionRequest → new customer wants to connect (consultant sees)
//    TypingStarted       → user is typing...
//    TypingStopped       → user stopped typing
//    Error               → something went wrong
// ═══════════════════════════════════════════════════════════════════════════

[Authorize]
public class ChatHub : Hub
{
    private readonly AppDbContext _db;
    private readonly CreditService _credits;

    // In-memory store: userId → connectionId (for online presence)
    // In production: use Redis or IDistributedCache
    private static readonly Dictionary<string, HashSet<string>> _userConnections = new();
    private static readonly object _lock = new();

    public ChatHub(AppDbContext db, CreditService credits) { _db = db; _credits = credits; }

    // ── ON CONNECTED ─────────────────────────────────────────────────────────
    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId == null) return;

        // Track connection
        lock (_lock)
        {
            if (!_userConnections.ContainsKey(userId))
                _userConnections[userId] = new HashSet<string>();
            _userConnections[userId].Add(Context.ConnectionId);
        }

        // Auto-join all conversation rooms this user belongs to
        var rooms = await GetUserConversationRooms(Guid.Parse(userId));
        foreach (var room in rooms)
            await Groups.AddToGroupAsync(Context.ConnectionId, room);

        // Notify others this user is online
        await Clients.Others.SendAsync("UserOnline", userId);

        await base.OnConnectedAsync();
    }

    // ── ON DISCONNECTED ──────────────────────────────────────────────────────
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId == null) return;

        lock (_lock)
        {
            if (_userConnections.ContainsKey(userId))
            {
                _userConnections[userId].Remove(Context.ConnectionId);
                if (_userConnections[userId].Count == 0)
                    _userConnections.Remove(userId);
            }
        }

        // Update consultant IsOnline flag if all connections dropped
        bool stillOnline;
        lock (_lock) { stillOnline = _userConnections.ContainsKey(userId); }

        if (!stillOnline)
        {
            var profile = await _db.ConsultantProfiles
                .FirstOrDefaultAsync(p => p.UserId == Guid.Parse(userId));
            if (profile != null)
            {
                profile.IsOnline  = false;
                profile.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
            await Clients.Others.SendAsync("UserOffline", userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    // ── JOIN CONVERSATION ROOM ───────────────────────────────────────────────
    // Frontend calls this after opening a chat window
    public async Task JoinConversation(Guid conversationId)
    {
        var userId = GetUserId();
        if (userId == null) return;

        // Verify user is part of this conversation
        if (!await IsUserInConversation(Guid.Parse(userId), conversationId))
        {
            await Clients.Caller.SendAsync("Error", "Access denied to this conversation.");
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, ConversationRoom(conversationId));
    }

    // ── LEAVE CONVERSATION ROOM ──────────────────────────────────────────────
    public async Task LeaveConversation(Guid conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, ConversationRoom(conversationId));
    }

    // ── SEND MESSAGE ─────────────────────────────────────────────────────────
    // Frontend: connection.invoke("SendMessage", conversationId, "Hello!", "text", null)
    // Everyone in room receives: ReceiveMessage event
    // durationSeconds: for audio/video messages (null for text/image/file)
    public async Task SendMessage(Guid conversationId, string body, string messageType = "text", double? durationSeconds = null)
    {
        var userId = GetUserId();
        if (userId == null) return;

        var senderGuid = Guid.Parse(userId);

        // Verify user is in conversation
        if (!await IsUserInConversation(senderGuid, conversationId))
        {
            await Clients.Caller.SendAsync("Error", "Access denied.");
            return;
        }

        if (string.IsNullOrWhiteSpace(body) && messageType == "text")
        {
            await Clients.Caller.SendAsync("Error", "Message cannot be empty.");
            return;
        }

        // ── CREDIT CHECK — Only charge clients, consultants are FREE ─────────
        var isConsultant = await _credits.IsConsultantAsync(senderGuid);

        if (!isConsultant)
        {
            var charCount = messageType == "text" ? body?.Length ?? 0 : 0;

            // Pre-check: does user have enough credits?
            var credits = await _credits.GetCreditsAsync(senderGuid);
            var insufficient = messageType switch
            {
                "text"  => credits.TextCharsRemaining < (charCount > 0 ? charCount : 1),
                "audio" => credits.AudioMinsRemaining < 0.1,
                "video" => credits.VideoMinsRemaining < 0.1,
                "image" => credits.ImageCreditsRemaining < 1,
                "file"  => credits.FileCreditsRemaining < 1,
                _       => false
            };

            if (insufficient)
            {
                await Clients.Caller.SendAsync("InsufficientCredits", new
                {
                    creditType = messageType,
                    remaining  = messageType switch {
                        "text"  => (double)credits.TextCharsRemaining,
                        "audio" => credits.AudioMinsRemaining,
                        "video" => credits.VideoMinsRemaining,
                        "image" => (double)credits.ImageCreditsRemaining,
                        "file"  => (double)credits.FileCreditsRemaining,
                        _       => 0.0
                    },
                    message = $"Not enough {messageType} credits. Please purchase more."
                });
                return;
            }
        }

        // Save message to DB
        var conv = await _db.Conversations.FindAsync(conversationId);
        if (conv == null) return;

        var message = new Message
        {
            ConversationId = conversationId,
            SenderId       = senderGuid,
            Body           = body?.Trim() ?? "",
            MessageType    = messageType,
            SentAt         = DateTime.UtcNow
        };
        _db.Messages.Add(message);
        conv.LastMessageAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // ── CHARGE after message is saved ────────────────────────────────────
        if (!isConsultant)
        {
            var charCount = messageType == "text" ? body?.Length ?? 0 : 0;
            var chargeResult = await _credits.ChargeForMessageAsync(
                senderGuid, message.Id, messageType, charCount, durationSeconds);

            // Send updated credits to the sender
            var updatedCredits = await _credits.GetCreditsAsync(senderGuid);
            await Clients.Caller.SendAsync("CreditsUpdated", new
            {
                textChars  = updatedCredits.TextCharsRemaining,
                audioMins  = updatedCredits.AudioMinsRemaining,
                videoMins  = updatedCredits.VideoMinsRemaining,
                images     = updatedCredits.ImageCreditsRemaining,
                files      = updatedCredits.FileCreditsRemaining,
            });
        }

        // Load sender name for response
        var sender = await _db.Users.FindAsync(senderGuid);

        // Broadcast to everyone in the conversation room (including sender)
        var payload = new
        {
            messageId    = message.Id,
            conversationId,
            senderId     = senderGuid,
            senderName   = sender?.UserName ?? "",
            senderAvatar = (string?)null,
            body         = message.Body,
            messageType  = message.MessageType,
            sentAt       = message.SentAt,
            isRead       = false
        };

        await Clients.Group(ConversationRoom(conversationId))
                     .SendAsync("ReceiveMessage", payload);
    }

    // ── TYPING INDICATOR ─────────────────────────────────────────────────────
    // Frontend: connection.invoke("StartTyping", conversationId)
    public async Task StartTyping(Guid conversationId)
    {
        var userId = GetUserId();
        if (userId == null) return;

        await Clients.OthersInGroup(ConversationRoom(conversationId))
                     .SendAsync("TypingStarted", new { userId, conversationId });
    }

    // Frontend: connection.invoke("StopTyping", conversationId)
    public async Task StopTyping(Guid conversationId)
    {
        var userId = GetUserId();
        if (userId == null) return;

        await Clients.OthersInGroup(ConversationRoom(conversationId))
                     .SendAsync("TypingStopped", new { userId, conversationId });
    }

    // ── MARK MESSAGES READ ───────────────────────────────────────────────────
    // Frontend: connection.invoke("MarkRead", conversationId)
    public async Task MarkRead(Guid conversationId)
    {
        var userId = GetUserId();
        if (userId == null) return;

        var senderGuid = Guid.Parse(userId);
        var unread = await _db.Messages
            .Where(m => m.ConversationId == conversationId && m.SenderId != senderGuid && !m.IsRead)
            .ToListAsync();

        unread.ForEach(m => { m.IsRead = true; m.ReadAt = DateTime.UtcNow; });
        await _db.SaveChangesAsync();

        // Notify sender their messages were read
        await Clients.OthersInGroup(ConversationRoom(conversationId))
                     .SendAsync("MessageRead", new { conversationId, readBy = userId });
    }

    // ── NOTIFY CONNECTION REQUEST (Consultant sees new request) ──────────────
    // Called from ConsultantController when customer hits Connect button
    public static async Task NotifyConnectionRequest(
        IHubContext<ChatHub> hubContext,
        string consultantUserId,
        object requestData)
    {
        // Send to consultant's personal group (user:{id})
        await hubContext.Clients
            .Group(UserGroup(consultantUserId))
            .SendAsync("NewConnectionRequest", requestData);
    }

    // ── NOTIFY CONNECTION ACCEPTED (Customer gets notified) ─────────────────
    public static async Task NotifyConnectionAccepted(
        IHubContext<ChatHub> hubContext,
        string customerUserId,
        object connectionData)
    {
        await hubContext.Clients
            .Group(UserGroup(customerUserId))
            .SendAsync("ConnectionAccepted", connectionData);
    }

    // ── JOIN PERSONAL GROUP (for targeted notifications) ─────────────────────
    // Each user has a personal group: user:{userId}
    // This is joined automatically on connect
    private async Task JoinPersonalGroup(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
    }

    // ── HELPERS ──────────────────────────────────────────────────────────────

    private string? GetUserId() => Context.User?.FindFirst("sub")?.Value;

    private static string ConversationRoom(Guid conversationId) => $"conv:{conversationId}";
    private static string UserGroup(string userId)              => $"user:{userId}";

    private async Task<bool> IsUserInConversation(Guid userId, Guid conversationId)
    {
        var conv = await _db.Conversations
            .Include(c => c.Consultant)
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.Id == conversationId);

        if (conv == null) return false;

        return conv.Consultant.UserId == userId || conv.Customer.UserId == userId;
    }

    private async Task<List<string>> GetUserConversationRooms(Guid userId)
    {
        // Get all conversations for this user (as consultant or customer)
        var asConsultant = await _db.Conversations
            .Include(c => c.Consultant)
            .Where(c => c.Consultant.UserId == userId && c.IsActive)
            .Select(c => ConversationRoom(c.Id))
            .ToListAsync();

        var asCustomer = await _db.Conversations
            .Include(c => c.Customer)
            .Where(c => c.Customer.UserId == userId && c.IsActive)
            .Select(c => ConversationRoom(c.Id))
            .ToListAsync();

        var allRooms = asConsultant.Concat(asCustomer).Distinct().ToList();

        // Also join personal group for targeted notifications
        allRooms.Add(UserGroup(userId.ToString()));

        return allRooms;
    }
}
