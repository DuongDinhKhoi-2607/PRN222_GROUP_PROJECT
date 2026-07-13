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
        private readonly IChunkingService _chunker;
        private readonly IEmbeddingService _embedder;
        private readonly ITextExtractionService _extractor;

        public BenchmarkService(
            RagchatbotDbContext db, 
            IRAGEvaluationService ragEval,
            IChunkingService chunker,
            IEmbeddingService embedder,
            ITextExtractionService extractor)
        {
            _db = db;
            _ragEval = ragEval;
            _chunker = chunker;
            _embedder = embedder;
            _extractor = extractor;
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

        public async Task<ExperimentRun> CreateExperimentRunAsync(string name, long subjectId, long chunkingStrategyId)
        {
            // Auto-index/re-chunk the corpus for this subject and strategy before starting the run
            await EnsureCorpusIndexedAsync(subjectId, chunkingStrategyId);

            var exp = new Experiment
            {
                Name = name,
                Type = chunkingStrategyId == 1 ? "rag_vs_finetune" : "chunking_bench",
                Status = "running",
                CreatedAt = DateTime.UtcNow
            };
            _db.Experiments.Add(exp);
            await _db.SaveChangesAsync();

            var run = new ExperimentRun
            {
                ExperimentId = exp.Id,
                RunName = name,
                Status = "running",
                StartedAt = DateTime.UtcNow,
                ChunkingStrategyId = chunkingStrategyId,
                EmbeddingModelId = 1 // Default model text-embedding-3-small
            };
            _db.ExperimentRuns.Add(run);
            await _db.SaveChangesAsync();
            
            return run;
        }

        private async Task EnsureCorpusIndexedAsync(long subjectId, long strategyId)
        {
            var docs = await _db.Documents
                .Where(d => d.SubjectId == subjectId && d.Status == "indexed")
                .ToListAsync();

            foreach (var doc in docs)
            {
                // Check if this document has chunks for this strategy
                var chunksCount = await _db.DocumentChunks
                    .CountAsync(c => c.DocumentId == doc.Id && c.ChunkingStrategyId == strategyId);

                if (chunksCount == 0)
                {
                    // Dynamically index the document
                    if (!File.Exists(doc.FilePath)) continue;

                    // Extract text
                    var text = await _extractor.ExtractTextAsync(doc.FilePath);

                    // Re-chunk document using selected strategy
                    var chunks = _chunker.Chunk(text, strategyId).ToList();
                    if (chunks.Count == 0) continue;

                    // Clear old chunks if any exist for this strategy (should be 0 anyway)
                    var oldChunks = await _db.DocumentChunks
                        .Where(c => c.DocumentId == doc.Id && c.ChunkingStrategyId == strategyId)
                        .ToListAsync();
                    if (oldChunks.Any())
                    {
                        var oldChunkIds = oldChunks.Select(c => c.Id).ToList();
                        var oldEmbeddings = await _db.ChunkEmbeddings
                            .Where(e => oldChunkIds.Contains(e.ChunkId))
                            .ToListAsync();
                        _db.ChunkEmbeddings.RemoveRange(oldEmbeddings);
                        _db.DocumentChunks.RemoveRange(oldChunks);
                        await _db.SaveChangesAsync();
                    }

                    // Save new chunks and generate embeddings
                    foreach (var dto in chunks)
                    {
                        var chunk = new DocumentChunk
                        {
                            DocumentId = doc.Id,
                            ChunkIndex = dto.Index,
                            Content = dto.Text,
                            TokenCount = dto.TokenCount,
                            PageNumber = 0,
                            CreatedAt = DateTime.UtcNow,
                            ChunkingStrategyId = strategyId
                        };
                        _db.DocumentChunks.Add(chunk);
                        await _db.SaveChangesAsync();

                        // Get vector embeddings
                        var vector = await _embedder.EmbedAsync(dto.Text);
                        var embedding = new ChunkEmbedding
                        {
                            ChunkId = chunk.Id,
                            EmbeddingModelId = 1, // Default embedding model
                            Vector = string.Join(',', vector.Select(v => v.ToString(System.Globalization.CultureInfo.InvariantCulture))),
                            Dimension = vector.Length,
                            CreatedAt = DateTime.UtcNow
                        };
                        _db.ChunkEmbeddings.Add(embedding);
                    }
                    await _db.SaveChangesAsync();
                }
            }
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

            var run = await _db.ExperimentRuns.FindAsync(experimentRunId);
            long? strategyId = run?.ChunkingStrategyId;

            try 
            {
                var resultDto = await _ragEval.EvaluateAsync(tq.Question, tq.GroundTruth, tq.SubjectId, strategyId);

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
