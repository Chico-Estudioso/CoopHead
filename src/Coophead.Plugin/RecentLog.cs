using System;
using System.Collections.Generic;

namespace Coophead.Plugin
{
    /// <summary>Últimas líneas del log de Coophead para la pestaña «Registro» de la ventana.</summary>
    internal static class RecentLog
    {
        private const int Capacity = 80;
        private static readonly Queue<string> Lines = new Queue<string>();
        private static readonly object Gate = new object();

        public static void Add(string level, string message)
        {
            lock (Gate)
            {
                if (Lines.Count == Capacity)
                {
                    Lines.Dequeue();
                }

                Lines.Enqueue(DateTime.Now.ToString("HH:mm:ss") + " " + level + " " + message);
            }
        }

        /// <summary>Copia de las líneas, de la más reciente a la más antigua.</summary>
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
