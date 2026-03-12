namespace TenXConvo.Domain.Entities;

// ═══════════════════════════════════════════════════════════════════════════
//  CONSULTANT PORTAL ENTITIES
//  Portal: consultant.10xdigitalventures.com
//  Role:   Consultant Role
//  URL:    /MHM/... (Messaging Hub Management)
// ═══════════════════════════════════════════════════════════════════════════

// ConsultantProfile — public card on user.10xdigitalventures.com/Consultants
// Card shows: Avatar (online dot), Name, Bio/tagline, Joined date, Connect button
public class ConsultantProfile
{
    public Guid      Id          { get; set; } = Guid.NewGuid();
    public Guid      UserId      { get; set; }
    public string?   Bio         { get; set; }      // tagline on card e.g. "Strategic Consulting Services..."
    public string?   AvatarUrl   { get; set; }
    public string    Slug        { get; set; } = string.Empty;  // unique URL slug e.g. "ali-khan" → /c/ali-khan
    public bool      IsOnline    { get; set; } = false;  // dot indicator on avatar
    public bool      IsPublic    { get; set; } = true;   // visible on public directory
    public DateTime  JoinedDate  { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt   { get; set; }
    public AppUser   User        { get; set; } = null!;

    // Consultant's expertise / service areas
    public string?   Specialization { get; set; }
    public string?   Experience     { get; set; }
    public decimal?  HourlyRate     { get; set; }
    public string?   Timezone       { get; set; }

    public ICollection<ConsultantAvailability> Availabilities { get; set; } = new List<ConsultantAvailability>();
    public ICollection<ClientConnection>       Connections    { get; set; } = new List<ClientConnection>();
    public ICollection<Conversation>           Conversations  { get; set; } = new List<Conversation>();
}

// Consultant availability slots
public class ConsultantAvailability
{
    public Guid              Id                { get; set; } = Guid.NewGuid();
    public Guid              ConsultantId      { get; set; }  // FK → ConsultantProfile
    public DayOfWeek         DayOfWeek         { get; set; }
    public TimeOnly          StartTime         { get; set; }
    public TimeOnly          EndTime           { get; set; }
    public bool              IsAvailable       { get; set; } = true;
    public ConsultantProfile Consultant        { get; set; } = null!;
}

// Connection between a consultant and a client (Connect ↗ button)
public class ClientConnection
{
    public Guid              Id           { get; set; } = Guid.NewGuid();
    public Guid              ConsultantId { get; set; }  // FK → ConsultantProfile
    public Guid              CustomerId   { get; set; }  // FK → CustomerProfile
    public string            Status       { get; set; } = "pending";  // pending | accepted | rejected
    public DateTime          RequestedAt  { get; set; } = DateTime.UtcNow;
    public DateTime?         AcceptedAt   { get; set; }
    public ConsultantProfile Consultant   { get; set; } = null!;
    public CustomerProfile   Customer     { get; set; } = null!;
}

// ── MESSAGING ────────────────────────────────────────────────────────────────

// Conversation thread between consultant and client
public class Conversation
{
    public Guid              Id           { get; set; } = Guid.NewGuid();
    public Guid              ConsultantId { get; set; }  // FK → ConsultantProfile
    public Guid              CustomerId   { get; set; }  // FK → CustomerProfile
    public DateTime          CreatedAt    { get; set; } = DateTime.UtcNow;
    public DateTime?         LastMessageAt { get; set; }
    public bool              IsActive     { get; set; } = true;
    public ConsultantProfile Consultant   { get; set; } = null!;
    public CustomerProfile   Customer     { get; set; } = null!;
    public ICollection<Message> Messages  { get; set; } = new List<Message>();
}

// Individual chat message
public class Message
{
    public Guid         Id             { get; set; } = Guid.NewGuid();
    public Guid         ConversationId { get; set; }
    public Guid         SenderId       { get; set; }  // FK → AppUser
    public string       Body           { get; set; } = string.Empty;
    public string       MessageType    { get; set; } = "text";  // text | image | file | audio
    public string?      AttachmentUrl  { get; set; }
    public bool         IsRead         { get; set; } = false;
    public DateTime     SentAt         { get; set; } = DateTime.UtcNow;
    public DateTime?    ReadAt         { get; set; }
    public DateTime?    DeletedAt      { get; set; }  // soft delete
    public Conversation Conversation   { get; set; } = null!;
    public AppUser      Sender         { get; set; } = null!;
}
