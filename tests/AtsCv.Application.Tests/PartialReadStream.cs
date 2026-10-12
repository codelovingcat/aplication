namespace AtsCv.Application.Tests;

/// <summary>
/// Seekable, readable stream that never returns more bytes per read than requested by
/// the test. Used to prove that signature validation copes with partial reads.
/// </summary>
internal sealed class PartialReadStream : Stream
{
    private readonly MemoryStream _inner;
    private readonly int _maxBytesPerRead;

    public PartialReadStream(byte[] content, int maxBytesPerRead = 1)
    {
        _inner = new MemoryStream(content);
        _maxBytesPerRead = maxBytesPerRead;
    }

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => false;

    public override long Length => _inner.Length;

    public override long Position
    {
        get => _inner.Position;
        set => _inner.Position = value;
    }

    public override void Flush() => _inner.Flush();

    public override int Read(byte[] buffer, int offset, int count) =>
        _inner.Read(buffer, offset, Math.Min(count, _maxBytesPerRead));

    public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

    public override void SetLength(long value) =>
        throw new NotSupportedException("The stream is read-only.");

    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException("The stream is read-only.");
}
