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
    public class IndexModel : PageModel
    {
        private readonly IDocumentService _docService;
        private readonly ISubjectService _subjectService;
        private readonly IPermissionService _permissionService;

        public IndexModel(
            IDocumentService docService,
            ISubjectService subjectService,
            IPermissionService permissionService)
        {
            _docService = docService;
            _subjectService = subjectService;
            _permissionService = permissionService;
        }

        [BindProperty(SupportsGet = true)]
        public long? SubjectId { get; set; }

        public IEnumerable<DocumentDto> Documents { get; set; } = new List<DocumentDto>();
        public IEnumerable<SubjectDto> Subjects { get; set; } = new List<SubjectDto>();
        public List<long> AllowedSubjectIds { get; set; } = new();
        public bool IsAdmin { get; set; }
        public string CurrentUrl { get; set; } = "";

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized();

            IsAdmin = roleClaim == "admin";
            Subjects = await _subjectService.GetAllAsync();

            var docs = await _docService.GetAllAsync();

            if (SubjectId.HasValue)
            {
                docs = docs.Where(d => d.SubjectId == SubjectId.Value);
            }
            Documents = docs;

            if (IsAdmin)
            {
                AllowedSubjectIds = Subjects.Select(s => s.Id).ToList();
            }
            else
            {
                var allowed = await _permissionService.GetAllowedSubjectIdsAsync(userId);
                AllowedSubjectIds = allowed.ToList();
            }

            CurrentUrl = Request.Path + Request.QueryString;
            return Page();
        }
    }
}
