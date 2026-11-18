using System;
using System.Collections.Generic;
using Coophead.Core.Diagnostics;
using Coophead.Core.Rules;

namespace Coophead.Core.Roulette
{
    public enum RouletteDecision
    {
        /// <summary>Tirada normal según los pesos.</summary>
        Rolled,

        /// <summary>La ruleta está desactivada.</summary>
        Disabled,

        /// <summary>El jugador recibió un castigo hace menos del cooldown.</summary>
        ProtectedCooldown,

        /// <summary>El jugador acumula el máximo de castigos seguidos.</summary>
        ProtectedConsecutive,

        /// <summary>No hay ningún resultado activado con peso positivo.</summary>
        NothingAvailable,
    }

    public sealed class EffectApplication
    {
        public EffectApplication(PenaltyEffect effect, float durationSeconds)
        {
            Effect = effect;
            DurationSeconds = durationSeconds;
        }

        public PenaltyEffect Effect { get; private set; }

        public float DurationSeconds { get; private set; }
    }

    public sealed class RouletteResult
    {
        public PlayerSlot Target { get; internal set; }

        public RouletteOutcome Outcome { get; internal set; }

        public RouletteDecision Decision { get; internal set; }

        /// <summary>Efectos a aplicar: ninguno, uno o dos (Castigo doble).</summary>
        public IList<EffectApplication> Effects { get; internal set; }

        /// <summary>Probabilidades usadas en la tirada (vacío si no hubo tirada). Útil para HUD y logs.</summary>
        public IDictionary<RouletteOutcome, float> Probabilities { get; internal set; }

        public PressureReport Pressure { get; internal set; }

        public bool IsPenalty
        {
            get { return Outcome != RouletteOutcome.NoPenalty; }
        }
    }

    /// <summary>
    /// Ruleta de penalizaciones: elige por pesos, evita repeticiones, respeta cooldown, protección por castigos
    /// seguidos y compatibilidad de efectos, y aplica los ajustes del director de equilibrio.
    /// </summary>
    public sealed class RouletteService
    {
        private readonly RouletteConfig config;
        private readonly BalanceConfig balanceConfig;
        private readonly BalanceDirector director;
        private readonly IRandomSource random;

        public RouletteService(RouletteConfig config, BalanceConfig balanceConfig, IRandomSource random)
        {
            this.config = config;
            this.balanceConfig = balanceConfig;
            director = new BalanceDirector(balanceConfig);
            this.random = random ?? CreateDefaultRandom(config);
        }

        public BalanceDirector Director
        {
            get { return director; }
        }

        /// <summary>
        /// Pesos efectivos para el jugador, ya ajustados por foco, presión, disponibilidad de «Castigo doble» y
        /// protección contra repeticiones. Sin normalizar.
        /// </summary>
        public IDictionary<RouletteOutcome, float> ComputeWeights(PlayerRoundStats target, PressureReport pressure)
        {
            var weights = new Dictionary<RouletteOutcome, float>();
            bool canDouble = HasCompatiblePair(EligibleEffects());

            foreach (RouletteOutcome outcome in RouletteOutcomes.All())
            {
                OutcomeSettings settings = config.Get(outcome);
                if (!settings.Enabled || !(settings.Weight > 0f))
                {
                    continue;
                }

                float weight = settings.Weight;
                if (outcome == RouletteOutcome.NoPenalty)
                {
                    if (target.Slot == config.FocusedPlayer)
                    {
                        weight *= 1f - (0.5f * config.FocusIntensity);
                    }

                    weight *= pressure.NoPenaltyMultiplier;
                }
                else if (outcome == RouletteOutcome.DoublePunishment)
                {
                    if (!canDouble)
                    {
                        continue;
                    }

                    weight *= pressure.DoublePunishmentMultiplier;
                }

                if (weight > 0f)
                {
                    weights[outcome] = weight;
                }
            }

            if (config.AvoidRepeats && target.LastOutcome.HasValue)
            {
                RouletteOutcome last = target.LastOutcome.Value;
                if (last != RouletteOutcome.NoPenalty && weights.ContainsKey(last) && weights.Count > 1)
                {
                    weights.Remove(last);
                }
            }

            return weights;
        }

        /// <summary>
        /// Tira la ruleta para el jugador y anota el resultado en sus contadores (último resultado, castigos
        /// seguidos y recientes). No aplica efectos: eso lo hace <see cref="PenaltyScheduler"/>.
        /// </summary>
        public RouletteResult Spin(PlayerRoundStats target, float now)
        {
            var result = new RouletteResult
            {
                Target = target.Slot,
                Outcome = RouletteOutcome.NoPenalty,
                Effects = new List<EffectApplication>(),
                Probabilities = new Dictionary<RouletteOutcome, float>(),
            };

            if (!config.Enabled)
            {
                result.Decision = RouletteDecision.Disabled;
                result.Pressure = director.Evaluate(target, now);
                return result;
            }

            PressureReport pressure = director.Evaluate(target, now);
            result.Pressure = pressure;

            if (config.MaxConsecutivePenalties > 0 && target.ConsecutivePenalties >= config.MaxConsecutivePenalties)
            {
                result.Decision = RouletteDecision.ProtectedConsecutive;
                target.RecordRouletteOutcome(RouletteOutcome.NoPenalty, now);
                return result;
            }

            if (config.CooldownSeconds > 0f && now - target.LastPenaltyTime < config.CooldownSeconds)
            {
                result.Decision = RouletteDecision.ProtectedCooldown;
                target.RecordRouletteOutcome(RouletteOutcome.NoPenalty, now);
                return result;
            }

            IDictionary<RouletteOutcome, float> weights = ComputeWeights(target, pressure);
            float total = 0f;
            foreach (float weight in weights.Values)
            {
                total += weight;
            }

            if (weights.Count == 0 || !(total > 0f))
            {
                result.Decision = RouletteDecision.NothingAvailable;
                target.RecordRouletteOutcome(RouletteOutcome.NoPenalty, now);
                return result;
            }

            foreach (RouletteOutcome outcome in RouletteOutcomes.All())
            {
                float weight;
                if (weights.TryGetValue(outcome, out weight))
                {
                    result.Probabilities[outcome] = weight / total;
                }
            }

            result.Decision = RouletteDecision.Rolled;
            result.Outcome = PickWeighted(RouletteOutcomes.All(), weights);

            if (result.Outcome.IsSingleEffect())
            {
                result.Effects.Add(BuildApplication(result.Outcome.ToEffect(), pressure));
            }
            else if (result.Outcome == RouletteOutcome.DoublePunishment)
            {
                PenaltyEffect first;
                PenaltyEffect second;
                PickCompatiblePair(EligibleEffects(), out first, out second);
                result.Effects.Add(BuildApplication(first, pressure));
                result.Effects.Add(BuildApplication(second, pressure));
            }

            target.RecordRouletteOutcome(result.Outcome, now);
            return result;
        }

        private EffectApplication BuildApplication(PenaltyEffect effect, PressureReport pressure)
        {
            float baseDuration = config.GetBaseDuration(effect);
            float duration = baseDuration * pressure.DurationMultiplier;
            if (duration < balanceConfig.MinEffectDurationSeconds)
            {
                duration = Math.Min(baseDuration, balanceConfig.MinEffectDurationSeconds);
            }

            return new EffectApplication(effect, duration);
        }

        /// <summary>Efectos temporales activados con peso positivo, en orden fijo.</summary>
        private List<PenaltyEffect> EligibleEffects()
        {
            var effects = new List<PenaltyEffect>();
            foreach (PenaltyEffect effect in RouletteOutcomes.AllEffects())
            {
                OutcomeSettings settings = config.Get(effect.ToOutcome());
                if (settings.Enabled && settings.Weight > 0f)
                {
                    effects.Add(effect);
                }
            }

            return effects;
        }

        private bool HasCompatiblePair(List<PenaltyEffect> effects)
        {
            for (int i = 0; i < effects.Count; i++)
            {
                for (int j = i + 1; j < effects.Count; j++)
                {
                    if (config.Compatibility.AreCompatible(effects[i], effects[j]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void PickCompatiblePair(List<PenaltyEffect> effects, out PenaltyEffect first, out PenaltyEffect second)
        {
            var firstWeights = new Dictionary<PenaltyEffect, float>();
            foreach (PenaltyEffect candidate in effects)
            {
                foreach (PenaltyEffect other in effects)
                {
                    if (config.Compatibility.AreCompatible(candidate, other))
                    {
                        firstWeights[candidate] = config.Get(candidate.ToOutcome()).Weight;
                        break;
                    }
                }
            }

            first = PickWeighted(effects, firstWeights);

            var secondWeights = new Dictionary<PenaltyEffect, float>();
            foreach (PenaltyEffect other in effects)
            {
                if (config.Compatibility.AreCompatible(first, other))
                {
                    secondWeights[other] = config.Get(other.ToOutcome()).Weight;
                }
            }

            second = PickWeighted(effects, secondWeights);
        }

        /// <summary>Elección ponderada recorriendo las claves en un orden fijo para que la semilla sea reproducible.</summary>
        private T PickWeighted<T>(IEnumerable<T> order, IDictionary<T, float> weights)
        {
            float total = 0f;
            foreach (float weight in weights.Values)
            {
                total += weight;
            }

            double roll = random.NextDouble() * total;
            double accumulated = 0d;
            T lastCandidate = default(T);
            bool found = false;
            foreach (T key in order)
            {
                float weight;
                if (!weights.TryGetValue(key, out weight))
                {
                    continue;
                }

                lastCandidate = key;
                found = true;
                accumulated += weight;
                if (roll < accumulated)
                {
                    return key;
                }
            }

            if (!found)
            {
                throw new InvalidOperationException("No hay candidatos para la elección ponderada.");
            }

            // Solo por redondeo en coma flotante: se devuelve el último candidato válido.
            return lastCandidate;
        }

        private static IRandomSource CreateDefaultRandom(RouletteConfig config)
        {
            return config.DebugSeed.HasValue
                ? new SystemRandomSource(config.DebugSeed.Value)
                : new SystemRandomSource();
        }
    }
}
