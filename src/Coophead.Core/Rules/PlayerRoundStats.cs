using System.Collections.Generic;
using Coophead.Core.Roulette;

namespace Coophead.Core.Rules
{
    /// <summary>
    /// Estado de un jugador durante un nivel. Son contadores del mod: no sustituyen ni modifican
    /// las vidas internas de Cuphead.
    /// </summary>
    public sealed class PlayerRoundStats
    {
        private List<float> penaltyTimes = new List<float>();

        public PlayerRoundStats(PlayerSlot slot)
        {
            Slot = slot;
            IsAlive = true;
            LastPenaltyTime = float.NegativeInfinity;
        }

        public PlayerSlot Slot { get; private set; }

        /// <summary>Muertes detectadas en el nivel.</summary>
        public int Deaths { get; internal set; }

        /// <summary>
        /// Reanimaciones recibidas netas: tras consumir créditos propios y descontar las recompensas por
        /// rescatar. Es el valor que se compara con el límite individual.
        /// </summary>
        public int RevivesReceived { get; internal set; }

        /// <summary>Reanimaciones recibidas en bruto, sin créditos ni recompensas. Cuenta para el límite de equipo.</summary>
        public int TotalRevivesReceived { get; internal set; }

        /// <summary>Rescates realizados a la pareja.</summary>
        public int RevivesGiven { get; internal set; }

        /// <summary>Créditos de ayuda guardados ahora mismo.</summary>
        public int HelpCredits { get; internal set; }

        public int CreditsEarned { get; internal set; }

        public int CreditsConsumed { get; internal set; }

        /// <summary>Veces que rescatar a la pareja restó uno al contador propio.</summary>
        public int CounterReductions { get; internal set; }

        /// <summary>Resultados de ruleta con castigo (todo salvo «Sin penalización») en el nivel.</summary>
        public int PenaltiesReceived { get; internal set; }

        /// <summary>Resultados con castigo seguidos, sin un «Sin penalización» entre medias.</summary>
        public int ConsecutivePenalties { get; internal set; }

        public int ThanksRecognized { get; internal set; }

        public int ThanksMissed { get; internal set; }

        public bool IsAlive { get; internal set; }

        /// <summary>Último resultado de ruleta de este jugador en el nivel, si lo hubo.</summary>
        public RouletteOutcome? LastOutcome { get; internal set; }

        /// <summary>Momento del último castigo; -infinito si no ha habido ninguno.</summary>
        public float LastPenaltyTime { get; internal set; }

        /// <summary>Castigos recibidos en los últimos <paramref name="windowSeconds"/> segundos.</summary>
        public int RecentPenalties(float now, float windowSeconds)
        {
            if (windowSeconds <= 0f)
            {
                return PenaltiesReceived;
            }

            int count = 0;
            foreach (float time in penaltyTimes)
            {
                if (now - time <= windowSeconds)
                {
                    count++;
                }
            }

            return count;
        }

        internal void RecordRouletteOutcome(RouletteOutcome outcome, float now)
        {
            LastOutcome = outcome;
            if (outcome == RouletteOutcome.NoPenalty)
            {
                ConsecutivePenalties = 0;
                return;
            }

            PenaltiesReceived++;
            ConsecutivePenalties++;
            LastPenaltyTime = now;
            penaltyTimes.Add(now);
        }

        /// <summary>Copia independiente para resúmenes de nivel.</summary>
        public PlayerRoundStats Clone()
        {
            var copy = (PlayerRoundStats)MemberwiseClone();
            copy.penaltyTimes = new List<float>(penaltyTimes);
            return copy;
        }
    }
}
