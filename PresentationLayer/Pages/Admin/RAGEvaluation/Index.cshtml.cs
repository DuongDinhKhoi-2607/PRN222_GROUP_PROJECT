using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BussinessLayer.Interfaces;
using BussinessLayer.DTOs;

namespace PresentationLayer.Pages.Admin.RAGEvaluation
{
    [Authorize(Roles = "admin")]
    public class IndexModel : PageModel
    {
        private readonly IRAGEvaluationService _evaluationService;

        public IndexModel(IRAGEvaluationService evaluationService)
        {
            _evaluationService = evaluationService;
        }

        public RAGEvaluationResultDto? Result { get; set; }
        public bool IsPostBack { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync(string Question, string ExpectedAnswer, long? SubjectId)
        {
            IsPostBack = true;

            if (string.IsNullOrWhiteSpace(Question))
            {
                ModelState.AddModelError("", "Vui lòng nhập câu hỏi.");
                return Page();
            }

            Result = await _evaluationService.EvaluateAsync(Question, ExpectedAnswer ?? "", SubjectId);
            
            return Page();
        }
    }
}
