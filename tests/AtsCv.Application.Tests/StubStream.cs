namespace AtsCv.Application.Tests;

/// <summary>
/// Stream double that reports a caller-controlled length together with chosen
/// readability and seekability. It keeps the size boundary tests allocation free and
/// lets the tests reproduce the stream states an upload can be handed to validation in.
/// <see cref="Read"/> intentionally throws so a test fails loudly if validation starts
/// consuming content that the tests never provided.
/// </summary>
internal sealed class StubStream : Stream
{
    private readonly long _declaredLength;
    private long _position;

    public StubStream(long length, bool canRead = true, bool canSeek = true)
    {
        _declaredLength = length;
        CanRead = canRead;
        CanSeek = canSeek;
    }

    public override bool CanRead { get; }

    public override bool CanSeek { get; }

    public override bool CanWrite => false;

    public override long Length => CanSeek
        ? _declaredLength
        : throw new NotSupportedException("A non-seekable stream does not report a length.");

    public override long Position
    {
        get => CanSeek
            ? _position
            : throw new NotSupportedException("A non-seekable stream does not report a position.");
        set
        {
            if (!CanSeek)
                throw new NotSupportedException("A non-seekable stream does not support seeking.");

            _position = value;
        }
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException("The stream double does not provide content.");

    public override long Seek(long offset, SeekOrigin origin)
    {
        if (!CanSeek)
            throw new NotSupportedException("A non-seekable stream does not support seeking.");

        _position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End => _declaredLength + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin))
        };

        return _position;
    }

    public override void SetLength(long value) =>
        throw new NotSupportedException("The stream double has a fixed length.");

    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException("The stream double is read-only.");
}
