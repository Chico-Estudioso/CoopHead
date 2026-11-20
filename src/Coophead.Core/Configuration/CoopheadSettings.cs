using System;
using System.Collections.Generic;
using Coophead.Core.Roulette;
using Coophead.Core.Thanks;

namespace Coophead.Core.Configuration
{
    /// <summary>
    /// Esquema de ajustes de Coophead: secciones, claves, valores por defecto, rangos y descripciones en español.
    /// Lee los valores del sistema de configuración y los vuelca en un <see cref="CoopheadConfig"/> validado.
    /// </summary>
    /// <remarks>
    /// Los valores por defecto salen de <see cref="CoopheadConfig"/>.
    /// </remarks>
    public sealed class CoopheadSettings
    {
        public const string RulesSection = "2 - Reglas";
        public const string RouletteSection = "3 - Ruleta";
        public const string BalanceSection = "4 - Equilibrio";
        public const string ThanksSection = "5 - Agradecimiento";

        private readonly List<Action<CoopheadConfig>> appliers = new List<Action<CoopheadConfig>>();

        private CoopheadSettings()
        {
        }

        /// <summary>Clave de configuración de cada resultado de la ruleta.</summary>
        public static string OutcomeKey(RouletteOutcome outcome)
        {
            switch (outcome)
            {
                case RouletteOutcome.NoPenalty:
                    return "SinPenalizacion";
                case RouletteOutcome.SlowMovement:
                    return "MovimientoLento";
                case RouletteOutcome.InvertedControls:
                    return "ControlesInvertidos";
                case RouletteOutcome.SlowFire:
                    return "DisparoLento";
                case RouletteOutcome.LimitedDash:
                    return "DashLimitado";
                default:
                    return "CastigoDoble";
            }
        }

        public static CoopheadSettings Bind(IConfigBinder binder)
        {
            var defaults = new CoopheadConfig();
            var settings = new CoopheadSettings();
            settings.BindRules(binder, defaults);
            settings.BindRoulette(binder, defaults);
            settings.BindBalance(binder, defaults);
            settings.BindThanks(binder, defaults);
            return settings;
        }

        /// <summary>Copia los valores actuales en <paramref name="config"/> y devuelve los avisos de validación.</summary>
        public IList<string> ApplyTo(CoopheadConfig config)
        {
            foreach (Action<CoopheadConfig> apply in appliers)
            {
                apply(config);
            }

            return config.Validate();
        }

        /// <summary>Crea una configuración nueva con los valores actuales.</summary>
        public CoopheadConfig Build(out IList<string> warnings)
        {
            var config = new CoopheadConfig();
            warnings = ApplyTo(config);
            return config;
        }

        /// <summary>Lista separada por comas → lista limpia de apelativos.</summary>
        public static List<string> ParseList(string text)
        {
            var items = new List<string>();
            if (string.IsNullOrEmpty(text))
            {
                return items;
            }

            foreach (string part in text.Split(','))
            {
                string trimmed = part.Trim();
                if (trimmed.Length > 0 && !items.Contains(trimmed))
                {
                    items.Add(trimmed);
                }
            }

            return items;
        }

        private void BindRules(IConfigBinder binder, CoopheadConfig d)
        {
            var enabled = binder.Bind(RulesSection, "ReglasActivadas", d.Rules.Enabled,
                "Activa el seguimiento de muertes y reanimaciones del mod.");
            var limitP1 = binder.BindRange(RulesSection, "LimiteReanimacionesP1", d.Rules.PlayerOneReviveLimit, 0, 20,
                "Reanimaciones recibidas netas de P1 (tras créditos) que provocan reinicio. 0 = sin límite.");
            var limitP2 = binder.BindRange(RulesSection, "LimiteReanimacionesP2", d.Rules.PlayerTwoReviveLimit, 0, 20,
                "Reanimaciones recibidas netas de P2 (tras créditos) que provocan reinicio. 0 = sin límite.");
            var teamLimit = binder.BindRange(RulesSection, "LimiteReanimacionesEquipo", d.Rules.TeamReviveLimit, 0, 50,
                "Reanimaciones totales del equipo en el nivel, sin descontar créditos, que provocan reinicio. 0 = sin límite.");
            var credits = binder.Bind(RulesSection, "CreditosDeAyuda", d.Rules.HelpCreditsEnabled,
                "Rescatar a tu pareja resta una reanimación recibida propia o guarda un crédito.");
            var maxCredits = binder.BindRange(RulesSection, "CreditosMaximos", d.Rules.MaxStoredCredits, 0, 5,
                "Créditos de ayuda que se pueden guardar a la vez.");
            var autoRestart = binder.Bind(RulesSection, "ReinicioAutomatico", d.Rules.AutoRestartEnabled,
                "Reinicia el nivel al alcanzar un límite (individual o de equipo). Si está desactivado, solo se avisa en el HUD y en el log.");

            appliers.Add(c =>
            {
                c.Rules.Enabled = enabled.Value;
                c.Rules.PlayerOneReviveLimit = limitP1.Value;
                c.Rules.PlayerTwoReviveLimit = limitP2.Value;
                c.Rules.TeamReviveLimit = teamLimit.Value;
                c.Rules.HelpCreditsEnabled = credits.Value;
                c.Rules.MaxStoredCredits = maxCredits.Value;
                c.Rules.AutoRestartEnabled = autoRestart.Value;
            });
        }

        private void BindRoulette(IConfigBinder binder, CoopheadConfig d)
        {
            RouletteConfig r = d.Roulette;
            var enabled = binder.Bind(RouletteSection, "RuletaActivada", r.Enabled,
                "Si está desactivada, no se castiga nunca.");
            var duration = binder.BindRange(RouletteSection, "DuracionGeneral", r.DefaultDurationSeconds,
                RouletteConfig.MinDurationSeconds, RouletteConfig.MaxDurationSeconds,
                "Duración en segundos de los efectos temporales.");

            foreach (RouletteOutcome outcome in RouletteOutcomes.All())
            {
                RouletteOutcome current = outcome;
                string key = OutcomeKey(current);
                var outcomeEnabled = binder.Bind(RouletteSection, key + "Activado", r.Get(current).Enabled,
                    "Incluye «" + current.DisplayName() + "» en la ruleta.");
                var weight = binder.BindRange(RouletteSection, key + "Peso", r.Get(current).Weight, 0f, 1000f,
                    "Peso relativo de «" + current.DisplayName() + "». Los pesos se normalizan.");
                appliers.Add(c =>
                {
                    c.Roulette.Get(current).Enabled = outcomeEnabled.Value;
                    c.Roulette.Get(current).Weight = weight.Value;
                });

                if (current.IsSingleEffect())
                {
                    PenaltyEffect effect = current.ToEffect();
                    var effectDuration = binder.BindRange(RouletteSection, key + "Duracion", 0f, 0f, RouletteConfig.MaxDurationSeconds,
                        "Duración específica de «" + current.DisplayName() + "» en segundos. 0 = usar la duración general.");
                    appliers.Add(c =>
                    {
                        if (effectDuration.Value > 0f)
                        {
                            c.Roulette.SetDurationOverride(effect, effectDuration.Value);
                        }
                        else
                        {
                            c.Roulette.ClearDurationOverride(effect);
                        }
                    });
                }
            }

            var cooldown = binder.BindRange(RouletteSection, "Cooldown", r.CooldownSeconds, 0f, 120f,
                "Segundos mínimos entre dos castigos al mismo jugador. 0 = sin cooldown.");
            var avoidRepeats = binder.Bind(RouletteSection, "EvitarRepeticiones", r.AvoidRepeats,
                "No repetir el mismo castigo dos veces seguidas al mismo jugador si hay alternativas.");
            var maxConsecutive = binder.BindRange(RouletteSection, "MaxCastigosSeguidos", r.MaxConsecutivePenalties, 0, 10,
                "Castigos seguidos antes de una tirada protegida («Sin penalización»). 0 = sin protección.");
            var focus = binder.BindRange(RouletteSection, "FocoP2", r.FocusIntensity, 0f, 1f,
                "Intensidad del foco en P2: reduce hasta la mitad su peso de «Sin penalización». 0 = reglas idénticas para ambos.");
            var target = binder.Bind(RouletteSection, "ObjetivoCastigo", d.PenaltyTarget,
                "A quién castiga la ruleta si falta el agradecimiento: Rescued (el rescatado) o Rescuer (quien rescató).");
            var seed = binder.Bind(RouletteSection, "SemillaDepuracion", -1,
                "Solo para reproducir incidencias. -1 = aleatorio normal.");

            appliers.Add(c =>
            {
                c.Roulette.Enabled = enabled.Value;
                c.Roulette.DefaultDurationSeconds = duration.Value;
                c.Roulette.CooldownSeconds = cooldown.Value;
                c.Roulette.AvoidRepeats = avoidRepeats.Value;
                c.Roulette.MaxConsecutivePenalties = maxConsecutive.Value;
                c.Roulette.FocusedPlayer = PlayerSlot.PlayerTwo;
                c.Roulette.FocusIntensity = focus.Value;
                c.Roulette.DebugSeed = seed.Value >= 0 ? seed.Value : (int?)null;
                c.PenaltyTarget = target.Value;
            });
        }

        private void BindBalance(IConfigBinder binder, CoopheadConfig d)
        {
            var b = d.Balance;
            var enabled = binder.Bind(BalanceSection, "DirectorActivado", b.Enabled,
                "Protege a quien acumula más presión. Si se desactiva, la ruleta usa siempre los pesos base.");
            var deaths = binder.BindRange(BalanceSection, "PesoMuertes", b.DeathWeight, 0f, 10f, "Presión por cada muerte.");
            var revives = binder.BindRange(BalanceSection, "PesoReanimacionesRecibidas", b.RevivesReceivedWeight, 0f, 10f,
                "Presión por cada reanimación recibida neta.");
            var recent = binder.BindRange(BalanceSection, "PesoCastigosRecientes", b.RecentPenaltyWeight, 0f, 10f,
                "Presión por cada castigo dentro de la ventana de castigos recientes.");
            var consecutive = binder.BindRange(BalanceSection, "PesoCastigosSeguidos", b.ConsecutivePenaltyWeight, 0f, 10f,
                "Presión por cada castigo seguido.");
            var given = binder.BindRange(BalanceSection, "PesoRescatesRealizados", b.RevivesGivenWeight, 0f, 10f,
                "Presión que resta cada rescate realizado.");
            var credits = binder.BindRange(BalanceSection, "PesoCreditosGuardados", b.HelpCreditWeight, 0f, 10f,
                "Presión que resta cada crédito guardado.");
            var window = binder.BindRange(BalanceSection, "VentanaCastigosRecientes", b.RecentPenaltyWindowSeconds, 0f, 600f,
                "Segundos que cuenta un castigo como reciente. 0 = todo el nivel.");
            var maxPressure = binder.BindRange(BalanceSection, "PresionMaxima", b.MaxPressure, 0.5f, 100f,
                "Presión a partir de la cual la protección es máxima.");
            var boost = binder.BindRange(BalanceSection, "RefuerzoSinPenalizacion", b.NoPenaltyBoost, 0f, 10f,
                "Con protección máxima, el peso de «Sin penalización» se multiplica por (1 + este valor).");
            var doubleReduction = binder.BindRange(BalanceSection, "ReduccionCastigoDoble", b.DoublePunishmentReduction, 0f, 1f,
                "Con protección máxima, el peso de «Castigo doble» se multiplica por (1 − este valor).");
            var durationReduction = binder.BindRange(BalanceSection, "ReduccionDuracion", b.DurationReduction, 0f, 1f,
                "Con protección máxima, la duración se multiplica por (1 − este valor).");
            var minDuration = binder.BindRange(BalanceSection, "DuracionMinima", b.MinEffectDurationSeconds, 0f, 10f,
                "Duración mínima de un efecto tras reducirlo.");

            appliers.Add(c =>
            {
                c.Balance.Enabled = enabled.Value;
                c.Balance.DeathWeight = deaths.Value;
                c.Balance.RevivesReceivedWeight = revives.Value;
                c.Balance.RecentPenaltyWeight = recent.Value;
                c.Balance.ConsecutivePenaltyWeight = consecutive.Value;
                c.Balance.RevivesGivenWeight = given.Value;
                c.Balance.HelpCreditWeight = credits.Value;
                c.Balance.RecentPenaltyWindowSeconds = window.Value;
                c.Balance.MaxPressure = maxPressure.Value;
                c.Balance.NoPenaltyBoost = boost.Value;
                c.Balance.DoublePunishmentReduction = doubleReduction.Value;
                c.Balance.DurationReduction = durationReduction.Value;
                c.Balance.MinEffectDurationSeconds = minDuration.Value;
            });
        }

        private void BindThanks(IConfigBinder binder, CoopheadConfig d)
        {
            ThanksConfig t = d.Thanks;
            var require = binder.Bind(ThanksSection, "ExigirAgradecimiento", t.RequireThanks,
                "Tras un rescate hay que dar las gracias o se tira la ruleta. Si se desactiva, no hay ventana ni ruleta.");
            var window = binder.BindRange(ThanksSection, "VentanaSegundos", t.WindowSeconds, 1f, 15f,
                "Segundos para dar las gracias, contados desde el parry de reanimación.");
            var word = binder.Bind(ThanksSection, "PalabraObligatoria", t.RequiredWord,
                "Palabra que hay que decir. Se ignoran mayúsculas, tildes y signos.");
            var names = binder.Bind(ThanksSection, "Apelativos", string.Join(", ", t.AcceptedNames.ToArray()),
                "Apelativos separados por comas («cari, kari, amor»). Valen para ambos jugadores y mejoran el reconocimiento.");
            var requireName = binder.Bind(ThanksSection, "ExigirApelativo", t.RequireName,
                "Si se activa, hace falta «gracias» más uno de los apelativos. Si no, basta con «gracias».");
            var manual = binder.Bind(ThanksSection, "RespaldoManual", t.ManualFallbackEnabled,
                "Permite confirmar el agradecimiento con una tecla.");

            appliers.Add(c =>
            {
                c.Thanks.RequireThanks = require.Value;
                c.Thanks.WindowSeconds = window.Value;
                c.Thanks.RequiredWord = word.Value;
                c.Thanks.AcceptedNames = ParseList(names.Value);
                c.Thanks.RequireName = requireName.Value;
                c.Thanks.ManualFallbackEnabled = manual.Value;
            });
        }
    }
}
