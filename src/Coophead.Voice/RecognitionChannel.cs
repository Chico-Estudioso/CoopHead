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
        private bool running;
        private bool restartRequested;

        public RecognitionChannel(string source, RecognizerInfo recognizer, Grammar grammar, float minConfidence, LoopbackSource loopback)
        {
            Source = source;
            MinConfidence = minConfidence;
            this.loopback = loopback;
            engine = new SpeechRecognitionEngine(recognizer);
            engine.LoadGrammar(grammar);
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

        public float MinConfidence { get; private set; }

        public void Start()
        {
            lock (gate)
            {
                if (running)
                {
                    restartRequested = true;
                    return;
                }

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
        }

        public void Stop()
        {
            lock (gate)
            {
                restartRequested = false;
                if (running)
                {
                    engine.RecognizeAsyncCancel();
                }
            }

            if (loopback != null)
            {
                // Cierra la tubería: el reconocedor recibe fin de flujo y termina aunque no haya sonido.
                loopback.Stop();
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

        private void OnRecognizeCompleted(object sender, RecognizeCompletedEventArgs e)
        {
            bool restart;
            lock (gate)
            {
                running = false;
                restart = restartRequested;
            }

            if (e.Error != null && Failed != null)
            {
                Failed(this, e.Error.Message);
            }

            if (restart)
            {
                Start();
            }
        }
    }
}
