using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DataAccessLayer.Models;
using BussinessLayer.Interfaces;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace PresentationLayer.Pages.Benchmark
{
    [Authorize(Roles = "benchmarkmanager,admin")]
    public class IndexModel : PageModel
    {
        private readonly RagchatbotDbContext _db;
        private readonly IBenchmarkService _benchmarkService;

        public IndexModel(RagchatbotDbContext db, IBenchmarkService benchmarkService)
        {
            _db = db;
            _benchmarkService = benchmarkService;
        }

        public List<DataAccessLayer.Models.Subject> Subjects { get; set; } = new List<DataAccessLayer.Models.Subject>();
        public List<ExperimentRun> Runs { get; set; } = new List<ExperimentRun>();

        public async Task OnGetAsync()
        {
            Subjects = await _db.Subjects
                .Include(s => s.TestQuestions)
                .OrderByDescending(s => s.Id)
                .ToListAsync();
            
            var runs = await _benchmarkService.GetExperimentRunsAsync();
            Runs = runs.ToList();
        }
    }
}
