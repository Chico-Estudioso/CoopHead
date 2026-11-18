using System.Collections.Generic;

namespace Coophead.Core.Roulette
{
    /// <summary>
    /// Parámetros del director de equilibrio. La fórmula está documentada en docs/REGLAS_DE_JUEGO.md
    /// y no usa ningún modelo ni perfil oculto: solo los contadores del nivel del propio jugador.
    /// </summary>
    public sealed class BalanceConfig
    {
        /// <summary>Si es false, la ruleta usa siempre los pesos base.</summary>
        public bool Enabled { get; set; } = true;

        public float DeathWeight { get; set; } = 1f;

        public float RevivesReceivedWeight { get; set; } = 1.5f;

        public float RecentPenaltyWeight { get; set; } = 1f;

        public float ConsecutivePenaltyWeight { get; set; } = 1f;

        /// <summary>Resta presión por cada rescate realizado.</summary>
        public float RevivesGivenWeight { get; set; } = 0.5f;

        /// <summary>Resta presión por cada crédito de ayuda guardado.</summary>
        public float HelpCreditWeight { get; set; } = 0.5f;

        /// <summary>Ventana, en segundos, para contar castigos «recientes». 0 = todo el nivel.</summary>
        public float RecentPenaltyWindowSeconds { get; set; } = 60f;

        /// <summary>Presión a partir de la cual la protección es máxima (1).</summary>
        public float MaxPressure { get; set; } = 8f;

        /// <summary>Con protección máxima, el peso de «Sin penalización» se multiplica por (1 + este valor).</summary>
        public float NoPenaltyBoost { get; set; } = 2f;

        /// <summary>Con protección máxima, el peso de «Castigo doble» se multiplica por (1 − este valor).</summary>
        public float DoublePunishmentReduction { get; set; } = 1f;

        /// <summary>Con protección máxima, la duración se multiplica por (1 − este valor).</summary>
        public float DurationReduction { get; set; } = 0.5f;

        /// <summary>Duración mínima de un efecto tras aplicar la reducción.</summary>
        public float MinEffectDurationSeconds { get; set; } = 1f;

        public IList<string> Validate()
        {
            var warnings = new List<string>();
            DeathWeight = NonNegative(DeathWeight, "Peso de muertes", warnings);
            RevivesReceivedWeight = NonNegative(RevivesReceivedWeight, "Peso de reanimaciones recibidas", warnings);
            RecentPenaltyWeight = NonNegative(RecentPenaltyWeight, "Peso de castigos recientes", warnings);
            ConsecutivePenaltyWeight = NonNegative(ConsecutivePenaltyWeight, "Peso de castigos seguidos", warnings);
            RevivesGivenWeight = NonNegative(RevivesGivenWeight, "Peso de rescates realizados", warnings);
            HelpCreditWeight = NonNegative(HelpCreditWeight, "Peso de créditos guardados", warnings);
            RecentPenaltyWindowSeconds = NonNegative(RecentPenaltyWindowSeconds, "Ventana de castigos recientes", warnings);
            NoPenaltyBoost = NonNegative(NoPenaltyBoost, "Refuerzo de «Sin penalización»", warnings);
            DoublePunishmentReduction = Fraction(DoublePunishmentReduction, "Reducción de «Castigo doble»", warnings);
            DurationReduction = Fraction(DurationReduction, "Reducción de duración", warnings);
            MinEffectDurationSeconds = NonNegative(MinEffectDurationSeconds, "Duración mínima", warnings);

            if (float.IsNaN(MaxPressure) || MaxPressure <= 0f)
            {
                warnings.Add("Presión máxima debe ser positiva; se usa 8.");
                MaxPressure = 8f;
            }

            return warnings;
        }

        private static float NonNegative(float value, string name, List<string> warnings)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
            {
                warnings.Add(name + " no válido (" + value + "); se usa 0.");
                return 0f;
            }

            return value;
        }

        private static float Fraction(float value, string name, List<string> warnings)
        {
            if (float.IsNaN(value) || value < 0f || value > 1f)
            {
                float corrected = value > 1f ? 1f : 0f;
                warnings.Add(name + " fuera de [0, 1]; se usa " + corrected + ".");
                return corrected;
            }

            return value;
        }
    }
}
