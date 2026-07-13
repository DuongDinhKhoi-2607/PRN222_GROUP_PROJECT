using System.Collections.Generic;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using DataAccessLayer.Models;

namespace BussinessLayer.Interfaces
{
    public interface IBenchmarkService
    {
        Task<int> ImportTestQuestionsAsync(long subjectId, IEnumerable<TestQuestionImportDto> questions, bool overwrite = false);
        Task<IEnumerable<TestQuestion>> GetTestQuestionsAsync(long subjectId);
        Task<bool> DeleteTestQuestionAsync(long questionId);
        
        Task<ExperimentRun> CreateExperimentRunAsync(string name, long subjectId, long chunkingStrategyId);
        Task<ExperimentRun> GetExperimentRunAsync(long experimentRunId);
        Task<IEnumerable<ExperimentRun>> GetExperimentRunsAsync();
        
        Task<EvaluationResult> EvaluateSingleQuestionAsync(long experimentRunId, long testQuestionId);
        Task UpdateRunMetricsAsync(long experimentRunId);
    }
}
