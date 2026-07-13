using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BussinessLayer.Interfaces;
using DataAccessLayer.Models;
using DataAccessLayer;

namespace PresentationLayer.Pages.Benchmark
{
    [Authorize(Roles = "benchmarkmanager,admin")]
    public class RunModel : PageModel
    {
        private readonly IBenchmarkService _benchmarkService;
        private readonly RagchatbotDbContext _db;

        public RunModel(IBenchmarkService benchmarkService, RagchatbotDbContext db)
        {
            _benchmarkService = benchmarkService;
            _db = db;
        }

        [BindProperty(SupportsGet = true)]
        public long SubjectId { get; set; }
        
        public ExperimentRun? ExperimentRun { get; set; }
        public List<long> QuestionIds { get; set; } = new List<long>();
        public List<ChunkingStrategy> Strategies { get; set; } = new List<ChunkingStrategy>();

        public async Task<IActionResult> OnGetAsync()
        {
            var questions = await _benchmarkService.GetTestQuestionsAsync(SubjectId);
            QuestionIds = questions.Select(q => q.Id).ToList();
            if (QuestionIds.Count == 0)
            {
                TempData["Error"] = "Môn học này chưa có câu hỏi nào. Vui lòng import câu hỏi trước khi chạy Benchmark.";
                return RedirectToPage("Index");
            }

            Strategies = await _db.ChunkingStrategies.ToListAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string RunName, long chunkingStrategyId)
        {
            var questions = await _benchmarkService.GetTestQuestionsAsync(SubjectId);
            QuestionIds = questions.Select(q => q.Id).ToList();
            Strategies = await _db.ChunkingStrategies.ToListAsync();
            
            ExperimentRun = await _benchmarkService.CreateExperimentRunAsync(RunName, SubjectId, chunkingStrategyId);
            
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
