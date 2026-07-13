using DataAccessLayer.Repositories;
using DataAccessLayer.Models;
using BussinessLayer.Interfaces;
using BussinessLayer.DTOs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace BussinessLayer.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly DashboardRepository _dashboardRepo;

        public DashboardService(DashboardRepository dashboardRepo)
        {
            _dashboardRepo = dashboardRepo;
        }

        public async Task<DashboardSummaryDto> GetSummaryAsync()
        {
            var totalStudents = await _dashboardRepo.GetTotalStudentsAsync();
            var totalProUsers = await _dashboardRepo.GetTotalProUsersAsync();
            var totalRevenue = await _dashboardRepo.GetTotalRevenueAsync();
            var totalTokensUsed = await _dashboardRepo.GetTotalTokensUsedAsync();

            return new DashboardSummaryDto
            {
                TotalStudents = totalStudents,
                TotalProUsers = totalProUsers,
                TotalRevenue = totalRevenue,
                TotalTokensUsed = totalTokensUsed,
                ProConversionRate = totalStudents > 0 ? Math.Round((double)totalProUsers / totalStudents * 100, 1) : 0
            };
        }

        public async Task<DashboardChartDataDto> GetChartDataAsync(string groupBy, int? year = null)
        {
            var targetYear = year ?? DateTime.Now.Year;
            DateTime from, to;

            switch (groupBy.ToLower())
            {
                case "week":
                    from = new DateTime(targetYear, 1, 1);
                    to = new DateTime(targetYear, 12, 31, 23, 59, 59);
                    break;
                case "year":
                    from = new DateTime(targetYear - 4, 1, 1); // Last 5 years
                    to = new DateTime(targetYear, 12, 31, 23, 59, 59);
                    break;
                default: // month
                    from = new DateTime(targetYear, 1, 1);
                    to = new DateTime(targetYear, 12, 31, 23, 59, 59);
                    break;
            }

            var proUpgrades = await _dashboardRepo.GetProUpgradesByPeriodAsync(from, to, groupBy);
            var tokenUsage = await _dashboardRepo.GetTokenUsageByPeriodAsync(from, to, groupBy);

            return new DashboardChartDataDto
            {
                GroupBy = groupBy,
                ProUpgrades = proUpgrades.Select(p => new TimeSeriesDataPointDto
                {
                    Label = FormatPeriodLabel(p.Period, groupBy),
                    Value = p.Count,
                    Revenue = p.Revenue
                }).ToList(),
                TokenUsage = tokenUsage.Select(t => new TimeSeriesDataPointDto
                {
                    Label = FormatPeriodLabel(t.Period, groupBy),
                    Value = t.TotalTokens
                }).ToList()
            };
        }

        public async Task<List<RecentProUpgradeDto>> GetRecentUpgradesAsync(int count = 10)
        {
            var upgrades = await _dashboardRepo.GetRecentProUpgradesAsync(count);
            return upgrades.Select(u => new RecentProUpgradeDto
            {
                UserId = u.UserId,
                UserName = u.User?.FullName ?? "Unknown",
                Email = u.User?.Email ?? "Unknown",
                Amount = u.Amount,
                PaymentMethod = u.PaymentMethod,
                UpgradedAt = u.UpgradedAt
            }).ToList();
        }

        public async Task<DashboardFullDto> GetFullDashboardAsync(string groupBy = "month", int? year = null)
        {
            var summary = await GetSummaryAsync();
            var chartData = await GetChartDataAsync(groupBy, year);
            var recentUpgrades = await GetRecentUpgradesAsync();

            return new DashboardFullDto
            {
                Summary = summary,
                ChartData = chartData,
                RecentUpgrades = recentUpgrades
            };
        }

        public async Task RecordProUpgradeAsync(long userId, decimal amount, string? transactionId = null)
        {
            var upgrade = new ProUpgrade
            {
                UserId = userId,
                Amount = amount,
                TransactionId = transactionId,
                UpgradedAt = DateTime.UtcNow
            };
            await _dashboardRepo.AddProUpgradeAsync(upgrade);
        }

        public async Task RecordTokenUsageAsync(long userId, int tokensUsed, string action = "chat")
        {
            var log = new TokenUsageLog
            {
                UserId = userId,
                TokensUsed = tokensUsed,
                Action = action,
                UsedAt = DateTime.UtcNow
            };
            await _dashboardRepo.AddTokenUsageLogAsync(log);
        }

        private static string FormatPeriodLabel(DateTime period, string groupBy)
        {
            return groupBy.ToLower() switch
            {
                "week" => $"T{CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(period, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday):D2}",
                "year" => period.ToString("yyyy"),
                _ => period.ToString("MM/yyyy")
            };
        }
    }
}
