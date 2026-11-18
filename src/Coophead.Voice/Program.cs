using System;
using System.Collections.Generic;
using System.Globalization;
using System.Speech.Recognition;
using System.Text;
using System.Threading;
using Coophead.Core.Voice;

namespace Coophead.Voice
{
    /// <summary>
    /// Ayudante de reconocimiento de voz local para Coophead.
    /// </summary>
    /// <remarks>
    /// <para>Usa el reconocedor de voz de escritorio de Windows (System.Speech/SAPI) con una gramática cerrada que
    /// solo contiene las frases de agradecimiento. Ese reconocedor emplea modelos acústicos estadísticos: es
    /// reconocimiento automático del habla (aprendizaje automático clásico), no IA generativa, y funciona sin conexión.</para>
    /// <para>Solo escucha entre una orden LISTEN y una STOP (o durante una prueba). No guarda audio ni transcripciones:
    /// el texto reconocido se escribe en la salida estándar para el plugin y se descarta.</para>
    /// <para>Uso: Coophead.Voice.exe --culture es-ES --phrases "gracias|gracias cari" --min-confidence 0.6</para>
    /// </remarks>
    internal static class Program
    {
        private static readonly object Gate = new object();
        private static SpeechRecognitionEngine engine;
        private static bool running;
        private static bool restartRequested;
        private static Timer testTimer;

        private static int Main(string[] args)
        {
            Console.OutputEncoding = new UTF8Encoding(false);
            Console.InputEncoding = new UTF8Encoding(false);

            string culture = "es-ES";
            IList<string> phrases = new List<string>();
            float minConfidence = 0.6f;
            for (int i = 0; i + 1 < args.Length; i += 2)
            {
                switch (args[i])
                {
                    case "--culture":
                        culture = args[i + 1];
                        break;
                    case "--phrases":
                        phrases = VoiceProtocol.SplitPhrases(args[i + 1]);
                        break;
                    case "--min-confidence":
                        float.TryParse(args[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out minConfidence);
                        break;
                }
            }

            if (phrases.Count == 0)
            {
                Write(VoiceProtocol.FormatUnavailable("No se han indicado frases de agradecimiento."));
                return 2;
            }

            RecognizerInfo recognizer = FindRecognizer(culture);
            if (recognizer == null)
            {
                Write(VoiceProtocol.FormatUnavailable("No hay un reconocedor de voz de Windows para " + culture
                    + ". Instalados: " + DescribeInstalled() + ". Añade el idioma con «Reconocimiento de voz» en Configuración de Windows."));
                return 3;
            }

            try
            {
                using (engine = new SpeechRecognitionEngine(recognizer))
                {
                    var builder = new GrammarBuilder(new Choices(ToArray(phrases))) { Culture = recognizer.Culture };
                    engine.LoadGrammar(new Grammar(builder) { Name = "coophead-gracias" });

                    try
                    {
                        engine.SetInputToDefaultAudioDevice();
                    }
                    catch (InvalidOperationException)
                    {
                        Write(VoiceProtocol.FormatUnavailable("No hay micrófono predeterminado en Windows o no se puede abrir."));
                        return 4;
                    }

                    engine.SpeechRecognized += (sender, e) =>
                    {
                        if (e.Result != null && e.Result.Confidence >= minConfidence)
                        {
                            Write(VoiceProtocol.FormatHeard(e.Result.Confidence, e.Result.Text));
                        }
                    };
                    engine.RecognizeCompleted += OnRecognizeCompleted;

                    Write(VoiceProtocol.FormatReady(recognizer.Culture.Name + " " + recognizer.Name));
                    CommandLoop();
                    StopListening();
                }
            }
            catch (Exception e)
            {
                Write(VoiceProtocol.FormatUnavailable("Error del reconocedor: " + e.Message));
                return 5;
            }

            return 0;
        }

        /// <summary>Lee órdenes hasta QUIT o hasta que el plugin cierre la entrada (el juego se ha cerrado).</summary>
        private static void CommandLoop()
        {
            string line;
            while ((line = Console.In.ReadLine()) != null)
            {
                line = line.Trim();
                if (line == VoiceProtocol.Quit)
                {
                    return;
                }

                if (line == VoiceProtocol.Listen)
                {
                    CancelTest();
                    StartListening();
                    continue;
                }

                if (line == VoiceProtocol.Stop)
                {
                    CancelTest();
                    StopListening();
                    continue;
                }

                float? testSeconds = VoiceProtocol.ParseTest(line);
                if (testSeconds.HasValue)
                {
                    StartListening();
                    CancelTest();
                    testTimer = new Timer(_ =>
                    {
                        StopListening();
                        Write(VoiceProtocol.TestEnd);
                    }, null, (int)(testSeconds.Value * 1000f), Timeout.Infinite);
                    continue;
                }

                Write(VoiceProtocol.FormatError("Orden desconocida."));
            }
        }

        private static void StartListening()
        {
            lock (Gate)
            {
                if (running)
                {
                    restartRequested = true;
                    return;
                }

                engine.RecognizeAsync(RecognizeMode.Multiple);
                running = true;
                restartRequested = false;
            }

            Write(VoiceProtocol.Listening);
        }

        private static void StopListening()
        {
            lock (Gate)
            {
                restartRequested = false;
                if (running)
                {
                    engine.RecognizeAsyncCancel();
                }
            }
        }

        private static void OnRecognizeCompleted(object sender, RecognizeCompletedEventArgs e)
        {
            bool restart;
            lock (Gate)
            {
                running = false;
                restart = restartRequested;
            }

            if (e.Error != null)
            {
                Write(VoiceProtocol.FormatError(e.Error.Message));
            }

            if (restart)
            {
                StartListening();
                return;
            }

            Write(VoiceProtocol.Stopped);
        }

        private static void CancelTest()
        {
            Timer timer = Interlocked.Exchange(ref testTimer, null);
            if (timer != null)
            {
                timer.Dispose();
            }
        }

        private static RecognizerInfo FindRecognizer(string culture)
        {
            RecognizerInfo sameLanguage = null;
            foreach (RecognizerInfo info in SpeechRecognitionEngine.InstalledRecognizers())
            {
                if (string.Equals(info.Culture.Name, culture, StringComparison.OrdinalIgnoreCase))
                {
                    return info;
                }

                if (sameLanguage == null && culture.StartsWith(info.Culture.TwoLetterISOLanguageName + "-", StringComparison.OrdinalIgnoreCase))
                {
                    sameLanguage = info;
                }
            }

            return sameLanguage;
        }

        private static string DescribeInstalled()
        {
            var names = new List<string>();
            foreach (RecognizerInfo info in SpeechRecognitionEngine.InstalledRecognizers())
            {
                names.Add(info.Culture.Name);
            }

            return names.Count == 0 ? "ninguno" : string.Join(", ", names.ToArray());
        }

        private static string[] ToArray(IList<string> phrases)
        {
            var array = new string[phrases.Count];
            phrases.CopyTo(array, 0);
            return array;
        }

        private static void Write(string line)
        {
            lock (Gate)
            {
                Console.Out.WriteLine(line);
                Console.Out.Flush();
            }
        }
    }
}
