using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BussinessLayer.Interfaces;
using BussinessLayer.DTOs;
using Microsoft.AspNetCore.SignalR;
using PresentationLayer.Hubs;

namespace PresentationLayer.Pages.Document
{
    [Authorize(Roles = "lecturer,admin")]
    public class EditModel : PageModel
    {
        private readonly IDocumentService _docService;
        private readonly ISubjectService _subjectService;
        private readonly IPermissionService _permissionService;
        private readonly IHubContext<DocumentHub> _hubContext;

        public EditModel(
            IDocumentService docService,
            ISubjectService subjectService,
            IPermissionService permissionService,
            IHubContext<DocumentHub> hubContext)
        {
            _docService = docService;
            _subjectService = subjectService;
            _permissionService = permissionService;
            _hubContext = hubContext;
        }

        public DocumentDto Document { get; set; } = null!;
        public string? ReturnUrl { get; set; }
        public IEnumerable<SubjectDto> Subjects { get; set; } = new List<SubjectDto>();

        public async Task<IActionResult> OnGetAsync(long id, string? returnUrl = null)
        {
            var doc = await _docService.GetByIdAsync(id);
            if (doc == null) return NotFound();
            Document = doc;
            ReturnUrl = returnUrl;

            // Permission check for lecturer
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            if (roleClaim == "lecturer")
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var lecturerId))
                    return Unauthorized();

                var hasPermission = await _permissionService.HasUploadPermissionAsync(lecturerId, doc.SubjectId);
                if (!hasPermission)
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền chỉnh sửa tài liệu cho môn học này.";
                    return RedirectToPage("Index");
                }
            }

            Subjects = await _subjectService.GetAllAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(long id, string title, long subjectId, string? returnUrl)
        {
            var doc = await _docService.GetByIdAsync(id);
            if (doc == null) return NotFound();
            Document = doc;
            ReturnUrl = returnUrl;

            // Permission check for lecturer
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            if (roleClaim == "lecturer")
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var lecturerId))
                    return Unauthorized();

                var hasPermission = await _permissionService.HasUploadPermissionAsync(lecturerId, doc.SubjectId);
                if (!hasPermission)
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền chỉnh sửa tài liệu cho môn học này.";
                    return RedirectToPage("Index");
                }
            }

            var subject = await _subjectService.GetByIdAsync(subjectId);
            if (subject == null)
            {
                ModelState.AddModelError("", $"Môn học Id={subjectId} không tồn tại.");
                Subjects = await _subjectService.GetAllAsync();
                return Page();
            }

            await _docService.UpdateAsync(id, title, subjectId);

            var updatedDoc = await _docService.GetByIdWithSubjectAsync(id);
            if (updatedDoc != null)
            {
                await _hubContext.Clients.All.SendAsync("ReceiveUpdatedDocument", updatedDoc);
            }

            TempData["SuccessMessage"] = "Cập nhật tài liệu thành công!";
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);
            return RedirectToPage("Index");
        }
    }
}
