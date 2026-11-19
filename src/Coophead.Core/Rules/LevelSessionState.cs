using System.Collections.Generic;

namespace Coophead.Core.Rules
{
    /// <summary>Estado de reglas de un intento de nivel. Se crea de cero en cada inicio o reinicio.</summary>
    public sealed class LevelSessionState
    {
        private readonly PlayerRoundStats[] players;
        private readonly HashSet<string> processedEvents = new HashSet<string>();

        internal LevelSessionState(string levelKey, int sessionNumber)
        {
            LevelKey = levelKey;
            SessionNumber = sessionNumber;
            players = new[] { new PlayerRoundStats(PlayerSlot.PlayerOne), new PlayerRoundStats(PlayerSlot.PlayerTwo) };
            IsActive = true;
        }

        /// <summary>Identificador del nivel tal como lo entrega la integración (p. ej. nombre de escena).</summary>
        public string LevelKey { get; private set; }

        /// <summary>Número correlativo de sesión; sirve para distinguir reintentos del mismo nivel en los logs.</summary>
        public int SessionNumber { get; private set; }

        public bool IsActive { get; internal set; }

        /// <summary>Momento de inicio según el reloj del juego; sirve para la duración del intento.</summary>
        public float StartTime { get; internal set; }

        /// <summary>Duración del intento al terminar; 0 si se desconoce.</summary>
        public float DurationSeconds { get; internal set; }

        /// <summary>Reanimaciones totales del equipo en este intento, sin descontar créditos.</summary>
        public int TeamRevives { get; internal set; }

        /// <summary>true en cuanto se ha pedido un reinicio; a partir de ahí se ignoran eventos.</summary>
        public bool RestartPending { get; internal set; }

        public RestartReason RestartReason { get; internal set; }

        /// <summary>Primer límite alcanzado en el intento, se haya pedido reinicio o no (reinicio automático desactivado).</summary>
        public RestartReason LimitReached { get; internal set; }

        public PlayerRoundStats Get(PlayerSlot slot)
        {
            return players[(int)slot];
        }

        /// <summary>Marca un evento como procesado. Devuelve false si ya lo estaba (duplicado).</summary>
        internal bool TryMarkProcessed(string key)
        {
            return processedEvents.Add(key);
        }
    }
}
