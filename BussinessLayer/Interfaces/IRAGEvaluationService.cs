using System.Threading.Tasks;
using BussinessLayer.DTOs;

namespace BussinessLayer.Interfaces
{
    public interface IRAGEvaluationService
    {
        Task<RAGEvaluationResultDto> EvaluateAsync(string question, string expectedAnswer, long? subjectId = null, long? strategyId = null);
    }
}
