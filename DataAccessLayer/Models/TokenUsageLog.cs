using System;

namespace DataAccessLayer.Models;

public partial class TokenUsageLog
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public int TokensUsed { get; set; }

    public string Action { get; set; } = "chat"; // chat, query, etc.

    public DateTime UsedAt { get; set; } = DateTime.UtcNow;

    public virtual User User { get; set; } = null!;
}
