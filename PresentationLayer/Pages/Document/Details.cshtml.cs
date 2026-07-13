using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BussinessLayer.Interfaces;
using BussinessLayer.DTOs;

namespace PresentationLayer.Pages.Document
{
    [Authorize(Roles = "lecturer,admin")]
    public class DetailsModel : PageModel
    {
        private readonly IDocumentService _docService;
        private readonly IPermissionService _permissionService;
        private readonly IDocumentIngestionService _ingestService;

        public DetailsModel(IDocumentService docService, IPermissionService permissionService, IDocumentIngestionService ingestService)
        {
            _docService = docService;
            _permissionService = permissionService;
            _ingestService = ingestService;
        }

        public DocumentDto Document { get; set; } = null!;
        public string? ReturnUrl { get; set; }
        public bool IsAdmin { get; set; }
        public List<long> AllowedSubjectIds { get; set; } = new();
        public List<DocumentChunkDto> Chunks { get; set; } = new();
        public int ChunksCount { get; set; }

        public async Task<IActionResult> OnGetAsync(long id, string? returnUrl = null)
        {
            var doc = await _docService.GetByIdWithSubjectAsync(id);
            if (doc == null) return NotFound();
            Document = doc;
            ReturnUrl = returnUrl;

            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userId = userIdClaim != null ? long.Parse(userIdClaim.Value) : 0;

            IsAdmin = roleClaim == "admin";
            if (!IsAdmin)
            {
                var allowed = await _permissionService.GetAllowedSubjectIdsAsync(userId);
                AllowedSubjectIds = allowed.ToList();
            }

            var chunks = await _docService.GetChunksByDocumentIdAsync(id);
            Chunks = chunks.OrderBy(c => c.ChunkIndex).ToList();
            ChunksCount = Chunks.Count;

            return Page();
        }
        public async Task<IActionResult> OnGetPreviewChunks(long id, long strategyId, int? maxChars)
        {
            try
            {
                var chunks = await _ingestService.PreviewChunksAsync(id, strategyId, maxChars);
                return new JsonResult(new { success = true, chunks = chunks.Select(c => new { content = c.Text }) });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostRechunk(long id, long strategyId, int? maxChars)
        {
            try
            {
                // Cho phép tất cả giảng viên Re-chunk, không cần check môn học
                await _ingestService.RechunkAsync(id, strategyId, maxChars);
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }
    }
}
