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
using System.ComponentModel.DataAnnotations;

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

        [BindProperty]
        public string Title { get; set; }

        [BindProperty]
        public long SubjectId { get; set; }

        [BindProperty]
        public long? ChapterId { get; set; }

        [BindProperty]
        public long StrategyId { get; set; } = 1;

        [BindProperty]
        public int? MaxChars { get; set; } = 1000;

        [BindProperty]
        public string? DuplicateAction { get; set; }

        [BindProperty]
        public long? DuplicateId { get; set; }

        public bool IsDuplicateDetected { get; set; }
        public double DuplicateSimilarity { get; set; }
        public string DuplicateTitle { get; set; }
        public string DuplicateSubject { get; set; }

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

        public async Task<IActionResult> OnPostAsync(IFormFile file, string? returnUrl)
        {
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            // Permission check for lecturer
            if (roleClaim == "lecturer")
            {
                if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var lecturerId))
                    return Unauthorized();

                var hasPermission = await _permissionService.HasUploadPermissionAsync(lecturerId, SubjectId);
                if (!hasPermission)
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền upload tài liệu cho môn học này.";
                    return RedirectToPage("Index");
                }
            }

            await LoadSubjectsAsync();

            if (file == null)
            {
                ModelState.AddModelError("", "Vui lòng chọn tệp để upload.");
                return Page();
            }

            var subject = await _subjectService.GetByIdAsync(SubjectId);
            if (subject == null)
            {
                ModelState.AddModelError("", $"Môn học không tồn tại.");
                return Page();
            }

            if (ChapterId.HasValue)
            {
                var chapterExists = await _chapterService.ExistsAsync(ChapterId.Value);
                if (!chapterExists)
                {
                    ModelState.AddModelError("", $"Chapter không tồn tại.");
                    return Page();
                }
            }

            // If duplicateAction is replace, delete the old file first
            if (DuplicateAction == "replace" && DuplicateId.HasValue)
            {
                try
                {
                    await _docService.DeleteAsync(DuplicateId.Value);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Không thể xóa tài liệu cũ: " + ex.Message);
                    return Page();
                }
            }

            // Only run similarity/duplicate check if we are not skipping it (i.e. keepBoth is not chosen, and replace is not already completed)
            if (DuplicateAction != "keepBoth" && DuplicateAction != "replace")
            {
                // 1. Check for duplicate document name or title in this subject
                var existingDocs = await _docService.GetBySubjectIdAsync(SubjectId);
                if (existingDocs.Any(d => d.Title.Equals(Title, StringComparison.OrdinalIgnoreCase) || d.FileName.Equals(file.FileName, StringComparison.OrdinalIgnoreCase)))
                {
                    ModelState.AddModelError("", "Đã có tài liệu trùng tên file hoặc tiêu đề trong môn học này.");
                    return Page();
                }

                // 2. Check for content similarity (>= 60%)
                var (similarDoc, similarity) = await _docService.CheckSimilarityAsync(file);
                if (similarity >= 0.6 && similarDoc != null)
                {
                    var simPercent = Math.Round(similarity * 100, 1);
                    
                    IsDuplicateDetected = true;
                    DuplicateSimilarity = simPercent;
                    DuplicateId = similarDoc.Id;
                    DuplicateTitle = similarDoc.Title;
                    DuplicateSubject = $"{similarDoc.SubjectCode} - {similarDoc.SubjectName}";
                    
                    ModelState.AddModelError("", $"Phát hiện trùng lặp! Tài liệu này giống {simPercent}% với tài liệu '{similarDoc.Title}' trong môn '{similarDoc.SubjectCode}'. Vui lòng chọn hành động bên dưới và chọn lại file để tiếp tục.");
                    return Page();
                }
            }

            long? userId = null;
            if (userIdClaim != null && long.TryParse(userIdClaim.Value, out var parsedId))
                userId = parsedId;

            try
            {
                var doc = await _ingest.IngestAsync(file, Title, SubjectId, ChapterId, userId, StrategyId, MaxChars);

                // Fetch details with subject/user name populated for the client UI
                var docDto = await _docService.GetByIdWithSubjectAsync(doc.Id);
                if (docDto != null)
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveNewDocument", docDto);
                }

                TempData["SuccessMessage"] = "Tài liệu đã được upload và xử lý thành công!";
                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return LocalRedirect(returnUrl);
                return RedirectToPage("Index");
            }
            catch (Exception ex)
            {
                var errMsg = ex.InnerException?.Message ?? ex.Message;
                ModelState.AddModelError("", "Lỗi: " + errMsg);
                return Page();
            }
        }
    }
}
