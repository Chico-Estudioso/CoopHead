using System;
using System.Speech.Recognition;

namespace Coophead.Voice
{
    /// <summary>
    /// Un reconocedor con la gramática de agradecimiento sobre una fuente de audio: el micrófono predeterminado o el
    /// audio del escritorio. Escucha solo entre <see cref="Start"/> y <see cref="Stop"/>.
    /// </summary>
    internal sealed class RecognitionChannel : IDisposable
    {
        private readonly object gate = new object();
        private readonly SpeechRecognitionEngine engine;
        private readonly LoopbackSource loopback;
        private volatile float minimumConfidence;
        private bool running;
        private bool restartRequested;
        private bool stopRequested = true;
        private int unexpectedRestarts;

        /// <summary>Reinicios automáticos por escucha si el reconocedor se para sin que nadie lo pida.</summary>
        private const int MaxUnexpectedRestarts = 3;

        public RecognitionChannel(string source, RecognizerInfo recognizer, Grammar grammar, float minConfidence, LoopbackSource loopback)
        {
            Source = source;
            MinConfidence = minConfidence;
            this.loopback = loopback;
            engine = new SpeechRecognitionEngine(recognizer);
            engine.LoadGrammar(grammar);
            try
            {
                // Windows descarta por su cuenta las frases que no llegan a su umbral interno (SpeechRecognitionRejected)
                // antes de que podamos aplicar ConfianzaMinima. Se baja para que decida la configuración del usuario.
                engine.UpdateRecognizerSetting("CFGConfidenceRejectionThreshold", 10);
            }
            catch (Exception)
            {
                // Si el reconocedor no admite el ajuste, se queda con el suyo.
            }

            if (loopback == null)
            {
                // Lanza InvalidOperationException si Windows no tiene micrófono predeterminado.
                engine.SetInputToDefaultAudioDevice();
            }

            engine.SpeechRecognized += (sender, e) =>
            {
                if (e.Result == null)
                {
                    return;
                }

                if (e.Result.Confidence >= MinConfidence)
                {
                    if (Recognized != null)
                    {
                        Recognized(this, e.Result.Text, e.Result.Confidence);
                    }
                }
                else if (LowConfidence != null)
                {
                    LowConfidence(this, e.Result.Text, e.Result.Confidence);
                }
            };
            engine.SpeechRecognitionRejected += (sender, e) =>
            {
                if (Rejected != null)
                {
                    Rejected(this);
                }
            };
            engine.RecognizeCompleted += OnRecognizeCompleted;
        }

        /// <summary>Fuente («mic» o «escritorio»), confianza y texto reconocido.</summary>
        public event Action<RecognitionChannel, string, float> Recognized;

        public event Action<RecognitionChannel, string> Failed;

        /// <summary>Frase de la gramática oída con confianza por debajo del mínimo (no cuenta).</summary>
        public event Action<RecognitionChannel, string, float> LowConfidence;

        /// <summary>Se oyó voz que no se parece a ninguna frase de la gramática.</summary>
        public event Action<RecognitionChannel> Rejected;

        public string Source { get; private set; }

        /// <summary>Descripción para el diagnóstico («escritorio1 = Auriculares…»).</summary>
        public string Description { get; set; }

        /// <summary>Confianza mínima; el plugin la cambia en caliente con la orden CONFIDENCE.</summary>
        public float MinConfidence
        {
            get { return minimumConfidence; }
            set { minimumConfidence = value; }
        }

        /// <summary>True si escucha una salida de audio (escritorio) en lugar del micrófono.</summary>
        public bool IsDesktop
        {
            get { return loopback != null; }
        }

        /// <summary>Salida que no se puede escuchar (p. ej. un formato que Windows no admite en bucle): se ignora.</summary>
        public bool Disabled { get; set; }

        /// <summary>Avisos de diagnóstico (p. ej. el reconocedor dejó de escuchar antes de tiempo).</summary>
        public event Action<RecognitionChannel, string> Notice;

        public void Start()
        {
            lock (gate)
            {
                stopRequested = false;
                unexpectedRestarts = 0;
                if (running)
                {
                    restartRequested = true;
                    return;
                }

                StartLocked();
            }
        }

        public void Stop()
        {
            lock (gate)
            {
                stopRequested = true;
                restartRequested = false;
                if (loopback != null)
                {
                    // Primero se cierra la tubería: el reconocedor recibe fin de flujo y suelta la lectura en la que
                    // está esperando, así la cancelación no se queda bloqueada aunque no suene nada.
                    loopback.Stop();
                }

                if (running)
                {
                    engine.RecognizeAsyncCancel();
                }
            }
        }

        /// <summary>Cuánto audio llegó por la salida del escritorio en la última escucha; null para el micrófono.</summary>
        public string DescribeActivity()
        {
            lock (gate)
            {
                return loopback == null ? null : loopback.DescribeActivity();
            }
        }

        public void Dispose()
        {
            Stop();
            engine.Dispose();
            if (loopback != null)
            {
                loopback.Dispose();
            }
        }

        private void StartLocked()
        {
            if (loopback != null)
            {
                try
                {
                    engine.SetInputToAudioStream(loopback.Start(), LoopbackSource.Format);
                }
                catch (Exception)
                {
                    // Si el reconocedor no acepta el flujo, no se deja la captura del escritorio encendida.
                    loopback.Stop();
                    throw;
                }
            }

            engine.RecognizeAsync(RecognizeMode.Multiple);
            running = true;
            restartRequested = false;
        }

        private void OnRecognizeCompleted(object sender, RecognizeCompletedEventArgs e)
        {
            string notice = null;
            string failure = null;
            lock (gate)
            {
                running = false;
                if (!stopRequested && !restartRequested)
                {
                    // Nadie pidió parar: el reconocedor se ha quedado sin audio o ha fallado en mitad de la escucha.
                    string reason = e.InputStreamEnded ? "se le acabó el audio" : "se detuvo solo";
                    if (unexpectedRestarts < MaxUnexpectedRestarts)
                    {
                        unexpectedRestarts++;
                        restartRequested = true;
                        notice = "el reconocedor dejó de escuchar antes de tiempo (" + reason + "); se vuelve a poner en marcha.";
                    }
                    else
                    {
                        notice = "el reconocedor dejó de escuchar antes de tiempo (" + reason + ") varias veces; esta fuente no escucha hasta la próxima ventana.";
                    }
                }

                if (restartRequested && !stopRequested)
                {
                    try
                    {
                        StartLocked();
                    }
                    catch (Exception ex)
                    {
                        failure = ex.Message;
                    }
                }
            }

            if (e.Error != null && Failed != null)
            {
                Failed(this, e.Error.Message);
            }

            if (notice != null && Notice != null)
            {
                Notice(this, notice);
            }

            if (failure != null && Failed != null)
            {
                Failed(this, failure);
            }
        }
    }
}
