using BussinessLayer.DTOs.VNPay;
using BussinessLayer.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PresentationLayer.Pages.Payment
{
    public class IndexModel : PageModel
    {
        private readonly IVnPayService _vnPayService;

        public IndexModel(IVnPayService vnPayService)
        {
            _vnPayService = vnPayService;
        }

        public void OnGet()
        {
        }

        public IActionResult OnPost(double amount)
        {
            if (amount < 10000)
            {
                ModelState.AddModelError("", "Số tiền tối thiểu là 10,000 VND.");
                return Page();
            }

            var model = new PaymentInformationModel
            {
                OrderID = new Random().Next(1000, 99999), // Mock Order ID
                Amount = amount,
                Name = $"Thanh_toan_test_{DateTime.Now.Ticks}",
                OrderDescription = $"Thanh toan test don hang. So tien: {amount}",
                OrderType = "other"
            };

            var url = _vnPayService.CreatePaymentUrl(model, HttpContext);

            return Redirect(url);
        }
    }
}
