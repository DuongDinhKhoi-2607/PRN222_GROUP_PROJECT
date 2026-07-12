using System.Collections.Generic;

namespace BussinessLayer.DTOs
{
    public class RAGEvaluationResultDto
    {
        public string Question { get; set; } = string.Empty;
        public string ExpectedAnswer { get; set; } = string.Empty;
        public string GeneratedAnswer { get; set; } = string.Empty;
        public double LatencyMilliseconds { get; set; }
        
        // Scores 1-5
        public int FaithfulnessScore { get; set; }
        public int AnswerRelevanceScore { get; set; }
        public int ContextRelevanceScore { get; set; }
        
        // Feedback/Reasoning from LLM
        public string Reasoning { get; set; } = string.Empty;

        // Contexts retrieved for reference
        public List<string> RetrievedContexts { get; set; } = new List<string>();
    }
}
