using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace SumeruAI.Talk
{
    public enum TalkPayloadKind
    {
        None,
        Meta,
        Cards,
        TextDelta,
        Audio,
        A2f,
        Rag,
        AsrFinal,
        Error,
        Done
    }

    public sealed class TalkStreamEvent
    {
        public string Event;
        public TalkPayloadKind Kind;
        public TalkPbMeta Meta;
        public TalkPbCards Cards;
        public string TextDelta;
        public TalkPbAudio Audio;
        public TalkPbA2f A2f;
        public TalkPbRag Rag;
        public TalkPbAsrFinal AsrFinal;
        public TalkPbError Error;
        public TalkPbDone Done;
    }

    public sealed class TalkPbMeta
    {
        public string session_id;
        public string trace_id;
        public string device_id;
        public string digital_human_id;
        public string model_type;
        public string model_id;
        public string voice_id;
        public string emotion;
        public string tts_model;
        public bool enable_tts;
        public bool enable_a2f;
        public bool cms_profile;
        public string history_backend;
        public string segment_protocol;
        public int segment_first_min_chars;
        public int segment_min_chars;
        public int segment_max_chars;
        public int tts_workers;
    }

    public sealed class TalkPbCard
    {
        public string id;
        public string type;
        public string title;
        public string content;
        public string image_url;
        public string link_url;
        public string qrcode_url;
    }

    public sealed class TalkPbCards
    {
        public readonly List<TalkPbCard> items = new List<TalkPbCard>();
    }

    public sealed class TalkPbAudio
    {
        public byte[] audio;
        public double audio_timelen;
        public string text;
        public string voice_id;
        public int seq;
        public string segment_id;
        public string emotion;
    }

    public sealed class TalkPbA2fPayload
    {
        public byte[] blendshapes;
        public int num_frames;
        public double fps;
        public string model_id;
    }

    public sealed class TalkPbA2f
    {
        public string model_id;
        public string model_type;
        public string status;
        public int seq;
        public string segment_id;
        public int code;
        public string message;
        public TalkPbA2fPayload data;
    }

    public sealed class TalkPbRagHit
    {
        public string id;
        public string title;
        public string source;
    }

    public sealed class TalkPbRag
    {
        public int count;
        public readonly List<TalkPbRagHit> hits = new List<TalkPbRagHit>();
    }

    public sealed class TalkPbAsrFinal
    {
        public string text;
        public string language;
    }

    public sealed class TalkPbError
    {
        public string code;
        public string message;
        public bool fallback;
        public bool recoverable;
        public string url;
        public string host;
        public string digital_human_id;
        public string trace_id;
        public string source;
        public string model_id;
        public string model_type;
        public string status;
        public int seq;
        public string segment_id;
    }

    public sealed class TalkPbLatency
    {
        public double llm_first_delta;
        public double first_audio;
        public double tts_total;
        public double a2f_total;
    }

    public sealed class TalkPbDone
    {
        public string session_id;
        public bool ok;
        public string reply;
        public string mode;
        public int segments;
        public double pipeline_ms;
        public TalkPbLatency latency_ms;
        public string a2f_phase;
        public int a2f_failures;
    }

    /// <summary>
    /// Length-prefixed protobuf stream: uint32_be(length) + TalkEvent bytes.
    /// Schema: talk_stream.proto
    /// </summary>
    public sealed class TalkFrameParser
    {
        private const int MaxFrameBytes = 32 * 1024 * 1024;
        private byte[] _buf = new byte[64 * 1024];
        private int _length;

        public void Append(byte[] data, int offset, int count)
        {
            if (data == null || count <= 0)
            {
                return;
            }

            EnsureCapacity(_length + count);
            Buffer.BlockCopy(data, offset, _buf, _length, count);
            _length += count;
        }

        public bool TryReadEvent(out TalkStreamEvent evt)
        {
            evt = null;
            while (_length >= 4)
            {
                uint frameSize = ReadUInt32Be(_buf, 0);
                if (frameSize > MaxFrameBytes)
                {
                    throw new InvalidOperationException(
                        $"Talk protobuf frame too large: {frameSize}, head={Hex(_buf, 0, Math.Min(16, _length))}");
                }

                int total = 4 + (int)frameSize;
                if (_length < total)
                {
                    return false;
                }

                try
                {
                    evt = TalkEventCodec.Parse(_buf, 4, (int)frameSize);
                    Consume(total);
                    return true;
                }
                catch (Exception ex)
                {
                    int dumpLen = Math.Min(total, 64);
                    Debug.LogWarning(
                        $"[Talk] skip bad protobuf frame len={frameSize} head={Hex(_buf, 0, dumpLen)}: {ex.Message}");
                    Consume(total);
                }
            }

            return false;
        }

        private void Consume(int total)
        {
            int remain = _length - total;
            if (remain > 0)
            {
                Buffer.BlockCopy(_buf, total, _buf, 0, remain);
            }

            _length = remain;
        }

        private void EnsureCapacity(int needed)
        {
            if (_buf.Length >= needed)
            {
                return;
            }

            int cap = _buf.Length;
            while (cap < needed)
            {
                cap *= 2;
            }

            byte[] next = new byte[cap];
            if (_length > 0)
            {
                Buffer.BlockCopy(_buf, 0, next, 0, _length);
            }

            _buf = next;
        }

        public static void ParseAll(byte[] data, Action<TalkStreamEvent> onEvent)
        {
            if (data == null || data.Length == 0 || onEvent == null)
            {
                return;
            }

            TalkFrameParser parser = new TalkFrameParser();
            parser.Append(data, 0, data.Length);
            TalkStreamEvent evt;
            while (parser.TryReadEvent(out evt))
            {
                onEvent(evt);
            }
        }

        private static uint ReadUInt32Be(byte[] buf, int offset)
        {
            return ((uint)buf[offset] << 24)
                   | ((uint)buf[offset + 1] << 16)
                   | ((uint)buf[offset + 2] << 8)
                   | buf[offset + 3];
        }

        private static string Hex(byte[] buf, int offset, int count)
        {
            if (buf == null || count <= 0)
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder(count * 3);
            int end = Math.Min(buf.Length, offset + count);
            for (int i = offset; i < end; i++)
            {
                if (i > offset)
                {
                    sb.Append(' ');
                }

                sb.Append(buf[i].ToString("x2"));
            }

            return sb.ToString();
        }
    }

    public sealed class TalkProtobufDownloadHandler : DownloadHandlerScript
    {
        private readonly TalkFrameParser _parser = new TalkFrameParser();
        private readonly Action<TalkStreamEvent> _onEvent;
        private readonly Action<Exception> _onError;

        public TalkProtobufDownloadHandler(Action<TalkStreamEvent> onEvent, Action<Exception> onError)
            : base(new byte[256 * 1024])
        {
            _onEvent = onEvent;
            _onError = onError;
        }

        protected override byte[] GetData()
        {
            return null;
        }

        protected override string GetText()
        {
            return string.Empty;
        }

        protected override bool ReceiveData(byte[] data, int dataLength)
        {
            if (data == null || dataLength <= 0)
            {
                return true;
            }

            try
            {
                _parser.Append(data, 0, dataLength);
                Drain();
                return true;
            }
            catch (Exception ex)
            {
                if (_onError != null)
                {
                    _onError(ex);
                }

                return false;
            }
        }

        public void Flush()
        {
            Drain();
        }

        private void Drain()
        {
            TalkStreamEvent evt;
            while (_parser.TryReadEvent(out evt))
            {
                if (_onEvent != null)
                {
                    _onEvent(evt);
                }
            }
        }
    }

    internal static class TalkEventCodec
    {
        public static TalkStreamEvent Parse(byte[] data, int offset, int count)
        {
            ProtoReader reader = new ProtoReader(data, offset, offset + count);
            TalkStreamEvent evt = new TalkStreamEvent();
            int field;
            int wire;
            while (reader.TryReadTag(out field, out wire))
            {
                if (field < 1 || wire == 3 || wire == 4 || wire == 6 || wire == 7)
                {
                    break;
                }

                switch (field)
                {
                    case 1:
                    {
                        string value = ReadString(reader, wire);
                        if (string.IsNullOrEmpty(evt.Event))
                        {
                            evt.Event = value;
                        }
                        else
                        {
                            evt.TextDelta = (evt.TextDelta ?? string.Empty) + value;
                            if (evt.Kind == TalkPayloadKind.None)
                            {
                                evt.Kind = TalkPayloadKind.TextDelta;
                            }
                        }
                        break;
                    }
                    case 2:
                    {
                        if (evt.Event == "text_delta" || evt.Kind == TalkPayloadKind.TextDelta)
                        {
                            evt.TextDelta = (evt.TextDelta ?? string.Empty) + ReadString(reader, wire);
                            evt.Kind = TalkPayloadKind.TextDelta;
                        }
                        else
                        {
                            reader.Skip(wire);
                        }
                        break;
                    }
                    case 10:
                        evt.Meta = ReadMeta(ReadEmbedded(reader, wire));
                        evt.Kind = TalkPayloadKind.Meta;
                        break;
                    case 11:
                        evt.Cards = ReadCards(ReadEmbedded(reader, wire));
                        evt.Kind = TalkPayloadKind.Cards;
                        break;
                    case 12:
                        evt.TextDelta = DecodeTextPayload(ReadBytes(reader, wire));
                        evt.Kind = TalkPayloadKind.TextDelta;
                        break;
                    case 13:
                        evt.Audio = ReadAudio(ReadEmbedded(reader, wire));
                        evt.Kind = TalkPayloadKind.Audio;
                        break;
                    case 14:
                        evt.A2f = ReadA2f(ReadEmbedded(reader, wire));
                        evt.Kind = TalkPayloadKind.A2f;
                        break;
                    case 15:
                        evt.Rag = ReadRag(ReadEmbedded(reader, wire));
                        evt.Kind = TalkPayloadKind.Rag;
                        break;
                    case 16:
                        evt.AsrFinal = ReadAsrFinal(ReadEmbedded(reader, wire));
                        evt.Kind = TalkPayloadKind.AsrFinal;
                        break;
                    case 17:
                        evt.Error = ReadError(ReadEmbedded(reader, wire));
                        evt.Kind = TalkPayloadKind.Error;
                        break;
                    case 18:
                        evt.Done = ReadDone(ReadEmbedded(reader, wire));
                        evt.Kind = TalkPayloadKind.Done;
                        break;
                    default:
                        reader.Skip(wire);
                        break;
                }
            }

            if (evt.Kind == TalkPayloadKind.None && !string.IsNullOrEmpty(evt.Event))
            {
                evt.Kind = KindFromEventName(evt.Event);
            }

            return evt;
        }

        private static string DecodeTextPayload(byte[] raw)
        {
            if (raw == null || raw.Length == 0)
            {
                return string.Empty;
            }

            if (raw[0] == 0x0a)
            {
                int pos = 1;
                int innerLen = ReadRawVarint(raw, ref pos);
                if (innerLen >= 0 && pos + innerLen == raw.Length)
                {
                    return innerLen == 0 ? string.Empty : Encoding.UTF8.GetString(raw, pos, innerLen);
                }
            }

            return Encoding.UTF8.GetString(raw);
        }

        private static int ReadRawVarint(byte[] data, ref int pos)
        {
            ulong result = 0;
            int shift = 0;
            while (pos < data.Length)
            {
                byte b = data[pos++];
                result |= (ulong)(b & 0x7F) << shift;
                if ((b & 0x80) == 0)
                {
                    return (int)result;
                }

                shift += 7;
                if (shift > 31)
                {
                    return -1;
                }
            }

            return -1;
        }

        private static TalkPayloadKind KindFromEventName(string name)
        {
            switch (name)
            {
                case "meta": return TalkPayloadKind.Meta;
                case "cards": return TalkPayloadKind.Cards;
                case "text_delta": return TalkPayloadKind.TextDelta;
                case "audio": return TalkPayloadKind.Audio;
                case "a2f": return TalkPayloadKind.A2f;
                case "rag": return TalkPayloadKind.Rag;
                case "asr_final": return TalkPayloadKind.AsrFinal;
                case "error": return TalkPayloadKind.Error;
                case "done": return TalkPayloadKind.Done;
                default: return TalkPayloadKind.None;
            }
        }

        private static TalkPbMeta ReadMeta(ProtoReader reader)
        {
            TalkPbMeta m = new TalkPbMeta();
            int field;
            int wire;
            while (reader.TryReadTag(out field, out wire))
            {
                switch (field)
                {
                    case 1: m.session_id = ReadString(reader, wire); break;
                    case 2: m.trace_id = ReadString(reader, wire); break;
                    case 3: m.device_id = ReadString(reader, wire); break;
                    case 4: m.digital_human_id = ReadString(reader, wire); break;
                    case 5: m.model_type = ReadString(reader, wire); break;
                    case 6: m.model_id = ReadString(reader, wire); break;
                    case 7: m.voice_id = ReadString(reader, wire); break;
                    case 8: m.emotion = ReadString(reader, wire); break;
                    case 9: m.tts_model = ReadString(reader, wire); break;
                    case 10: m.enable_tts = ReadBool(reader, wire); break;
                    case 11: m.enable_a2f = ReadBool(reader, wire); break;
                    case 12: m.cms_profile = ReadBool(reader, wire); break;
                    case 13: m.history_backend = ReadString(reader, wire); break;
                    case 14: m.segment_protocol = ReadString(reader, wire); break;
                    case 15: m.segment_first_min_chars = ReadInt32(reader, wire); break;
                    case 16: m.segment_min_chars = ReadInt32(reader, wire); break;
                    case 17: m.segment_max_chars = ReadInt32(reader, wire); break;
                    case 18: m.tts_workers = ReadInt32(reader, wire); break;
                    default: reader.Skip(wire); break;
                }
            }

            return m;
        }

        private static TalkPbCards ReadCards(ProtoReader reader)
        {
            TalkPbCards cards = new TalkPbCards();
            int field;
            int wire;
            while (reader.TryReadTag(out field, out wire))
            {
                if (field == 1)
                {
                    cards.items.Add(ReadCard(ReadEmbedded(reader, wire)));
                }
                else
                {
                    reader.Skip(wire);
                }
            }

            return cards;
        }

        private static TalkPbCard ReadCard(ProtoReader reader)
        {
            TalkPbCard card = new TalkPbCard();
            int field;
            int wire;
            while (reader.TryReadTag(out field, out wire))
            {
                switch (field)
                {
                    case 1: card.id = ReadString(reader, wire); break;
                    case 2: card.type = ReadString(reader, wire); break;
                    case 3: card.title = ReadString(reader, wire); break;
                    case 4: card.content = ReadString(reader, wire); break;
                    case 5: card.image_url = ReadString(reader, wire); break;
                    case 6: card.link_url = ReadString(reader, wire); break;
                    case 7: card.qrcode_url = ReadString(reader, wire); break;
                    default: reader.Skip(wire); break;
                }
            }

            return card;
        }

        private static TalkPbAudio ReadAudio(ProtoReader reader)
        {
            TalkPbAudio audio = new TalkPbAudio();
            int field;
            int wire;
            while (reader.TryReadTag(out field, out wire))
            {
                switch (field)
                {
                    case 1: audio.audio = ReadBytes(reader, wire); break;
                    case 2: audio.audio_timelen = ReadDouble(reader, wire); break;
                    case 3: audio.text = ReadString(reader, wire); break;
                    case 4: audio.voice_id = ReadString(reader, wire); break;
                    case 5: audio.seq = ReadInt32(reader, wire); break;
                    case 6: audio.segment_id = ReadString(reader, wire); break;
                    case 7: audio.emotion = ReadString(reader, wire); break;
                    default: reader.Skip(wire); break;
                }
            }

            return audio;
        }

        private static TalkPbA2f ReadA2f(ProtoReader reader)
        {
            TalkPbA2f a2f = new TalkPbA2f();
            int field;
            int wire;
            while (reader.TryReadTag(out field, out wire))
            {
                switch (field)
                {
                    case 1: a2f.model_id = ReadString(reader, wire); break;
                    case 2: a2f.model_type = ReadString(reader, wire); break;
                    case 3: a2f.status = ReadString(reader, wire); break;
                    case 4: a2f.seq = ReadInt32(reader, wire); break;
                    case 5: a2f.segment_id = ReadString(reader, wire); break;
                    case 6: a2f.code = ReadInt32(reader, wire); break;
                    case 7: a2f.message = ReadString(reader, wire); break;
                    case 8: a2f.data = ReadA2fPayload(ReadEmbedded(reader, wire)); break;
                    default: reader.Skip(wire); break;
                }
            }

            return a2f;
        }

        private static TalkPbA2fPayload ReadA2fPayload(ProtoReader reader)
        {
            TalkPbA2fPayload payload = new TalkPbA2fPayload();
            int field;
            int wire;
            while (reader.TryReadTag(out field, out wire))
            {
                switch (field)
                {
                    case 1: payload.blendshapes = ReadBytes(reader, wire); break;
                    case 2: payload.num_frames = ReadInt32(reader, wire); break;
                    case 3: payload.fps = ReadDouble(reader, wire); break;
                    case 4: payload.model_id = ReadString(reader, wire); break;
                    default: reader.Skip(wire); break;
                }
            }

            return payload;
        }

        private static TalkPbRag ReadRag(ProtoReader reader)
        {
            TalkPbRag rag = new TalkPbRag();
            int field;
            int wire;
            while (reader.TryReadTag(out field, out wire))
            {
                switch (field)
                {
                    case 1: rag.count = ReadInt32(reader, wire); break;
                    case 2: rag.hits.Add(ReadRagHit(ReadEmbedded(reader, wire))); break;
                    default: reader.Skip(wire); break;
                }
            }

            return rag;
        }

        private static TalkPbRagHit ReadRagHit(ProtoReader reader)
        {
            TalkPbRagHit hit = new TalkPbRagHit();
            int field;
            int wire;
            while (reader.TryReadTag(out field, out wire))
            {
                switch (field)
                {
                    case 1: hit.id = ReadString(reader, wire); break;
                    case 2: hit.title = ReadString(reader, wire); break;
                    case 3: hit.source = ReadString(reader, wire); break;
                    default: reader.Skip(wire); break;
                }
            }

            return hit;
        }

        private static TalkPbAsrFinal ReadAsrFinal(ProtoReader reader)
        {
            TalkPbAsrFinal asr = new TalkPbAsrFinal();
            int field;
            int wire;
            while (reader.TryReadTag(out field, out wire))
            {
                switch (field)
                {
                    case 1: asr.text = ReadString(reader, wire); break;
                    case 2: asr.language = ReadString(reader, wire); break;
                    default: reader.Skip(wire); break;
                }
            }

            return asr;
        }

        private static TalkPbError ReadError(ProtoReader reader)
        {
            TalkPbError err = new TalkPbError();
            int field;
            int wire;
            while (reader.TryReadTag(out field, out wire))
            {
                switch (field)
                {
                    case 1: err.code = ReadString(reader, wire); break;
                    case 2: err.message = ReadString(reader, wire); break;
                    case 3: err.fallback = ReadBool(reader, wire); break;
                    case 4: err.recoverable = ReadBool(reader, wire); break;
                    case 5: err.url = ReadString(reader, wire); break;
                    case 6: err.host = ReadString(reader, wire); break;
                    case 7: err.digital_human_id = ReadString(reader, wire); break;
                    case 8: err.trace_id = ReadString(reader, wire); break;
                    case 9: err.source = ReadString(reader, wire); break;
                    case 10: err.model_id = ReadString(reader, wire); break;
                    case 11: err.model_type = ReadString(reader, wire); break;
                    case 12: err.status = ReadString(reader, wire); break;
                    case 13: err.seq = ReadInt32(reader, wire); break;
                    case 14: err.segment_id = ReadString(reader, wire); break;
                    default: reader.Skip(wire); break;
                }
            }

            return err;
        }

        private static TalkPbDone ReadDone(ProtoReader reader)
        {
            TalkPbDone done = new TalkPbDone();
            int field;
            int wire;
            while (reader.TryReadTag(out field, out wire))
            {
                switch (field)
                {
                    case 1: done.session_id = ReadString(reader, wire); break;
                    case 2: done.ok = ReadBool(reader, wire); break;
                    case 3: done.reply = ReadString(reader, wire); break;
                    case 4: done.mode = ReadString(reader, wire); break;
                    case 5: done.segments = ReadInt32(reader, wire); break;
                    case 6: done.pipeline_ms = ReadDouble(reader, wire); break;
                    case 7: done.latency_ms = ReadLatency(ReadEmbedded(reader, wire)); break;
                    case 8: done.a2f_phase = ReadString(reader, wire); break;
                    case 9: done.a2f_failures = ReadInt32(reader, wire); break;
                    default: reader.Skip(wire); break;
                }
            }

            return done;
        }

        private static TalkPbLatency ReadLatency(ProtoReader reader)
        {
            TalkPbLatency latency = new TalkPbLatency();
            int field;
            int wire;
            while (reader.TryReadTag(out field, out wire))
            {
                switch (field)
                {
                    case 1: latency.llm_first_delta = ReadDouble(reader, wire); break;
                    case 2: latency.first_audio = ReadDouble(reader, wire); break;
                    case 3: latency.tts_total = ReadDouble(reader, wire); break;
                    case 4: latency.a2f_total = ReadDouble(reader, wire); break;
                    default: reader.Skip(wire); break;
                }
            }

            return latency;
        }

        private static string ReadString(ProtoReader reader, int wire)
        {
            return wire == 2 ? reader.ReadString() : SkipAndDefault(reader, wire, (string)null);
        }

        private static byte[] ReadBytes(ProtoReader reader, int wire)
        {
            return wire == 2 ? reader.ReadBytes() : SkipAndDefault(reader, wire, (byte[])null);
        }

        private static int ReadInt32(ProtoReader reader, int wire)
        {
            if (wire == 0)
            {
                return (int)reader.ReadVarint();
            }

            reader.Skip(wire);
            return 0;
        }

        private static bool ReadBool(ProtoReader reader, int wire)
        {
            return ReadInt32(reader, wire) != 0;
        }

        private static double ReadDouble(ProtoReader reader, int wire)
        {
            if (wire == 1)
            {
                return reader.ReadDouble();
            }

            reader.Skip(wire);
            return 0;
        }

        private static ProtoReader ReadEmbedded(ProtoReader reader, int wire)
        {
            if (wire != 2)
            {
                reader.Skip(wire);
                return ProtoReader.Empty;
            }

            return reader.ReadEmbedded();
        }

        private static T SkipAndDefault<T>(ProtoReader reader, int wire, T fallback)
        {
            reader.Skip(wire);
            return fallback;
        }
    }

    internal struct ProtoReader
    {
        public static readonly ProtoReader Empty = new ProtoReader(new byte[0], 0, 0);

        private readonly byte[] _data;
        private int _pos;
        private readonly int _end;

        public ProtoReader(byte[] data, int start, int end)
        {
            _data = data;
            _pos = start;
            _end = end;
        }

        public bool TryReadTag(out int field, out int wire)
        {
            field = 0;
            wire = 0;
            if (_pos >= _end)
            {
                return false;
            }

            ulong tag = ReadVarint();
            field = (int)(tag >> 3);
            wire = (int)(tag & 7);
            return true;
        }

        public ulong ReadVarint()
        {
            ulong result = 0;
            int shift = 0;
            while (_pos < _end)
            {
                byte b = _data[_pos++];
                result |= (ulong)(b & 0x7F) << shift;
                if ((b & 0x80) == 0)
                {
                    return result;
                }

                shift += 7;
                if (shift > 63)
                {
                    throw new InvalidOperationException("Varint overflow");
                }
            }

            throw new InvalidOperationException("Truncated varint");
        }

        public string ReadString()
        {
            byte[] bytes = ReadBytes();
            return bytes.Length == 0 ? string.Empty : Encoding.UTF8.GetString(bytes);
        }

        public byte[] ReadBytes()
        {
            int len = (int)ReadVarint();
            if (len < 0)
            {
                len = 0;
            }

            int remain = _end - _pos;
            if (len > remain)
            {
                len = remain;
            }

            byte[] copy = new byte[len];
            if (len > 0)
            {
                Buffer.BlockCopy(_data, _pos, copy, 0, len);
            }

            _pos += len;
            return copy;
        }

        public double ReadDouble()
        {
            Ensure(8);
            double value = BitConverter.ToDouble(_data, _pos);
            _pos += 8;
            return value;
        }

        public ProtoReader ReadEmbedded()
        {
            int len = (int)ReadVarint();
            if (len < 0)
            {
                len = 0;
            }

            int remain = _end - _pos;
            if (len > remain)
            {
                len = remain;
            }

            int start = _pos;
            _pos += len;
            return new ProtoReader(_data, start, start + len);
        }

        public void Skip(int wire)
        {
            switch (wire)
            {
                case 0:
                    ReadVarint();
                    break;
                case 1:
                    Ensure(8);
                    _pos += 8;
                    break;
                case 2:
                {
                    int len = (int)ReadVarint();
                    int remain = _end - _pos;
                    if (len > remain)
                    {
                        len = remain;
                    }

                    if (len > 0)
                    {
                        _pos += len;
                    }
                    break;
                }
                case 3:
                    SkipGroup();
                    break;
                case 4:
                    break;
                case 5:
                    Ensure(4);
                    _pos += 4;
                    break;
                default:
                    throw new InvalidOperationException("Unsupported protobuf wire type " + wire);
            }
        }

        private void SkipGroup()
        {
            int field;
            int wire;
            while (TryReadTag(out field, out wire))
            {
                if (wire == 4)
                {
                    return;
                }

                Skip(wire);
            }
        }

        private void Ensure(int count)
        {
            if (_pos + count > _end)
            {
                throw new InvalidOperationException("Truncated protobuf field");
            }
        }
    }
}
