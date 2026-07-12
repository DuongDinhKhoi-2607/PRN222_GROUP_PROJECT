using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BussinessLayer.Interfaces;
using DataAccessLayer.Models;

namespace PresentationLayer.Pages.Benchmark
{
    [Authorize(Roles = "benchmarkmanager,admin")]
    public class QuestionsModel : PageModel
    {
        private readonly IBenchmarkService _benchmarkService;
        private readonly ISubjectService _subjectService;

        public QuestionsModel(IBenchmarkService benchmarkService, ISubjectService subjectService)
        {
            _benchmarkService = benchmarkService;
            _subjectService = subjectService;
        }

        [BindProperty(SupportsGet = true)]
        public long SubjectId { get; set; }

        public BussinessLayer.DTOs.SubjectDto? Subject { get; set; }
        public IEnumerable<TestQuestion>? Questions { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            Subject = await _subjectService.GetByIdAsync(SubjectId);
            if (Subject == null) return NotFound();

            Questions = await _benchmarkService.GetTestQuestionsAsync(SubjectId);
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(long questionId)
        {
            var success = await _benchmarkService.DeleteTestQuestionAsync(questionId);
            if (success)
            {
                TempData["Message"] = "Đã xóa câu hỏi thành công.";
            }
            else
            {
                TempData["Error"] = "Lỗi khi xóa câu hỏi.";
            }
            return RedirectToPage(new { subjectId = SubjectId });
        }
    }
}
