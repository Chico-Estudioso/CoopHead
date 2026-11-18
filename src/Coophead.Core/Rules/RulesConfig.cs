using System.Collections.Generic;

namespace Coophead.Core.Rules
{
    /// <summary>
    /// Configuración del motor de reglas. Los valores por defecto son los acordados en
    /// docs/REGLAS_DE_JUEGO.md y docs/decisions/ADR-004-REVISION-DEL-KIT.md.
    /// </summary>
    public sealed class RulesConfig
    {
        public const int MaxAllowedStoredCredits = 5;

        /// <summary>Interruptor general de las reglas de reanimación.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Límite individual de reanimaciones recibidas (netas) de P1. 0 = sin límite.</summary>
        public int PlayerOneReviveLimit { get; set; } = 3;

        /// <summary>Límite individual de reanimaciones recibidas (netas) de P2. 0 = sin límite.</summary>
        public int PlayerTwoReviveLimit { get; set; } = 3;

        /// <summary>
        /// Límite de reanimaciones totales del equipo en el nivel, sin descontar créditos. 0 = sin límite.
        /// Evita que dos jugadores que se rescatan por turnos compensen siempre sus contadores.
        /// </summary>
        public int TeamReviveLimit { get; set; } = 6;

        /// <summary>Créditos de ayuda bilaterales por rescatar a la pareja.</summary>
        public bool HelpCreditsEnabled { get; set; } = true;

        /// <summary>Créditos que un jugador puede tener guardados a la vez en un nivel.</summary>
        public int MaxStoredCredits { get; set; } = 1;

        /// <summary>
        /// Si es false, al alcanzar un límite solo se avisa y no se pide reinicio.
        /// La integración debe forzarlo a false mientras el hook de reinicio no esté verificado.
        /// </summary>
        public bool AutoRestartEnabled { get; set; } = true;

        public int GetReviveLimit(PlayerSlot slot)
        {
            return slot == PlayerSlot.PlayerOne ? PlayerOneReviveLimit : PlayerTwoReviveLimit;
        }

        /// <summary>
        /// Corrige valores fuera de rango y devuelve un aviso legible por cada corrección.
        /// </summary>
        public IList<string> Validate()
        {
            var warnings = new List<string>();
            PlayerOneReviveLimit = ClampMin(PlayerOneReviveLimit, 0, "Límite de reanimaciones de P1", warnings);
            PlayerTwoReviveLimit = ClampMin(PlayerTwoReviveLimit, 0, "Límite de reanimaciones de P2", warnings);
            TeamReviveLimit = ClampMin(TeamReviveLimit, 0, "Límite de reanimaciones del equipo", warnings);

            if (MaxStoredCredits < 0 || MaxStoredCredits > MaxAllowedStoredCredits)
            {
                int corrected = MaxStoredCredits < 0 ? 0 : MaxAllowedStoredCredits;
                warnings.Add("Créditos máximos guardados fuera de rango (" + MaxStoredCredits + "); se usa " + corrected + ".");
                MaxStoredCredits = corrected;
            }

            return warnings;
        }

        private static int ClampMin(int value, int min, string name, List<string> warnings)
        {
            if (value >= min)
            {
                return value;
            }

            warnings.Add(name + " no puede ser negativo (" + value + "); se usa " + min + ".");
            return min;
        }
    }
}
