using System.Collections.Generic;
using Coophead.Core.Thanks;
using Xunit;

namespace Coophead.Core.Tests
{
    public class ThanksTests
    {
        private const PlayerSlot P1 = PlayerSlot.PlayerOne;
        private const PlayerSlot P2 = PlayerSlot.PlayerTwo;

        [Theory]
        [InlineData("¡Gracias, Kari!", "gracias kari")]
        [InlineData("  GRÁCIAS   amor ", "gracias amor")]
        [InlineData("Muchas gracias, cari.", "muchas gracias cari")]
        [InlineData("¿Señor?", "senor")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void NormalizaTextoReconocido(string input, string expected)
        {
            Assert.Equal(expected, ThanksPhraseMatcher.Normalize(input));
        }

        [Theory]
        [InlineData("Gracias, Kari", true)]
        [InlineData("gracias cari", true)]
        [InlineData("Gracias, amor", true)]
        [InlineData("muchas gracias", true)]
        [InlineData("gracias cielo", true)]
        [InlineData("gracia", false)]
        [InlineData("de nada", false)]
        [InlineData("engraciado", false)]
        [InlineData("", false)]
        public void PorDefectoBastaConDecirGracias(string transcript, bool expected)
        {
            var matcher = new ThanksPhraseMatcher(new ThanksConfig());

            Assert.Equal(expected, matcher.Matches(transcript));
        }

        [Theory]
        [InlineData("Gracias, Kari", true)]
        [InlineData("gracias amor", true)]
        [InlineData("cari, gracias", true)]
        [InlineData("gracias", false)]
        [InlineData("gracias cielo", false)]
        public void ExigiendoApelativoSoloValenLosConfigurados(string transcript, bool expected)
        {
            var matcher = new ThanksPhraseMatcher(new ThanksConfig { RequireName = true });

            Assert.Equal(expected, matcher.Matches(transcript));
        }

        [Fact]
        public void LaGramaticaIncluyeGraciasYLosApelativos()
        {
            var matcher = new ThanksPhraseMatcher(new ThanksConfig());

            Assert.Equal(new[] { "gracias", "gracias cari", "gracias kari", "gracias amor" }, matcher.BuildGrammar());
        }

        [Fact]
        public void LaValidacionCorrigeValoresVacios()
        {
            var config = new ThanksConfig { RequiredWord = " ¡! ", WindowSeconds = 0f, RequireName = true, AcceptedNames = new List<string> { "", "  " } };

            Assert.Equal(3, config.Validate().Count);
            Assert.Equal("gracias", config.RequiredWord);
            Assert.Equal(3f, config.WindowSeconds);
            Assert.False(config.RequireName);
        }

        private static ThanksWindowTracker VoiceTracker(ThanksConfig config = null)
        {
            var tracker = new ThanksWindowTracker(config ?? new ThanksConfig());
            tracker.SetVoiceAvailable(true);
            return tracker;
        }

        [Fact]
        public void LaFraseDentroDeLaVentanaCuentaComoAgradecimiento()
        {
            ThanksWindowTracker tracker = VoiceTracker();
            ThanksWindow window = tracker.Open(P1, P2, 10f, new List<ThanksWindow>());

            ThanksWindow resolved = tracker.OnTranscript("Gracias, Kari", 12.5f);

            Assert.Same(window, resolved);
            Assert.Equal(ThanksResolution.Thanked, window.Resolution);
            Assert.Empty(tracker.Pending);
        }

        [Fact]
        public void SinFraseLaVentanaExpiraComoFallada()
        {
            ThanksWindowTracker tracker = VoiceTracker();
            ThanksWindow window = tracker.Open(P1, P2, 10f, new List<ThanksWindow>());
            var resolved = new List<ThanksWindow>();

            tracker.Tick(12.9f, resolved);
            Assert.Empty(resolved);

            tracker.Tick(13f, resolved);
            Assert.Single(resolved);
            Assert.Equal(ThanksResolution.Missed, window.Resolution);
        }

        [Fact]
        public void LaFraseFueraDeVentanaNoCuenta()
        {
            ThanksWindowTracker tracker = VoiceTracker();
            Assert.Null(tracker.OnTranscript("gracias", 0f));

            tracker.Open(P1, P2, 10f, new List<ThanksWindow>());
            Assert.Null(tracker.OnTranscript("gracias", 13.5f));
        }

        [Fact]
        public void UnaFraseNoReconocidaNoCierraLaVentana()
        {
            ThanksWindowTracker tracker = VoiceTracker();
            tracker.Open(P1, P2, 0f, new List<ThanksWindow>());

            Assert.Null(tracker.OnTranscript("de nada", 1f));
            Assert.Single(tracker.Pending);
        }

        [Fact]
        public void UnaFraseCierraSoloLaVentanaMasAntigua()
        {
            ThanksWindowTracker tracker = VoiceTracker();
            ThanksWindow first = tracker.Open(P1, P2, 0f, new List<ThanksWindow>());
            ThanksWindow second = tracker.Open(P2, P1, 1f, new List<ThanksWindow>());

            Assert.Same(first, tracker.OnTranscript("gracias", 1.5f));
            Assert.True(second.IsPending);
            Assert.Same(second, tracker.OnTranscript("gracias amor", 2f));
        }

        [Fact]
        public void UnaNuevaReanimacionDelMismoJugadorSustituyeLaVentanaSinCastigo()
        {
            ThanksWindowTracker tracker = VoiceTracker();
            ThanksWindow old = tracker.Open(P1, P2, 0f, new List<ThanksWindow>());
            var resolved = new List<ThanksWindow>();

            tracker.Open(P1, P2, 1f, resolved);

            Assert.Equal(ThanksResolution.NotEvaluated, old.Resolution);
            Assert.Contains(old, resolved);
            Assert.Single(tracker.Pending);
        }

        [Fact]
        public void UnFalloTecnicoNuncaCastiga()
        {
            ThanksWindowTracker tracker = VoiceTracker(new ThanksConfig { ManualFallbackEnabled = false });
            ThanksWindow window = tracker.Open(P1, P2, 0f, new List<ThanksWindow>());

            tracker.ReportTechnicalFailure();
            var resolved = new List<ThanksWindow>();
            tracker.Tick(5f, resolved);

            Assert.Equal(ThanksResolution.NotEvaluated, window.Resolution);
        }

        [Fact]
        public void PerderLaVozSinRespaldoManualNoCastiga()
        {
            ThanksWindowTracker tracker = VoiceTracker(new ThanksConfig { ManualFallbackEnabled = false });
            ThanksWindow window = tracker.Open(P1, P2, 0f, new List<ThanksWindow>());

            tracker.SetVoiceAvailable(false);
            tracker.Tick(5f, new List<ThanksWindow>());

            Assert.Equal(ThanksResolution.NotEvaluated, window.Resolution);
        }

        [Fact]
        public void SinVozNiRespaldoLaVentanaNoSeEvalua()
        {
            var tracker = new ThanksWindowTracker(new ThanksConfig { ManualFallbackEnabled = false });
            ThanksWindow window = tracker.Open(P1, P2, 0f, new List<ThanksWindow>());

            Assert.False(tracker.CanEvaluate);
            tracker.Tick(5f, new List<ThanksWindow>());

            Assert.Equal(ThanksResolution.NotEvaluated, window.Resolution);
        }

        [Fact]
        public void ElRespaldoManualCierraLaVentanaDelJugadorIndicado()
        {
            var tracker = new ThanksWindowTracker(new ThanksConfig());
            ThanksWindow p1Window = tracker.Open(P1, P2, 0f, new List<ThanksWindow>());
            ThanksWindow p2Window = tracker.Open(P2, P1, 0.5f, new List<ThanksWindow>());

            Assert.Same(p2Window, tracker.OnManualConfirm(P2, 1f));
            Assert.Equal(ThanksResolution.ThankedManually, p2Window.Resolution);
            Assert.True(p1Window.IsPending);
        }

        [Fact]
        public void SinVozLaTranscripcionSeIgnora()
        {
            var tracker = new ThanksWindowTracker(new ThanksConfig());
            tracker.Open(P1, P2, 0f, new List<ThanksWindow>());

            Assert.Null(tracker.OnTranscript("gracias", 1f));
        }

        [Fact]
        public void SiNoSeExigeAgradecimientoNoSeAbreVentana()
        {
            var tracker = new ThanksWindowTracker(new ThanksConfig { RequireThanks = false });

            Assert.Null(tracker.Open(P1, P2, 0f, new List<ThanksWindow>()));
        }

        [Fact]
        public void CerrarTodoNoCastigaANadie()
        {
            ThanksWindowTracker tracker = VoiceTracker();
            ThanksWindow a = tracker.Open(P1, P2, 0f, new List<ThanksWindow>());
            ThanksWindow b = tracker.Open(P2, P1, 0f, new List<ThanksWindow>());
            var resolved = new List<ThanksWindow>();

            tracker.CloseAll("Fin de nivel.", resolved);

            Assert.Equal(2, resolved.Count);
            Assert.Equal(ThanksResolution.NotEvaluated, a.Resolution);
            Assert.Equal(ThanksResolution.NotEvaluated, b.Resolution);
        }
    }
}
