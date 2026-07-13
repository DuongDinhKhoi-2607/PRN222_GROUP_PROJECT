using BussinessLayer.DTOs.VNPay;
using BussinessLayer.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PresentationLayer.Pages.Payment
{
    [Authorize]
    public class CallbackModel : PageModel
    {
        private readonly IVnPayService _vnPayService;
        private readonly IUserService _userService;

        public PaymentResponseModel ResponseModel { get; set; } = null!;

        public CallbackModel(IVnPayService vnPayService, IUserService userService)
        {
            _vnPayService = vnPayService;
            _userService = userService;
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
                        TempData["SuccessMessage"] = "Chúc mừng! Bạn đã nâng cấp thành công lên gói Pro. Giờ đây bạn có thể Chat thoải mái không giới hạn!";
                        return RedirectToPage("/Upgrade/Index");
                    }
                }
            }

            return Page();
        }
    }
}
