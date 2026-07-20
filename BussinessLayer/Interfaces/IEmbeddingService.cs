using System.Threading.Tasks;
using System.Collections.Generic;

namespace BussinessLayer.Interfaces
{
    public interface IEmbeddingService
    {
        Task<float[]> EmbedAsync(string text, int dimension = 256);
        Task<float[][]> EmbedBatchAsync(IEnumerable<string> texts, int dimension = 256);
    }
}
