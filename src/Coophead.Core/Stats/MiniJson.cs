using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Coophead.Core.Stats
{
    /// <summary>
    /// JSON mínimo para guardar el libro de estadísticas sin dependencias (el runtime del juego es .NET 3.5).
    /// Objetos → Dictionary&lt;string, object&gt;, listas → List&lt;object&gt;, números → double, y string, bool o null.
    /// </summary>
    public static class MiniJson
    {
        public static string Serialize(object value)
        {
            var builder = new StringBuilder();
            Write(builder, value, 0);
            return builder.ToString();
        }

        /// <summary>Analiza un texto JSON. Lanza <see cref="FormatException"/> si no es válido.</summary>
        public static object Parse(string text)
        {
            if (text == null)
            {
                throw new FormatException("Texto vacío.");
            }

            int index = 0;
            object value = ParseValue(text, ref index);
            SkipWhitespace(text, ref index);
            if (index != text.Length)
            {
                throw new FormatException("Contenido sobrante en la posición " + index + ".");
            }

            return value;
        }

        private static void Write(StringBuilder builder, object value, int indent)
        {
            if (value == null)
            {
                builder.Append("null");
                return;
            }

            var text = value as string;
            if (text != null)
            {
                WriteString(builder, text);
                return;
            }

            if (value is bool)
            {
                builder.Append((bool)value ? "true" : "false");
                return;
            }

            var map = value as IDictionary<string, object>;
            if (map != null)
            {
                builder.Append('{');
                bool first = true;
                foreach (KeyValuePair<string, object> pair in map)
                {
                    builder.Append(first ? "\n" : ",\n");
                    first = false;
                    builder.Append(' ', (indent + 1) * 2);
                    WriteString(builder, pair.Key);
                    builder.Append(": ");
                    Write(builder, pair.Value, indent + 1);
                }

                if (!first)
                {
                    builder.Append('\n').Append(' ', indent * 2);
                }

                builder.Append('}');
                return;
            }

            var list = value as IList<object>;
            if (list != null)
            {
                builder.Append('[');
                for (int i = 0; i < list.Count; i++)
                {
                    builder.Append(i == 0 ? "\n" : ",\n");
                    builder.Append(' ', (indent + 1) * 2);
                    Write(builder, list[i], indent + 1);
                }

                if (list.Count > 0)
                {
                    builder.Append('\n').Append(' ', indent * 2);
                }

                builder.Append(']');
                return;
            }

            if (value is float)
            {
                float single = (float)value;
                builder.Append(float.IsNaN(single) || float.IsInfinity(single) ? "0" : single.ToString("R", CultureInfo.InvariantCulture));
                return;
            }

            double number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            if (double.IsNaN(number) || double.IsInfinity(number))
            {
                builder.Append('0');
                return;
            }

            builder.Append(number.ToString("R", CultureInfo.InvariantCulture));
        }

        private static void WriteString(StringBuilder builder, string text)
        {
            builder.Append('"');
            foreach (char c in text)
            {
                switch (c)
                {
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        if (c < 0x20)
                        {
                            builder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            builder.Append(c);
                        }

                        break;
                }
            }

            builder.Append('"');
        }

        private static object ParseValue(string text, ref int index)
        {
            SkipWhitespace(text, ref index);
            if (index >= text.Length)
            {
                throw new FormatException("Fin inesperado.");
            }

            char c = text[index];
            switch (c)
            {
                case '{':
                    return ParseObject(text, ref index);
                case '[':
                    return ParseArray(text, ref index);
                case '"':
                    return ParseString(text, ref index);
                case 't':
                    Expect(text, ref index, "true");
                    return true;
                case 'f':
                    Expect(text, ref index, "false");
                    return false;
                case 'n':
                    Expect(text, ref index, "null");
                    return null;
                default:
                    return ParseNumber(text, ref index);
            }
        }

        private static Dictionary<string, object> ParseObject(string text, ref int index)
        {
            var result = new Dictionary<string, object>();
            index++;
            SkipWhitespace(text, ref index);
            if (index < text.Length && text[index] == '}')
            {
                index++;
                return result;
            }

            while (true)
            {
                SkipWhitespace(text, ref index);
                if (index >= text.Length || text[index] != '"')
                {
                    throw new FormatException("Se esperaba una clave en la posición " + index + ".");
                }

                string key = ParseString(text, ref index);
                SkipWhitespace(text, ref index);
                if (index >= text.Length || text[index] != ':')
                {
                    throw new FormatException("Se esperaba «:» en la posición " + index + ".");
                }

                index++;
                result[key] = ParseValue(text, ref index);
                SkipWhitespace(text, ref index);
                if (index < text.Length && text[index] == ',')
                {
                    index++;
                    continue;
                }

                if (index < text.Length && text[index] == '}')
                {
                    index++;
                    return result;
                }

                throw new FormatException("Se esperaba «,» o «}» en la posición " + index + ".");
            }
        }

        private static List<object> ParseArray(string text, ref int index)
        {
            var result = new List<object>();
            index++;
            SkipWhitespace(text, ref index);
            if (index < text.Length && text[index] == ']')
            {
                index++;
                return result;
            }

            while (true)
            {
                result.Add(ParseValue(text, ref index));
                SkipWhitespace(text, ref index);
                if (index < text.Length && text[index] == ',')
                {
                    index++;
                    continue;
                }

                if (index < text.Length && text[index] == ']')
                {
                    index++;
                    return result;
                }

                throw new FormatException("Se esperaba «,» o «]» en la posición " + index + ".");
            }
        }

        private static string ParseString(string text, ref int index)
        {
            var builder = new StringBuilder();
            index++;
            while (index < text.Length)
            {
                char c = text[index++];
                if (c == '"')
                {
                    return builder.ToString();
                }

                if (c != '\\')
                {
                    builder.Append(c);
                    continue;
                }

                if (index >= text.Length)
                {
                    break;
                }

                char escape = text[index++];
                switch (escape)
                {
                    case 'n':
                        builder.Append('\n');
                        break;
                    case 'r':
                        builder.Append('\r');
                        break;
                    case 't':
                        builder.Append('\t');
                        break;
                    case 'b':
                        builder.Append('\b');
                        break;
                    case 'f':
                        builder.Append('\f');
                        break;
                    case 'u':
                        if (index + 4 > text.Length)
                        {
                            throw new FormatException("Escape \\u incompleto.");
                        }

                        builder.Append((char)int.Parse(text.Substring(index, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                        index += 4;
                        break;
                    default:
                        builder.Append(escape);
                        break;
                }
            }

            throw new FormatException("Cadena sin cerrar.");
        }

        private static double ParseNumber(string text, ref int index)
        {
            int start = index;
            while (index < text.Length && "+-0123456789.eE".IndexOf(text[index]) >= 0)
            {
                index++;
            }

            double value;
            if (start == index || !double.TryParse(text.Substring(start, index - start), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                throw new FormatException("Número no válido en la posición " + start + ".");
            }

            return value;
        }

        private static void Expect(string text, ref int index, string word)
        {
            if (index + word.Length > text.Length || string.CompareOrdinal(text, index, word, 0, word.Length) != 0)
            {
                throw new FormatException("Se esperaba «" + word + "» en la posición " + index + ".");
            }

            index += word.Length;
        }

        private static void SkipWhitespace(string text, ref int index)
        {
            while (index < text.Length && char.IsWhiteSpace(text[index]))
            {
                index++;
            }
        }
    }
}
