using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class AudioRecord : MonoBehaviour
{
    private AudioClip recordingClip;
    private string deviceName;
    private int sampleRate = 16000;
    private bool isRecording = false;

    public void StartRecord(int maxLengthSeconds = 300)
    {
        if (isRecording) return;

        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("no microphone device!");
            return;
        }

        deviceName = Microphone.devices[0];
        recordingClip = Microphone.Start(deviceName, false, maxLengthSeconds, sampleRate);
        isRecording = true;

        Debug.Log("start record...");
    }

    public void StopRecord(Action<string, byte[]> onFinish)
    {
        if (!isRecording) return;

        int position = Microphone.GetPosition(deviceName);
        Microphone.End(deviceName);
        isRecording = false;

        if (position <= 0)
        {
            Debug.LogError("record fail");
            return;
        }

        float[] samples = new float[position * recordingClip.channels];
        recordingClip.GetData(samples, 0);

        byte[] wavBytes = ConvertToWav(samples, recordingClip.channels, sampleRate);

        string base64 = Convert.ToBase64String(wavBytes);

        Debug.Log("stop record");

        onFinish?.Invoke(base64, wavBytes);
    }

    private byte[] ConvertToWav(float[] samples, int channels, int sampleRate)
    {
        MemoryStream stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);

        int sampleCount = samples.Length;
        int byteCount = sampleCount * 2;

        // WAV Header
        writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
        writer.Write(36 + byteCount);
        writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));
        writer.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1); // PCM
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * channels * 2);
        writer.Write((short)(channels * 2));
        writer.Write((short)16);
        writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
        writer.Write(byteCount);

        // PCM Data
        foreach (var sample in samples)
        {
            short intSample = (short)(sample * short.MaxValue);
            writer.Write(intSample);
        }

        writer.Flush();
        return stream.ToArray();
    }
}
