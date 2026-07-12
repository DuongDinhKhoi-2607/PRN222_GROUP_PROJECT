using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BussinessLayer.Interfaces;
using BussinessLayer.DTOs.VNPay;
using System;

namespace PresentationLayer.Pages.Upgrade
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly IVnPayService _vnPayService;

        public IndexModel(IUserService userService, IVnPayService vnPayService)
        {
            _userService = userService;
            _vnPayService = vnPayService;
        }

        public bool IsPro { get; set; }
        public int AvailableTokens { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized();

            var tokenInfo = await _userService.GetUserTokenInfoAsync(userId);
            IsPro = tokenInfo.IsPro;
            AvailableTokens = tokenInfo.AvailableTokens;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized();

            // Create VNPay payment URL for Pro package (49,000 VND)
            var model = new PaymentInformationModel
            {
                OrderID = new Random().Next(1000, 99999), 
                Amount = 49000,
                Name = $"Upgrade_Pro_{userId}_{DateTime.Now.Ticks}",
                OrderDescription = $"Upgrade to Pro plan for user {userId}",
                OrderType = "other"
            };

            var url = _vnPayService.CreatePaymentUrl(model, HttpContext);

            return Redirect(url);
        }
    }
}
