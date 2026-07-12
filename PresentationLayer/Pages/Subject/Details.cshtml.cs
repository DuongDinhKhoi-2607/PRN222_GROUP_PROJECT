using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BussinessLayer.Interfaces;
using BussinessLayer.DTOs;

namespace PresentationLayer.Pages.Subject
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly ISubjectService _subjectService;

        public DetailsModel(ISubjectService subjectService)
        {
            _subjectService = subjectService;
        }

        public SubjectDto Subject { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(long id)
        {
            var subject = await _subjectService.GetByIdAsync(id);
            if (subject == null)
            {
                return NotFound();
            }
            Subject = subject;
            return Page();
        }
    }
}
