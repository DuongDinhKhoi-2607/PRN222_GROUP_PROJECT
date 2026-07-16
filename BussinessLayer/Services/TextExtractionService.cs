using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig;
using System.Text;
using BussinessLayer.Interfaces;

namespace BussinessLayer.Services
{
    public class TextExtractionService : ITextExtractionService
    {
        public Task<string> ExtractTextAsync(string filePath)
        {
            var ext = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
            if (ext == ".pdf") return Task.FromResult(ExtractTextFromPdf(filePath));
            if (ext == ".docx") return Task.FromResult(ExtractTextFromDocx(filePath));
            if (ext == ".pptx") return Task.FromResult(ExtractTextFromPptx(filePath));
            return Task.FromResult(System.IO.File.ReadAllText(filePath));
        }

        private static string ExtractTextFromPdf(string path)
        {
            using var doc = UglyToad.PdfPig.PdfDocument.Open(path);
            var sb = new StringBuilder();
            foreach (var page in doc.GetPages()) sb.AppendLine(page.Text);
            return sb.ToString();
        }

        private static string ExtractTextFromDocx(string path)
        {
            using var doc = WordprocessingDocument.Open(path, false);
            var body = doc.MainDocumentPart?.Document?.Body;
            return body?.InnerText ?? string.Empty;
        }

        private static string ExtractTextFromPptx(string path)
        {
            using var doc = PresentationDocument.Open(path, false);
            var sb = new StringBuilder();
            var presentationPart = doc.PresentationPart;
            if (presentationPart?.SlideParts != null)
            {
                foreach (var slidePart in presentationPart.SlideParts)
                {
                    if (slidePart.Slide != null)
                    {
                        foreach (var text in slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>())
                        {
                            sb.AppendLine(text.Text);
                        }
                    }
                }
            }
            return sb.ToString();
        }
    }
}
