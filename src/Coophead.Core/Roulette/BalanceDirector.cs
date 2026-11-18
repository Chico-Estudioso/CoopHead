using System;
using System.Globalization;
using Coophead.Core.Rules;

namespace Coophead.Core.Roulette
{
    /// <summary>Resultado transparente del cálculo de presión de un jugador.</summary>
    public sealed class PressureReport
    {
        internal PressureReport(PlayerSlot player, float pressure, float protection, BalanceConfig config, bool enabled)
        {
            Player = player;
            Pressure = pressure;
            Protection = protection;
            NoPenaltyMultiplier = 1f + (protection * config.NoPenaltyBoost);
            DoublePunishmentMultiplier = 1f - (protection * config.DoublePunishmentReduction);
            DurationMultiplier = 1f - (protection * config.DurationReduction);
            Enabled = enabled;
        }

        public PlayerSlot Player { get; private set; }

        public bool Enabled { get; private set; }

        /// <summary>Presión sin normalizar (≥ 0).</summary>
        public float Pressure { get; private set; }

        /// <summary>Protección de 0 (ninguna) a 1 (máxima) = min(presión / presión máxima, 1).</summary>
        public float Protection { get; private set; }

        public float NoPenaltyMultiplier { get; private set; }

        public float DoublePunishmentMultiplier { get; private set; }

        public float DurationMultiplier { get; private set; }

        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}: presión {1:0.##}, protección {2:0%}, sin penalización ×{3:0.##}, castigo doble ×{4:0.##}, duración ×{5:0.##}",
                Player.Label(),
                Pressure,
                Protection,
                NoPenaltyMultiplier,
                DoublePunishmentMultiplier,
                DurationMultiplier);
        }
    }

    /// <summary>
    /// Director de equilibrio adaptativo. Calcula la presión de cada jugador solo con sus propios contadores
    /// y la convierte en protección: más probabilidad de «Sin penalización», menos «Castigo doble» y efectos
    /// más cortos. Nunca aumenta el castigo de la pareja para compensar.
    /// </summary>
    /// <remarks>
    /// presión = muertes·pM + reanimacionesRecibidas·pR + castigosRecientes·pC + castigosSeguidos·pS
    ///           − rescatesRealizados·pG − créditosGuardados·pH   (mínimo 0)
    /// protección = min(presión / presiónMáxima, 1)
    /// </remarks>
    public sealed class BalanceDirector
    {
        private readonly BalanceConfig config;

        public BalanceDirector(BalanceConfig config)
        {
            this.config = config;
        }

        public PressureReport Evaluate(PlayerRoundStats stats, float now)
        {
            if (!config.Enabled)
            {
                return new PressureReport(stats.Slot, 0f, 0f, config, false);
            }

            float pressure =
                (stats.Deaths * config.DeathWeight)
                + (stats.RevivesReceived * config.RevivesReceivedWeight)
                + (stats.RecentPenalties(now, config.RecentPenaltyWindowSeconds) * config.RecentPenaltyWeight)
                + (stats.ConsecutivePenalties * config.ConsecutivePenaltyWeight)
                - (stats.RevivesGiven * config.RevivesGivenWeight)
                - (stats.HelpCredits * config.HelpCreditWeight);

            pressure = Math.Max(0f, pressure);
            float protection = Math.Min(1f, pressure / config.MaxPressure);
            return new PressureReport(stats.Slot, pressure, protection, config, true);
        }
    }
}
