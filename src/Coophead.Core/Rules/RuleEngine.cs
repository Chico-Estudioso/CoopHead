using Coophead.Core.Diagnostics;

namespace Coophead.Core.Rules
{
    /// <summary>
    /// Motor de reglas bilaterales. Recibe eventos ya verificados por la integración con el juego y
    /// actualiza contadores por jugador y nivel. No conoce Unity ni Cuphead.
    /// </summary>
    /// <remarks>
    /// Cada evento llega con una clave única proporcionada por la integración (por ejemplo, el identificador
    /// de instancia del fantasma). Con ella se descartan duplicados, de modo que una misma muerte o
    /// reanimación nunca se cuenta dos veces.
    /// </remarks>
    public sealed class RuleEngine
    {
        private readonly RulesConfig config;
        private readonly HelpCreditService credits;
        private readonly ICoreLog log;
        private int sessionCounter;

        public RuleEngine(RulesConfig config, ICoreLog log = null)
        {
            this.config = config;
            this.log = log ?? NullLog.Instance;
            credits = new HelpCreditService(config);
        }

        /// <summary>Intento de nivel en curso, o el último terminado. Null antes del primer nivel.</summary>
        public LevelSessionState Session { get; private set; }

        public RulesConfig Config
        {
            get { return config; }
        }

        /// <summary>
        /// Empieza un intento de nivel con contadores y créditos a cero. También se llama al reintentar el mismo nivel.
        /// </summary>
        public LevelSessionState StartLevel(string levelKey)
        {
            return StartLevel(levelKey, 0f);
        }

        public LevelSessionState StartLevel(string levelKey, float now)
        {
            sessionCounter++;
            Session = new LevelSessionState(levelKey ?? string.Empty, sessionCounter) { StartTime = now };
            log.Info("Nivel iniciado: " + Session.LevelKey + " (sesión " + sessionCounter + ").");
            return Session;
        }

        /// <summary>Cierra el intento actual y devuelve su resumen. Null si no había nivel activo.</summary>
        public LevelSummary EndLevel(LevelEndReason reason)
        {
            return EndLevel(reason, float.NaN);
        }

        /// <param name="reason">Motivo del fin del intento.</param>
        /// <param name="now">Reloj del juego al terminar; NaN si se desconoce (la duración queda a 0).</param>
        public LevelSummary EndLevel(LevelEndReason reason, float now)
        {
            if (Session == null || !Session.IsActive)
            {
                return null;
            }

            Session.IsActive = false;
            Session.DurationSeconds = float.IsNaN(now) ? 0f : System.Math.Max(0f, now - Session.StartTime);
            log.Info("Nivel terminado: " + Session.LevelKey + " (" + reason + ").");
            return new LevelSummary(Session, reason);
        }

        public DeathResult OnPlayerDied(PlayerSlot player, string eventKey)
        {
            var result = new DeathResult { Player = player };
            string reason = CheckCommon(player, eventKey);
            if (reason != null)
            {
                return Ignore(result, reason);
            }

            if (!Session.TryMarkProcessed("death:" + eventKey))
            {
                result.Status = EventStatus.Duplicate;
                return result;
            }

            PlayerRoundStats stats = Session.Get(player);
            if (!stats.IsAlive)
            {
                return Ignore(result, player.Label() + " ya figuraba como muerto.");
            }

            stats.IsAlive = false;
            stats.Deaths++;
            result.Status = EventStatus.Applied;
            result.BothDown = !Session.Get(player.Partner()).IsAlive;
            return result;
        }

        public ReviveResult OnPlayerRevived(PlayerSlot rescued, PlayerSlot rescuer, string eventKey)
        {
            var result = new ReviveResult { Rescued = rescued, Rescuer = rescuer };
            string reason = CheckCommon(rescued, eventKey);
            if (reason == null && !PlayerSlots.IsDefined(rescuer))
            {
                reason = "Rescatador desconocido.";
            }

            if (reason == null && rescued == rescuer)
            {
                reason = "Un jugador no puede rescatarse a sí mismo.";
            }

            if (reason != null)
            {
                return Ignore(result, reason);
            }

            if (!Session.TryMarkProcessed("revive:" + eventKey))
            {
                result.Status = EventStatus.Duplicate;
                return result;
            }

            PlayerRoundStats rescuedStats = Session.Get(rescued);
            PlayerRoundStats rescuerStats = Session.Get(rescuer);
            if (rescuedStats.IsAlive)
            {
                return Ignore(result, rescued.Label() + " no figuraba como muerto.");
            }

            if (!rescuerStats.IsAlive)
            {
                return Ignore(result, rescuer.Label() + " no puede rescatar estando muerto.");
            }

            rescuedStats.IsAlive = true;
            Session.TeamRevives++;
            result.CreditConsumed = credits.RegisterReviveReceived(rescuedStats);
            result.Reward = credits.RewardRescuer(rescuerStats);
            result.Status = EventStatus.Applied;

            result.LimitReached = EvaluateLimits(rescuedStats);
            if (result.LimitReached != RestartReason.None)
            {
                if (Session.LimitReached == RestartReason.None)
                {
                    Session.LimitReached = result.LimitReached;
                }

                if (config.AutoRestartEnabled)
                {
                    Session.RestartPending = true;
                    Session.RestartReason = result.LimitReached;
                    result.RestartRequested = true;
                    log.Info("Límite alcanzado (" + result.LimitReached + "): se solicita reiniciar el nivel.");
                }
                else
                {
                    log.Warning("Límite alcanzado (" + result.LimitReached + "), pero el reinicio automático está desactivado.");
                }
            }

            return result;
        }

        /// <summary>
        /// El jugador vuelve a la partida sin que su pareja le rescate (por ejemplo, robando un corazón con el fantasma
        /// fuera de pantalla). Solo se marca como vivo: no cuenta para contadores, créditos ni límites (ADR-004).
        /// </summary>
        /// <returns><see cref="EventStatus.Ignored"/> si ya estaba vivo, que es lo normal tras un rescate por parry.</returns>
        public EventStatus OnPlayerRevivedWithoutRescue(PlayerSlot player, string eventKey)
        {
            string reason = CheckCommon(player, eventKey);
            if (reason != null)
            {
                log.Warning("Evento ignorado: " + reason);
                return EventStatus.Ignored;
            }

            if (!Session.TryMarkProcessed("self:" + eventKey))
            {
                return EventStatus.Duplicate;
            }

            PlayerRoundStats stats = Session.Get(player);
            if (stats.IsAlive)
            {
                return EventStatus.Ignored;
            }

            stats.IsAlive = true;
            log.Info(player.Label() + " vuelve a la partida sin rescate: no cuenta para las reglas.");
            return EventStatus.Applied;
        }

        private RestartReason EvaluateLimits(PlayerRoundStats rescued)
        {
            int playerLimit = config.GetReviveLimit(rescued.Slot);
            if (playerLimit > 0 && rescued.RevivesReceived >= playerLimit)
            {
                return RestartReason.PlayerLimit;
            }

            if (config.TeamReviveLimit > 0 && Session.TeamRevives >= config.TeamReviveLimit)
            {
                return RestartReason.TeamLimit;
            }

            return RestartReason.None;
        }

        /// <summary>Estadísticas de combate: no afectan a las reglas. Devuelve null si no hay intento activo.</summary>
        internal PlayerRoundStats ActiveStats(PlayerSlot player)
        {
            if (!config.Enabled || Session == null || !Session.IsActive || !PlayerSlots.IsDefined(player))
            {
                return null;
            }

            return Session.Get(player);
        }

        /// <summary>Comprobaciones compartidas. Devuelve el motivo para ignorar o null si se puede seguir.</summary>
        private string CheckCommon(PlayerSlot player, string eventKey)
        {
            if (!config.Enabled)
            {
                return "Reglas desactivadas.";
            }

            if (Session == null || !Session.IsActive)
            {
                return "No hay ningún nivel activo.";
            }

            if (Session.RestartPending)
            {
                return "Hay un reinicio pendiente.";
            }

            if (!PlayerSlots.IsDefined(player))
            {
                return "Jugador desconocido.";
            }

            if (string.IsNullOrEmpty(eventKey))
            {
                return "El evento no trae clave de deduplicación.";
            }

            return null;
        }

        private T Ignore<T>(T result, string reason) where T : class
        {
            log.Warning("Evento ignorado: " + reason);
            var death = result as DeathResult;
            if (death != null)
            {
                death.Status = EventStatus.Ignored;
                death.IgnoredReason = reason;
            }

            var revive = result as ReviveResult;
            if (revive != null)
            {
                revive.Status = EventStatus.Ignored;
                revive.IgnoredReason = reason;
            }

            return result;
        }
    }
}
