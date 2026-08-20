using System;
using System.Text;
using UnityEngine;

namespace SumeruAI.ATF
{
    public sealed class AtfProtobufResult
    {
        public int Code;
        public string Message;
        public float Fps;
        public int NumFrames;
        public byte[] Audio;
        public byte[] Blendshapes;
    }

    /// <summary>
    /// Unary protobuf for ATF mesh:
    /// code=1, message=2, data=3 { fps=1, num_frames=2, audio=3, blendshapes=4, flag=5 }.
    /// </summary>
    public static class AtfProtobufParser
    {
        public static AtfProtobufResult Parse(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return new AtfProtobufResult();
            }

            Debug.Log("[ATF] protobuf dump:" + Dump(data));

            AtfProtobufResult result = new AtfProtobufResult();
            ReadMessage(data, 0, data.Length, result, 0);

            Debug.Log(
                $"[ATF] extracted code={result.Code}, message={result.Message}, fps={result.Fps}, num_frames={result.NumFrames}, audio={(result.Audio != null ? result.Audio.Length : 0)}, blendshapes={(result.Blendshapes != null ? result.Blendshapes.Length : 0)}");

            return result;
        }

        public static string DescribeHead(byte[] data)
        {
            return Hex(data, 32);
        }

        private static void ReadMessage(byte[] data, int start, int end, AtfProtobufResult result, int depth)
        {
            ProtoReader reader = new ProtoReader(data, start, end);
            int field;
            int wire;
            while (reader.TryReadTag(out field, out wire))
            {
                if (field < 1 || wire == 3 || wire == 4 || wire == 6 || wire == 7)
                {
                    break;
                }

                if (wire == 0)
                {
                    int value = (int)reader.ReadVarint();
                    if (depth == 0 && field == 1)
                    {
                        result.Code = value;
                    }
                    else if (depth == 1)
                    {
                        if (field == 1)
                        {
                            result.Fps = value;
                        }
                        else if (field == 2)
                        {
                            result.NumFrames = value;
                        }
                    }

                    continue;
                }

                if (wire == 1)
                {
                    double value = reader.ReadDouble();
                    if (depth == 1 && field == 1)
                    {
                        result.Fps = (float)value;
                    }

                    continue;
                }

                if (wire == 5)
                {
                    float value = reader.ReadFloat();
                    if (depth == 1 && field == 1)
                    {
                        result.Fps = value;
                    }

                    continue;
                }

                if (wire != 2)
                {
                    reader.Skip(wire);
                    continue;
                }

                byte[] raw = reader.ReadBytes();
                if (depth == 0)
                {
                    if (field == 2)
                    {
                        result.Message = Encoding.UTF8.GetString(raw);
                    }
                    else if (field == 3)
                    {
                        ReadMessage(raw, 0, raw.Length, result, 1);
                    }
                }
                else if (field == 3)
                {
                    result.Audio = raw;
                }
                else if (field == 4)
                {
                    result.Blendshapes = raw;
                }
            }
        }

        public static string Dump(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();
            Dump(data, 0, data.Length, 0, sb);
            return sb.ToString();
        }

        private static void Dump(byte[] data, int start, int end, int depth, StringBuilder sb)
        {
            ProtoReader reader = new ProtoReader(data, start, end);
            int field;
            int wire;
            int lastField = 0;
            while (reader.TryReadTag(out field, out wire))
            {
                sb.Append('\n');
                Indent(sb, depth);
                if (lastField > 0 && field > lastField + 1)
                {
                    sb.Append(FieldName(depth, lastField + 1))
                        .Append(" missing (not present in this response)\n");
                    Indent(sb, depth);
                }

                sb.Append(FieldName(depth, field)).Append(" wire=").Append(wire).Append(' ');
                if (wire == 0)
                {
                    sb.Append("varint=").Append(reader.ReadVarint());
                }
                else if (wire == 1)
                {
                    sb.Append("f64=").Append(reader.ReadDouble().ToString("0.###"));
                }
                else if (wire == 5)
                {
                    sb.Append("f32=").Append(reader.ReadFloat().ToString("0.###"));
                }
                else if (wire == 2)
                {
                    byte[] raw = reader.ReadBytes();
                    sb.Append("bytes len=").Append(raw.Length).Append(" head=").Append(Hex(raw, 24));
                    if (raw.Length <= 128)
                    {
                        try
                        {
                            sb.Append(" text=").Append(Encoding.UTF8.GetString(raw).Replace('\n', ' '));
                        }
                        catch
                        {
                        }
                    }

                    if (depth == 0 && field == 3)
                    {
                        Dump(raw, 0, raw.Length, 1, sb);
                    }
                }
                else
                {
                    reader.Skip(wire);
                    sb.Append("skipped");
                }

                lastField = field;
            }

            sb.Append('\n');
            Indent(sb, depth);
            sb.Append("end unread=").Append(reader.Remaining).Append(" bytes");
        }

        private static string FieldName(int depth, int field)
        {
            if (depth == 0)
            {
                switch (field)
                {
                    case 1: return "field=1(code)";
                    case 2: return "field=2(message)";
                    case 3: return "field=3(data)";
                }
            }
            else
            {
                switch (field)
                {
                    case 1: return "field=1(fps)";
                    case 2: return "field=2(num_frames)";
                    case 3: return "field=3(audio)";
                    case 4: return "field=4(blendshapes)";
                    case 5: return "field=5(flag)";
                }
            }

            return "field=" + field;
        }

        private static void Indent(StringBuilder sb, int depth)
        {
            for (int i = 0; i < depth; i++)
            {
                sb.Append("  ");
            }
        }

        private static string Hex(byte[] data, int max)
        {
            if (data == null || data.Length == 0)
            {
                return string.Empty;
            }

            int n = Math.Min(data.Length, max);
            StringBuilder sb = new StringBuilder(n * 3);
            for (int i = 0; i < n; i++)
            {
                if (i > 0)
                {
                    sb.Append(' ');
                }

                sb.Append(data[i].ToString("x2"));
            }

            if (data.Length > max)
            {
                sb.Append(" ...");
            }

            return sb.ToString();
        }

        private sealed class ProtoReader
        {
            private readonly byte[] _data;
            private int _pos;
            private readonly int _end;

            public ProtoReader(byte[] data, int start, int end)
            {
                _data = data;
                _pos = start;
                _end = end;
            }

            public int Remaining
            {
                get { return _end - _pos; }
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

            public float ReadFloat()
            {
                Ensure(4);
                float value = BitConverter.ToSingle(_data, _pos);
                _pos += 4;
                return value;
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
                    case 5:
                        Ensure(4);
                        _pos += 4;
                        break;
                    default:
                        throw new InvalidOperationException("Unsupported protobuf wire type " + wire);
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
}
