using System;
using System.Collections.Generic;

namespace Coophead.Plugin
{
    /// <summary>
    /// Últimas frases que ha oído el reconocedor (aceptadas, dudosas y no entendidas), para ajustar la voz desde la
    /// ventana F6. Solo texto ya reconocido de la gramática, nunca audio; se borra al cerrar el juego.
    /// </summary>
    internal static class VoiceLog
    {
        private const int Capacity = 15;
        private static readonly List<Entry> Lines = new List<Entry>();
        private static readonly object Gate = new object();

        /// <summary>
        /// Añade una línea. Si es igual que la anterior (p. ej. la música del juego rechazada una y otra vez), solo
        /// suma «(×N)» para no tapar las líneas útiles.
        /// </summary>
        public static void Add(string line)
        {
            lock (Gate)
            {
                string time = DateTime.Now.ToString("HH:mm:ss");
                if (Lines.Count > 0 && Lines[Lines.Count - 1].Text == line)
                {
                    Entry last = Lines[Lines.Count - 1];
                    last.Count++;
                    last.Time = time;
                    return;
                }

                if (Lines.Count == Capacity)
                {
                    Lines.RemoveAt(0);
                }

                Lines.Add(new Entry { Text = line, Time = time, Count = 1 });
            }
        }

        /// <summary>De la más reciente a la más antigua.</summary>
        public static string[] Snapshot()
        {
            lock (Gate)
            {
                var copy = new string[Lines.Count];
                for (int i = 0; i < Lines.Count; i++)
                {
                    Entry entry = Lines[Lines.Count - 1 - i];
                    copy[i] = entry.Time + "  " + entry.Text + (entry.Count > 1 ? "  (×" + entry.Count + ")" : string.Empty);
                }

                return copy;
            }
        }

        private sealed class Entry
        {
            public string Text;
            public string Time;
            public int Count;
        }
    }
}
