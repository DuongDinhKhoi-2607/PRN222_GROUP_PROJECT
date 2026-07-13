using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BussinessLayer.Interfaces;
using BussinessLayer.DTOs;

namespace PresentationLayer.Pages.Admin
{
    [Authorize(Roles = "admin")]
    public class DashboardModel : PageModel
    {
        private readonly IDashboardService _dashboardService;

        public DashboardModel(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        public DashboardSummaryDto Summary { get; set; } = new();
        public DashboardChartDataDto ChartData { get; set; } = new();
        public List<RecentProUpgradeDto> RecentUpgrades { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string GroupBy { get; set; } = "month";

        [BindProperty(SupportsGet = true)]
        public int? Year { get; set; }

        // Serialized JSON for JavaScript charts
        public string ChartDataJson { get; set; } = "{}";

        public async Task OnGetAsync()
        {
            var fullData = await _dashboardService.GetFullDashboardAsync(GroupBy, Year);
            Summary = fullData.Summary;
            ChartData = fullData.ChartData;
            RecentUpgrades = fullData.RecentUpgrades;

            ChartDataJson = JsonSerializer.Serialize(new
            {
                proUpgrades = ChartData.ProUpgrades.Select(p => new { label = p.Label, value = p.Value, revenue = p.Revenue }),
                tokenUsage = ChartData.TokenUsage.Select(t => new { label = t.Label, value = t.Value }),
                groupBy = ChartData.GroupBy
            });
        }

        /// <summary>
        /// AJAX endpoint to refresh chart data without full page reload.
        /// </summary>
        public async Task<IActionResult> OnGetChartDataAsync(string groupBy, int? year)
        {
            var chartData = await _dashboardService.GetChartDataAsync(groupBy ?? "month", year);
            return new JsonResult(new
            {
                proUpgrades = chartData.ProUpgrades.Select(p => new { label = p.Label, value = p.Value, revenue = p.Revenue }),
                tokenUsage = chartData.TokenUsage.Select(t => new { label = t.Label, value = t.Value }),
                groupBy = chartData.GroupBy
            });
        }
    }
}
