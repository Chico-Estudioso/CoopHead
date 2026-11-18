using System;
using Coophead.Core.Diagnostics;
using Coophead.Core.Rules;

namespace Coophead.Core.Integration
{
    /// <summary>
    /// Traduce los datos crudos que leen los hooks del juego (identificadores numéricos de jugador e instancias de
    /// fantasma) a llamadas de <see cref="CoopheadSession"/>. Es independiente de Unity para poder probarlo.
    /// </summary>
    /// <remarks>
    /// <para>Mientras «aplicar reglas» esté desactivado, las muertes y reanimaciones solo se registran en el log:
    /// es la fase de verificación de M2. El inicio y el fin de nivel se envían siempre porque no tienen efectos.</para>
    /// <para>La muerte y su rescate comparten la clave del fantasma («ghost:&lt;id&gt;»), de modo que un mismo
    /// fantasma no puede contarse dos veces.</para>
    /// </remarks>
    public sealed class GameEventTranslator
    {
        private readonly CoopheadSession session;
        private readonly ICoreLog log;
        private readonly Func<bool> applyRules;
        private readonly Func<bool> diagnostics;
        private int sequence;
        private int currentLevelInstance;

        public GameEventTranslator(CoopheadSession session, ICoreLog log, Func<bool> applyRules, Func<bool> diagnostics)
        {
            this.session = session;
            this.log = log ?? NullLog.Instance;
            this.applyRules = applyRules;
            this.diagnostics = diagnostics;
        }

        /// <summary>Último número de secuencia asignado; útil para cruzar el log con pruebas manuales.</summary>
        public int Sequence
        {
            get { return sequence; }
        }

        /// <summary>
        /// Valor del enum PlayerId del juego → jugador del mod. Según la evidencia comunitaria PlayerOne = 0 y
        /// PlayerTwo = 1; cualquier otro valor (Any, None) se descarta.
        /// </summary>
        public static bool TryMapPlayer(int gamePlayerId, out PlayerSlot slot)
        {
            slot = gamePlayerId == 1 ? PlayerSlot.PlayerTwo : PlayerSlot.PlayerOne;
            return gamePlayerId == 0 || gamePlayerId == 1;
        }

        public static string GhostKey(int ghostInstanceId)
        {
            return "ghost:" + ghostInstanceId;
        }

        /// <param name="levelKey">Nombre del nivel (p. ej. el tipo de nivel del juego).</param>
        /// <param name="levelInstanceId">Instancia del objeto de nivel; 0 si se desconoce.</param>
        public void LevelStarted(string levelKey, int levelInstanceId)
        {
            Trace("Nivel iniciado: " + levelKey + " (instancia " + levelInstanceId + ")");
            currentLevelInstance = levelInstanceId;
            session.StartLevel(levelKey);
        }

        /// <summary>
        /// Fin de nivel. Si llega de una instancia distinta de la actual (p. ej. el nivel anterior se destruye después
        /// de que arranque el siguiente), se ignora para no cerrar el intento nuevo.
        /// </summary>
        public void LevelEnded(LevelEndReason reason, string source, int levelInstanceId)
        {
            if (levelInstanceId != 0 && currentLevelInstance != 0 && levelInstanceId != currentLevelInstance)
            {
                Trace("Fin de una instancia de nivel anterior (" + levelInstanceId + ") vía " + source + ": se ignora");
                return;
            }

            Trace("Nivel terminado (" + reason + ") vía " + source);
            session.EndLevel(reason);
        }

        /// <summary>Muerte notificada por el nivel. Solo diagnóstico: se compara con la aparición del fantasma.</summary>
        public void DeathReported(int gamePlayerId)
        {
            Trace("Muerte notificada por el nivel: " + Describe(gamePlayerId));
        }

        /// <summary>Aparece el fantasma de un jugador: es la muerte que cuenta para las reglas.</summary>
        public void GhostSpawned(int gamePlayerId, int ghostInstanceId, float now)
        {
            Trace("Fantasma " + ghostInstanceId + " de " + Describe(gamePlayerId));
            PlayerSlot slot;
            if (!ShouldApply() || !Map(gamePlayerId, out slot))
            {
                return;
            }

            DeathResult result = session.OnPlayerDied(slot, GhostKey(ghostInstanceId), now);
            Report("Muerte de " + slot.Label(), result.Status, result.IgnoredReason);
        }

        /// <summary>Rescate por parry sobre el corazón del fantasma.</summary>
        public void ParryRevive(int rescuedGamePlayerId, int rescuerGamePlayerId, int ghostInstanceId, float now)
        {
            Trace("Rescate por parry del fantasma " + ghostInstanceId + ": " + Describe(rescuerGamePlayerId)
                + " rescata a " + Describe(rescuedGamePlayerId));
            PlayerSlot rescued;
            PlayerSlot rescuer;
            if (!ShouldApply() || !Map(rescuedGamePlayerId, out rescued) || !Map(rescuerGamePlayerId, out rescuer))
            {
                return;
            }

            ReviveResult result = session.OnPlayerRevived(rescued, rescuer, GhostKey(ghostInstanceId), now);
            Report("Rescate de " + rescued.Label() + " por " + rescuer.Label(), result.Status, result.IgnoredReason);
        }

        /// <summary>Robo de corazón con el fantasma fuera de pantalla. Fuera de alcance: solo se registra.</summary>
        public void StealRevive(int gamePlayerId, int ghostInstanceId)
        {
            Trace("Robo de corazón del fantasma " + ghostInstanceId + " de " + Describe(gamePlayerId) + " (no cuenta)");
        }

        /// <summary>
        /// El juego confirma que el jugador vuelve a estar vivo. Si el Core aún lo tenía por muerto (robo de corazón u
        /// otra vía sin rescate), se marca como vivo sin contar nada.
        /// </summary>
        public void PlayerBackInGame(int gamePlayerId, int frame, float now)
        {
            Trace("Reanimación confirmada por el juego: " + Describe(gamePlayerId));
            PlayerSlot slot;
            if (!ShouldApply() || !Map(gamePlayerId, out slot))
            {
                return;
            }

            session.OnPlayerRevivedWithoutRescue(slot, slot.Label() + "@" + frame, now);
        }

        private bool ShouldApply()
        {
            return applyRules();
        }

        private bool Map(int gamePlayerId, out PlayerSlot slot)
        {
            if (TryMapPlayer(gamePlayerId, out slot))
            {
                return true;
            }

            log.Warning("Identificador de jugador inesperado (" + gamePlayerId + "): evento descartado.");
            return false;
        }

        private void Trace(string message)
        {
            sequence++;
            if (diagnostics())
            {
                log.Info("[#" + sequence + "] " + message);
            }
        }

        private void Report(string what, EventStatus status, string reason)
        {
            if (status == EventStatus.Applied)
            {
                return;
            }

            log.Warning(what + ": " + status + (string.IsNullOrEmpty(reason) ? string.Empty : " (" + reason + ")"));
        }

        private static string Describe(int gamePlayerId)
        {
            PlayerSlot slot;
            return TryMapPlayer(gamePlayerId, out slot) ? slot.Label() : "jugador " + gamePlayerId;
        }
    }
}
