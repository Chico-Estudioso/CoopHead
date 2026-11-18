using System.Collections.Generic;
using Coophead.Core.Roulette;
using Coophead.Core.Rules;
using Coophead.Core.Thanks;
using Xunit;

namespace Coophead.Core.Tests
{
    public class CoopheadSessionTests
    {
        private const PlayerSlot P1 = PlayerSlot.PlayerOne;
        private const PlayerSlot P2 = PlayerSlot.PlayerTwo;

        private readonly FakeApplier applier = new FakeApplier();
        private readonly List<RouletteResult> spins = new List<RouletteResult>();
        private readonly List<RestartRequest> restarts = new List<RestartRequest>();
        private readonly List<ThanksWindow> opened = new List<ThanksWindow>();

        private CoopheadSession NewSession(CoopheadConfig config = null)
        {
            config = config ?? new CoopheadConfig();

            // Ruleta determinista: solo «Disparo lento».
            foreach (RouletteOutcome outcome in RouletteOutcomes.All())
            {
                config.Roulette.Get(outcome).Enabled = outcome == RouletteOutcome.SlowFire;
            }

            var session = new CoopheadSession(config, applier, new ListLog(), new SequenceRandom(0.5));
            session.RouletteSpun += spins.Add;
            session.RestartRequested += restarts.Add;
            session.ThanksWindowOpened += opened.Add;
            session.StartLevel("nivel-prueba");
            return session;
        }

        private static void Rescue(CoopheadSession session, PlayerSlot rescued, PlayerSlot rescuer, string key, float now)
        {
            session.OnPlayerDied(rescued, key, now - 1f);
            session.OnPlayerRevived(rescued, rescuer, key, now);
        }

        [Fact]
        public void SinAgradecimientoLaRuletaCastigaAlRescatado()
        {
            CoopheadSession session = NewSession();
            Rescue(session, P2, P1, "g1", 10f);
            Assert.Single(opened);

            session.Tick(13f);

            Assert.Single(spins);
            Assert.Equal(P2, spins[0].Target);
            Assert.Contains("P2 SlowFire", applier.Active);
            Assert.Equal(1, session.Rules.Session.Get(P2).ThanksMissed);

            session.Tick(17f);
            Assert.Empty(applier.Active);
        }

        [Fact]
        public void ConAgradecimientoNoHayRuleta()
        {
            CoopheadSession session = NewSession();
            session.SetVoiceAvailable(true);
            Rescue(session, P1, P2, "g1", 10f);

            session.OnTranscript("¡Gracias, amor!", 11f);
            session.Tick(20f);

            Assert.Empty(spins);
            Assert.Equal(1, session.Rules.Session.Get(P1).ThanksRecognized);
        }

        [Fact]
        public void ElRespaldoManualTambienEvitaLaRuleta()
        {
            CoopheadSession session = NewSession();
            Rescue(session, P1, P2, "g1", 10f);

            session.OnManualThanks(P1, 11f);
            session.Tick(20f);

            Assert.Empty(spins);
        }

        [Fact]
        public void AlAlcanzarElLimiteSePideReinicioSinVentanaNiRuleta()
        {
            CoopheadSession session = NewSession();
            session.OnManualThanks(null, 0f);
            for (int i = 0; i < 3; i++)
            {
                Rescue(session, P1, P2, "g" + i, (i * 20f) + 10f);
                session.OnManualThanks(P1, (i * 20f) + 11f);
            }

            Assert.Single(restarts);
            Assert.Equal(RestartReason.PlayerLimit, restarts[0].Reason);
            Assert.Equal(P1, restarts[0].TriggeredBy);
            Assert.Equal(2, opened.Count);
            Assert.Empty(session.Thanks.Pending);
        }

        [Fact]
        public void MorirConUnEfectoActivoLoRestaura()
        {
            CoopheadSession session = NewSession();
            Rescue(session, P2, P1, "g1", 10f);
            session.Tick(13f);
            Assert.Contains("P2 SlowFire", applier.Active);

            session.OnPlayerDied(P2, "g2", 14f);

            Assert.Empty(applier.Active);
        }

        [Fact]
        public void MorirDuranteLaVentanaLaCierraSinCastigo()
        {
            CoopheadSession session = NewSession();
            Rescue(session, P2, P1, "g1", 10f);

            session.OnPlayerDied(P2, "g2", 11f);
            session.Tick(20f);

            Assert.Empty(spins);
        }

        [Fact]
        public void TerminarElNivelRestauraEfectosYCierraVentanas()
        {
            CoopheadSession session = NewSession();
            LevelSummary summary = null;
            session.LevelEnded += s => summary = s;
            Rescue(session, P2, P1, "g1", 10f);
            session.Tick(13f);
            Rescue(session, P1, P2, "g2", 14f);

            session.EndLevel(LevelEndReason.Won);
            session.Tick(30f);

            Assert.Empty(applier.Active);
            Assert.Single(spins);
            Assert.NotNull(summary);
            Assert.Equal(1, summary.PlayerTwo.PenaltiesReceived);
        }

        [Fact]
        public void ConfigurandoElObjetivoSePuedeCastigarAlRescatador()
        {
            var config = new CoopheadConfig { PenaltyTarget = PenaltyTargetMode.Rescuer };
            CoopheadSession session = NewSession(config);
            Rescue(session, P2, P1, "g1", 10f);

            session.Tick(13f);

            Assert.Equal(P1, spins[0].Target);
        }

        [Fact]
        public void SinExigirAgradecimientoNoHayRuleta()
        {
            var config = new CoopheadConfig();
            config.Thanks.RequireThanks = false;
            CoopheadSession session = NewSession(config);
            Rescue(session, P2, P1, "g1", 10f);

            session.Tick(20f);

            Assert.Empty(opened);
            Assert.Empty(spins);
        }

        [Fact]
        public void UnSuscriptorQueFallaNoRompeElFlujo()
        {
            CoopheadSession session = NewSession();
            session.ThanksWindowOpened += _ => throw new System.InvalidOperationException("HUD roto");

            Rescue(session, P2, P1, "g1", 10f);
            session.Tick(13f);

            Assert.Single(spins);
        }

        [Fact]
        public void ApagarElModRestauraTodo()
        {
            CoopheadSession session = NewSession();
            Rescue(session, P2, P1, "g1", 10f);
            session.Tick(13f);

            session.Shutdown();

            Assert.Empty(applier.Active);
        }

        [Fact]
        public void LaConfiguracionPorDefectoEsValida()
        {
            Assert.Empty(new CoopheadConfig().Validate());
        }
    }
}
