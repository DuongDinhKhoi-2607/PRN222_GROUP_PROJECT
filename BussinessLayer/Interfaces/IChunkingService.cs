using System.Collections.Generic;

namespace BussinessLayer.Interfaces
{
    public class ChunkDto
    {
        public int Index { get; set; }
        public string Text { get; set; } = string.Empty;
        public int TokenCount { get; set; }
    }

    public interface IChunkingService
    {
        IEnumerable<ChunkDto> Chunk(string text, long strategyId, int? maxChars = null);
    }
}
