using System;
using System.Collections.Generic;

namespace DataAccessLayer.Models;

public partial class User
{
    public long Id { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Role { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public string PasswordHash { get; set; } = null!;

    public bool? IsActive { get; set; }

    public int AvailableTokens { get; set; } = 20;

    public DateTime LastTokenUpdateTime { get; set; } = DateTime.UtcNow;

    public bool IsPro { get; set; } = false;

    public virtual ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

    public virtual ICollection<LecturerUploadPermission> LecturerUploadPermissionGrantedByNavigations { get; set; } = new List<LecturerUploadPermission>();

    public virtual ICollection<LecturerUploadPermission> LecturerUploadPermissionLecturers { get; set; } = new List<LecturerUploadPermission>();

    public virtual ICollection<ProUpgrade> ProUpgrades { get; set; } = new List<ProUpgrade>();

    public virtual ICollection<TokenUsageLog> TokenUsageLogs { get; set; } = new List<TokenUsageLog>();
}
