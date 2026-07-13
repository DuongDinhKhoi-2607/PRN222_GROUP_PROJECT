using System;
using System.Collections.Generic;

namespace BussinessLayer.DTOs
{
    public class DashboardSummaryDto
    {
        public int TotalStudents { get; set; }
        public int TotalProUsers { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalTokensUsed { get; set; }
        public double ProConversionRate { get; set; }
    }

    public class TimeSeriesDataPointDto
    {
        public string Label { get; set; } = string.Empty;
        public int Value { get; set; }
        public decimal Revenue { get; set; }
    }

    public class DashboardChartDataDto
    {
        public List<TimeSeriesDataPointDto> ProUpgrades { get; set; } = new();
        public List<TimeSeriesDataPointDto> TokenUsage { get; set; } = new();
        public string GroupBy { get; set; } = "month";
    }

    public class RecentProUpgradeDto
    {
        public long UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public DateTime UpgradedAt { get; set; }
    }

    public class DashboardFullDto
    {
        public DashboardSummaryDto Summary { get; set; } = new();
        public DashboardChartDataDto ChartData { get; set; } = new();
        public List<RecentProUpgradeDto> RecentUpgrades { get; set; } = new();
    }
}
