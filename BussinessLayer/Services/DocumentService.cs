using System;
using System.IO;
using DataAccessLayer.Repositories;
using DataAccessLayer.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using BussinessLayer.Interfaces;
using BussinessLayer.DTOs;

namespace BussinessLayer.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly DocumentRepository _repo;
        private readonly DocumentChunkRepository _chunkRepo;
        private readonly IFileStorageService _storage;
        private readonly ITextExtractionService _extractor;

        public DocumentService(DocumentRepository repo, DocumentChunkRepository chunkRepo, IFileStorageService storage, ITextExtractionService extractor)
        {
            _repo = repo;
            _chunkRepo = chunkRepo;
            _storage = storage;
            _extractor = extractor;
        }

        private static DocumentDto MapToDto(Document d) => new DocumentDto
        {
            Id = d.Id,
            SubjectId = d.SubjectId,
            ChapterId = d.ChapterId,
            Title = d.Title,
            FileName = d.FileName,
            FileType = d.FileType,
            FileSize = d.FileSize,
            Status = d.Status,
            UploadedAt = d.UploadedAt,
            IndexedAt = d.IndexedAt,
            UserId = d.UserId,
            ContentHash = d.ContentHash,
            SubjectName = d.Subject?.Name,
            SubjectCode = d.Subject?.Code,
            UploadedByName = d.UploadedByNavigation?.FullName
        };

        public async Task<IEnumerable<DocumentDto>> GetAllAsync()
        {
            var list = await _repo.GetAllAsync();
            return list.Select(MapToDto);
        }

        public async Task<IEnumerable<DocumentDto>> GetBySubjectIdAsync(long subjectId)
        {
            var list = await _repo.GetBySubjectIdAsync(subjectId);
            return list.Select(MapToDto);
        }

        public async Task<DocumentDto?> GetByIdAsync(long id)
        {
            var d = await _repo.GetByIdAsync(id);
            return d == null ? null : MapToDto(d);
        }

        public async Task<DocumentDto?> GetByIdWithSubjectAsync(long id)
        {
            var d = await _repo.GetByIdWithSubjectAsync(id);
            return d == null ? null : MapToDto(d);
        }

        public async Task<DocumentDto?> GetByContentHashAsync(string contentHash)
        {
            var d = await _repo.GetByContentHashAsync(contentHash);
            return d == null ? null : MapToDto(d);
        }

        public async Task UpdateAsync(long id, string title, long subjectId)
        {
            var doc = await _repo.GetByIdAsync(id);
            if (doc != null)
            {
                doc.Title = title;
                doc.SubjectId = subjectId;
                await _repo.UpdateAsync(doc);
            }
        }

        public async Task DeleteAsync(long id)
        {
            var doc = await _repo.GetByIdAsync(id);
            if (doc != null)
            {
                if (!string.IsNullOrEmpty(doc.FilePath))
                {
                    try
                    {
                        if (System.IO.File.Exists(doc.FilePath))
                            System.IO.File.Delete(doc.FilePath);
                    }
                    catch { /* ignore */ }
                }
                await _repo.DeleteAsync(id);
            }
        }

        public async Task<IEnumerable<DocumentDto>> GetByUserIdAsync(long userId)
        {
            var list = await _repo.GetAllAsync();
            return list.Where(d => d.UserId == userId).Select(MapToDto);
        }

        public async Task<IEnumerable<DocumentChunkDto>> GetChunksByDocumentIdAsync(long documentId)
        {
            var chunks = await _chunkRepo.GetByDocumentIdAsync(documentId);
            return chunks.Select(c => new DocumentChunkDto
            {
                Id = c.Id,
                DocumentId = c.DocumentId,
                ChunkIndex = c.ChunkIndex,
                Content = c.Content,
                TokenCount = c.TokenCount,
                PageNumber = c.PageNumber,
                Metadata = c.Metadata,
                CreatedAt = c.CreatedAt
            });
        }

        public async Task<(DocumentDto? SimilarDoc, double Similarity)> CheckSimilarityAsync(Microsoft.AspNetCore.Http.IFormFile file)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));

            var tempFolder = Path.Combine(Path.GetTempPath(), "rag_uploads");
            Directory.CreateDirectory(tempFolder);
            var tempFilePath = Path.Combine(tempFolder, Guid.NewGuid().ToString() + Path.GetExtension(file.FileName));

            try
            {
                using (var stream = File.Create(tempFilePath))
                {
                    await file.CopyToAsync(stream);
                }

                var text = await _extractor.ExtractTextAsync(tempFilePath);
                if (string.IsNullOrWhiteSpace(text))
                {
                    return (null, 0.0);
                }

                var chunks = await _chunkRepo.GetAllAsync();
                var docChunksGroup = chunks.GroupBy(c => c.DocumentId);

                DocumentDto? highestSimilarDoc = null;
                double highestSimilarity = 0.0;

                foreach (var group in docChunksGroup)
                {
                    var existingDocText = string.Join(" ", group.OrderBy(c => c.ChunkIndex).Select(c => c.Content));
                    var sim = TextSimilarityHelper.GetCosineSimilarity(text, existingDocText);
                    if (sim > highestSimilarity)
                    {
                        highestSimilarity = sim;
                        var doc = await _repo.GetByIdWithSubjectAsync(group.Key);
                        if (doc != null)
                        {
                            highestSimilarDoc = MapToDto(doc);
                        }
                    }
                }

                return (highestSimilarDoc, highestSimilarity);
            }
            finally
            {
                if (File.Exists(tempFilePath))
                {
                    try { File.Delete(tempFilePath); } catch { }
                }
            }
        }
    }
}
