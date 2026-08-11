using System.Text;
using UglyToad.PdfPig;

namespace Windrop.Infrastructure.Documents;

public sealed record PdfContentAnalysis(bool IsTextOnly, string? Text, int PageCount, int ImageCount);

public static class PdfContentAnalyzer
{
    public static PdfContentAnalysis Analyze(string path)
    {
        try
        {
            using var pdf = PdfDocument.Open(path);
            var builder = new StringBuilder();
            var imageCount = 0;
            foreach (var page in pdf.GetPages())
            {
                imageCount += page.NumberOfImages;
                if (builder.Length > 0) builder.AppendLine().AppendLine();
                builder.Append(page.Text);
            }

            var text = builder.ToString().Trim();
            var hasText = text.Length > 0;
            return new PdfContentAnalysis(hasText && imageCount == 0, hasText ? text : null,
                pdf.NumberOfPages, imageCount);
        }
        catch
        {
            return new PdfContentAnalysis(false, null, 0, 0);
        }
    }
}
