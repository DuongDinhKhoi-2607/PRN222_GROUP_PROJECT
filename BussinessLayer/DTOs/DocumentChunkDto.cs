using System;

namespace BussinessLayer.DTOs
{
    public class DocumentChunkDto
    {
        public long Id { get; set; }
        public long DocumentId { get; set; }
        public int ChunkIndex { get; set; }
        public string Content { get; set; } = null!;
        public int TokenCount { get; set; }
        public int? PageNumber { get; set; }
        public string? Metadata { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
