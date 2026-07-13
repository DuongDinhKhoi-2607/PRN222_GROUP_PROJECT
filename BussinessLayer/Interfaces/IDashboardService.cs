using BussinessLayer.DTOs;
using System.Threading.Tasks;

namespace BussinessLayer.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardSummaryDto> GetSummaryAsync();
        Task<DashboardChartDataDto> GetChartDataAsync(string groupBy, int? year = null);
        Task<List<RecentProUpgradeDto>> GetRecentUpgradesAsync(int count = 10);
        Task<DashboardFullDto> GetFullDashboardAsync(string groupBy = "month", int? year = null);
        Task RecordProUpgradeAsync(long userId, decimal amount, string? transactionId = null);
        Task RecordTokenUsageAsync(long userId, int tokensUsed, string action = "chat");
    }
}
