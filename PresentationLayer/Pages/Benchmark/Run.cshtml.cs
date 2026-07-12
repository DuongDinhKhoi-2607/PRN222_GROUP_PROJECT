using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BussinessLayer.Interfaces;
using DataAccessLayer.Models;

namespace PresentationLayer.Pages.Benchmark
{
    [Authorize(Roles = "benchmarkmanager,admin")]
    public class RunModel : PageModel
    {
        private readonly IBenchmarkService _benchmarkService;

        public RunModel(IBenchmarkService benchmarkService)
        {
            _benchmarkService = benchmarkService;
        }

        [BindProperty(SupportsGet = true)]
        public long SubjectId { get; set; }
        
        public ExperimentRun? ExperimentRun { get; set; }
        public List<long> QuestionIds { get; set; } = new List<long>();

        public async Task<IActionResult> OnGetAsync()
        {
            var questions = await _benchmarkService.GetTestQuestionsAsync(SubjectId);
            QuestionIds = questions.Select(q => q.Id).ToList();
            if (QuestionIds.Count == 0)
            {
                TempData["Error"] = "Môn học này chưa có câu hỏi nào. Vui lòng import câu hỏi trước khi chạy Benchmark.";
                return RedirectToPage("Index");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string RunName)
        {
            var questions = await _benchmarkService.GetTestQuestionsAsync(SubjectId);
            QuestionIds = questions.Select(q => q.Id).ToList();
            
            ExperimentRun = await _benchmarkService.CreateExperimentRunAsync(RunName, SubjectId);
            
            return Page();
        }

        public async Task<IActionResult> OnPostEvaluateAsync(long runId, long questionId)
        {
            try
            {
                var result = await _benchmarkService.EvaluateSingleQuestionAsync(runId, questionId);
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostFinishAsync(long runId)
        {
            await _benchmarkService.UpdateRunMetricsAsync(runId);
            return new JsonResult(new { success = true });
        }
    }
}
