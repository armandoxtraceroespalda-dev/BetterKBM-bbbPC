using System.Buffers.Binary;

namespace BloodborneLauncher.Core;

/// <summary>Reject unsupported or incomplete WAV files before SoundPlayer can fall back to a system sound.</summary>
internal static class MenuWave
{
    public const double LauncherGain = 0.25;
    /// <summary>Scale only PCM samples in memory; keep cached game clips and Windows volume unchanged.</summary>
    public static void ApplyGain(Span<byte> wave, double gain)
    {
        if (!double.IsFinite(gain) || gain < 0 || gain > 1 || !IsPcm16(wave))
            throw new ArgumentException(BloodborneLauncher.Core.UiText.T("Expected valid PCM16 audio and a gain between zero and one."));
        for (int at = 12; at < wave.Length;)
        {
            int length = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(wave[(at + 4)..]));
            if (wave.Slice(at, 4).SequenceEqual("data"u8))
                for (int sample = at + 8; sample < at + 8 + length; sample += 2)
                {
                    short value = BinaryPrimitives.ReadInt16LittleEndian(wave[sample..]);
                    BinaryPrimitives.WriteInt16LittleEndian(wave[sample..], (short)Math.Round(value * gain));
                }
            at += 8 + length + (length & 1);
        }
    }
    /// <summary>Validate headers/chunk bounds without allocating or reading the PCM payload.</summary>
    public static bool TryDuration(System.IO.Stream stream,out int milliseconds)
    {
        milliseconds=0;
        if(!stream.CanRead||!stream.CanSeek||stream.Length is <44 or >2*1024*1024)return false;
        Span<byte> header=stackalloc byte[16];
        try {
            stream.Position=0;stream.ReadExactly(header[..12]);
            if(!header[..4].SequenceEqual("RIFF"u8)||!header.Slice(8,4).SequenceEqual("WAVE"u8)||BinaryPrimitives.ReadUInt32LittleEndian(header[4..])!=stream.Length-8)return false;
            bool format=false,data=false;int alignment=0;long dataLength=0;uint bytesPerSecond=0;
            while(stream.Position<stream.Length) {
                if(stream.Length-stream.Position<8)return false;
                stream.ReadExactly(header[..8]);uint count=BinaryPrimitives.ReadUInt32LittleEndian(header[4..]);
                long next=stream.Position+count+(count&1);
                if(next>stream.Length)return false;
                if(header[..4].SequenceEqual("fmt "u8)) {
                    if(format||count<16)return false;stream.ReadExactly(header);
                    int channels=BinaryPrimitives.ReadUInt16LittleEndian(header[2..]);uint rate=BinaryPrimitives.ReadUInt32LittleEndian(header[4..]);
                    alignment=BinaryPrimitives.ReadUInt16LittleEndian(header[12..]);
                    if(BinaryPrimitives.ReadUInt16LittleEndian(header)!=1||channels is not(1 or 2)||rate is <8000 or >192000||alignment!=channels*2||BinaryPrimitives.ReadUInt16LittleEndian(header[14..])!=16||BinaryPrimitives.ReadUInt32LittleEndian(header[8..])!=rate*alignment)return false;
                    bytesPerSecond=rate*(uint)alignment;format=true;
                } else if(header[..4].SequenceEqual("data"u8)) {if(data||count==0)return false;data=true;dataLength=count;}
                stream.Position=next;
            }
            if(!format||!data||dataLength%alignment!=0)return false;
            milliseconds=(int)((dataLength*1000+bytesPerSecond-1)/bytesPerSecond);return milliseconds<=5000;
        } catch(System.IO.EndOfStreamException){return false;}
    }
    public static bool IsPcm16(ReadOnlySpan<byte> wave) => TryDuration(wave, out _);
    public static bool TryDuration(ReadOnlySpan<byte> wave, out int milliseconds)
    {
        milliseconds = 0;
        if (wave.Length < 44 || !wave[..4].SequenceEqual("RIFF"u8) || !wave.Slice(8, 4).SequenceEqual("WAVE"u8)) return false;
        if (BinaryPrimitives.ReadUInt32LittleEndian(wave[4..]) != wave.Length - 8) return false;
        bool format = false;
        int dataLength = 0, alignment = 0;
        uint bytesPerSecond = 0;
        for (int at = 12; at < wave.Length;)
        {
            if (wave.Length - at < 8) return false;
            uint length = BinaryPrimitives.ReadUInt32LittleEndian(wave[(at + 4)..]);
            if (length > wave.Length - at - 8) return false;
            var payload = wave.Slice(at + 8, (int)length);
            if (wave.Slice(at, 4).SequenceEqual("fmt "u8))
            {
                if (format || length < 16 || BinaryPrimitives.ReadUInt16LittleEndian(payload) != 1) return false;
                int channels = BinaryPrimitives.ReadUInt16LittleEndian(payload[2..]);
                uint rate = BinaryPrimitives.ReadUInt32LittleEndian(payload[4..]);
                alignment = BinaryPrimitives.ReadUInt16LittleEndian(payload[12..]);
                if (channels is not (1 or 2) || rate is < 8000 or > 192000 ||
                    BinaryPrimitives.ReadUInt16LittleEndian(payload[14..]) != 16 || alignment != channels * 2 ||
                    BinaryPrimitives.ReadUInt32LittleEndian(payload[8..]) != rate * alignment) return false;
                bytesPerSecond = rate * (uint)alignment;
                format = true;
            }
            else if (wave.Slice(at, 4).SequenceEqual("data"u8))
            {
                if (dataLength != 0) return false;
                dataLength = (int)length;
            }
            long next = at + 8L + length + (length & 1);
            if (next > wave.Length) return false;
            at = (int)next;
        }
        if (!format || dataLength <= 0 || dataLength % alignment != 0) return false;
        milliseconds = (int)((dataLength * 1000L + bytesPerSecond - 1) / bytesPerSecond);
        return milliseconds <= 5000;
    }
}
