using System.Collections.Generic;

namespace Coophead.Core.Thanks
{
    public enum ThanksResolution
    {
        Pending,

        /// <summary>Frase reconocida por voz dentro de la ventana.</summary>
        Thanked,

        /// <summary>Confirmado con la tecla o botón de respaldo.</summary>
        ThankedManually,

        /// <summary>La ventana expiró sin agradecimiento y había forma de evaluarlo: procede la ruleta.</summary>
        Missed,

        /// <summary>
        /// No se pudo evaluar con justicia (fallo técnico, nivel terminado, nueva muerte, ventana sustituida).
        /// Nunca provoca castigo.
        /// </summary>
        NotEvaluated,
    }

    public sealed class ThanksWindow
    {
        internal ThanksWindow(int id, PlayerSlot rescued, PlayerSlot rescuer, float openedAt, float expiresAt)
        {
            Id = id;
            Rescued = rescued;
            Rescuer = rescuer;
            OpenedAt = openedAt;
            ExpiresAt = expiresAt;
            Resolution = ThanksResolution.Pending;
        }

        public int Id { get; private set; }

        /// <summary>Quien debe dar las gracias: el jugador rescatado.</summary>
        public PlayerSlot Rescued { get; private set; }

        public PlayerSlot Rescuer { get; private set; }

        public float OpenedAt { get; private set; }

        public float ExpiresAt { get; internal set; }

        /// <summary>
        /// El rescate ya ocurrió pero el jugador aún está en la animación de reaparecer. Mientras tanto la ventana
        /// acepta el agradecimiento pero no caduca: el plazo cuenta desde que vuelve a la partida.
        /// </summary>
        public bool AwaitingRevive { get; internal set; }

        /// <summary>Límite de espera a la confirmación, por si el juego nunca la notifica.</summary>
        internal float ReviveDeadline { get; set; }

        public ThanksResolution Resolution { get; internal set; }

        /// <summary>Motivo legible cuando la resolución es <see cref="ThanksResolution.NotEvaluated"/>.</summary>
        public string Note { get; internal set; }

        /// <summary>Hubo un fallo técnico mientras estaba abierta: al expirar no se castiga.</summary>
        internal bool TechnicalIssue { get; set; }

        public bool IsPending
        {
            get { return Resolution == ThanksResolution.Pending; }
        }

        /// <summary>Segundos que quedan; mientras espera la reaparición, el plazo completo aún no ha empezado.</summary>
        public float Remaining(float now)
        {
            return ExpiresAt > now ? ExpiresAt - now : 0f;
        }

        internal bool AcceptsThanks(float now)
        {
            return AwaitingRevive || now <= ExpiresAt;
        }
    }

    /// <summary>
    /// Ventanas de agradecimiento abiertas. Como solo hay un micrófono y no se distingue quién habla, una frase
    /// reconocida cierra la ventana pendiente más antigua (orden de llegada).
    /// </summary>
    public sealed class ThanksWindowTracker
    {
        /// <summary>Espera máxima a que el juego confirme la reaparición antes de contar el plazo igualmente.</summary>
        public const float MaxReviveWaitSeconds = 6f;

        private readonly ThanksConfig config;
        private readonly ThanksPhraseMatcher matcher;
        private readonly List<ThanksWindow> open = new List<ThanksWindow>();
        private int nextId = 1;
        private bool voiceAvailable;

        public ThanksWindowTracker(ThanksConfig config)
        {
            this.config = config;
            matcher = new ThanksPhraseMatcher(config);
        }

        public ThanksPhraseMatcher Matcher
        {
            get { return matcher; }
        }

        /// <summary>Lo indica el servicio de voz (M5). Por defecto false: aún no hay reconocedor.</summary>
        public bool VoiceAvailable
        {
            get { return voiceAvailable; }
        }

        /// <summary>Hay al menos un canal (voz o respaldo manual) para dar las gracias.</summary>
        public bool CanEvaluate
        {
            get { return voiceAvailable || config.ManualFallbackEnabled; }
        }

        public IList<ThanksWindow> Pending
        {
            get { return open.ToArray(); }
        }

        /// <summary>
        /// Abre una ventana para el jugador rescatado. Devuelve null si el agradecimiento no se exige.
        /// Si ese jugador ya tenía una ventana abierta, la anterior se cierra sin castigo.
        /// </summary>
        public ThanksWindow Open(PlayerSlot rescued, PlayerSlot rescuer, float now, List<ThanksWindow> resolved)
        {
            return Open(rescued, rescuer, now, resolved, false);
        }

        /// <param name="rescued">Jugador que debe dar las gracias.</param>
        /// <param name="rescuer">Jugador que le ha rescatado.</param>
        /// <param name="now">Reloj del juego.</param>
        /// <param name="resolved">Recibe la ventana anterior del mismo jugador si se sustituye.</param>
        /// <param name="awaitingRevive">
        /// true si el jugador aún tiene que reaparecer: el plazo empezará con <see cref="ConfirmRevive"/>.
        /// </param>
        public ThanksWindow Open(PlayerSlot rescued, PlayerSlot rescuer, float now, List<ThanksWindow> resolved, bool awaitingRevive)
        {
            if (!config.RequireThanks)
            {
                return null;
            }

            CloseFor(rescued, "Sustituida por una nueva reanimación.", resolved);
            var window = new ThanksWindow(nextId++, rescued, rescuer, now, now + config.WindowSeconds);
            if (awaitingRevive)
            {
                window.AwaitingRevive = true;
                window.ReviveDeadline = now + MaxReviveWaitSeconds;
            }

            if (!CanEvaluate)
            {
                window.TechnicalIssue = true;
            }

            open.Add(window);
            return window;
        }

        /// <summary>
        /// El jugador ha vuelto a la partida: el plazo para dar las gracias cuenta desde ahora (sin acortar el que ya tenía).
        /// </summary>
        /// <returns>La ventana confirmada, o null si no había ninguna esperando.</returns>
        public ThanksWindow ConfirmRevive(PlayerSlot rescued, float now)
        {
            foreach (ThanksWindow window in open)
            {
                if (window.Rescued == rescued && window.AwaitingRevive)
                {
                    window.AwaitingRevive = false;
                    window.ExpiresAt = System.Math.Max(window.ExpiresAt, now + config.WindowSeconds);
                    return window;
                }
            }

            return null;
        }

        /// <summary>
        /// Texto reconocido por el servicio de voz. Si coincide con la frase, cierra la ventana pendiente más antigua.
        /// </summary>
        public ThanksWindow OnTranscript(string transcript, float now)
        {
            if (!voiceAvailable || !matcher.Matches(transcript))
            {
                return null;
            }

            ThanksWindow window = OldestOpen(now, null);
            if (window != null)
            {
                Resolve(window, ThanksResolution.Thanked, null);
            }

            return window;
        }

        /// <summary>
        /// Confirmación manual. Con jugador, cierra su ventana; sin jugador, la pendiente más antigua.
        /// </summary>
        public ThanksWindow OnManualConfirm(PlayerSlot? player, float now)
        {
            if (!config.ManualFallbackEnabled)
            {
                return null;
            }

            ThanksWindow window = OldestOpen(now, player);
            if (window != null)
            {
                Resolve(window, ThanksResolution.ThankedManually, null);
            }

            return window;
        }

        /// <summary>
        /// Cambia la disponibilidad de la voz. Si se pierde con ventanas abiertas, se marca fallo técnico
        /// para no castigar por ello (salvo que exista respaldo manual).
        /// </summary>
        public void SetVoiceAvailable(bool available)
        {
            bool lostVoice = voiceAvailable && !available;
            voiceAvailable = available;
            if (lostVoice && !config.ManualFallbackEnabled)
            {
                ReportTechnicalFailure();
            }
        }

        /// <summary>Error del micrófono o del reconocedor: las ventanas abiertas no castigarán al expirar.</summary>
        public void ReportTechnicalFailure()
        {
            foreach (ThanksWindow window in open)
            {
                window.TechnicalIssue = true;
            }
        }

        /// <summary>Cierra las ventanas expiradas y las añade a <paramref name="resolved"/>.</summary>
        public void Tick(float now, List<ThanksWindow> resolved)
        {
            for (int i = 0; i < open.Count;)
            {
                ThanksWindow window = open[i];
                if (window.AwaitingRevive && now >= window.ReviveDeadline)
                {
                    // El juego no confirmó la reaparición a tiempo: se cuenta el plazo desde ahora.
                    window.AwaitingRevive = false;
                    window.ExpiresAt = System.Math.Max(window.ExpiresAt, now + config.WindowSeconds);
                }

                if (window.AwaitingRevive || now < window.ExpiresAt)
                {
                    i++;
                    continue;
                }

                if (window.TechnicalIssue || !CanEvaluate)
                {
                    Resolve(window, ThanksResolution.NotEvaluated, "No había forma fiable de dar las gracias.");
                }
                else
                {
                    Resolve(window, ThanksResolution.Missed, null);
                }

                resolved.Add(window);
            }
        }

        /// <summary>Cierra sin castigo la ventana de un jugador (por ejemplo, si vuelve a morir).</summary>
        public void CloseFor(PlayerSlot rescued, string note, List<ThanksWindow> resolved)
        {
            for (int i = 0; i < open.Count;)
            {
                if (open[i].Rescued == rescued)
                {
                    ThanksWindow window = open[i];
                    Resolve(window, ThanksResolution.NotEvaluated, note);
                    if (resolved != null)
                    {
                        resolved.Add(window);
                    }
                }
                else
                {
                    i++;
                }
            }
        }

        /// <summary>Cierra todas sin castigo (fin o cambio de nivel, reinicio).</summary>
        public void CloseAll(string note, List<ThanksWindow> resolved)
        {
            while (open.Count > 0)
            {
                ThanksWindow window = open[0];
                Resolve(window, ThanksResolution.NotEvaluated, note);
                if (resolved != null)
                {
                    resolved.Add(window);
                }
            }
        }

        private ThanksWindow OldestOpen(float now, PlayerSlot? player)
        {
            foreach (ThanksWindow window in open)
            {
                if (window.AcceptsThanks(now) && (!player.HasValue || window.Rescued == player.Value))
                {
                    return window;
                }
            }

            return null;
        }

        private void Resolve(ThanksWindow window, ThanksResolution resolution, string note)
        {
            window.Resolution = resolution;
            window.Note = note;
            open.Remove(window);
        }
    }
}
