using BussinessLayer.DTOs.VNPay;
using BussinessLayer.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using PresentationLayer.Hubs;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PresentationLayer.Pages.Payment
{
    [Authorize]
    public class CallbackModel : PageModel
    {
        private readonly IVnPayService _vnPayService;
        private readonly IUserService _userService;
        private readonly IDashboardService _dashboardService;
        private readonly IHubContext<DashboardHub> _dashboardHub;

        public PaymentResponseModel ResponseModel { get; set; } = null!;

        public CallbackModel(IVnPayService vnPayService, IUserService userService,
            IDashboardService dashboardService, IHubContext<DashboardHub> dashboardHub)
        {
            _vnPayService = vnPayService;
            _userService = userService;
            _dashboardService = dashboardService;
            _dashboardHub = dashboardHub;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            ResponseModel = _vnPayService.PaymentExecute(Request.Query);

            if (ResponseModel.Success && ResponseModel.VnPayResponseCode == "00")
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && long.TryParse(userIdClaim.Value, out var userId))
                {
                    bool success = await _userService.UpgradeToProAsync(userId);
                    if (success)
                    {
                        // Record upgrade for dashboard statistics with the correct package price 49000
                        await _dashboardService.RecordProUpgradeAsync(userId, 49000, ResponseModel.TransactionId);

                        // Push real-time update to admin dashboard
                        await _dashboardHub.Clients.Group("AdminDashboard")
                            .SendAsync("DashboardUpdated", "ProUpgrade", new { userId });
                        
                        var summary = await _dashboardService.GetSummaryAsync();
                        await _dashboardHub.Clients.Group("AdminDashboard")
                            .SendAsync("SummaryUpdated", summary);

                        TempData["SuccessMessage"] = "Chúc mừng! Bạn đã nâng cấp thành công lên gói Pro. Giờ đây bạn có thể Chat thoải mái không giới hạn!";
                        return RedirectToPage("/Upgrade/Index");
                    }
                }
            }

            return Page();
        }
    }
}
