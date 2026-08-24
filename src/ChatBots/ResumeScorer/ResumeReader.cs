using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.IO;
using System.Text;
using UglyToad.PdfPig;

namespace ChatBots.ResumeScorer;

static class ResumeReader
{
    static string readPdf(string filePath)
    {
        var builder = new StringBuilder();
        using var document = PdfDocument.Open(filePath);
        foreach (var page in document.GetPages())
        {
            foreach (var word in page.GetWords())
                builder.Append(word.Text).Append(' ');

            builder.AppendLine();
        }
        return builder.ToString();
    }

    static string readDocx(string filePath)
    {
        var builder = new StringBuilder();
        using var document = WordprocessingDocument.Open(filePath, false);
        var mainPart = document.MainDocumentPart;
        if (mainPart is null) return string.Empty;
        var body = mainPart.Document!.Body;
        if (body is null) return string.Empty;

        foreach (var paragraph in body.Descendants<Paragraph>())
            builder.AppendLine(paragraph.InnerText);

        return builder.ToString();
    }

    public static string Read(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        switch (extension)
        {
            case ".pdf":
                return readPdf(filePath);
            case ".docx":
                return readDocx(filePath);
            case ".txt":
                return File.ReadAllText(filePath, Encoding.UTF8);
            default:
                throw new NotSupportedException($"Unsupported file type: {extension}");
        }
    }
}
