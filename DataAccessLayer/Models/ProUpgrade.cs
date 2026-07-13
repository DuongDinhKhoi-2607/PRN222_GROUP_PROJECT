using System;

namespace DataAccessLayer.Models;

public partial class ProUpgrade
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public decimal Amount { get; set; }

    public string PaymentMethod { get; set; } = "VNPay";

    public string? TransactionId { get; set; }

    public DateTime UpgradedAt { get; set; } = DateTime.UtcNow;

    public virtual User User { get; set; } = null!;
}
