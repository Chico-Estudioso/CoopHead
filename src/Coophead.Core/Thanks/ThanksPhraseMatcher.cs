using System;
using System.Collections.Generic;
using System.Text;

namespace Coophead.Core.Thanks
{
    /// <summary>
    /// Decide si un texto reconocido cuenta como agradecimiento. Trabaja sobre texto ya transcrito en memoria:
    /// no captura, guarda ni transmite audio.
    /// </summary>
    public sealed class ThanksPhraseMatcher
    {
        private readonly ThanksConfig config;

        public ThanksPhraseMatcher(ThanksConfig config)
        {
            this.config = config;
        }

        /// <summary>
        /// Minúsculas, sin tildes ni signos, con espacios simples. «¡Gracias, Kari!» → «gracias kari».
        /// </summary>
        /// <remarks>
        /// Se usa una tabla propia de equivalencias en lugar de string.Normalize para no depender de las tablas
        /// Unicode del runtime Mono que incluye el juego.
        /// </remarks>
        public static string Normalize(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(text.Length);
            bool pendingSpace = false;
            foreach (char raw in text)
            {
                char c = Fold(char.ToLowerInvariant(raw));
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

        private static char Fold(char c)
        {
            switch (c)
            {
                case 'á':
                case 'à':
                case 'â':
                case 'ä':
                    return 'a';
                case 'é':
                case 'è':
                case 'ê':
                case 'ë':
                    return 'e';
                case 'í':
                case 'ì':
                case 'î':
                case 'ï':
                    return 'i';
                case 'ó':
                case 'ò':
                case 'ô':
                case 'ö':
                    return 'o';
                case 'ú':
                case 'ù':
                case 'û':
                case 'ü':
                    return 'u';
                case 'ñ':
                    return 'n';
                case 'ç':
                    return 'c';
                default:
                    return c;
            }
        }

        public bool Matches(string transcript)
        {
            string[] tokens = Tokenize(transcript);
            string[] required = Tokenize(config.RequiredWord);
            if (tokens.Length == 0 || required.Length == 0)
            {
                return false;
            }

            for (int start = 0; start + required.Length <= tokens.Length; start++)
            {
                if (!SequenceAt(tokens, start, required))
                {
                    continue;
                }

                if (!config.RequireName)
                {
                    return true;
                }

                foreach (string name in config.AcceptedNames)
                {
                    string[] nameTokens = Tokenize(name);
                    if (nameTokens.Length == 0)
                    {
                        continue;
                    }

                    bool after = SequenceAt(tokens, start + required.Length, nameTokens);
                    bool before = start - nameTokens.Length >= 0 && SequenceAt(tokens, start - nameTokens.Length, nameTokens);
                    if (after || before)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Frases para un reconocedor con gramática cerrada (M5): «gracias» y «gracias {apelativo}».
        /// </summary>
        public IList<string> BuildGrammar()
        {
            var phrases = new List<string>();
            string required = Normalize(config.RequiredWord);
            if (!config.RequireName)
            {
                phrases.Add(required);
            }

            foreach (string name in config.AcceptedNames)
            {
                string normalized = Normalize(name);
                if (normalized.Length > 0 && !phrases.Contains(required + " " + normalized))
                {
                    phrases.Add(required + " " + normalized);
                }
            }

            return phrases;
        }

        private static string[] Tokenize(string text)
        {
            string normalized = Normalize(text);
            return normalized.Length == 0
                ? new string[0]
                : normalized.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static bool SequenceAt(string[] tokens, int start, string[] sequence)
        {
            if (start < 0 || start + sequence.Length > tokens.Length)
            {
                return false;
            }

            for (int i = 0; i < sequence.Length; i++)
            {
                if (tokens[start + i] != sequence[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
