namespace TenXConvo.Domain.Entities;

// ═══════════════════════════════════════════════════════════════════════════
//  USER / CUSTOMER PORTAL ENTITIES
//  Portal: user.10xdigitalventures.com
//  Role:   Client Role | Web Role | Anonymous (browse consultants)
// ═══════════════════════════════════════════════════════════════════════════

// CustomerProfile — the end user / client who browses and connects with consultants
public class CustomerProfile
{
    public Guid      Id          { get; set; } = Guid.NewGuid();
    public Guid      UserId      { get; set; }
    public string?   AvatarUrl   { get; set; }
    public string?   Bio         { get; set; }
    public string?   CompanyName { get; set; }
    public string?   Industry    { get; set; }
    public string?   CityName    { get; set; }
    public bool      IsActive    { get; set; } = true;
    public DateTime  JoinedDate  { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt   { get; set; }
    public AppUser   User        { get; set; } = null!;

    public ICollection<ClientConnection>  Connections   { get; set; } = new List<ClientConnection>();
    public ICollection<Conversation>      Conversations { get; set; } = new List<Conversation>();
    public ICollection<ConsultantReview>  Reviews       { get; set; } = new List<ConsultantReview>();
}

// Customer review of a consultant (star rating + comment)
public class ConsultantReview
{
    public Guid              Id           { get; set; } = Guid.NewGuid();
    public Guid              ConsultantId { get; set; }  // FK → ConsultantProfile
    public Guid              CustomerId   { get; set; }  // FK → CustomerProfile
    public int               Rating       { get; set; }  // 1–5 stars
    public string?           Comment      { get; set; }
    public DateTime          CreatedAt    { get; set; } = DateTime.UtcNow;
    public DateTime?         UpdatedAt    { get; set; }
    public ConsultantProfile Consultant   { get; set; } = null!;
    public CustomerProfile   Customer     { get; set; } = null!;
}
