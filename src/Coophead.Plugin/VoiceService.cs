using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Coophead.Core.Diagnostics;
using Coophead.Core.Voice;

namespace Coophead.Plugin
{
    /// <summary>
    /// Gestiona el ayudante de voz local (Coophead.Voice.exe). Las líneas llegan en un hilo secundario y se
    /// encolan; el plugin las procesa en el hilo principal con <see cref="Drain"/>.
    /// </summary>
    /// <summary>Fuentes de audio para reconocer el agradecimiento.</summary>
    public enum VoiceSourceMode
    {
        Microfono,
        Escritorio,
        Ambos,
    }

    internal sealed class VoiceService : IDisposable
    {
        public sealed class Options
        {
            public string Culture { get; set; }

            public IList<string> Phrases { get; set; }

            public VoiceSourceMode Sources { get; set; }

            public float MinConfidence { get; set; }

            public float DesktopMinConfidence { get; set; }

            public float DesktopDelaySeconds { get; set; }

            /// <summary>«todos», «predeterminado» o parte del nombre de una salida de audio.</summary>
            public string DesktopDevices { get; set; }
        }

        private const string ExitedMarker = "\u0000EXITED";

        private readonly ICoreLog log;
        private readonly object gate = new object();
        private readonly Queue<string> inbox = new Queue<string>();
        private Process process;
        private bool ready;
        private bool listening;

        public VoiceService(ICoreLog log)
        {
            this.log = log;
        }

        public bool Available
        {
            get { return ready && process != null; }
        }

        public bool Listening
        {
            get { return listening; }
        }

        public bool Start(string executablePath, Options options)
        {
            if (!File.Exists(executablePath))
            {
                log.Warning("Voz: no se encuentra el ayudante en " + Path.GetFileName(executablePath) + "; la voz queda desactivada.");
                return false;
            }

            string joined = VoiceProtocol.JoinPhrases(options.Phrases);
            var info = new ProcessStartInfo(executablePath)
            {
                Arguments = "--culture \"" + SanitizeCulture(options.Culture) + "\" --phrases \"" + joined
                    + "\" --sources " + DescribeSources(options.Sources)
                    + " --min-confidence " + options.MinConfidence.ToString("0.00", CultureInfo.InvariantCulture)
                    + " --desktop-min-confidence " + options.DesktopMinConfidence.ToString("0.00", CultureInfo.InvariantCulture)
                    + " --desktop-delay-ms " + ((int)(options.DesktopDelaySeconds * 1000f)).ToString(CultureInfo.InvariantCulture)
                    + " --desktop-devices \"" + SanitizeDeviceChoice(options.DesktopDevices) + "\"",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
            };

            try
            {
                process = new Process { StartInfo = info, EnableRaisingEvents = true };
                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        Enqueue(e.Data);
                    }
                };
                process.Exited += (sender, e) => Enqueue(ExitedMarker);
                process.Start();
                process.BeginOutputReadLine();
                log.Info("Voz: ayudante iniciado; esperando a que cargue el reconocedor de Windows.");
                return true;
            }
            catch (Exception e)
            {
                log.Warning("Voz: no se pudo iniciar el ayudante (" + e.Message + "); la voz queda desactivada.");
                process = null;
                return false;
            }
        }

        public void Send(string command)
        {
            if (process == null)
            {
                return;
            }

            try
            {
                process.StandardInput.WriteLine(command);
                process.StandardInput.Flush();
            }
            catch (Exception e)
            {
                log.Warning("Voz: no se pudo enviar «" + command + "» al ayudante: " + e.Message);
                MarkGone();
            }
        }

        /// <summary>Mensajes pendientes del ayudante. Llamar desde el hilo principal.</summary>
        public IList<VoiceMessage> Drain()
        {
            var messages = new List<VoiceMessage>();
            lock (gate)
            {
                while (inbox.Count > 0)
                {
                    string line = inbox.Dequeue();
                    if (line == ExitedMarker)
                    {
                        messages.Add(new VoiceMessage(VoiceMessageKind.Unavailable, "El ayudante de voz se ha cerrado.", 0f));
                        continue;
                    }

                    messages.Add(VoiceProtocol.Parse(line));
                }
            }

            foreach (VoiceMessage message in messages)
            {
                switch (message.Kind)
                {
                    case VoiceMessageKind.Ready:
                        ready = true;
                        break;
                    case VoiceMessageKind.Unavailable:
                        MarkGone();
                        break;
                    case VoiceMessageKind.Listening:
                        listening = true;
                        break;
                    case VoiceMessageKind.Stopped:
                    case VoiceMessageKind.TestEnd:
                        listening = false;
                        break;
                }
            }

            return messages;
        }

        public void Dispose()
        {
            Process current = process;
            if (current == null)
            {
                return;
            }

            Send(VoiceProtocol.Quit);
            try
            {
                if (!current.WaitForExit(500))
                {
                    current.Kill();
                }
            }
            catch (Exception)
            {
                // El proceso ya no existe.
            }

            MarkGone();
        }

        private static string DescribeSources(VoiceSourceMode mode)
        {
            switch (mode)
            {
                case VoiceSourceMode.Escritorio:
                    return VoiceProtocol.SourceDesktop;
                case VoiceSourceMode.Ambos:
                    return VoiceProtocol.SourceMicrophone + "," + VoiceProtocol.SourceDesktop;
                default:
                    return VoiceProtocol.SourceMicrophone;
            }
        }

        /// <summary>Nombre de dispositivo sin comillas ni caracteres de control, para la línea de órdenes.</summary>
        private static string SanitizeDeviceChoice(string choice)
        {
            var builder = new StringBuilder();
            foreach (char c in (choice ?? string.Empty).Trim())
            {
                if (c != '"' && c != '\\' && !char.IsControl(c))
                {
                    builder.Append(c);
                }
            }

            return builder.Length == 0 ? "todos" : builder.ToString();
        }

        /// <summary>Solo letras y guiones («es-ES»); cualquier otra cosa vuelve al valor por defecto.</summary>
        private static string SanitizeCulture(string culture)
        {
            var builder = new StringBuilder();
            foreach (char c in (culture ?? string.Empty).Trim())
            {
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '-')
                {
                    builder.Append(c);
                }
            }

            return builder.Length >= 2 && builder.Length <= 12 ? builder.ToString() : "es-ES";
        }

        private void Enqueue(string line)
        {
            lock (gate)
            {
                inbox.Enqueue(line);
            }
        }

        private void MarkGone()
        {
            ready = false;
            listening = false;
            process = null;
        }
    }
}
