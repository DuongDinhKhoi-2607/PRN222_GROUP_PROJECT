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
    public class DeleteModel : PageModel
    {
        private readonly ISubjectService _svc;
        private readonly IHubContext<DocumentHub> _hubContext;

        public DeleteModel(ISubjectService svc, IHubContext<DocumentHub> hubContext)
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
            try
            {
                var subject = await _svc.GetByIdAsync(id);
                var subjectCode = subject?.Code ?? id.ToString();
                var subjectName = subject?.Name ?? "";

                await _svc.DeleteAsync(id);
                await _hubContext.Clients.All.SendAsync("ReceiveSystemUpdate", "SubjectDeleted", $"Môn học '{subjectCode} - {subjectName}' đã được xóa bởi quản trị viên.");
                
                TempData["SuccessMessage"] = "Xóa môn học thành công!";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Không thể xóa môn học này vì đã có dữ liệu (tài liệu, câu hỏi, phiên chat...) liên kết với nó.";
            }
            return RedirectToPage("Index");
        }
    }
}
