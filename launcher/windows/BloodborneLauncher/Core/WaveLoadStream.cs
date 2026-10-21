using System.IO;

namespace BloodborneLauncher.Core;

/// <summary>Import PCM once, then release the staging array after SoundPlayer.Load caches it.</summary>
internal sealed class WaveLoadStream : Stream
{
    private byte[]? _samples;
    private readonly int _length;
    private int _position;
    internal int RetainedBytes => _samples?.Length ?? 0;
    internal WaveLoadStream(byte[] samples) { _samples=samples;_length=samples.Length; }
    internal void ReleaseSamples() => _samples=null;
    public override bool CanRead => _samples!=null;
    public override bool CanSeek => _samples!=null;
    public override bool CanWrite => false;
    public override long Length => _length;
    public override long Position {
        get=>_position;
        set { if(value<0 || value>int.MaxValue)throw new ArgumentOutOfRangeException(nameof(value));_position=(int)value; }
    }
    public override int Read(byte[] buffer,int offset,int count) => Read(buffer.AsSpan(offset,count));
    public override int Read(Span<byte> buffer)
    {
        var samples=_samples??throw new ObjectDisposedException(nameof(WaveLoadStream));
        int count=Math.Min(buffer.Length,Math.Max(0,_length-_position));
        samples.AsSpan(Math.Min(_position,_length),count).CopyTo(buffer);_position+=count;return count;
    }
    public override long Seek(long offset,SeekOrigin origin)
    {
        Position=checked((origin switch {SeekOrigin.Begin=>0,SeekOrigin.Current=>Position,SeekOrigin.End=>Length,_=>throw new ArgumentOutOfRangeException(nameof(origin))})+offset);
        return Position;
    }
    public override void Flush() { }
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer,int offset,int count) => throw new NotSupportedException();
    protected override void Dispose(bool disposing) { ReleaseSamples();base.Dispose(disposing); }
}
