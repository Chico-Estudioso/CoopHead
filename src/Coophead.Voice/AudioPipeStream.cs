using System;
using System.IO;
using System.Threading;

namespace Coophead.Voice
{
    /// <summary>
    /// Tubería en memoria entre la captura del escritorio y el reconocedor: Write añade audio y Read espera hasta
    /// que haya datos. Al completarla, Read devuelve 0 (fin de flujo) y el reconocedor termina. Nada se guarda.
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

        public override int Read(byte[] destination, int offset, int requested)
        {
            lock (gate)
            {
                while (count == 0 && !completed)
                {
                    Monitor.Wait(gate);
                }

                if (count == 0)
                {
                    return 0;
                }

                int toCopy = Math.Min(requested, count);
                for (int i = 0; i < toCopy; i++)
                {
                    destination[offset + i] = buffer[(start + i) % buffer.Length];
                }

                start = (start + toCopy) % buffer.Length;
                count -= toCopy;
                return toCopy;
            }
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

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }
    }
}
