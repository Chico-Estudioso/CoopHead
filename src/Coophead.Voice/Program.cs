using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// <para>Fuentes de audio: el micrófono predeterminado y/o lo que suena en el escritorio (por ejemplo, la voz de la
    /// pareja que llega por Parsec). Cada fuente tiene su propio reconocedor.</para>
    /// <para>Solo escucha entre una orden LISTEN y una STOP (o durante una prueba). No guarda audio ni transcripciones:
    /// el texto reconocido se escribe en la salida estándar para el plugin y se descarta.</para>
    /// <para>Uso: Coophead.Voice.exe --culture es-ES --phrases "gracias|gracias cari" --sources mic,escritorio
    /// --min-confidence 0.6 --desktop-min-confidence 0.6 --desktop-delay-ms 800</para>
    /// </remarks>
    internal static class Program
    {
        /// <summary>Una misma frase oída por el micrófono y por los altavoces solo cuenta una vez.</summary>
        private const long DuplicateWindowMilliseconds = 1000;

        private static readonly object Gate = new object();
        private static readonly Stopwatch Clock = Stopwatch.StartNew();
        private static readonly List<RecognitionChannel> Channels = new List<RecognitionChannel>();
        private static long lastHeardAt = -DuplicateWindowMilliseconds;
        private static Timer testTimer;

        private static int Main(string[] args)
        {
            Console.OutputEncoding = new UTF8Encoding(false);
            Console.InputEncoding = new UTF8Encoding(false);

            string culture = "es-ES";
            IList<string> phrases = new List<string>();
            string sources = VoiceProtocol.SourceMicrophone;
            float minConfidence = 0.6f;
            float desktopMinConfidence = 0.6f;
            int desktopDelayMs = 300;
            for (int i = 0; i + 1 < args.Length; i += 2)
            {
                string value = args[i + 1];
                switch (args[i])
                {
                    case "--culture":
                        culture = value;
                        break;
                    case "--phrases":
                        phrases = VoiceProtocol.SplitPhrases(value);
                        break;
                    case "--sources":
                        sources = value;
                        break;
                    case "--min-confidence":
                        float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out minConfidence);
                        break;
                    case "--desktop-min-confidence":
                        float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out desktopMinConfidence);
                        break;
                    case "--desktop-delay-ms":
                        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out desktopDelayMs);
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

            bool wantMic = sources.IndexOf(VoiceProtocol.SourceMicrophone, StringComparison.OrdinalIgnoreCase) >= 0;
            bool wantDesktop = sources.IndexOf(VoiceProtocol.SourceDesktop, StringComparison.OrdinalIgnoreCase) >= 0;
            var problems = new List<string>();
            var active = new List<string>();

            try
            {
                if (wantMic)
                {
                    TryAddChannel(VoiceProtocol.SourceMicrophone, recognizer, phrases, minConfidence, null, problems, active,
                        "micrófono predeterminado");
                }

                if (wantDesktop)
                {
                    string device;
                    try
                    {
                        device = "escritorio (" + LoopbackSource.DescribeDevice() + ")";
                    }
                    catch (Exception e)
                    {
                        device = null;
                        problems.Add("No se puede capturar el audio del escritorio: " + e.Message);
                    }

                    if (device != null)
                    {
                        TryAddChannel(VoiceProtocol.SourceDesktop, recognizer, phrases, desktopMinConfidence,
                            new LoopbackSource(desktopDelayMs), problems, active, device);
                    }
                }

                foreach (string problem in problems)
                {
                    Write(VoiceProtocol.FormatError(problem));
                }

                if (Channels.Count == 0)
                {
                    Write(VoiceProtocol.FormatUnavailable("Ninguna fuente de audio disponible. " + string.Join(" ", problems.ToArray())));
                    return 4;
                }

                Write(VoiceProtocol.FormatReady(recognizer.Culture.Name + " " + recognizer.Name + " · " + string.Join(" + ", active.ToArray())));
                CommandLoop();
            }
            catch (Exception e)
            {
                Write(VoiceProtocol.FormatUnavailable("Error del reconocedor: " + e.Message));
                return 5;
            }
            finally
            {
                foreach (RecognitionChannel channel in Channels)
                {
                    channel.Dispose();
                }
            }

            return 0;
        }

        private static void TryAddChannel(
            string source,
            RecognizerInfo recognizer,
            IList<string> phrases,
            float minConfidence,
            LoopbackSource loopback,
            List<string> problems,
            List<string> active,
            string description)
        {
            try
            {
                var builder = new GrammarBuilder(new Choices(ToArray(phrases))) { Culture = recognizer.Culture };
                var channel = new RecognitionChannel(source, recognizer, new Grammar(builder) { Name = "coophead-gracias-" + source }, minConfidence, loopback);
                channel.Recognized += OnRecognized;
                channel.LowConfidence += (c, text, confidence) => Write(VoiceProtocol.FormatLow(confidence, text, c.Source));
                channel.Rejected += c => Write(VoiceProtocol.FormatRejected(c.Source));
                channel.Failed += (c, message) => Write(VoiceProtocol.FormatError(c.Source + ": " + message));
                Channels.Add(channel);
                active.Add(description);
            }
            catch (InvalidOperationException)
            {
                problems.Add("No hay micrófono predeterminado en Windows o no se puede abrir.");
            }
            catch (Exception e)
            {
                problems.Add("No se pudo preparar la fuente " + source + ": " + e.Message);
            }
        }

        private static void OnRecognized(RecognitionChannel channel, string text, float confidence)
        {
            lock (Gate)
            {
                long now = Clock.ElapsedMilliseconds;
                if (now - lastHeardAt < DuplicateWindowMilliseconds)
                {
                    return;
                }

                lastHeardAt = now;
            }

            Write(VoiceProtocol.FormatHeard(confidence, text, channel.Source));
        }

        /// <summary>Lee órdenes hasta QUIT o hasta que el plugin cierre la entrada (el juego se ha cerrado).</summary>
        private static void CommandLoop()
        {
            string line;
            while ((line = Console.In.ReadLine()) != null)
            {
                // La primera línea puede traer un BOM invisible del plugin: sin limpiarla, se perdía el primer LISTEN.
                line = VoiceProtocol.CleanLine(line);
                if (line.Length == 0)
                {
                    continue;
                }

                if (line == VoiceProtocol.Quit)
                {
                    return;
                }

                if (line == VoiceProtocol.Listen)
                {
                    CancelTest();
                    StartAll();
                    continue;
                }

                if (line == VoiceProtocol.Stop)
                {
                    CancelTest();
                    StopAll();
                    continue;
                }

                float? testSeconds = VoiceProtocol.ParseTest(line);
                if (testSeconds.HasValue)
                {
                    CancelTest();
                    StartAll();
                    testTimer = new Timer(_ =>
                    {
                        StopAll();
                        Write(VoiceProtocol.TestEnd);
                    }, null, (int)(testSeconds.Value * 1000f), Timeout.Infinite);
                    continue;
                }

                Write(VoiceProtocol.FormatError("Orden desconocida."));
            }
        }

        private static void StartAll()
        {
            foreach (RecognitionChannel channel in Channels)
            {
                try
                {
                    channel.Start();
                }
                catch (Exception e)
                {
                    Write(VoiceProtocol.FormatError(channel.Source + ": " + e.Message));
                }
            }

            Write(VoiceProtocol.Listening);
        }

        private static void StopAll()
        {
            foreach (RecognitionChannel channel in Channels)
            {
                try
                {
                    channel.Stop();
                }
                catch (Exception e)
                {
                    Write(VoiceProtocol.FormatError(channel.Source + ": " + e.Message));
                }
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
