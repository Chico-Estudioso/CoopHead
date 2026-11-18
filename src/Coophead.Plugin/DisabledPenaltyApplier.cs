using System.Collections.Generic;
using Coophead.Core;
using Coophead.Core.Diagnostics;
using Coophead.Core.Roulette;

namespace Coophead.Plugin
{
    /// <summary>
    /// Aplicador provisional: registra el efecto pero no toca el juego. Los efectos reales llegarán en M4,
    /// cuando cada uno tenga un hook verificado que permita restaurar el valor original.
    /// </summary>
    internal sealed class DisabledPenaltyApplier : IPenaltyApplier
    {
        private readonly ICoreLog log;
        private readonly HashSet<PenaltyEffect> warned = new HashSet<PenaltyEffect>();

        public DisabledPenaltyApplier(ICoreLog log)
        {
            this.log = log;
        }

        public void Apply(PlayerSlot player, PenaltyEffect effect, float durationSeconds)
        {
            if (warned.Add(effect))
            {
                log.Warning("«" + effect.DisplayName() + "» se simula: AplicarEfectos está desactivado o el efecto no es compatible con esta versión. No se modifica el juego.");
            }
        }

        public void Restore(PlayerSlot player, PenaltyEffect effect)
        {
        }
    }
}
