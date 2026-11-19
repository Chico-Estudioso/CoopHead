using System;
using System.Collections.Generic;
using System.Globalization;
using Coophead.Core.Roulette;
using Coophead.Core.Rules;
using Coophead.Core.Stats;

namespace Coophead.Core.Presentation
{
    /// <summary>Probabilidad actual de un resultado de la ruleta para cada jugador.</summary>
    public sealed class RouletteRow
    {
        internal RouletteRow(RouletteOutcome outcome, bool enabled, float weight, float playerOne, float playerTwo)
        {
            Outcome = outcome;
            Enabled = enabled;
            Weight = weight;
            ProbabilityPlayerOne = playerOne;
            ProbabilityPlayerTwo = playerTwo;
        }

        public RouletteOutcome Outcome { get; private set; }

        public bool Enabled { get; private set; }

        public float Weight { get; private set; }

        /// <summary>Probabilidad de 0 a 1 en la próxima tirada de P1 (sin contar cooldown ni castigos seguidos).</summary>
        public float ProbabilityPlayerOne { get; private set; }

        public float ProbabilityPlayerTwo { get; private set; }
    }

    /// <summary>Textos y tablas de la ventana de Coophead. Puro para poder probarlo; el plugin solo los dibuja.</summary>
    public static class BookViews
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        /// <summary>Probabilidades de la próxima tirada con la presión actual de cada jugador.</summary>
        public static IList<RouletteRow> Roulette(CoopheadSession session, CoopheadConfig config, float now)
        {
            IDictionary<RouletteOutcome, float> p1 = Probabilities(session, PlayerSlot.PlayerOne, now);
            IDictionary<RouletteOutcome, float> p2 = Probabilities(session, PlayerSlot.PlayerTwo, now);
            var rows = new List<RouletteRow>();
            foreach (RouletteOutcome outcome in RouletteOutcomes.All())
            {
                OutcomeSettings settings = config.Roulette.Get(outcome);
                float a;
                float b;
                p1.TryGetValue(outcome, out a);
                p2.TryGetValue(outcome, out b);
                rows.Add(new RouletteRow(outcome, settings.Enabled, settings.Weight, a, b));
            }

            return rows;
        }

        /// <summary>Explicación de la presión de cada jugador en el intento actual.</summary>
        public static string Pressure(CoopheadSession session, PlayerSlot slot, float now)
        {
            return session.Roulette.Director.Evaluate(CurrentStats(session, slot), now).ToString();
        }

        /// <summary>Comparativa del intento en curso; null si no hay nivel activo.</summary>
        public static IList<ComparisonRow> CurrentAttempt(CoopheadSession session)
        {
            LevelSessionState state = session.Rules.Session;
            if (state == null || !state.IsActive)
            {
                return null;
            }

            return StatsComparison.Build(
                PlayerTotals.From(state.Get(PlayerSlot.PlayerOne)),
                PlayerTotals.From(state.Get(PlayerSlot.PlayerTwo)));
        }

        public static IList<string> RecordHeader(BossRecord record)
        {
            return new List<string>
            {
                "Intentos " + record.Attempts + " · Victorias " + record.Wins + " · Derrotas " + record.Losses
                    + " · Reinicios " + record.Restarts + " · Salidas " + record.Exits,
                "Tiempo jugado " + StatsComparison.FormatDuration(record.TotalPlaySeconds)
                    + " · Mejor victoria " + StatsComparison.FormatDuration(record.BestWinSeconds),
                "Primera vez " + FormatDate(record.FirstPlayedUtc) + " · Última vez " + FormatDate(record.LastPlayedUtc)
                    + " · Primera victoria " + FormatDate(record.FirstWinUtc),
            };
        }

        public static string AttemptHeader(AttemptRecord attempt)
        {
            if (attempt == null)
            {
                return "Todavía no hay ninguno.";
            }

            return FormatDate(attempt.WhenUtc) + " · " + DescribeEnd(attempt.Reason) + " · " + StatsComparison.FormatDuration(attempt.DurationSeconds);
        }

        public static IList<string> BookHeader(StatsBook book)
        {
            return new List<string>
            {
                "Niveles " + book.Count + " · Intentos " + book.TotalAttempts + " · Victorias " + book.TotalWins,
                "Tiempo jugado " + StatsComparison.FormatDuration(book.TotalPlaySeconds),
            };
        }

        /// <summary>«2026-11-19T20:05:00Z» → «19/11/2026 21:05» en la hora local; «—» si está vacío o no se entiende.</summary>
        public static string FormatDate(string isoUtc)
        {
            DateTime value;
            if (string.IsNullOrEmpty(isoUtc)
                || !DateTime.TryParseExact(isoUtc, "yyyy-MM-ddTHH:mm:ssZ", Invariant, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out value))
            {
                return "—";
            }

            return value.ToLocalTime().ToString("dd/MM/yyyy HH:mm", Invariant);
        }

        public static string DescribeEnd(LevelEndReason reason)
        {
            switch (reason)
            {
                case LevelEndReason.Won:
                    return "victoria";
                case LevelEndReason.Lost:
                    return "derrota";
                case LevelEndReason.Restarted:
                    return "reinicio";
                case LevelEndReason.Exited:
                    return "salida";
                default:
                    return "fin";
            }
        }

        public static string Percent(float probability)
        {
            return (probability * 100f).ToString("0", Invariant) + " %";
        }

        private static IDictionary<RouletteOutcome, float> Probabilities(CoopheadSession session, PlayerSlot slot, float now)
        {
            PlayerRoundStats stats = CurrentStats(session, slot);
            PressureReport pressure = session.Roulette.Director.Evaluate(stats, now);
            IDictionary<RouletteOutcome, float> weights = session.Roulette.ComputeWeights(stats, pressure);
            float total = 0f;
            foreach (float weight in weights.Values)
            {
                total += weight;
            }

            var result = new Dictionary<RouletteOutcome, float>();
            if (total > 0f)
            {
                foreach (KeyValuePair<RouletteOutcome, float> pair in weights)
                {
                    result[pair.Key] = pair.Value / total;
                }
            }

            return result;
        }

        private static PlayerRoundStats CurrentStats(CoopheadSession session, PlayerSlot slot)
        {
            LevelSessionState state = session.Rules.Session;
            return state != null && state.IsActive ? state.Get(slot) : new PlayerRoundStats(slot);
        }
    }
}
