using System;

namespace Coophead.Core.Roulette
{
    /// <summary>Resultados posibles de la ruleta.</summary>
    public enum RouletteOutcome
    {
        NoPenalty,
        SlowMovement,
        InvertedControls,
        SlowFire,
        LimitedDash,
        DoublePunishment,
    }

    /// <summary>Efectos temporales que se aplican sobre un jugador.</summary>
    public enum PenaltyEffect
    {
        SlowMovement,
        InvertedControls,
        SlowFire,
        LimitedDash,
    }

    public static class RouletteOutcomes
    {
        public static RouletteOutcome[] All()
        {
            return new[]
            {
                RouletteOutcome.NoPenalty,
                RouletteOutcome.SlowMovement,
                RouletteOutcome.InvertedControls,
                RouletteOutcome.SlowFire,
                RouletteOutcome.LimitedDash,
                RouletteOutcome.DoublePunishment,
            };
        }

        public static PenaltyEffect[] AllEffects()
        {
            return new[]
            {
                PenaltyEffect.SlowMovement,
                PenaltyEffect.InvertedControls,
                PenaltyEffect.SlowFire,
                PenaltyEffect.LimitedDash,
            };
        }

        /// <summary>true para los resultados que aplican exactamente un efecto.</summary>
        public static bool IsSingleEffect(this RouletteOutcome outcome)
        {
            return outcome != RouletteOutcome.NoPenalty && outcome != RouletteOutcome.DoublePunishment;
        }

        public static PenaltyEffect ToEffect(this RouletteOutcome outcome)
        {
            switch (outcome)
            {
                case RouletteOutcome.SlowMovement:
                    return PenaltyEffect.SlowMovement;
                case RouletteOutcome.InvertedControls:
                    return PenaltyEffect.InvertedControls;
                case RouletteOutcome.SlowFire:
                    return PenaltyEffect.SlowFire;
                case RouletteOutcome.LimitedDash:
                    return PenaltyEffect.LimitedDash;
                default:
                    throw new ArgumentException("El resultado " + outcome + " no corresponde a un único efecto.", "outcome");
            }
        }

        public static RouletteOutcome ToOutcome(this PenaltyEffect effect)
        {
            switch (effect)
            {
                case PenaltyEffect.SlowMovement:
                    return RouletteOutcome.SlowMovement;
                case PenaltyEffect.InvertedControls:
                    return RouletteOutcome.InvertedControls;
                case PenaltyEffect.SlowFire:
                    return RouletteOutcome.SlowFire;
                default:
                    return RouletteOutcome.LimitedDash;
            }
        }

        /// <summary>Nombre en español para HUD y logs.</summary>
        public static string DisplayName(this RouletteOutcome outcome)
        {
            switch (outcome)
            {
                case RouletteOutcome.NoPenalty:
                    return "Sin penalización";
                case RouletteOutcome.SlowMovement:
                    return "Movimiento lento";
                case RouletteOutcome.InvertedControls:
                    return "Controles invertidos";
                case RouletteOutcome.SlowFire:
                    return "Disparo lento";
                case RouletteOutcome.LimitedDash:
                    return "Dash limitado";
                default:
                    return "Castigo doble";
            }
        }

        public static string DisplayName(this PenaltyEffect effect)
        {
            return effect.ToOutcome().DisplayName();
        }
    }
}
