using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BussinessLayer.Interfaces;
using BussinessLayer.DTOs;
using Microsoft.AspNetCore.SignalR;
using PresentationLayer.Hubs;

namespace PresentationLayer.Pages.Subject
{
    [Authorize(Roles = "admin")]
    public class CreateModel : PageModel
    {
        private readonly ISubjectService _svc;
        private readonly IHubContext<DocumentHub> _hubContext;

        public CreateModel(ISubjectService svc, IHubContext<DocumentHub> hubContext)
        {
            _svc = svc;
            _hubContext = hubContext;
        }

        [BindProperty]
        public SubjectDto Subject { get; set; } = new();

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            bool isUnique = await _svc.IsCodeUniqueAsync(Subject.Code);
            if (!isUnique)
            {
                ModelState.AddModelError("Subject.Code", "Mã môn học này đã tồn tại.");
                return Page();
            }

            await _svc.CreateAsync(Subject);
            await _hubContext.Clients.All.SendAsync("ReceiveSystemUpdate", "SubjectCreated", $"Môn học mới '{Subject.Code} - {Subject.Name}' đã được tạo.");
            
            TempData["SuccessMessage"] = "Tạo môn học thành công!";
            return RedirectToPage("Create");
        }
    }
}
