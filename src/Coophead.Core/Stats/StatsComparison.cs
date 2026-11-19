using System.Collections.Generic;
using System.Globalization;
using Coophead.Core.Roulette;

namespace Coophead.Core.Stats
{
    /// <summary>Una fila de la comparativa P1 contra P2.</summary>
    public sealed class ComparisonRow
    {
        internal ComparisonRow(string label, string playerOne, string playerTwo, PlayerSlot? leader)
        {
            Label = label;
            PlayerOne = playerOne;
            PlayerTwo = playerTwo;
            Leader = leader;
        }

        public string Label { get; private set; }

        public string PlayerOne { get; private set; }

        public string PlayerTwo { get; private set; }

        /// <summary>Quién tiene más en esta estadística; null si empatan.</summary>
        public PlayerSlot? Leader { get; private set; }
    }

    /// <summary>Construye la comparativa entre los dos jugadores para el libro de recuerdos.</summary>
    public static class StatsComparison
    {
        private static readonly CultureInfo Spanish = CultureInfo.InvariantCulture;

        public static IList<ComparisonRow> Build(PlayerTotals p1, PlayerTotals p2)
        {
            var rows = new List<ComparisonRow>();
            Add(rows, "Daño infligido", p1.DamageDealt, p2.DamageDealt);
            Add(rows, "Parrys", p1.Parries, p2.Parries);
            Add(rows, "Súper", p1.Supers, p2.Supers);
            Add(rows, "EX", p1.ExMoves, p2.ExMoves);
            Add(rows, "Golpes recibidos", p1.HitsTaken, p2.HitsTaken);
            Add(rows, "Muertes", p1.Deaths, p2.Deaths);
            Add(rows, "Rescates dados", p1.RevivesGiven, p2.RevivesGiven);
            Add(rows, "Reanimaciones recibidas", p1.RevivesReceived, p2.RevivesReceived);
            Add(rows, "Gracias dadas", p1.ThanksTotal, p2.ThanksTotal);
            Add(rows, "  · por voz", p1.ThanksVoice, p2.ThanksVoice);
            Add(rows, "  · con tecla", p1.ThanksManual, p2.ThanksManual);
            Add(rows, "Gracias olvidadas", p1.ThanksMissed, p2.ThanksMissed);
            Add(rows, "Castigos recibidos", p1.Penalties, p2.Penalties);
            foreach (PenaltyEffect effect in RouletteOutcomes.AllEffects())
            {
                Add(rows, "  · " + effect.DisplayName(), p1.EffectCounts[(int)effect], p2.EffectCounts[(int)effect]);
            }

            Add(rows, "Castigos dobles", p1.DoublePunishments, p2.DoublePunishments);
            Add(rows, "Créditos usados", p1.CreditsConsumed, p2.CreditsConsumed);
            return rows;
        }

        public static string FormatDuration(float seconds)
        {
            if (seconds <= 0f)
            {
                return "—";
            }

            int total = (int)(seconds + 0.5f);
            int hours = total / 3600;
            int minutes = (total % 3600) / 60;
            int rest = total % 60;
            return hours > 0
                ? string.Format(Spanish, "{0}:{1:00}:{2:00}", hours, minutes, rest)
                : string.Format(Spanish, "{0}:{1:00}", minutes, rest);
        }

        private static void Add(List<ComparisonRow> rows, string label, double p1, double p2)
        {
            PlayerSlot? leader = null;
            if (p1 > p2)
            {
                leader = PlayerSlot.PlayerOne;
            }
            else if (p2 > p1)
            {
                leader = PlayerSlot.PlayerTwo;
            }

            rows.Add(new ComparisonRow(label, Format(p1), Format(p2), leader));
        }

        private static string Format(double value)
        {
            return value == System.Math.Floor(value)
                ? ((long)value).ToString(Spanish)
                : value.ToString("0.#", Spanish);
        }
    }
}
