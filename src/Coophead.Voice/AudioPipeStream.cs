using System;
using System.IO;
using System.Threading;

namespace Coophead.Voice
{
    /// <summary>
    /// Tubería en memoria entre la captura del escritorio y el reconocedor: Write añade audio y Read espera hasta
    /// tener todo lo pedido. Al completarla, Read devuelve lo que quede (fin de flujo) y el reconocedor termina.
    /// Nada se guarda.
    /// </summary>
    internal sealed class AudioPipeStream : Stream
    {
        private const int MaxBufferedBytes = 16000 * 2 * 5; // 5 s a 16 kHz, 16 bits, mono.
        private readonly object gate = new object();
        private readonly byte[] buffer = new byte[MaxBufferedBytes];
        private int start;
        private int count;
        private bool completed;

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        // System.Speech consulta estas propiedades; un flujo en vivo no tiene longitud ni posición reales.
        public override long Length
        {
            get { return -1L; }
        }

        public override long Position
        {
            get { return 0L; }
            set { }
        }

        public void Complete()
        {
            lock (gate)
            {
                completed = true;
                Monitor.PulseAll(gate);
            }
        }

        /// <summary>
        /// Espera hasta tener todo lo pedido. El reconocedor de Windows (SAPI) interpreta una lectura más corta de lo
        /// pedido como fin del flujo y deja de escuchar sin avisar: por eso el escritorio no reconocía nada. Solo se
        /// devuelve menos cuando la tubería se ha completado (fin real).
        /// </summary>
        public override int Read(byte[] destination, int offset, int requested)
        {
            int copied = 0;
            lock (gate)
            {
                while (copied < requested)
                {
                    while (count == 0 && !completed)
                    {
                        Monitor.Wait(gate);
                    }

                    if (count == 0)
                    {
                        break;
                    }

                    int toCopy = Math.Min(requested - copied, count);
                    for (int i = 0; i < toCopy; i++)
                    {
                        destination[offset + copied + i] = buffer[(start + i) % buffer.Length];
                    }

                    start = (start + toCopy) % buffer.Length;
                    count -= toCopy;
                    copied += toCopy;
                }
            }

            return copied;
        }

        public override void Write(byte[] source, int offset, int length)
        {
            lock (gate)
            {
                if (completed)
                {
                    return;
                }

                for (int i = 0; i < length; i++)
                {
                    if (count == buffer.Length)
                    {
                        // Si el reconocedor se retrasa, se descarta lo más antiguo: solo importa el audio reciente.
                        start = (start + 1) % buffer.Length;
                        count--;
                    }

                    buffer[(start + count) % buffer.Length] = source[offset + i];
                    count++;
                }

                Monitor.PulseAll(gate);
            }
        }

        public override void Flush()
        {
        }

        // System.Speech llama a Seek y SetLength al preparar la entrada: en un flujo en vivo no tienen sentido, pero
        // lanzar NotSupportedException hace que SetInputToAudioStream falle («El método especificado no es compatible»).
        public override long Seek(long offset, SeekOrigin origin)
        {
            return 0L;
        }

        public override void SetLength(long value)
        {
        }
    }
}
