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
        private static readonly Queue<string> Lines = new Queue<string>();
        private static readonly object Gate = new object();

        public static void Add(string line)
        {
            lock (Gate)
            {
                if (Lines.Count == Capacity)
                {
                    Lines.Dequeue();
                }

                Lines.Enqueue(DateTime.Now.ToString("HH:mm:ss") + "  " + line);
            }
        }

        /// <summary>De la más reciente a la más antigua.</summary>
        public static string[] Snapshot()
        {
            lock (Gate)
            {
                string[] copy = Lines.ToArray();
                Array.Reverse(copy);
                return copy;
            }
        }
    }
}
