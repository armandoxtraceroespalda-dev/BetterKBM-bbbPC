using BloodborneLauncher.Core;

static void Check(bool result, string message) { if (!result) throw new Exception(message); }
var gate = new MenuSoundGate();
Check(gate.TryPlay(MenuSoundCue.Hover, 0), "The first pointer entry must be audible.");
Check(!gate.TryPlay(MenuSoundCue.Hover, 30), "Rapid pointer bursts must not stack.");
Check(gate.TryPlay(MenuSoundCue.Select, 40), "Selection must supersede a hover.");
Check(!gate.TryPlay(MenuSoundCue.Select, 41), "The same click routed twice must not play twice.");
Check(!gate.TryPlay(MenuSoundCue.Hover, 120), "A new page under the pointer must not cut off selection.");
Check(gate.TryPlay(MenuSoundCue.Hover, 200), "Hover must resume after selection.");
Check(gate.TryPlay(MenuSoundCue.Start, 300), "Play has a dedicated cue.");
Check(!gate.TryPlay(MenuSoundCue.Select, 350) && !gate.TryPlay(MenuSoundCue.Hover, 1499), "Navigation must not interrupt the start cue.");
Check(gate.TryPlay(MenuSoundCue.Hover, 1500), "The start hold must not mute the launcher permanently.");

var longStart = new MenuSoundGate();
Check(longStart.TryPlay(MenuSoundCue.Start, 0, 2400) && !longStart.TryPlay(MenuSoundCue.Hover, 2300), "The start cue must be protected for its actual clip duration.");
Check(longStart.TryPlay(MenuSoundCue.Hover, 2425), "Hover resumes after the actual start clip finishes.");

byte[] Wave()
{
    using var stream = new MemoryStream();
    using var writer = new BinaryWriter(stream);
    writer.Write("RIFF"u8); writer.Write(40); writer.Write("WAVEfmt "u8); writer.Write(16);
    writer.Write((ushort)1); writer.Write((ushort)1); writer.Write(44100); writer.Write(88200);
    writer.Write((ushort)2); writer.Write((ushort)16); writer.Write("data"u8); writer.Write(4); writer.Write(0);
    return stream.ToArray();
}
var valid = Wave();
Check(MenuWave.TryDuration(valid, out int duration) && duration == 1, "Audio duration must come from sample data and byte rate.");
Check(MenuWave.IsPcm16(valid), "A valid PCM16 WAV must load.");
var broken = (byte[])valid.Clone(); broken[20] = 3;
Check(!MenuWave.IsPcm16(broken), "Unsupported floating-point/compressed audio must not reach SoundPlayer.");
broken = (byte[])valid.Clone(); broken[22] = 3;
Check(!MenuWave.IsPcm16(broken), "Only mono/stereo clips are accepted.");
broken = (byte[])valid.Clone(); broken[32] = 4;
Check(!MenuWave.IsPcm16(broken), "Invalid sample alignment must be rejected.");
broken = (byte[])valid.Clone(); broken[28] = 0;
Check(!MenuWave.IsPcm16(broken), "Invalid byte rates must be rejected.");
broken = (byte[])valid.Clone(); broken[40] = 255;
Check(!MenuWave.IsPcm16(broken), "Truncated sample chunks must be rejected.");
Check(!MenuWave.IsPcm16(valid.AsSpan(0, 43)) && !MenuWave.IsPcm16(Array.Empty<byte>()), "Truncated/missing audio cannot cause a system fallback sound.");
Console.WriteLine("PASS: cue priority, hover throttling, duplicate suppression, start protection and safe PCM16 WAV validation.");

var attenuated = Wave();
System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(attenuated.AsSpan(44), 30000);
System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(attenuated.AsSpan(46), -30000);
var header = attenuated[..44];
MenuWave.ApplyGain(attenuated, MenuWave.LauncherGain);
Check(attenuated[..44].SequenceEqual(header), "Gain must preserve RIFF metadata.");
Check(System.Buffers.Binary.BinaryPrimitives.ReadInt16LittleEndian(attenuated.AsSpan(44)) == 7500 &&
      System.Buffers.Binary.BinaryPrimitives.ReadInt16LittleEndian(attenuated.AsSpan(46)) == -7500,
      "Default gain must attenuate positive and negative samples to 25 percent.");
MenuWave.ApplyGain(attenuated, 0);
Check(attenuated[44..].All(value => value == 0), "Zero gain must produce silence.");
var identity = Wave(); var original = (byte[])identity.Clone(); MenuWave.ApplyGain(identity, 1);
Check(identity.SequenceEqual(original), "Unity gain must preserve the waveform.");
foreach (var gain in new[] { double.NaN, double.PositiveInfinity, -1, 1.1 })
{
    bool rejected = false; try { MenuWave.ApplyGain(identity, gain); } catch (ArgumentException) { rejected = true; }
    Check(rejected, "Invalid gains must not corrupt a cached waveform.");
}
Console.WriteLine("PASS: 25 percent PCM attenuation; cue behavior is independent of memory collection.");
