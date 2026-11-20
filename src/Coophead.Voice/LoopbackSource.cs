using System;
using System.Diagnostics;
using System.Speech.AudioFormat;
using System.Threading;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Coophead.Voice
{
    /// <summary>
    /// Captura lo que suena por el dispositivo de salida predeterminado de Windows (WASAPI en bucle): por ejemplo,
    /// la voz de la pareja que llega por Parsec. Convierte el audio a 16 kHz, 16 bits, mono para el reconocedor.
    /// Solo captura entre <see cref="Start"/> y <see cref="Stop"/>; no guarda nada.
    /// </summary>
    internal sealed class LoopbackSource : IDisposable
    {
        private const int BytesPerMillisecond = 32; // 16000 muestras/s × 2 bytes.
        private const int ChunkBytes = 3200; // 100 ms.

        /// <summary>Si el escritorio deja de enviar audio (no suena nada), se rellena con silencio a partir de este tiempo.</summary>
        private const int SilenceAfterMilliseconds = 120;

        private readonly int ignoreInitialMilliseconds;
        private WasapiLoopbackCapture capture;
        private BufferedWaveProvider buffer;
        private IWaveProvider pcm16;
        private AudioPipeStream pipe;
        private Thread pump;
        private volatile bool pumping;

        /// <param name="ignoreInitialMilliseconds">
        /// Silencio inicial: al abrirse la ventana suena el efecto de reanimación del juego, que no debe confundirse
        /// con un agradecimiento.
        /// </param>
        public LoopbackSource(int ignoreInitialMilliseconds)
        {
            this.ignoreInitialMilliseconds = Math.Max(0, ignoreInitialMilliseconds);
        }

        public static SpeechAudioFormatInfo Format
        {
            get { return new SpeechAudioFormatInfo(16000, AudioBitsPerSample.Sixteen, AudioChannel.Mono); }
        }

        /// <summary>Descripción del dispositivo que se captura, para el log.</summary>
        public static string DescribeDevice()
        {
            using (var enumerator = new MMDeviceEnumerator())
            using (MMDevice device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia))
            {
                return device.FriendlyName;
            }
        }

        public AudioPipeStream Start()
        {
            Stop();
            capture = new WasapiLoopbackCapture();
            WaveFormat format = Standardize(capture.WaveFormat);
            buffer = new BufferedWaveProvider(format)
            {
                DiscardOnBufferOverflow = true,
                BufferDuration = TimeSpan.FromSeconds(5),
                // Solo se entrega lo que ha llegado de verdad: antes se rellenaba con ceros cada lectura y la voz llegaba
                // al reconocedor con microcortes dentro de las palabras.
                ReadFully = false,
            };
            capture.DataAvailable += (sender, e) => buffer.AddSamples(e.Buffer, 0, e.BytesRecorded);

            ISampleProvider samples = buffer.ToSampleProvider();
            if (samples.WaveFormat.Channels > 1)
            {
                samples = new DownmixToMono(samples);
            }

            if (samples.WaveFormat.SampleRate != 16000)
            {
                samples = new WdlResamplingSampleProvider(samples, 16000);
            }

            pcm16 = new SampleToWaveProvider16(samples);
            pipe = new AudioPipeStream();
            capture.StartRecording();

            pumping = true;
            pump = new Thread(Pump) { IsBackground = true, Name = "Coophead loopback" };
            pump.Start();
            return pipe;
        }

        public void Stop()
        {
            pumping = false;
            if (pump != null)
            {
                pump.Join(500);
                pump = null;
            }

            if (capture != null)
            {
                try
                {
                    capture.StopRecording();
                }
                catch (Exception)
                {
                    // Ya estaba parada.
                }

                capture.Dispose();
                capture = null;
            }

            if (pipe != null)
            {
                pipe.Complete();
                pipe = null;
            }
        }

        public void Dispose()
        {
            Stop();
        }

        /// <summary>
        /// Entrega el audio tal como llega, sin huecos. Si el escritorio deja de enviar (no suena nada), añade silencio al
        /// ritmo del reloj para que el reconocedor pueda cerrar una frase aunque después no haya más sonido.
        /// </summary>
        private void Pump()
        {
            var clock = Stopwatch.StartNew();
            var chunk = new byte[ChunkBytes];
            long lastWrite = 0;
            while (pumping)
            {
                int read = pcm16.Read(chunk, 0, ChunkBytes);
                long now = clock.ElapsedMilliseconds;
                if (read > 0)
                {
                    if (now < ignoreInitialMilliseconds)
                    {
                        Array.Clear(chunk, 0, read);
                    }

                    pipe.Write(chunk, 0, read);
                    lastWrite = now;
                    continue;
                }

                if (now - lastWrite >= SilenceAfterMilliseconds)
                {
                    int bytes = (int)Math.Min(ChunkBytes, (now - lastWrite) * BytesPerMillisecond);
                    bytes -= bytes % 2;
                    Array.Clear(chunk, 0, bytes);
                    pipe.Write(chunk, 0, bytes);
                    lastWrite = now;
                }

                Thread.Sleep(10);
            }
        }

        /// <summary>WASAPI suele dar un formato «extensible»: se describe como float o PCM estándar con la misma disposición.</summary>
        private static WaveFormat Standardize(WaveFormat format)
        {
            if (format.Encoding != WaveFormatEncoding.Extensible)
            {
                return format;
            }

            return format.BitsPerSample == 32
                ? WaveFormat.CreateIeeeFloatWaveFormat(format.SampleRate, format.Channels)
                : new WaveFormat(format.SampleRate, format.BitsPerSample, format.Channels);
        }

        /// <summary>Mezcla cualquier número de canales en uno promediándolos.</summary>
        private sealed class DownmixToMono : ISampleProvider
        {
            private readonly ISampleProvider source;
            private readonly int channels;
            private float[] sourceBuffer = new float[0];

            public DownmixToMono(ISampleProvider source)
            {
                this.source = source;
                channels = source.WaveFormat.Channels;
                WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(source.WaveFormat.SampleRate, 1);
            }

            public WaveFormat WaveFormat { get; private set; }

            public int Read(float[] destination, int offset, int count)
            {
                int needed = count * channels;
                if (sourceBuffer.Length < needed)
                {
                    sourceBuffer = new float[needed];
                }

                int read = source.Read(sourceBuffer, 0, needed);
                int frames = read / channels;
                for (int frame = 0; frame < frames; frame++)
                {
                    float sum = 0f;
                    for (int channel = 0; channel < channels; channel++)
                    {
                        sum += sourceBuffer[(frame * channels) + channel];
                    }

                    destination[offset + frame] = sum / channels;
                }

                return frames;
            }
        }
    }
}
