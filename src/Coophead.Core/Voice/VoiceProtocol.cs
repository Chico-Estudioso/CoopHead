using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Coophead.Core.Voice
{
    public enum VoiceMessageKind
    {
        Unknown,

        /// <summary>El reconocedor está cargado y el micrófono disponible.</summary>
        Ready,

        /// <summary>No hay reconocedor o micrófono: la voz queda desactivada sin castigar a nadie.</summary>
        Unavailable,

        /// <summary>Frase reconocida con confianza suficiente.</summary>
        Heard,

        Listening,
        Stopped,
        Error,

        /// <summary>Fin de una prueba de micrófono.</summary>
        TestEnd,
    }

    public sealed class VoiceMessage
    {
        public VoiceMessage(VoiceMessageKind kind, string text, float confidence)
        {
            Kind = kind;
            Text = text ?? string.Empty;
            Confidence = confidence;
        }

        public VoiceMessageKind Kind { get; private set; }

        public string Text { get; private set; }

        public float Confidence { get; private set; }
    }

    /// <summary>
    /// Protocolo de líneas de texto entre el plugin y el ayudante de voz local (Coophead.Voice.exe) por
    /// entrada/salida estándar. Solo viajan órdenes fijas y texto reconocido: nunca audio.
    /// </summary>
    public static class VoiceProtocol
    {
        // Órdenes del plugin al ayudante.
        public const string Listen = "LISTEN";
        public const string Stop = "STOP";
        public const string Quit = "QUIT";
        public const string TestPrefix = "TEST";

        // Mensajes del ayudante al plugin.
        public const string Ready = "READY";
        public const string Unavailable = "UNAVAILABLE";
        public const string Heard = "HEARD";
        public const string Listening = "LISTENING";
        public const string Stopped = "STOPPED";
        public const string Error = "ERROR";
        public const string TestEnd = "TEST_END";

        public const float MaxTestSeconds = 15f;

        public static string Test(float seconds)
        {
            float clamped = Math.Max(1f, Math.Min(MaxTestSeconds, seconds));
            return TestPrefix + " " + clamped.ToString("0.#", CultureInfo.InvariantCulture);
        }

        /// <summary>Segundos de una orden TEST; null si la línea no es una orden TEST válida.</summary>
        public static float? ParseTest(string line)
        {
            if (line == null || !line.StartsWith(TestPrefix + " ", StringComparison.Ordinal))
            {
                return null;
            }

            float seconds;
            if (!float.TryParse(line.Substring(TestPrefix.Length + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out seconds))
            {
                return null;
            }

            return Math.Max(1f, Math.Min(MaxTestSeconds, seconds));
        }

        public static string FormatReady(string detail)
        {
            return Ready + " " + SingleLine(detail);
        }

        public static string FormatUnavailable(string reason)
        {
            return Unavailable + " " + SingleLine(reason);
        }

        public static string FormatError(string message)
        {
            return Error + " " + SingleLine(message);
        }

        public static string FormatHeard(float confidence, string text)
        {
            return Heard + " " + confidence.ToString("0.00", CultureInfo.InvariantCulture) + " " + SingleLine(text);
        }

        public static VoiceMessage Parse(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return new VoiceMessage(VoiceMessageKind.Unknown, string.Empty, 0f);
            }

            string head = line;
            string rest = string.Empty;
            int space = line.IndexOf(' ');
            if (space >= 0)
            {
                head = line.Substring(0, space);
                rest = line.Substring(space + 1);
            }

            switch (head)
            {
                case Ready:
                    return new VoiceMessage(VoiceMessageKind.Ready, rest, 0f);
                case Unavailable:
                    return new VoiceMessage(VoiceMessageKind.Unavailable, rest, 0f);
                case Listening:
                    return new VoiceMessage(VoiceMessageKind.Listening, rest, 0f);
                case Stopped:
                    return new VoiceMessage(VoiceMessageKind.Stopped, rest, 0f);
                case Error:
                    return new VoiceMessage(VoiceMessageKind.Error, rest, 0f);
                case TestEnd:
                    return new VoiceMessage(VoiceMessageKind.TestEnd, rest, 0f);
                case Heard:
                    return ParseHeard(rest);
                default:
                    return new VoiceMessage(VoiceMessageKind.Unknown, line, 0f);
            }
        }

        /// <summary>Frases separadas por «|» para pasarlas como argumento al ayudante.</summary>
        public static string JoinPhrases(IList<string> phrases)
        {
            var clean = new List<string>();
            foreach (string phrase in phrases)
            {
                string safe = SafePhrase(phrase);
                if (safe.Length > 0 && !clean.Contains(safe))
                {
                    clean.Add(safe);
                }
            }

            return string.Join("|", clean.ToArray());
        }

        public static IList<string> SplitPhrases(string joined)
        {
            var phrases = new List<string>();
            if (string.IsNullOrEmpty(joined))
            {
                return phrases;
            }

            foreach (string part in joined.Split('|'))
            {
                string safe = SafePhrase(part);
                if (safe.Length > 0 && !phrases.Contains(safe))
                {
                    phrases.Add(safe);
                }
            }

            return phrases;
        }

        /// <summary>Solo letras (con tildes), dígitos y espacios simples; nada que pueda romper la línea de órdenes.</summary>
        public static string SafePhrase(string phrase)
        {
            if (string.IsNullOrEmpty(phrase))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(phrase.Length);
            bool pendingSpace = false;
            foreach (char c in phrase.Trim().ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(c))
                {
                    if (pendingSpace && builder.Length > 0)
                    {
                        builder.Append(' ');
                    }

                    pendingSpace = false;
                    builder.Append(c);
                }
                else
                {
                    pendingSpace = true;
                }
            }

            return builder.ToString();
        }

        private static VoiceMessage ParseHeard(string rest)
        {
            int space = rest.IndexOf(' ');
            string number = space >= 0 ? rest.Substring(0, space) : rest;
            string text = space >= 0 ? rest.Substring(space + 1) : string.Empty;
            float confidence;
            if (!float.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out confidence))
            {
                return new VoiceMessage(VoiceMessageKind.Unknown, rest, 0f);
            }

            return new VoiceMessage(VoiceMessageKind.Heard, text, confidence);
        }

        private static string SingleLine(string text)
        {
            return string.IsNullOrEmpty(text) ? string.Empty : text.Replace('\r', ' ').Replace('\n', ' ');
        }
    }
}
