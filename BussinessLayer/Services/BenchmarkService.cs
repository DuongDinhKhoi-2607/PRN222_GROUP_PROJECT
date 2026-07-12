using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using BussinessLayer.Interfaces;
using DataAccessLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace BussinessLayer.Services
{
    public class BenchmarkService : IBenchmarkService
    {
        private readonly RagchatbotDbContext _db;
        private readonly IRAGEvaluationService _ragEval;

        public BenchmarkService(RagchatbotDbContext db, IRAGEvaluationService ragEval)
        {
            _db = db;
            _ragEval = ragEval;
        }

        public async Task<int> ImportTestQuestionsAsync(long subjectId, IEnumerable<TestQuestionImportDto> questions, bool overwrite = false)
        {
            if (overwrite)
            {
                var oldQuestions = await _db.TestQuestions.Where(q => q.SubjectId == subjectId).ToListAsync();
                if (oldQuestions.Any())
                {
                    var oldQuestionIds = oldQuestions.Select(q => q.Id).ToList();
                    var oldResults = await _db.EvaluationResults.Where(r => oldQuestionIds.Contains(r.TestQuestionId)).ToListAsync();
                    if (oldResults.Any())
                    {
                        _db.EvaluationResults.RemoveRange(oldResults);
                    }
                    _db.TestQuestions.RemoveRange(oldQuestions);
                    await _db.SaveChangesAsync();
                }
            }

            int count = 0;
            foreach (var q in questions)
            {
                var tq = new TestQuestion
                {
                    SubjectId = subjectId,
                    Question = q.Question,
                    GroundTruth = q.GroundTruth,
                    ReferenceContext = q.ReferenceContext,
                    Difficulty = q.Difficulty,
                    CreatedAt = DateTime.UtcNow
                };
                _db.TestQuestions.Add(tq);
                count++;
            }
            await _db.SaveChangesAsync();
            return count;
        }

        public async Task<IEnumerable<TestQuestion>> GetTestQuestionsAsync(long subjectId)
        {
            return await _db.TestQuestions
                .Where(t => t.SubjectId == subjectId)
                .OrderBy(t => t.Id)
                .ToListAsync();
        }

        public async Task<bool> DeleteTestQuestionAsync(long questionId)
        {
            var question = await _db.TestQuestions.FindAsync(questionId);
            if (question == null) return false;

            var results = await _db.EvaluationResults.Where(r => r.TestQuestionId == questionId).ToListAsync();
            if (results.Any())
            {
                _db.EvaluationResults.RemoveRange(results);
            }
            
            _db.TestQuestions.Remove(question);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<ExperimentRun> CreateExperimentRunAsync(string name, long subjectId)
        {
            var exp = new Experiment
            {
                Name = name,
                Type = "rag_vs_finetune", // Default type for this simple implementation
                Status = "running", // Valid values: draft, running, done
                CreatedAt = DateTime.UtcNow
            };
            _db.Experiments.Add(exp);
            await _db.SaveChangesAsync();

            var run = new ExperimentRun
            {
                ExperimentId = exp.Id,
                RunName = name,
                Status = "running",
                StartedAt = DateTime.UtcNow
            };
            _db.ExperimentRuns.Add(run);
            await _db.SaveChangesAsync();
            
            return run;
        }
        
        public async Task<ExperimentRun> GetExperimentRunAsync(long experimentRunId)
        {
            var run = await _db.ExperimentRuns
                .Include(r => r.Experiment)
                .Include(r => r.EvaluationResults)
                .ThenInclude(er => er.TestQuestion)
                .Include(r => r.ExperimentRunMetric)
                .FirstOrDefaultAsync(r => r.Id == experimentRunId);
            
            if (run == null) throw new Exception("ExperimentRun not found");
            return run;
        }

        public async Task<IEnumerable<ExperimentRun>> GetExperimentRunsAsync()
        {
            return await _db.ExperimentRuns
                .Include(r => r.Experiment)
                .OrderByDescending(r => r.Id)
                .ToListAsync();
        }

        public async Task<EvaluationResult> EvaluateSingleQuestionAsync(long experimentRunId, long testQuestionId)
        {
            var tq = await _db.TestQuestions.FindAsync(testQuestionId);
            if (tq == null) throw new Exception("Question not found");

            try 
            {
                var resultDto = await _ragEval.EvaluateAsync(tq.Question, tq.GroundTruth, tq.SubjectId);

                var evalResult = new EvaluationResult
                {
                    ExperimentRunId = experimentRunId,
                    TestQuestionId = testQuestionId,
                    GeneratedAnswer = resultDto.GeneratedAnswer,
                    RetrievedContexts = string.Join("\n", resultDto.RetrievedContexts),
                    Faithfulness = resultDto.FaithfulnessScore,
                    AnswerRelevancy = resultDto.AnswerRelevanceScore,
                    ContextPrecision = resultDto.ContextRelevanceScore,
                    ContextRecall = 0, // Ignored in simple eval
                    AnswerCorrectness = resultDto.AnswerRelevanceScore, // Treating as Answer Relevance
                    LatencyMs = (int)resultDto.LatencyMilliseconds
                };

                _db.EvaluationResults.Add(evalResult);
                await _db.SaveChangesAsync();

                return evalResult;
            }
            catch (Exception ex)
            {
                // Record the error as a failed evaluation result so it shows on the UI
                var errResult = new EvaluationResult
                {
                    ExperimentRunId = experimentRunId,
                    TestQuestionId = testQuestionId,
                    GeneratedAnswer = "LỖI: " + ex.Message,
                    RetrievedContexts = "",
                    Faithfulness = 0,
                    AnswerRelevancy = 0,
                    ContextPrecision = 0,
                    ContextRecall = 0,
                    AnswerCorrectness = 0,
                    LatencyMs = 0
                };
                
                _db.EvaluationResults.Add(errResult);
                await _db.SaveChangesAsync();

                return errResult;
            }
        }

        public async Task UpdateRunMetricsAsync(long experimentRunId)
        {
            var run = await _db.ExperimentRuns
                .Include(r => r.EvaluationResults)
                .FirstOrDefaultAsync(r => r.Id == experimentRunId);
                
            if (run == null) return;
            
            if (run.EvaluationResults.Any())
            {
                var results = run.EvaluationResults;
                var metric = await _db.ExperimentRunMetrics.FirstOrDefaultAsync(m => m.ExperimentRunId == experimentRunId);
                if (metric == null)
                {
                    metric = new ExperimentRunMetric { ExperimentRunId = experimentRunId };
                    _db.ExperimentRunMetrics.Add(metric);
                }

                metric.AvgFaithfulness = results.Average(r => r.Faithfulness);
                metric.AvgAnswerRelevancy = results.Average(r => r.AnswerRelevancy);
                metric.AvgContextPrecision = results.Average(r => r.ContextPrecision);
                metric.AvgContextRecall = results.Average(r => r.ContextRecall);
                metric.AvgAnswerCorrectness = results.Average(r => r.AnswerCorrectness);
                metric.AvgLatencyMs = results.Average(r => r.LatencyMs);
            }

            run.Status = "done";
            run.FinishedAt = DateTime.UtcNow;

            // Also mark the parent experiment as done
            var exp = await _db.Experiments.FirstOrDefaultAsync(e => e.Id == run.ExperimentId);
            if (exp != null) exp.Status = "done";

            await _db.SaveChangesAsync();
        }
    }
}
