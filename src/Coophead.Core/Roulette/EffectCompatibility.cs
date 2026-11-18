using System.Collections.Generic;

namespace Coophead.Core.Roulette
{
    /// <summary>
    /// Pares de efectos que no pueden coexistir sobre el mismo jugador.
    /// Empieza vacía: la auditoría de hooks (M2/M4) decidirá qué combinaciones son incompatibles.
    /// </summary>
    public sealed class EffectCompatibility
    {
        private readonly HashSet<int> incompatiblePairs = new HashSet<int>();

        public void MarkIncompatible(PenaltyEffect a, PenaltyEffect b)
        {
            incompatiblePairs.Add(Key(a, b));
        }

        public void MarkCompatible(PenaltyEffect a, PenaltyEffect b)
        {
            incompatiblePairs.Remove(Key(a, b));
        }

        /// <summary>Un efecto nunca es compatible consigo mismo: «Castigo doble» exige dos distintos.</summary>
        public bool AreCompatible(PenaltyEffect a, PenaltyEffect b)
        {
            return a != b && !incompatiblePairs.Contains(Key(a, b));
        }

        private static int Key(PenaltyEffect a, PenaltyEffect b)
        {
            int low = (int)a < (int)b ? (int)a : (int)b;
            int high = (int)a < (int)b ? (int)b : (int)a;
            return (low * 16) + high;
        }
    }
}
