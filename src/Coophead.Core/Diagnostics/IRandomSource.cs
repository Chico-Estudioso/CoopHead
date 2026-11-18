using System;

namespace Coophead.Core.Diagnostics
{
    /// <summary>
    /// Fuente de aleatoriedad inyectable. Permite reproducir una incidencia con una semilla de
    /// depuración sin hacer la ruleta predecible en el uso normal.
    /// </summary>
    public interface IRandomSource
    {
        /// <summary>Número en el intervalo [0, 1).</summary>
        double NextDouble();
    }

    public sealed class SystemRandomSource : IRandomSource
    {
        private readonly Random random;

        /// <summary>Sin semilla: comportamiento normal, no reproducible.</summary>
        public SystemRandomSource()
        {
            random = new Random();
        }

        /// <summary>Con semilla: solo para depuración y pruebas.</summary>
        public SystemRandomSource(int seed)
        {
            random = new Random(seed);
        }

        public double NextDouble()
        {
            return random.NextDouble();
        }
    }
}
