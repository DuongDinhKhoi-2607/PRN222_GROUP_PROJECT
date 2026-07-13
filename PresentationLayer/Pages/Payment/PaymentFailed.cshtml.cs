using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PresentationLayer.Pages.Payment
{
    [Authorize]
    public class PaymentFailedModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public string OrderId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string ErrorCode { get; set; }

        public void OnGet()
        {
        }
    }
}
