using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BussinessLayer.Interfaces;
using BussinessLayer.DTOs;

namespace PresentationLayer.Pages.Benchmark
{
    [Authorize(Roles = "benchmarkmanager,admin")]
    public class ImportModel : PageModel
    {
        private readonly IBenchmarkService _benchmarkService;

        public ImportModel(IBenchmarkService benchmarkService)
        {
            _benchmarkService = benchmarkService;
        }

        [BindProperty(SupportsGet = true)]
        public long SubjectId { get; set; }

        [BindProperty]
        public bool Overwrite { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync(string JsonContent)
        {
            if (string.IsNullOrWhiteSpace(JsonContent))
            {
                TempData["Error"] = "Nội dung JSON không được để trống.";
                return Page();
            }

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var questions = JsonSerializer.Deserialize<List<TestQuestionImportDto>>(JsonContent, options);

                if (questions == null || questions.Count == 0)
                {
                    TempData["Error"] = "Không tìm thấy câu hỏi nào hợp lệ trong chuỗi JSON.";
                    return Page();
                }

                int count = await _benchmarkService.ImportTestQuestionsAsync(SubjectId, questions, Overwrite);
                TempData["Message"] = $"Đã import thành công {count} câu hỏi!";
                return RedirectToPage("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi parse JSON hoặc lưu vào DB: " + ex.Message;
                return Page();
            }
        }
    }
}
