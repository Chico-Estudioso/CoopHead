namespace Coophead.Core.Rules
{
    public enum EventStatus
    {
        /// <summary>El evento se aplicó a los contadores.</summary>
        Applied,

        /// <summary>Ya se había procesado un evento con la misma clave; no se cuenta dos veces.</summary>
        Duplicate,

        /// <summary>Se descartó por estado incoherente, falta de nivel activo o reinicio pendiente.</summary>
        Ignored,
    }

    public enum RestartReason
    {
        None,

        /// <summary>El jugador rescatado alcanzó su límite individual de reanimaciones netas.</summary>
        PlayerLimit,

        /// <summary>El equipo alcanzó el límite de reanimaciones totales del nivel.</summary>
        TeamLimit,
    }

    public enum LevelEndReason
    {
        Won,
        Lost,
        Exited,
        Restarted,
        Unknown,
    }

    public sealed class DeathResult
    {
        public EventStatus Status { get; internal set; }

        /// <summary>Motivo legible cuando el estado es <see cref="EventStatus.Ignored"/>.</summary>
        public string IgnoredReason { get; internal set; }

        public PlayerSlot Player { get; internal set; }

        /// <summary>
        /// Los dos jugadores están muertos a la vez. Es la derrota normal del juego: el mod no hace nada más.
        /// </summary>
        public bool BothDown { get; internal set; }
    }

    public sealed class ReviveResult
    {
        public EventStatus Status { get; internal set; }

        public string IgnoredReason { get; internal set; }

        public PlayerSlot Rescued { get; internal set; }

        public PlayerSlot Rescuer { get; internal set; }

        /// <summary>El rescatado tenía un crédito y se consumió en lugar de subir su contador.</summary>
        public bool CreditConsumed { get; internal set; }

        public RescuerReward Reward { get; internal set; }

        /// <summary>Límite alcanzado con este rescate, se pida o no el reinicio.</summary>
        public RestartReason LimitReached { get; internal set; }

        /// <summary>
        /// true solo la primera vez que se alcanza un límite en el intento y con el reinicio automático activado.
        /// </summary>
        public bool RestartRequested { get; internal set; }
    }

    /// <summary>Resumen de un intento de nivel con copias independientes de los contadores.</summary>
    public sealed class LevelSummary
    {
        internal LevelSummary(LevelSessionState state, LevelEndReason reason)
        {
            LevelKey = state.LevelKey;
            SessionNumber = state.SessionNumber;
            Reason = reason;
            TeamRevives = state.TeamRevives;
            DurationSeconds = state.DurationSeconds;
            RestartReason = state.RestartReason;
            PlayerOne = state.Get(PlayerSlot.PlayerOne).Clone();
            PlayerTwo = state.Get(PlayerSlot.PlayerTwo).Clone();
        }

        public string LevelKey { get; private set; }

        public int SessionNumber { get; private set; }

        public LevelEndReason Reason { get; private set; }

        public int TeamRevives { get; private set; }

        /// <summary>Duración del intento en segundos de juego; 0 si se desconoce.</summary>
        public float DurationSeconds { get; private set; }

        public RestartReason RestartReason { get; private set; }

        public PlayerRoundStats PlayerOne { get; private set; }

        public PlayerRoundStats PlayerTwo { get; private set; }

        public PlayerRoundStats Get(PlayerSlot slot)
        {
            return slot == PlayerSlot.PlayerOne ? PlayerOne : PlayerTwo;
        }
    }
}
