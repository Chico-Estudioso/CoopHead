using System.Threading;
using Coophead.Voice;
using Xunit;

namespace Coophead.Core.Tests
{
    public class AudioPipeStreamTests
    {
        [Fact]
        public void LaLecturaEsperaHastaTenerTodoLoPedido()
        {
            // SAPI toma una lectura corta como fin del flujo: la tubería no puede devolver menos de lo pedido mientras siga viva.
            var pipe = new AudioPipeStream();
            pipe.Write(Bytes(100, 1), 0, 100);
            var destination = new byte[300];
            int read = -1;
            var reader = new Thread(() => read = pipe.Read(destination, 0, 300));
            reader.Start();

            Assert.False(reader.Join(150));

            pipe.Write(Bytes(200, 2), 0, 200);
            Assert.True(reader.Join(2000));
            Assert.Equal(300, read);
            Assert.Equal(1, destination[0]);
            Assert.Equal(1, destination[99]);
            Assert.Equal(2, destination[100]);
            Assert.Equal(2, destination[299]);
        }

        [Fact]
        public void AlCompletarseDevuelveLoQueQuedaYDespuesCero()
        {
            var pipe = new AudioPipeStream();
            pipe.Write(Bytes(50, 7), 0, 50);
            var destination = new byte[300];
            int read = -1;
            var reader = new Thread(() => read = pipe.Read(destination, 0, 300));
            reader.Start();
            Assert.False(reader.Join(100));

            pipe.Complete();

            Assert.True(reader.Join(2000));
            Assert.Equal(50, read);
            Assert.Equal(0, pipe.Read(destination, 0, 300));
        }

        [Fact]
        public void LeeEnElOrdenEscritoAunqueDeLaVuelta()
        {
            var pipe = new AudioPipeStream();
            var destination = new byte[150000];
            for (int round = 0; round < 3; round++)
            {
                pipe.Write(Bytes(150000, (byte)(round + 1)), 0, 150000);
                Assert.Equal(150000, pipe.Read(destination, 0, 150000));
                Assert.Equal(round + 1, destination[0]);
                Assert.Equal(round + 1, destination[149999]);
            }
        }

        [Fact]
        public void SiElLectorSeRetrasaSeDescartaLoMasAntiguo()
        {
            var pipe = new AudioPipeStream();
            pipe.Write(Bytes(160000, 1), 0, 160000);
            pipe.Write(Bytes(10, 9), 0, 10);
            pipe.Complete();
            var destination = new byte[200000];

            int read = pipe.Read(destination, 0, destination.Length);

            Assert.Equal(160000, read);
            Assert.Equal(1, destination[0]);
            Assert.Equal(9, destination[read - 1]);
        }

        private static byte[] Bytes(int length, byte value)
        {
            var bytes = new byte[length];
            for (int i = 0; i < length; i++)
            {
                bytes[i] = value;
            }

            return bytes;
        }
    }
}
