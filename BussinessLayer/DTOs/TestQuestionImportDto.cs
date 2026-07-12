namespace BussinessLayer.DTOs
{
    public class TestQuestionImportDto
    {
        public string Question { get; set; } = string.Empty;
        public string GroundTruth { get; set; } = string.Empty;
        public string? ReferenceContext { get; set; }
        public string? Difficulty { get; set; }
    }
}
