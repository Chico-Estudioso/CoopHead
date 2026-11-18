using System.Collections.Generic;

namespace Coophead.Core.Roulette
{
    /// <summary>Activación y peso de un resultado de la ruleta.</summary>
    public sealed class OutcomeSettings
    {
        public OutcomeSettings(bool enabled, float weight)
        {
            Enabled = enabled;
            Weight = weight;
        }

        public bool Enabled { get; set; }

        /// <summary>Peso relativo; los pesos se normalizan para obtener probabilidades.</summary>
        public float Weight { get; set; }
    }

    /// <summary>Configuración de la ruleta de penalizaciones (docs/REGLAS_DE_JUEGO.md).</summary>
    public sealed class RouletteConfig
    {
        public const float MinDurationSeconds = 0.5f;
        public const float MaxDurationSeconds = 30f;

        private readonly Dictionary<RouletteOutcome, OutcomeSettings> outcomes = new Dictionary<RouletteOutcome, OutcomeSettings>();
        private readonly Dictionary<PenaltyEffect, float> durationOverrides = new Dictionary<PenaltyEffect, float>();

        public RouletteConfig()
        {
            // Pesos orientativos iniciales del kit: son valores editables, no un balance definitivo.
            outcomes[RouletteOutcome.SlowMovement] = new OutcomeSettings(true, 20f);
            outcomes[RouletteOutcome.InvertedControls] = new OutcomeSettings(true, 15f);
            outcomes[RouletteOutcome.SlowFire] = new OutcomeSettings(true, 15f);
            outcomes[RouletteOutcome.NoPenalty] = new OutcomeSettings(true, 20f);
            outcomes[RouletteOutcome.LimitedDash] = new OutcomeSettings(true, 15f);
            outcomes[RouletteOutcome.DoublePunishment] = new OutcomeSettings(true, 15f);
            Compatibility = new EffectCompatibility();
        }

        /// <summary>Si es false, nunca se aplica castigo.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Duración general de los efectos temporales.</summary>
        public float DefaultDurationSeconds { get; set; } = 4f;

        /// <summary>
        /// Tiempo mínimo entre dos castigos al mismo jugador. Dentro de ese margen la tirada sale
        /// «Sin penalización» de forma protegida. 0 = sin cooldown.
        /// </summary>
        public float CooldownSeconds { get; set; } = 8f;

        /// <summary>Evita repetir el mismo castigo dos veces seguidas al mismo jugador si hay alternativas.</summary>
        public bool AvoidRepeats { get; set; } = true;

        /// <summary>
        /// Castigos seguidos permitidos antes de forzar una tirada protegida («Sin penalización»). 0 = sin protección.
        /// </summary>
        public int MaxConsecutivePenalties { get; set; } = 2;

        /// <summary>Jugador que recibe el foco opcional. Por defecto P2.</summary>
        public PlayerSlot FocusedPlayer { get; set; } = PlayerSlot.PlayerTwo;

        /// <summary>
        /// Intensidad del foco, de 0 a 1. Reduce el peso de «Sin penalización» del jugador con foco hasta la mitad
        /// (×(1 − 0,5·intensidad)). 0 por defecto: reglas idénticas para P1 y P2.
        /// </summary>
        public float FocusIntensity { get; set; }

        /// <summary>Semilla solo para reproducir incidencias. Null en uso normal.</summary>
        public int? DebugSeed { get; set; }

        public EffectCompatibility Compatibility { get; private set; }

        public OutcomeSettings Get(RouletteOutcome outcome)
        {
            return outcomes[outcome];
        }

        public void SetDurationOverride(PenaltyEffect effect, float seconds)
        {
            durationOverrides[effect] = seconds;
        }

        public void ClearDurationOverride(PenaltyEffect effect)
        {
            durationOverrides.Remove(effect);
        }

        /// <summary>Duración base de un efecto: la específica si existe y es positiva; si no, la general.</summary>
        public float GetBaseDuration(PenaltyEffect effect)
        {
            float seconds;
            if (durationOverrides.TryGetValue(effect, out seconds) && seconds > 0f)
            {
                return seconds;
            }

            return DefaultDurationSeconds;
        }

        public IList<string> Validate()
        {
            var warnings = new List<string>();
            foreach (RouletteOutcome outcome in RouletteOutcomes.All())
            {
                OutcomeSettings settings = outcomes[outcome];
                if (float.IsNaN(settings.Weight) || float.IsInfinity(settings.Weight) || settings.Weight < 0f)
                {
                    warnings.Add("Peso no válido para «" + outcome.DisplayName() + "» (" + settings.Weight + "); se usa 0.");
                    settings.Weight = 0f;
                }

                if (settings.Enabled && settings.Weight == 0f)
                {
                    warnings.Add("«" + outcome.DisplayName() + "» está activado con peso 0: nunca saldrá.");
                }
            }

            DefaultDurationSeconds = ClampDuration(DefaultDurationSeconds, "Duración general", warnings);
            foreach (PenaltyEffect effect in RouletteOutcomes.AllEffects())
            {
                float seconds;
                if (durationOverrides.TryGetValue(effect, out seconds) && seconds > 0f)
                {
                    durationOverrides[effect] = ClampDuration(seconds, "Duración de «" + effect.DisplayName() + "»", warnings);
                }
            }

            if (float.IsNaN(CooldownSeconds) || CooldownSeconds < 0f)
            {
                warnings.Add("Cooldown no válido; se usa 0.");
                CooldownSeconds = 0f;
            }

            if (MaxConsecutivePenalties < 0)
            {
                warnings.Add("Máximo de castigos seguidos negativo; se usa 0 (sin protección).");
                MaxConsecutivePenalties = 0;
            }

            if (float.IsNaN(FocusIntensity) || FocusIntensity < 0f || FocusIntensity > 1f)
            {
                float corrected = FocusIntensity > 1f ? 1f : 0f;
                warnings.Add("Intensidad del foco fuera de [0, 1]; se usa " + corrected + ".");
                FocusIntensity = corrected;
            }

            return warnings;
        }

        private static float ClampDuration(float seconds, string name, List<string> warnings)
        {
            if (float.IsNaN(seconds) || seconds < MinDurationSeconds)
            {
                warnings.Add(name + " demasiado corta; se usa " + MinDurationSeconds + " s.");
                return MinDurationSeconds;
            }

            if (seconds > MaxDurationSeconds)
            {
                warnings.Add(name + " demasiado larga; se usa " + MaxDurationSeconds + " s.");
                return MaxDurationSeconds;
            }

            return seconds;
        }
    }
}
