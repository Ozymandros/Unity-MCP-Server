using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace UnityMcp.Bridge
{
    /// <summary>Minimal JSON reader/writer for Unity Editor scripts (no System.Text.Json dependency).</summary>
    public static class MiniJson
    {
        public static object Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;
            return new Parser(json).ParseValue();
        }

        public static string Serialize(object obj)
        {
            var sb = new StringBuilder();
            SerializeValue(obj, sb);
            return sb.ToString();
        }

        private static void SerializeValue(object value, StringBuilder sb)
        {
            if (value == null)
            {
                sb.Append("null");
                return;
            }

            switch (value)
            {
                case string s:
                    sb.Append('"');
                    foreach (char c in s)
                    {
                        switch (c)
                        {
                            case '\\': sb.Append("\\\\"); break;
                            case '"': sb.Append("\\\""); break;
                            case '\n': sb.Append("\\n"); break;
                            case '\r': sb.Append("\\r"); break;
                            case '\t': sb.Append("\\t"); break;
                            default: sb.Append(c); break;
                        }
                    }
                    sb.Append('"');
                    break;
                case bool b:
                    sb.Append(b ? "true" : "false");
                    break;
                case IDictionary dict:
                    sb.Append('{');
                    bool first = true;
                    foreach (DictionaryEntry entry in dict)
                    {
                        if (!first) sb.Append(',');
                        first = false;
                        SerializeValue(Convert.ToString(entry.Key), sb);
                        sb.Append(':');
                        SerializeValue(entry.Value, sb);
                    }
                    sb.Append('}');
                    break;
                case IList list:
                    sb.Append('[');
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (i > 0) sb.Append(',');
                        SerializeValue(list[i], sb);
                    }
                    sb.Append(']');
                    break;
                case IFormattable formattable:
                    sb.Append(formattable.ToString(null, CultureInfo.InvariantCulture));
                    break;
                default:
                    SerializeValue(value.ToString(), sb);
                    break;
            }
        }

        private sealed class Parser
        {
            private readonly string _json;
            private int _index;

            public Parser(string json) => _json = json;

            public object ParseValue()
            {
                SkipWs();
                if (_index >= _json.Length) return null;
                char c = _json[_index];
                if (c == '{') return ParseObject();
                if (c == '[') return ParseArray();
                if (c == '"') return ParseString();
                if (c == 't' || c == 'f') return ParseBool();
                if (c == 'n') { _index += 4; return null; }
                return ParseNumber();
            }

            private Dictionary<string, object> ParseObject()
            {
                var dict = new Dictionary<string, object>();
                _index++;
                while (true)
                {
                    SkipWs();
                    if (_index < _json.Length && _json[_index] == '}') { _index++; break; }
                    string key = ParseString();
                    SkipWs();
                    _index++; // :
                    object value = ParseValue();
                    dict[key] = value;
                    SkipWs();
                    if (_index < _json.Length && _json[_index] == ',') _index++;
                }
                return dict;
            }

            private List<object> ParseArray()
            {
                var list = new List<object>();
                _index++;
                while (true)
                {
                    SkipWs();
                    if (_index < _json.Length && _json[_index] == ']') { _index++; break; }
                    list.Add(ParseValue());
                    SkipWs();
                    if (_index < _json.Length && _json[_index] == ',') _index++;
                }
                return list;
            }

            private string ParseString()
            {
                _index++;
                var sb = new StringBuilder();
                while (_index < _json.Length)
                {
                    char c = _json[_index++];
                    if (c == '"') break;
                    if (c == '\\' && _index < _json.Length)
                    {
                        char n = _json[_index++];
                        sb.Append(n switch
                        {
                            'n' => '\n',
                            'r' => '\r',
                            't' => '\t',
                            '"' => '"',
                            '\\' => '\\',
                            _ => n
                        });
                    }
                    else sb.Append(c);
                }
                return sb.ToString();
            }

            private object ParseBool()
            {
                if (_json.AsSpan(_index).StartsWith("true")) { _index += 4; return true; }
                _index += 5;
                return false;
            }

            private object ParseNumber()
            {
                int start = _index;
                while (_index < _json.Length && "0123456789+-.eE".IndexOf(_json[_index]) >= 0)
                    _index++;
                string token = _json.Substring(start, _index - start);
                if (token.Contains('.') || token.Contains('e') || token.Contains('E'))
                    return double.Parse(token, CultureInfo.InvariantCulture);
                if (long.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out long l))
                    return l;
                return double.Parse(token, CultureInfo.InvariantCulture);
            }

            private void SkipWs()
            {
                while (_index < _json.Length && char.IsWhiteSpace(_json[_index]))
                    _index++;
            }
        }
    }
}
