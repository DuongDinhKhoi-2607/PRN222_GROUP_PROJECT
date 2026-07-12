using System.Threading.Tasks;
using DataAccessLayer.Models;
using Microsoft.AspNetCore.Http;

namespace BussinessLayer.Interfaces
{
    public interface IDocumentIngestionService
    {
        Task<Document> IngestAsync(IFormFile file, string title, long subjectId, long? chapterId = null, long? userId = null);
    }
}
