using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PresentationLayer.Pages
{
    public class IndexModel : PageModel
    {
        public IActionResult OnGet()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("student"))
                {
                    return RedirectToPage("/Chat/Index");
                }
                return RedirectToPage("/Subject/Index");
            }
            return RedirectToPage("/Auth/Login");
        }
    }
}
