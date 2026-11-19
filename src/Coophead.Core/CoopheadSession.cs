using System;
using System.Collections.Generic;
using Coophead.Core.Diagnostics;
using Coophead.Core.Roulette;
using Coophead.Core.Rules;
using Coophead.Core.Thanks;

namespace Coophead.Core
{
    public sealed class RestartRequest
    {
        internal RestartRequest(string levelKey, RestartReason reason, PlayerSlot triggeredBy)
        {
            LevelKey = levelKey;
            Reason = reason;
            TriggeredBy = triggeredBy;
        }

        public string LevelKey { get; private set; }

        public RestartReason Reason { get; private set; }

        /// <summary>Jugador cuya reanimación hizo saltar el límite.</summary>
        public PlayerSlot TriggeredBy { get; private set; }
    }

    /// <summary>
    /// Coordina el flujo de una reanimación (docs/ARQUITECTURA_PROPUESTA.md): reglas y créditos, límites,
    /// ventana de agradecimiento, ruleta con equilibrio adaptativo y efectos temporales.
    /// La integración con Cuphead solo traduce hooks verificados a estas llamadas y ejecuta lo que piden los eventos.
    /// </summary>
    public sealed class CoopheadSession
    {
        private readonly CoopheadConfig config;
        private readonly ICoreLog log;
        private readonly RuleEngine rules;
        private readonly ThanksWindowTracker thanks;
        private readonly RouletteService roulette;
        private readonly PenaltyScheduler penalties;

        public CoopheadSession(CoopheadConfig config, IPenaltyApplier applier, ICoreLog log = null, IRandomSource random = null)
        {
            this.config = config;
            this.log = log ?? NullLog.Instance;
            rules = new RuleEngine(config.Rules, this.log);
            thanks = new ThanksWindowTracker(config.Thanks);
            roulette = new RouletteService(config.Roulette, config.Balance, random);
            penalties = new PenaltyScheduler(applier, config.Roulette.Compatibility, this.log);
        }

        /// <summary>El juego debe reiniciar el nivel por la vía verificada.</summary>
        public event Action<RestartRequest> RestartRequested;

        public event Action<ThanksWindow> ThanksWindowOpened;

        public event Action<ThanksWindow> ThanksWindowClosed;

        public event Action<RouletteResult> RouletteSpun;

        public event Action<LevelSummary> LevelEnded;

        public RuleEngine Rules
        {
            get { return rules; }
        }

        public ThanksWindowTracker Thanks
        {
            get { return thanks; }
        }

        public RouletteService Roulette
        {
            get { return roulette; }
        }

        public PenaltyScheduler Penalties
        {
            get { return penalties; }
        }

        public void StartLevel(string levelKey)
        {
            StartLevel(levelKey, 0f);
        }

        public void StartLevel(string levelKey, float now)
        {
            ResetTransientState("Empieza un nivel nuevo.");
            rules.StartLevel(levelKey, now);
        }

        public LevelSummary EndLevel(LevelEndReason reason)
        {
            return EndLevel(reason, float.NaN);
        }

        public LevelSummary EndLevel(LevelEndReason reason, float now)
        {
            ResetTransientState("El nivel ha terminado.");
            LevelSummary summary = rules.EndLevel(reason, now);
            if (summary != null)
            {
                Raise(LevelEnded, summary);
            }

            return summary;
        }

        /// <summary>El mod se desactiva o se descarga: restaura todo sin castigar a nadie.</summary>
        public void Shutdown()
        {
            ResetTransientState("Mod desactivado.");
        }

        public DeathResult OnPlayerDied(PlayerSlot player, string eventKey, float now)
        {
            DeathResult result = rules.OnPlayerDied(player, eventKey);
            if (result.Status != EventStatus.Applied)
            {
                return result;
            }

            // Un efecto nunca sobrevive a la muerte del jugador, y no se le castiga por no dar las gracias muerto.
            penalties.ClearPlayer(player);
            var resolved = new List<ThanksWindow>();
            thanks.CloseFor(player, "Ha vuelto a morir.", resolved);
            RaiseClosed(resolved);
            return result;
        }

        public ReviveResult OnPlayerRevived(PlayerSlot rescued, PlayerSlot rescuer, string eventKey, float now)
        {
            ReviveResult result = rules.OnPlayerRevived(rescued, rescuer, eventKey);
            if (result.Status != EventStatus.Applied)
            {
                return result;
            }

            if (result.RestartRequested)
            {
                ResetTransientState("Se reinicia el nivel.");
                Raise(RestartRequested, new RestartRequest(rules.Session.LevelKey, result.LimitReached, rescued));
                return result;
            }

            var resolved = new List<ThanksWindow>();
            ThanksWindow window = thanks.Open(rescued, rescuer, now, resolved);
            RaiseClosed(resolved);
            if (window != null)
            {
                Raise(ThanksWindowOpened, window);
            }

            return result;
        }

        /// <summary>Vuelta a la partida sin rescate (robo de corazón u otra vía): no cuenta ni abre ventana.</summary>
        public EventStatus OnPlayerRevivedWithoutRescue(PlayerSlot player, string eventKey, float now)
        {
            return rules.OnPlayerRevivedWithoutRescue(player, eventKey);
        }

        // --- Estadísticas de combate ---

        public void RecordParry(PlayerSlot player)
        {
            PlayerRoundStats stats = rules.ActiveStats(player);
            if (stats != null)
            {
                stats.Parries++;
            }
        }

        public void RecordSuper(PlayerSlot player)
        {
            PlayerRoundStats stats = rules.ActiveStats(player);
            if (stats != null)
            {
                stats.Supers++;
            }
        }

        public void RecordEx(PlayerSlot player)
        {
            PlayerRoundStats stats = rules.ActiveStats(player);
            if (stats != null)
            {
                stats.ExMoves++;
            }
        }

        public void RecordDamageDealt(PlayerSlot player, float amount)
        {
            PlayerRoundStats stats = rules.ActiveStats(player);
            if (stats != null && amount > 0f && !float.IsInfinity(amount))
            {
                stats.DamageDealt += amount;
            }
        }

        public void RecordHitTaken(PlayerSlot player)
        {
            PlayerRoundStats stats = rules.ActiveStats(player);
            if (stats != null)
            {
                stats.HitsTaken++;
            }
        }

        /// <summary>Texto reconocido por el servicio de voz local, entregado solo durante una ventana.</summary>
        public ThanksWindow OnTranscript(string transcript, float now)
        {
            ThanksWindow window = thanks.OnTranscript(transcript, now);
            CountThanks(window);
            return window;
        }

        /// <summary>Confirmación con la tecla o botón de respaldo.</summary>
        public ThanksWindow OnManualThanks(PlayerSlot? player, float now)
        {
            ThanksWindow window = thanks.OnManualConfirm(player, now);
            CountThanks(window);
            return window;
        }

        public void SetVoiceAvailable(bool available)
        {
            thanks.SetVoiceAvailable(available);
        }

        public void ReportVoiceFailure()
        {
            thanks.ReportTechnicalFailure();
        }

        /// <summary>Avanza el tiempo: restaura efectos expirados y resuelve ventanas vencidas.</summary>
        public void Tick(float now)
        {
            penalties.Tick(now);

            var resolved = new List<ThanksWindow>();
            thanks.Tick(now, resolved);
            foreach (ThanksWindow window in resolved)
            {
                Raise(ThanksWindowClosed, window);
                if (window.Resolution == ThanksResolution.Missed)
                {
                    HandleMissedThanks(window, now);
                }
            }
        }

        private void HandleMissedThanks(ThanksWindow window, float now)
        {
            LevelSessionState session = rules.Session;
            if (session == null || !session.IsActive || session.RestartPending)
            {
                return;
            }

            session.Get(window.Rescued).ThanksMissed++;
            PlayerSlot target = config.PenaltyTarget == PenaltyTargetMode.Rescued ? window.Rescued : window.Rescuer;
            PlayerRoundStats targetStats = session.Get(target);
            if (!targetStats.IsAlive)
            {
                log.Info("Sin ruleta: " + target.Label() + " está muerto.");
                return;
            }

            RouletteResult result = roulette.Spin(targetStats, now);
            if (result.Outcome == RouletteOutcome.DoublePunishment)
            {
                targetStats.DoublePunishments++;
            }

            foreach (EffectApplication effect in result.Effects)
            {
                targetStats.AddEffect(effect.Effect);
                penalties.Start(target, effect.Effect, effect.DurationSeconds, now);
            }

            log.Info("Ruleta para " + target.Label() + ": " + result.Outcome.DisplayName() + " (" + result.Decision + "). " + result.Pressure);
            Raise(RouletteSpun, result);
        }

        private void CountThanks(ThanksWindow window)
        {
            if (window == null)
            {
                return;
            }

            LevelSessionState session = rules.Session;
            if (session != null && session.IsActive)
            {
                PlayerRoundStats stats = session.Get(window.Rescued);
                stats.ThanksRecognized++;
                if (window.Resolution == ThanksResolution.Thanked)
                {
                    stats.ThanksByVoice++;
                }
                else
                {
                    stats.ThanksManual++;
                }
            }

            Raise(ThanksWindowClosed, window);
        }

        private void ResetTransientState(string note)
        {
            penalties.ClearAll();
            var resolved = new List<ThanksWindow>();
            thanks.CloseAll(note, resolved);
            RaiseClosed(resolved);
        }

        private void RaiseClosed(List<ThanksWindow> windows)
        {
            foreach (ThanksWindow window in windows)
            {
                Raise(ThanksWindowClosed, window);
            }
        }

        /// <summary>Un fallo en un suscriptor (HUD, integración) no debe romper el estado del Core.</summary>
        private void Raise<T>(Action<T> handler, T argument)
        {
            if (handler == null)
            {
                return;
            }

            try
            {
                handler(argument);
            }
            catch (Exception e)
            {
                log.Error("Error en un suscriptor de eventos de Coophead: " + e.Message);
            }
        }
    }
}
