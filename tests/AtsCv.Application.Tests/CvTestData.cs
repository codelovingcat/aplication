using AtsCv.Application.Documents;
using AtsCv.Application.Resumes;

namespace AtsCv.Application.Tests;

/// <summary>
/// Upload payloads shared by the CV intake validation tests.
/// The byte sequences match the signatures that <see cref="CvFileSignatureValidator"/> accepts.
/// </summary>
internal static class CvTestData
{
    public const string PdfContentType = "application/pdf";

    public const string DocxContentType =
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    public const string ValidPdfFileName = "cv.pdf";

    public const string ValidDocxFileName = "cv.docx";

    // "%PDF-1"
    private static readonly byte[] PdfSignature = { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31 };

    // "PK\u0003\u0004"
    private static readonly byte[] DocxSignature = { 0x50, 0x4B, 0x03, 0x04, 0x14, 0x00 };

    // "NOTPDF"
    private static readonly byte[] UnsupportedSignature = { 0x4E, 0x4F, 0x54, 0x50, 0x44, 0x46 };

    public static ResumeUploadRequest ValidPdfRequest() =>
        new(ValidPdfFileName, PdfContentType, DocumentType.Pdf, PdfContent(), null);

    public static ResumeUploadRequest ValidDocxRequest() =>
        new(ValidDocxFileName, DocxContentType, DocumentType.Docx, DocxContent(), null);

    public static MemoryStream PdfContent() => new(PdfSignature);

    public static MemoryStream DocxContent() => new(DocxSignature);

    public static MemoryStream UnsupportedContent() => new(UnsupportedSignature);
}
