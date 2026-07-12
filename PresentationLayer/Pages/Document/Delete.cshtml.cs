using System;
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
    public class DeleteModel : PageModel
    {
        private readonly IDocumentService _docService;
        private readonly IPermissionService _permissionService;
        private readonly IHubContext<DocumentHub> _hubContext;

        public DeleteModel(
            IDocumentService docService,
            IPermissionService permissionService,
            IHubContext<DocumentHub> hubContext)
        {
            _docService = docService;
            _permissionService = permissionService;
            _hubContext = hubContext;
        }

        public DocumentDto Document { get; set; } = null!;
        public string? ReturnUrl { get; set; }

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
                    TempData["ErrorMessage"] = "Bạn không có quyền xóa tài liệu cho môn học này.";
                    return RedirectToPage("Index");
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(long id, string? returnUrl)
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
                    TempData["ErrorMessage"] = "Bạn không có quyền xóa tài liệu cho môn học này.";
                    return RedirectToPage("Index");
                }
            }

            await _docService.DeleteAsync(id);
            await _hubContext.Clients.All.SendAsync("ReceiveDeletedDocument", id);
            TempData["SuccessMessage"] = "Xóa tài liệu thành công!";
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);
            return RedirectToPage("Index");
        }
    }
}
