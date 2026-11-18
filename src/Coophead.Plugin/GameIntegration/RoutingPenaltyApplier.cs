using System;
using System.Collections.Generic;
using Coophead.Core;
using Coophead.Core.Roulette;

namespace Coophead.Plugin.GameIntegration
{
    /// <summary>
    /// Envía cada efecto al juego si «AplicarEfectos» está activado y el efecto es compatible; si no, lo simula.
    /// Recuerda adónde fue cada efecto para restaurarlo por la misma vía aunque el ajuste cambie entre medias.
    /// </summary>
    internal sealed class RoutingPenaltyApplier : IPenaltyApplier
    {
        private readonly GameEffectsApplier game;
        private readonly IPenaltyApplier simulated;
        private readonly Func<bool> applyEffects;
        private readonly HashSet<string> inGame = new HashSet<string>();

        public RoutingPenaltyApplier(GameEffectsApplier game, IPenaltyApplier simulated, Func<bool> applyEffects)
        {
            this.game = game;
            this.simulated = simulated;
            this.applyEffects = applyEffects;
        }

        public void Apply(PlayerSlot player, PenaltyEffect effect, float durationSeconds)
        {
            string key = Key(player, effect);
            if (applyEffects() && game.IsSupported(effect))
            {
                game.Apply(player, effect, durationSeconds);
                inGame.Add(key);
                return;
            }

            simulated.Apply(player, effect, durationSeconds);
        }

        public void Restore(PlayerSlot player, PenaltyEffect effect)
        {
            if (inGame.Remove(Key(player, effect)))
            {
                game.Restore(player, effect);
                return;
            }

            simulated.Restore(player, effect);
        }

        private static string Key(PlayerSlot player, PenaltyEffect effect)
        {
            return player.Label() + ":" + effect;
        }
    }
}
