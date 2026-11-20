using System.Collections.Generic;
using Coophead.Core.Thanks;
using Coophead.Core.Voice;
using Xunit;

namespace Coophead.Core.Tests
{
    public class VoiceProtocolTests
    {
        [Fact]
        public void HeardIdaYVuelta()
        {
            string line = VoiceProtocol.FormatHeard(0.8234f, "gracias cariño");

            Assert.Equal("HEARD 0.82 gracias cariño", line);
            VoiceMessage message = VoiceProtocol.Parse(line);
            Assert.Equal(VoiceMessageKind.Heard, message.Kind);
            Assert.Equal("gracias cariño", message.Text);
            Assert.Equal(0.82f, message.Confidence, 3);
        }

        [Theory]
        [InlineData("escritorio", "gracias cari")]
        [InlineData("mic", "gracias")]
        public void HeardConFuente(string source, string text)
        {
            VoiceMessage message = VoiceProtocol.Parse(VoiceProtocol.FormatHeard(0.7f, text, source));

            Assert.Equal(VoiceMessageKind.Heard, message.Kind);
            Assert.Equal(source, message.Source);
            Assert.Equal(text, message.Text);
        }

        [Theory]
        [InlineData("mic", true)]
        [InlineData("escritorio", true)]
        [InlineData("escritorio2", true)]
        [InlineData("escritorio12", true)]
        [InlineData("escritorioX", false)]
        [InlineData("gracias", false)]
        [InlineData("", false)]
        public void FuentesConocidas(string source, bool known)
        {
            Assert.Equal(known, VoiceProtocol.IsKnownSource(source));
        }

        [Fact]
        public void HeardDeUnaSalidaConcreta()
        {
            VoiceMessage message = VoiceProtocol.Parse(VoiceProtocol.FormatHeard(0.72f, "gracias amor", "escritorio2"));

            Assert.Equal("escritorio2", message.Source);
            Assert.Equal("gracias amor", message.Text);
        }

        [Fact]
        public void FrasesDudosasYNoEntendidas()
        {
            VoiceMessage low = VoiceProtocol.Parse(VoiceProtocol.FormatLow(0.41f, "gracias cari", "escritorio"));
            VoiceMessage rejected = VoiceProtocol.Parse(VoiceProtocol.FormatRejected("mic"));

            Assert.Equal(VoiceMessageKind.Low, low.Kind);
            Assert.Equal(0.41f, low.Confidence, 3);
            Assert.Equal("escritorio", low.Source);
            Assert.Equal("gracias cari", low.Text);
            Assert.Equal(VoiceMessageKind.Rejected, rejected.Kind);
            Assert.Equal("mic", rejected.Source);
        }

        [Theory]
        [InlineData("\uFEFFLISTEN", "LISTEN")]
        [InlineData("LISTEN\r", "LISTEN")]
        [InlineData("  STOP \r\n", "STOP")]
        [InlineData(null, "")]
        public void LimpiaBomYRetornosDeCarro(string raw, string expected)
        {
            Assert.Equal(expected, VoiceProtocol.CleanLine(raw));
        }

        [Fact]
        public void ElTextoReconocidoNoArrastraElRetornoDeCarro()
        {
            VoiceMessage message = VoiceProtocol.Parse("HEARD 0.67 mic gracias cari\r");

            Assert.Equal("gracias cari", message.Text);
        }

        [Fact]
        public void HeardSinFuenteSigueFuncionando()
        {
            VoiceMessage message = VoiceProtocol.Parse("HEARD 0.70 gracias micro");

            Assert.Equal("gracias micro", message.Text);
            Assert.Equal(string.Empty, message.Source);
        }

        [Theory]
        [InlineData("READY es-ES Microsoft Speech Recognizer", VoiceMessageKind.Ready, "es-ES Microsoft Speech Recognizer")]
        [InlineData("UNAVAILABLE no hay micrófono", VoiceMessageKind.Unavailable, "no hay micrófono")]
        [InlineData("LISTENING", VoiceMessageKind.Listening, "")]
        [InlineData("STOPPED", VoiceMessageKind.Stopped, "")]
        [InlineData("ERROR algo", VoiceMessageKind.Error, "algo")]
        [InlineData("TEST_END", VoiceMessageKind.TestEnd, "")]
        [InlineData("HEARD x gracias", VoiceMessageKind.Unknown, "x gracias")]
        [InlineData("BASURA", VoiceMessageKind.Unknown, "BASURA")]
        [InlineData("", VoiceMessageKind.Unknown, "")]
        [InlineData(null, VoiceMessageKind.Unknown, "")]
        public void ParseaLosMensajesDelAyudante(string line, VoiceMessageKind kind, string text)
        {
            VoiceMessage message = VoiceProtocol.Parse(line);

            Assert.Equal(kind, message.Kind);
            Assert.Equal(text, message.Text);
        }

        [Fact]
        public void LosMensajesNoPuedenRomperLaLinea()
        {
            Assert.Equal("ERROR a b", VoiceProtocol.FormatError("a\nb"));
            Assert.Equal("UNAVAILABLE x y", VoiceProtocol.FormatUnavailable("x\r\ny").Replace("  ", " "));
        }

        [Theory]
        [InlineData(5f, "TEST 5", 5f)]
        [InlineData(0f, "TEST 1", 1f)]
        [InlineData(99f, "TEST 15", 15f)]
        public void OrdenDePruebaAcotada(float seconds, string expected, float parsed)
        {
            Assert.Equal(expected, VoiceProtocol.Test(seconds));
            Assert.Equal(parsed, VoiceProtocol.ParseTest(expected));
        }

        [Fact]
        public void OrdenesDesconocidasNoSonPruebas()
        {
            Assert.Null(VoiceProtocol.ParseTest("TEST abc"));
            Assert.Null(VoiceProtocol.ParseTest("LISTEN"));
            Assert.Null(VoiceProtocol.ParseTest(null));
        }

        [Fact]
        public void LasFrasesSeLimpianYSeUnenSinDuplicados()
        {
            string joined = VoiceProtocol.JoinPhrases(new List<string> { "Gracias", "gracias  cariño!", "gracias", "\"; rm -rf", "" });

            Assert.Equal("gracias|gracias cariño|rm rf", joined);
            Assert.Equal(new[] { "gracias", "gracias cariño", "rm rf" }, VoiceProtocol.SplitPhrases(joined));
            Assert.Empty(VoiceProtocol.SplitPhrases(null));
        }

        [Fact]
        public void LaGramaticaHabladaConservaTildes()
        {
            var config = new ThanksConfig { AcceptedNames = new List<string> { "cari", "cariño", "Amor" } };
            var matcher = new ThanksPhraseMatcher(config);

            Assert.Equal(new[] { "gracias", "gracias cari", "gracias cariño", "gracias amor" }, matcher.BuildSpokenGrammar());
            Assert.True(matcher.Matches("Gracias, cariño"));
        }

        [Fact]
        public void ExigiendoApelativoLaGramaticaNoIncluyeGraciasSolo()
        {
            var matcher = new ThanksPhraseMatcher(new ThanksConfig { RequireName = true });

            Assert.DoesNotContain("gracias", matcher.BuildSpokenGrammar());
        }
    }
}
