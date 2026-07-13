using System.Threading.Tasks;
using DataAccessLayer.Models;
using Microsoft.AspNetCore.Http;

namespace BussinessLayer.Interfaces
{
    public interface IDocumentIngestionService
    {
        Task<Document> IngestAsync(IFormFile file, string title, long subjectId, long? chapterId = null, long? userId = null, long strategyId = 1, int? maxChars = null);
        Task RechunkAsync(long documentId, long strategyId, int? maxChars = null);
        Task<IEnumerable<ChunkDto>> PreviewChunksAsync(long documentId, long strategyId, int? maxChars = null);
    }
}
