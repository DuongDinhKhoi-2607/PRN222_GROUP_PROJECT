using DataAccessLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories
{
    public class DashboardRepository
    {
        private readonly RagchatbotDbContext _db;
        public DashboardRepository(RagchatbotDbContext db) { _db = db; }

        // ── Pro Upgrade Queries ───────────────────────────────────────────────

        public async Task<int> GetTotalProUsersAsync()
        {
            return await _db.Users.CountAsync(u => u.IsPro && u.IsActive == true && !u.Email.StartsWith("deleted_"));
        }

        public async Task<int> GetTotalStudentsAsync()
        {
            return await _db.Users.CountAsync(u => u.Role == "student" && u.IsActive == true && !u.Email.StartsWith("deleted_"));
        }

        public async Task<decimal> GetTotalRevenueAsync()
        {
            return await _db.ProUpgrades.SumAsync(p => p.Amount);
        }

        public async Task<int> GetTotalTokensUsedAsync()
        {
            return await _db.TokenUsageLogs.SumAsync(t => t.TokensUsed);
        }

        /// <summary>
        /// Gets pro upgrade counts grouped by time period.
        /// </summary>
        public async Task<List<(DateTime Period, int Count, decimal Revenue)>> GetProUpgradesByPeriodAsync(
            DateTime from, DateTime to, string groupBy)
        {
            var query = _db.ProUpgrades
                .Where(p => p.UpgradedAt >= from && p.UpgradedAt <= to);

            var grouped = groupBy.ToLower() switch
            {
                "week" => query.GroupBy(p => new { p.UpgradedAt.Year, Week = EF.Functions.DateDiffWeek(new DateTime(p.UpgradedAt.Year, 1, 1), p.UpgradedAt) })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Period = g.Key.Week,
                        Count = g.Count(),
                        Revenue = g.Sum(x => x.Amount)
                    }),
                "year" => query.GroupBy(p => new { p.UpgradedAt.Year, Period = 0 })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Period = g.Key.Period,
                        Count = g.Count(),
                        Revenue = g.Sum(x => x.Amount)
                    }),
                _ => query.GroupBy(p => new { p.UpgradedAt.Year, Period = p.UpgradedAt.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Period = g.Key.Period,
                        Count = g.Count(),
                        Revenue = g.Sum(x => x.Amount)
                    })
            };

            var results = await grouped.OrderBy(g => g.Year).ThenBy(g => g.Period).ToListAsync();

            return results.Select(r => groupBy.ToLower() switch
            {
                "year" => (new DateTime(r.Year, 1, 1), r.Count, r.Revenue),
                "week" => (new DateTime(r.Year, 1, 1).AddDays(r.Period * 7), r.Count, r.Revenue),
                _ => (new DateTime(r.Year, r.Period, 1), r.Count, r.Revenue)
            }).ToList();
        }

        /// <summary>
        /// Gets token usage counts grouped by time period.
        /// </summary>
        public async Task<List<(DateTime Period, int TotalTokens)>> GetTokenUsageByPeriodAsync(
            DateTime from, DateTime to, string groupBy)
        {
            var query = _db.TokenUsageLogs
                .Where(t => t.UsedAt >= from && t.UsedAt <= to);

            var grouped = groupBy.ToLower() switch
            {
                "week" => query.GroupBy(t => new { t.UsedAt.Year, Week = EF.Functions.DateDiffWeek(new DateTime(t.UsedAt.Year, 1, 1), t.UsedAt) })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Period = g.Key.Week,
                        TotalTokens = g.Sum(x => x.TokensUsed)
                    }),
                "year" => query.GroupBy(t => new { t.UsedAt.Year, Period = 0 })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Period = g.Key.Period,
                        TotalTokens = g.Sum(x => x.TokensUsed)
                    }),
                _ => query.GroupBy(t => new { t.UsedAt.Year, Period = t.UsedAt.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Period = g.Key.Period,
                        TotalTokens = g.Sum(x => x.TokensUsed)
                    })
            };

            var results = await grouped.OrderBy(g => g.Year).ThenBy(g => g.Period).ToListAsync();

            return results.Select(r => groupBy.ToLower() switch
            {
                "year" => (new DateTime(r.Year, 1, 1), r.TotalTokens),
                "week" => (new DateTime(r.Year, 1, 1).AddDays(r.Period * 7), r.TotalTokens),
                _ => (new DateTime(r.Year, r.Period, 1), r.TotalTokens)
            }).ToList();
        }

        /// <summary>
        /// Gets recent pro upgrade records with user info.
        /// </summary>
        public async Task<List<ProUpgrade>> GetRecentProUpgradesAsync(int count = 10)
        {
            return await _db.ProUpgrades
                .Include(p => p.User)
                .OrderByDescending(p => p.UpgradedAt)
                .Take(count)
                .ToListAsync();
        }

        /// <summary>
        /// Records a new pro upgrade purchase.
        /// </summary>
        public async Task<ProUpgrade> AddProUpgradeAsync(ProUpgrade upgrade)
        {
            _db.ProUpgrades.Add(upgrade);
            await _db.SaveChangesAsync();
            return upgrade;
        }

        /// <summary>
        /// Records a token usage log entry.
        /// </summary>
        public async Task<TokenUsageLog> AddTokenUsageLogAsync(TokenUsageLog log)
        {
            _db.TokenUsageLogs.Add(log);
            await _db.SaveChangesAsync();
            return log;
        }
    }
}
