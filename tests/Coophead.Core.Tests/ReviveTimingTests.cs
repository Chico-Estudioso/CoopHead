using System.Collections.Generic;
using Coophead.Core.Integration;
using Coophead.Core.Presentation;
using Coophead.Core.Roulette;
using Coophead.Core.Thanks;
using Xunit;

namespace Coophead.Core.Tests
{
    /// <summary>
    /// Caso real del 20/11/2026: el parry llega más de 2 s antes de que el rescatado reaparezca. La ventana no debe
    /// caducar ni castigar mientras el jugador está reapareciendo.
    /// </summary>
    public class ReviveTimingTests
    {
        private const PlayerSlot P1 = PlayerSlot.PlayerOne;
        private const PlayerSlot P2 = PlayerSlot.PlayerTwo;

        [Fact]
        public void LaVentanaNoCaducaMientrasElJugadorReaparece()
        {
            var tracker = new ThanksWindowTracker(new ThanksConfig { WindowSeconds = 2f });
            ThanksWindow window = tracker.Open(P2, P1, 10f, new List<ThanksWindow>(), true);
            var resolved = new List<ThanksWindow>();

            tracker.Tick(13f, resolved);
            Assert.Empty(resolved);
            Assert.True(window.AwaitingRevive);

            tracker.ConfirmRevive(P2, 13.5f);
            tracker.Tick(15.4f, resolved);
            Assert.Empty(resolved);

            tracker.Tick(15.5f, resolved);
            Assert.Equal(ThanksResolution.Missed, window.Resolution);
        }

        [Fact]
        public void SePuedeDarLasGraciasAntesDeReaparecer()
        {
            var tracker = new ThanksWindowTracker(new ThanksConfig { WindowSeconds = 2f });
            tracker.SetVoiceAvailable(true);
            ThanksWindow window = tracker.Open(P2, P1, 10f, new List<ThanksWindow>(), true);

            Assert.Same(window, tracker.OnTranscript("gracias cari", 12.5f));
            Assert.Equal(ThanksResolution.Thanked, window.Resolution);
        }

        [Fact]
        public void SiElJuegoNoConfirmaSeCuentaElPlazoTrasLaEsperaMaxima()
        {
            var tracker = new ThanksWindowTracker(new ThanksConfig { WindowSeconds = 3f });
            ThanksWindow window = tracker.Open(P2, P1, 0f, new List<ThanksWindow>(), true);
            var resolved = new List<ThanksWindow>();

            tracker.Tick(ThanksWindowTracker.MaxReviveWaitSeconds, resolved);
            Assert.False(window.AwaitingRevive);
            Assert.Empty(resolved);

            tracker.Tick(ThanksWindowTracker.MaxReviveWaitSeconds + 3f, resolved);
            Assert.Equal(ThanksResolution.Missed, window.Resolution);
        }

        [Fact]
        public void ConfirmarNoAcortaUnPlazoMasLargo()
        {
            var tracker = new ThanksWindowTracker(new ThanksConfig { WindowSeconds = 3f });
            ThanksWindow window = tracker.Open(P2, P1, 10f, new List<ThanksWindow>(), true);

            tracker.ConfirmRevive(P2, 10.2f);

            Assert.Equal(13.2f, window.ExpiresAt, 3);
            Assert.Null(tracker.ConfirmRevive(P2, 11f));
        }

        [Fact]
        public void ElCastigoLlegaDespuesDeReaparecer()
        {
            var config = new CoopheadConfig();
            config.Thanks.WindowSeconds = 2f;
            foreach (RouletteOutcome outcome in RouletteOutcomes.All())
            {
                config.Roulette.Get(outcome).Enabled = outcome == RouletteOutcome.SlowMovement;
            }

            var applier = new FakeApplier();
            var session = new CoopheadSession(config, applier, null, new SequenceRandom(0.5));
            var translator = new GameEventTranslator(session, null, () => true, () => false);
            translator.LevelStarted("FlowerLevel", 1, 0f);
            translator.GhostSpawned(1, 500, 5f);
            translator.ParryRevive(1, 0, 500, 10f);

            session.Tick(12.5f);
            Assert.Empty(applier.Active);
            Assert.Contains("P2: ¡da las gracias! (reapareciendo…)", HudFormatter.BuildLines(session, config, new HudContext(), 12.5f));

            translator.PlayerBackInGame(1, 900, 12.8f);
            session.Tick(14.7f);
            Assert.Empty(applier.Active);

            session.Tick(14.8f);
            Assert.Contains("P2 SlowMovement", applier.Active);
        }

        [Fact]
        public void ElOnDestroyTrasGanarNoHaceNada()
        {
            var log = new ListLog();
            var session = new CoopheadSession(new CoopheadConfig(), new FakeApplier(), log);
            var translator = new GameEventTranslator(session, log, () => true, () => true);
            translator.LevelStarted("FlowerLevel", 1, 0f);
            translator.LevelEnded(Rules.LevelEndReason.Won, "Level.OnWin", 1, 60f);
            int before = log.Entries.Count;

            translator.LevelEnded(Rules.LevelEndReason.Exited, "Level.OnDestroy", 1, 61f);

            Assert.Equal(before, log.Entries.Count);
        }
    }
}
