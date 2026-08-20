using UnityEngine;
using System.IO;
using System;
using NAudio.Wave;

public static class NAudioPlayer
{
    /// <summary>
    /// Decode audio bytes into an AudioClip. Supports WAV (RIFF) and MP3.
    /// </summary>
    public static AudioClip FromAudioData(byte[] buffer)
    {
        if (buffer == null || buffer.Length < 4)
        {
            throw new ArgumentException("Audio buffer is empty or too short.");
        }

        using (MemoryStream input = new MemoryStream(buffer))
        using (WaveStream reader = CreateReader(input, buffer))
        {
            WaveStream pcmStream = null;
            WaveStream source = reader;

            try
            {
                if (reader.WaveFormat.Encoding != WaveFormatEncoding.Pcm ||
                    reader.WaveFormat.BitsPerSample != 16)
                {
                    pcmStream = WaveFormatConversionStream.CreatePcmStream(reader);
                    source = pcmStream;
                }

                byte[] pcmBytes = ReadAllBytes(source);
                return CreateClipFromPcm16(pcmBytes, source.WaveFormat);
            }
            finally
            {
                if (pcmStream != null)
                {
                    pcmStream.Dispose();
                }
            }
        }
    }

    /// <summary>
    /// Backward-compatible entry. Auto-detects WAV / MP3.
    /// </summary>
    public static AudioClip FromWavData(byte[] buffer)
    {
        return FromAudioData(buffer);
    }

    private static WaveStream CreateReader(Stream stream, byte[] buffer)
    {
        if (IsWav(buffer))
        {
            return new WaveFileReader(stream);
        }

        if (IsMp3(buffer))
        {
            return new Mp3FileReader(stream);
        }

        // Last resort: try MP3 then WAV for unusual headers.
        long pos = stream.Position;
        try
        {
            return new Mp3FileReader(stream);
        }
        catch (Exception)
        {
            stream.Position = pos;
            return new WaveFileReader(stream);
        }
    }

    private static bool IsWav(byte[] buffer)
    {
        return buffer.Length >= 12
               && buffer[0] == (byte)'R'
               && buffer[1] == (byte)'I'
               && buffer[2] == (byte)'F'
               && buffer[3] == (byte)'F'
               && buffer[8] == (byte)'W'
               && buffer[9] == (byte)'A'
               && buffer[10] == (byte)'V'
               && buffer[11] == (byte)'E';
    }

    private static bool IsMp3(byte[] buffer)
    {
        if (buffer.Length < 3)
        {
            return false;
        }

        // ID3v2 tag
        if (buffer[0] == (byte)'I' && buffer[1] == (byte)'D' && buffer[2] == (byte)'3')
        {
            return true;
        }

        // MPEG frame sync: 11 bits set
        return buffer[0] == 0xFF && (buffer[1] & 0xE0) == 0xE0;
    }

    private static byte[] ReadAllBytes(WaveStream source)
    {
        source.Position = 0;

        if (source.Length > 0)
        {
            byte[] bytes = new byte[source.Length];
            int offset = 0;
            int read;
            while (offset < bytes.Length &&
                   (read = source.Read(bytes, offset, bytes.Length - offset)) > 0)
            {
                offset += read;
            }

            if (offset == bytes.Length)
            {
                return bytes;
            }

            byte[] trimmed = new byte[offset];
            Buffer.BlockCopy(bytes, 0, trimmed, 0, offset);
            return trimmed;
        }

        // Some compressed streams report Length as 0; read until EOF.
        using (MemoryStream ms = new MemoryStream())
        {
            byte[] buffer = new byte[4096];
            int read;
            while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
            {
                ms.Write(buffer, 0, read);
            }

            return ms.ToArray();
        }
    }

    private static AudioClip CreateClipFromPcm16(byte[] pcmBytes, WaveFormat format)
    {
        int channels = Math.Max(1, format.Channels);
        int sampleRate = format.SampleRate;
        int bytesPerSample = Math.Max(1, format.BitsPerSample / 8);
        int frameCount = pcmBytes.Length / (bytesPerSample * channels);

        if (frameCount <= 0)
        {
            throw new FormatException("Decoded audio has no samples.");
        }

        // Keep mono left-channel behavior for compatibility with previous WAV path.
        float[] samples = new float[frameCount];
        int step = bytesPerSample * channels;

        for (int i = 0; i < frameCount; i++)
        {
            int pos = i * step;
            short s = (short)(pcmBytes[pos] | (pcmBytes[pos + 1] << 8));
            samples[i] = s / 32768.0f;
        }

        AudioClip audioClip = AudioClip.Create("audioClip", frameCount, 1, sampleRate, false);
        audioClip.SetData(samples, 0);
        return audioClip;
    }
}
