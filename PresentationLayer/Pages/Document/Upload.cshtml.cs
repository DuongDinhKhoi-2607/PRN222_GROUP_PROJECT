using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BussinessLayer.Interfaces;
using BussinessLayer.DTOs;
using Microsoft.AspNetCore.SignalR;
using PresentationLayer.Hubs;

namespace PresentationLayer.Pages.Document
{
    [Authorize(Roles = "lecturer,admin")]
    public class UploadModel : PageModel
    {
        private readonly IDocumentIngestionService _ingest;
        private readonly ISubjectService _subjectService;
        private readonly IChapterService _chapterService;
        private readonly IDocumentService _docService;
        private readonly IPermissionService _permissionService;
        private readonly IHubContext<DocumentHub> _hubContext;

        public UploadModel(
            IDocumentIngestionService ingest,
            ISubjectService subjectService,
            IChapterService chapterService,
            IDocumentService docService,
            IPermissionService permissionService,
            IHubContext<DocumentHub> hubContext)
        {
            _ingest = ingest;
            _subjectService = subjectService;
            _chapterService = chapterService;
            _docService = docService;
            _permissionService = permissionService;
            _hubContext = hubContext;
        }

        public string? ReturnUrl { get; set; }
        public IEnumerable<SubjectDto> Subjects { get; set; } = new List<SubjectDto>();

        private async Task LoadSubjectsAsync()
        {
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (roleClaim == "lecturer")
            {
                if (userIdClaim != null && long.TryParse(userIdClaim.Value, out var lecturerId))
                {
                    var allowedIds = await _permissionService.GetAllowedSubjectIdsAsync(lecturerId);
                    var allowedList = allowedIds.ToList();
                    var allSubjects = await _subjectService.GetAllAsync();
                    Subjects = allSubjects.Where(s => allowedList.Contains(s.Id));
                }
            }
            else
            {
                Subjects = await _subjectService.GetAllAsync();
            }
        }

        public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (roleClaim == "lecturer")
            {
                if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var lecturerId))
                    return Unauthorized();

                var allowedIds = await _permissionService.GetAllowedSubjectIdsAsync(lecturerId);
                if (!allowedIds.Any())
                {
                    TempData["ErrorMessage"] = "Bạn chưa được cấp quyền upload cho môn học nào. Vui lòng liên hệ Admin.";
                    return RedirectToPage("Index");
                }
            }

            await LoadSubjectsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(
            IFormFile file, 
            string title, 
            long subjectId, 
            long? chapterId, 
            string? returnUrl,
            string? duplicateAction = null,
            long? duplicateId = null)
        {
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            // Permission check for lecturer
            if (roleClaim == "lecturer")
            {
                if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var lecturerId))
                    return Unauthorized();

                var hasPermission = await _permissionService.HasUploadPermissionAsync(lecturerId, subjectId);
                if (!hasPermission)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return new JsonResult(new { success = false, message = "Bạn không có quyền upload tài liệu cho môn học này." });
                    TempData["ErrorMessage"] = "Bạn không có quyền upload tài liệu cho môn học này.";
                    return RedirectToPage("Index");
                }
            }

            await LoadSubjectsAsync();

            if (file == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return new JsonResult(new { success = false, message = "Vui lòng chọn tệp để upload." });
                ModelState.AddModelError("", "Vui lòng chọn tệp để upload.");
                return Page();
            }

            var subject = await _subjectService.GetByIdAsync(subjectId);
            if (subject == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return new JsonResult(new { success = false, message = $"Môn học Id={subjectId} không tồn tại." });
                ModelState.AddModelError("", $"Môn học Id={subjectId} không tồn tại.");
                return Page();
            }

            if (chapterId.HasValue)
            {
                var chapterExists = await _chapterService.ExistsAsync(chapterId.Value);
                if (!chapterExists)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return new JsonResult(new { success = false, message = $"Chapter Id={chapterId.Value} không tồn tại." });
                    ModelState.AddModelError("", $"Chapter Id={chapterId.Value} không tồn tại.");
                    return Page();
                }
            }

            // If duplicateAction is replace, delete the old file first
            if (duplicateAction == "replace" && duplicateId.HasValue)
            {
                try
                {
                    await _docService.DeleteAsync(duplicateId.Value);
                }
                catch (Exception ex)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return new JsonResult(new { success = false, message = "Không thể xóa tài liệu cũ: " + ex.Message });
                    ModelState.AddModelError("", "Không thể xóa tài liệu cũ: " + ex.Message);
                    return Page();
                }
            }

            // Only run similarity/duplicate check if we are not skipping it (i.e. keepBoth is not chosen, and replace is not already completed)
            if (duplicateAction != "keepBoth" && duplicateAction != "replace")
            {
                // 1. Check for duplicate document name or title in this subject
                var existingDocs = await _docService.GetBySubjectIdAsync(subjectId);
                if (existingDocs.Any(d => d.Title.Equals(title, StringComparison.OrdinalIgnoreCase) || d.FileName.Equals(file.FileName, StringComparison.OrdinalIgnoreCase)))
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return new JsonResult(new { success = false, message = "Đã có tài liệu trùng tên file hoặc tiêu đề trong môn học này." });
                    ModelState.AddModelError("", "Đã có tài liệu trùng tên file hoặc tiêu đề trong môn học này.");
                    return Page();
                }

                // 2. Check for content similarity (>= 60%)
                var (similarDoc, similarity) = await _docService.CheckSimilarityAsync(file);
                if (similarity >= 0.6 && similarDoc != null)
                {
                    var simPercent = Math.Round(similarity * 100, 1);
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return new JsonResult(new
                        {
                            isSimilar = true,
                            similarity = simPercent,
                            duplicateId = similarDoc.Id,
                            duplicateTitle = similarDoc.Title,
                            duplicateSubject = $"{similarDoc.SubjectCode} - {similarDoc.SubjectName}"
                        });
                    }

                    ModelState.AddModelError("", $"Tài liệu này giống {simPercent}% với tài liệu '{similarDoc.Title}' trong môn '{similarDoc.SubjectCode}'.");
                    return Page();
                }
            }

            long? userId = null;
            if (userIdClaim != null && long.TryParse(userIdClaim.Value, out var parsedId))
                userId = parsedId;

            try
            {
                var doc = await _ingest.IngestAsync(file, title, subjectId, chapterId, userId);

                // Fetch details with subject/user name populated for the client UI
                var docDto = await _docService.GetByIdWithSubjectAsync(doc.Id);
                if (docDto != null)
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveNewDocument", docDto);
                }

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return new JsonResult(new { success = true, message = "Tài liệu đã được upload và xử lý thành công!" });
                }

                TempData["SuccessMessage"] = "Tài liệu đã được upload và xử lý thành công!";
                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return LocalRedirect(returnUrl);
                return RedirectToPage("Index");
            }
            catch (Exception ex)
            {
                var errMsg = ex.InnerException?.Message ?? ex.Message;
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return new JsonResult(new { success = false, message = "Lỗi: " + errMsg });
                }

                ModelState.AddModelError("", "Lỗi: " + errMsg);
                return Page();
            }
        }
    }
}
