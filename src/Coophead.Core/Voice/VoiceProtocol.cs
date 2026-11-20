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

        /// <summary>Frase de la gramática oída con confianza por debajo del mínimo: no cuenta, solo para ajustar.</summary>
        Low,

        /// <summary>Se oyó voz pero no se pareció a ninguna frase de la gramática.</summary>
        Rejected,

        /// <summary>Diagnóstico informativo (p. ej. cuánto audio llegó por cada salida en una prueba).</summary>
        Info,
    }

    public sealed class VoiceMessage
    {
        public VoiceMessage(VoiceMessageKind kind, string text, float confidence)
            : this(kind, text, confidence, string.Empty)
        {
        }

        public VoiceMessage(VoiceMessageKind kind, string text, float confidence, string source)
        {
            Kind = kind;
            Text = text ?? string.Empty;
            Confidence = confidence;
            Source = source ?? string.Empty;
        }

        /// <summary>Fuente de audio que reconoció la frase («mic» o «escritorio»); vacío si no se indica.</summary>
        public string Source { get; private set; }

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
        public const string ConfidencePrefix = "CONFIDENCE";

        // Mensajes del ayudante al plugin.
        public const string Ready = "READY";
        public const string Unavailable = "UNAVAILABLE";
        public const string Heard = "HEARD";
        public const string Listening = "LISTENING";
        public const string Stopped = "STOPPED";
        public const string Error = "ERROR";
        public const string TestEnd = "TEST_END";
        public const string LowConfidence = "LOW";
        public const string RejectedSpeech = "REJECTED";
        public const string InfoMessage = "INFO";

        public const float MaxTestSeconds = 15f;

        // Fuentes de audio del ayudante.
        public const string SourceMicrophone = "mic";
        public const string SourceDesktop = "escritorio";

        public static string Test(float seconds)
        {
            float clamped = Math.Max(1f, Math.Min(MaxTestSeconds, seconds));
            return TestPrefix + " " + clamped.ToString("0.#", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// «CONFIDENCE 0.60 0.50»: confianza mínima del micrófono y del escritorio. Se envía al cambiarla en F6 o en la
        /// configuración, para que se aplique sin reiniciar el juego.
        /// </summary>
        public static string Confidence(float microphone, float desktop)
        {
            return ConfidencePrefix + " " + ClampConfidence(microphone).ToString("0.00", CultureInfo.InvariantCulture)
                + " " + ClampConfidence(desktop).ToString("0.00", CultureInfo.InvariantCulture);
        }

        /// <summary>Lee una orden CONFIDENCE; false si la línea no lo es o no tiene dos números válidos.</summary>
        public static bool TryParseConfidence(string line, out float microphone, out float desktop)
        {
            microphone = 0f;
            desktop = 0f;
            if (line == null || !line.StartsWith(ConfidencePrefix + " ", StringComparison.Ordinal))
            {
                return false;
            }

            string[] parts = line.Substring(ConfidencePrefix.Length + 1).Split(' ');
            if (parts.Length != 2
                || !float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out microphone)
                || !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out desktop))
            {
                return false;
            }

            microphone = ClampConfidence(microphone);
            desktop = ClampConfidence(desktop);
            return true;
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

        /// <summary>«INFO escritorio1 = Auriculares: llegó audio 4.9 s…»: diagnóstico sin efecto en las reglas.</summary>
        public static string FormatInfo(string message)
        {
            return InfoMessage + " " + SingleLine(message);
        }

        public static string FormatError(string message)
        {
            return Error + " " + SingleLine(message);
        }

        public static string FormatHeard(float confidence, string text)
        {
            return Heard + " " + confidence.ToString("0.00", CultureInfo.InvariantCulture) + " " + SingleLine(text);
        }

        /// <summary>«HEARD 0.82 escritorio gracias cari»: incluye la fuente de audio.</summary>
        public static string FormatHeard(float confidence, string text, string source)
        {
            return Heard + " " + confidence.ToString("0.00", CultureInfo.InvariantCulture) + " " + SingleLine(source) + " " + SingleLine(text);
        }

        /// <summary>«LOW 0.41 escritorio gracias cari»: frase reconocida pero por debajo de la confianza mínima.</summary>
        public static string FormatLow(float confidence, string text, string source)
        {
            return LowConfidence + " " + confidence.ToString("0.00", CultureInfo.InvariantCulture) + " " + SingleLine(source) + " " + SingleLine(text);
        }

        /// <summary>«REJECTED escritorio»: se oyó voz que no encaja con la gramática.</summary>
        public static string FormatRejected(string source)
        {
            return RejectedSpeech + " " + SingleLine(source);
        }

        /// <summary>«mic», «escritorio» o «escritorio» seguido de un número (una salida de audio concreta).</summary>
        public static bool IsKnownSource(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return false;
            }

            if (source == SourceMicrophone || source == SourceDesktop)
            {
                return true;
            }

            if (!source.StartsWith(SourceDesktop, StringComparison.Ordinal) || source.Length == SourceDesktop.Length)
            {
                return false;
            }

            for (int i = SourceDesktop.Length; i < source.Length; i++)
            {
                if (source[i] < '0' || source[i] > '9')
                {
                    return false;
                }
            }

            return true;
        }

        public static VoiceMessage Parse(string line)
        {
            line = CleanLine(line);
            if (line.Length == 0)
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
                    return ParseHeard(rest, VoiceMessageKind.Heard);
                case LowConfidence:
                    return ParseHeard(rest, VoiceMessageKind.Low);
                case RejectedSpeech:
                    return new VoiceMessage(VoiceMessageKind.Rejected, string.Empty, 0f, rest);
                case InfoMessage:
                    return new VoiceMessage(VoiceMessageKind.Info, rest, 0f);
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

        /// <summary>
        /// Quita marcas de orden de bytes (BOM), retornos de carro y espacios de los extremos. Al redirigir la entrada o
        /// la salida estándar en Windows, la primera línea puede llegar con un BOM invisible y cada línea con «\r».
        /// </summary>
        public static string CleanLine(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return string.Empty;
            }

            return line.Trim().Trim('\uFEFF', '\u200B', '\r', '\n').Trim();
        }

        private static VoiceMessage ParseHeard(string rest, VoiceMessageKind kind)
        {
            int space = rest.IndexOf(' ');
            string number = space >= 0 ? rest.Substring(0, space) : rest;
            string text = space >= 0 ? rest.Substring(space + 1) : string.Empty;
            float confidence;
            if (!float.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out confidence))
            {
                return new VoiceMessage(VoiceMessageKind.Unknown, rest, 0f);
            }

            string source = string.Empty;
            int next = text.IndexOf(' ');
            string first = next >= 0 ? text.Substring(0, next) : text;
            if (IsKnownSource(first))
            {
                source = first;
                text = next >= 0 ? text.Substring(next + 1) : string.Empty;
            }

            return new VoiceMessage(kind, text, confidence, source);
        }

        private static float ClampConfidence(float value)
        {
            if (float.IsNaN(value))
            {
                return 1f;
            }

            return Math.Max(0.05f, Math.Min(1f, value));
        }

        private static string SingleLine(string text)
        {
            return string.IsNullOrEmpty(text) ? string.Empty : text.Replace('\r', ' ').Replace('\n', ' ');
        }
    }
}
