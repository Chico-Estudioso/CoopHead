using System.Collections.Generic;
using System.Globalization;
using Coophead.Core.Roulette;
using Coophead.Core.Rules;
using Coophead.Core.Thanks;

namespace Coophead.Core.Presentation
{
    /// <summary>Estado de la integración que el HUD necesita y el Core no conoce.</summary>
    public sealed class HudContext
    {
        /// <summary>Las reglas reciben eventos del juego (AplicarReglas activado).</summary>
        public bool RulesApplied { get; set; }

        /// <summary>Hay un reconocedor de voz funcionando.</summary>
        public bool VoiceAvailable { get; set; }

        /// <summary>El reconocedor está escuchando ahora mismo (solo durante una ventana).</summary>
        public bool VoiceListening { get; set; }

        /// <summary>Los efectos se cuentan y se muestran, pero no se aplican al juego.</summary>
        public bool EffectsSimulated { get; set; }
    }

    /// <summary>
    /// Construye el texto del HUD y del resumen de nivel. Es puro para poder probarlo; el plugin solo lo dibuja.
    /// </summary>
    public static class HudFormatter
    {
        private static readonly CultureInfo Spanish = CultureInfo.InvariantCulture;

        public static IList<string> BuildLines(CoopheadSession session, CoopheadConfig config, HudContext context, float now)
        {
            var lines = new List<string>();
            lines.Add("Coophead · " + (context.RulesApplied ? "reglas activas" : "solo registro"));

            LevelSessionState state = session.Rules.Session;
            if (state == null || !state.IsActive)
            {
                lines.Add("Esperando a que empiece un nivel");
                return lines;
            }

            foreach (PlayerSlot slot in PlayerSlots.All())
            {
                lines.Add(DescribePlayer(state.Get(slot), config.Rules.GetReviveLimit(slot)));
            }

            if (config.Rules.TeamReviveLimit > 0)
            {
                lines.Add("Equipo: " + state.TeamRevives + "/" + config.Rules.TeamReviveLimit + " reanimaciones");
            }

            if (state.RestartPending)
            {
                lines.Add("¡Límite alcanzado! (" + DescribeLimit(state.RestartReason) + ") · reiniciando");
            }
            else if (state.LimitReached != RestartReason.None)
            {
                lines.Add("¡Límite alcanzado! (" + DescribeLimit(state.LimitReached) + ") · reinicio automático desactivado");
            }

            foreach (ThanksWindow window in session.Thanks.Pending)
            {
                lines.Add(window.Rescued.Label() + ": ¡da las gracias! " + Seconds(window.Remaining(now)));
            }

            foreach (ActivePenalty penalty in session.Penalties.Active)
            {
                lines.Add(penalty.Player.Label() + ": " + penalty.Effect.DisplayName() + " " + Seconds(penalty.Remaining(now))
                    + (context.EffectsSimulated ? " (simulado)" : string.Empty));
            }

            lines.Add(DescribeThanksChannel(config, context));
            return lines;
        }

        public static IList<string> BuildSummary(LevelSummary summary)
        {
            var lines = new List<string>();
            lines.Add("Resumen · " + summary.LevelKey + " (" + DescribeEnd(summary.Reason) + ")");
            foreach (PlayerSlot slot in PlayerSlots.All())
            {
                PlayerRoundStats stats = summary.Get(slot);
                lines.Add(slot.Label() + ": daño " + stats.DamageDealt.ToString("0", Spanish)
                    + " · parrys " + stats.Parries
                    + " · muertes " + stats.Deaths
                    + " · rescates dados " + stats.RevivesGiven
                    + " · recibidos " + stats.TotalRevivesReceived
                    + " · créditos usados " + stats.CreditsConsumed
                    + " · gracias " + stats.ThanksRecognized
                    + " · castigos " + stats.PenaltiesReceived);
            }

            lines.Add("Equipo: " + summary.TeamRevives + (summary.TeamRevives == 1 ? " reanimación" : " reanimaciones")
                + (summary.DurationSeconds > 0f ? " · " + Stats.StatsComparison.FormatDuration(summary.DurationSeconds) : string.Empty));
            return lines;
        }

        private static string DescribePlayer(PlayerRoundStats stats, int limit)
        {
            string received = limit > 0 ? stats.RevivesReceived + "/" + limit : stats.RevivesReceived.ToString(Spanish);
            if (stats.TotalRevivesReceived != stats.RevivesReceived)
            {
                received += " (total " + stats.TotalRevivesReceived + ")";
            }
            string line = stats.Slot.Label() + (stats.IsAlive ? "" : " (fantasma)")
                + ": recibidas " + received
                + " · dadas " + stats.RevivesGiven
                + " · muertes " + stats.Deaths;
            if (stats.HelpCredits > 0)
            {
                line += " · crédito " + stats.HelpCredits;
            }

            return line;
        }

        private static string DescribeLimit(RestartReason reason)
        {
            return reason == RestartReason.TeamLimit ? "equipo" : "individual";
        }

        private static string DescribeThanksChannel(CoopheadConfig config, HudContext context)
        {
            if (!config.Thanks.RequireThanks)
            {
                return "Agradecimiento: no se exige";
            }

            string voice = !context.VoiceAvailable ? "voz no disponible" : context.VoiceListening ? "voz escuchando" : "voz lista";
            string manual = config.Thanks.ManualFallbackEnabled ? " · tecla activa" : string.Empty;
            return "Agradecimiento: " + voice + manual;
        }

        private static string DescribeEnd(LevelEndReason reason)
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

        private static string Seconds(float value)
        {
            return value.ToString("0.0", Spanish) + " s";
        }
    }
}
