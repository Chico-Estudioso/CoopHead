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
        private int[] effectCounts = new int[4];

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

        /// <summary>Agradecimientos reconocidos por voz (incluidos en <see cref="ThanksRecognized"/>).</summary>
        public int ThanksByVoice { get; internal set; }

        /// <summary>Agradecimientos confirmados con la tecla (incluidos en <see cref="ThanksRecognized"/>).</summary>
        public int ThanksManual { get; internal set; }

        // --- Estadísticas de combate (no afectan a las reglas) ---

        /// <summary>Daño infligido a enemigos, en las unidades del juego.</summary>
        public float DamageDealt { get; internal set; }

        public int Parries { get; internal set; }

        public int Supers { get; internal set; }

        public int ExMoves { get; internal set; }

        /// <summary>Golpes que le han quitado vida.</summary>
        public int HitsTaken { get; internal set; }

        /// <summary>Veces que la ruleta sacó «Castigo doble» para este jugador.</summary>
        public int DoublePunishments { get; internal set; }

        /// <summary>Veces que este jugador recibió cada efecto (los dobles cuentan sus dos efectos).</summary>
        public int GetEffectCount(PenaltyEffect effect)
        {
            return effectCounts[(int)effect];
        }

        internal void AddEffect(PenaltyEffect effect)
        {
            effectCounts[(int)effect]++;
        }

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
            copy.effectCounts = (int[])effectCounts.Clone();
            return copy;
        }
    }
}
