using System;
using System.Collections.Generic;
using Coophead.Core.Diagnostics;

namespace Coophead.Core.Roulette
{
    /// <summary>
    /// Aplica y restaura un efecto sobre el juego. Lo implementa la integración con Cuphead.
    /// </summary>
    /// <remarks>
    /// Contrato: <see cref="Apply"/> guarda el valor original que modifica y <see cref="Restore"/> devuelve
    /// exactamente ese valor (nunca uno fijo). Restore debe ser seguro aunque el jugador o el nivel ya no existan.
    /// </remarks>
    public interface IPenaltyApplier
    {
        void Apply(PlayerSlot player, PenaltyEffect effect);

        void Restore(PlayerSlot player, PenaltyEffect effect);
    }

    public sealed class ActivePenalty
    {
        internal ActivePenalty(PlayerSlot player, PenaltyEffect effect, float startTime, float endTime)
        {
            Player = player;
            Effect = effect;
            StartTime = startTime;
            EndTime = endTime;
        }

        public PlayerSlot Player { get; private set; }

        public PenaltyEffect Effect { get; private set; }

        public float StartTime { get; private set; }

        public float EndTime { get; internal set; }

        public float Remaining(float now)
        {
            return Math.Max(0f, EndTime - now);
        }
    }

    /// <summary>
    /// Lleva la cuenta de los efectos activos y garantiza su restauración al expirar, al morir el jugador,
    /// al cambiar de nivel, al desactivar el mod o ante un error.
    /// </summary>
    public sealed class PenaltyScheduler
    {
        private readonly IPenaltyApplier applier;
        private readonly EffectCompatibility compatibility;
        private readonly ICoreLog log;
        private readonly List<ActivePenalty> active = new List<ActivePenalty>();

        public PenaltyScheduler(IPenaltyApplier applier, EffectCompatibility compatibility, ICoreLog log = null)
        {
            this.applier = applier;
            this.compatibility = compatibility ?? new EffectCompatibility();
            this.log = log ?? NullLog.Instance;
        }

        /// <summary>Copia de los efectos activos.</summary>
        public IList<ActivePenalty> Active
        {
            get { return active.ToArray(); }
        }

        /// <summary>
        /// Inicia un efecto. Si ese mismo efecto ya está activo en el jugador, solo amplía su final (no se apila).
        /// </summary>
        /// <returns>true si el efecto queda activo.</returns>
        public bool Start(PlayerSlot player, PenaltyEffect effect, float durationSeconds, float now)
        {
            if (!(durationSeconds > 0f))
            {
                return false;
            }

            ActivePenalty existing = Find(player, effect);
            if (existing != null)
            {
                existing.EndTime = Math.Max(existing.EndTime, now + durationSeconds);
                return true;
            }

            foreach (ActivePenalty penalty in active)
            {
                if (penalty.Player == player && !compatibility.AreCompatible(penalty.Effect, effect))
                {
                    log.Warning("No se aplica «" + effect.DisplayName() + "» a " + player.Label()
                        + ": es incompatible con «" + penalty.Effect.DisplayName() + "», que ya está activo.");
                    return false;
                }
            }

            try
            {
                applier.Apply(player, effect);
            }
            catch (Exception e)
            {
                log.Error("Error al aplicar «" + effect.DisplayName() + "» a " + player.Label() + ": " + e.Message);
                SafeRestore(player, effect);
                return false;
            }

            active.Add(new ActivePenalty(player, effect, now, now + durationSeconds));
            return true;
        }

        /// <summary>Restaura los efectos cuyo tiempo ha terminado. Llamar en cada frame o con frecuencia similar.</summary>
        public void Tick(float now)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (now >= active[i].EndTime)
                {
                    ActivePenalty expired = active[i];
                    active.RemoveAt(i);
                    SafeRestore(expired.Player, expired.Effect);
                }
            }
        }

        /// <summary>Restaura todos los efectos de un jugador (por ejemplo, al morir).</summary>
        public void ClearPlayer(PlayerSlot player)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (active[i].Player == player)
                {
                    ActivePenalty penalty = active[i];
                    active.RemoveAt(i);
                    SafeRestore(penalty.Player, penalty.Effect);
                }
            }
        }

        /// <summary>Restaura todos los efectos (cambio o fin de nivel, reinicio, mod desactivado, error).</summary>
        public void ClearAll()
        {
            while (active.Count > 0)
            {
                ActivePenalty penalty = active[active.Count - 1];
                active.RemoveAt(active.Count - 1);
                SafeRestore(penalty.Player, penalty.Effect);
            }
        }

        private ActivePenalty Find(PlayerSlot player, PenaltyEffect effect)
        {
            foreach (ActivePenalty penalty in active)
            {
                if (penalty.Player == player && penalty.Effect == effect)
                {
                    return penalty;
                }
            }

            return null;
        }

        private void SafeRestore(PlayerSlot player, PenaltyEffect effect)
        {
            try
            {
                applier.Restore(player, effect);
            }
            catch (Exception e)
            {
                log.Error("Error al restaurar «" + effect.DisplayName() + "» de " + player.Label() + ": " + e.Message);
            }
        }
    }
}
