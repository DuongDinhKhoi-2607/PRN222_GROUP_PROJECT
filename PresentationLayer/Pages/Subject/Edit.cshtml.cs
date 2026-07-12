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
    public class EditModel : PageModel
    {
        private readonly ISubjectService _svc;
        private readonly IHubContext<DocumentHub> _hubContext;

        public EditModel(ISubjectService svc, IHubContext<DocumentHub> hubContext)
        {
            _svc = svc;
            _hubContext = hubContext;
        }

        [BindProperty]
        public SubjectDto Subject { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(long id)
        {
            var subject = await _svc.GetByIdAsync(id);
            if (subject == null)
            {
                return NotFound();
            }
            Subject = subject;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(long id)
        {
            if (id != Subject.Id) return BadRequest();
            if (!ModelState.IsValid)
                return Page();

            bool isUnique = await _svc.IsCodeUniqueAsync(Subject.Code, id);
            if (!isUnique)
            {
                ModelState.AddModelError("Subject.Code", "Mã môn học này đã tồn tại.");
                return Page();
            }

            await _svc.UpdateAsync(Subject);
            await _hubContext.Clients.All.SendAsync("ReceiveSystemUpdate", "SubjectUpdated", $"Thông tin môn học '{Subject.Code} - {Subject.Name}' đã được cập nhật.");
            
            TempData["SuccessMessage"] = "Cập nhật môn học thành công!";
            return RedirectToPage("Edit", new { id = Subject.Id });
        }
    }
}
