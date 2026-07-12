using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BussinessLayer.Interfaces;
using DataAccessLayer.Models;

namespace PresentationLayer.Pages.Benchmark
{
    [Authorize(Roles = "benchmarkmanager,admin")]
    public class ResultModel : PageModel
    {
        private readonly IBenchmarkService _benchmarkService;

        public ResultModel(IBenchmarkService benchmarkService)
        {
            _benchmarkService = benchmarkService;
        }

        [BindProperty(SupportsGet = true)]
        public long RunId { get; set; }

        public ExperimentRun? ExperimentRun { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                ExperimentRun = await _benchmarkService.GetExperimentRunAsync(RunId);
            }
            catch
            {
                return RedirectToPage("Index");
            }
            return Page();
        }
    }
}
