namespace AtsCv.Application.Documents;

public sealed class CvFileSignatureValidator
{
    public bool IsValid(
        Stream content,
        DocumentType documentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        cancellationToken.ThrowIfCancellationRequested();

        if (!content.CanSeek || !content.CanRead)
            return false;

        var originalPosition = content.Position;

        try
        {
            var signature = documentType switch
            {
                DocumentType.Pdf => new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D },
                DocumentType.Docx => new byte[] { 0x50, 0x4B, 0x03, 0x04 },
                _ => []
            };

            if (signature.Length == 0)
                return false;

            var buffer = new byte[signature.Length];
            var read = 0;

            while (read < buffer.Length)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var count = content.Read(buffer, read, buffer.Length - read);
                if (count == 0)
                    return false;

                read += count;
            }

            return buffer.AsSpan().SequenceEqual(signature);
        }
        finally
        {
            content.Position = originalPosition;
        }
    }
}
