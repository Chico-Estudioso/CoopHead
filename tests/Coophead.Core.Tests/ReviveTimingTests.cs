using System.Collections.Generic;
using Coophead.Core.Integration;
using Coophead.Core.Presentation;
using Coophead.Core.Roulette;
using Coophead.Core.Thanks;
using Xunit;

namespace Coophead.Core.Tests
{
    /// <summary>
    /// Regla: el «gracias» se da al hacer el parry de reanimación; el plazo cuenta desde el parry. Caso real del
    /// 20/11/2026: el rescatado tarda más de 2 s en reaparecer, así que si falta el agradecimiento el castigo espera a
    /// que vuelva a la partida en lugar de perderse durante la animación.
    /// </summary>
    public class ReviveTimingTests
    {
        private const PlayerSlot P1 = PlayerSlot.PlayerOne;
        private const PlayerSlot P2 = PlayerSlot.PlayerTwo;

        private static CoopheadSession NewSession(FakeApplier applier, float windowSeconds)
        {
            var config = new CoopheadConfig();
            config.Thanks.WindowSeconds = windowSeconds;
            foreach (RouletteOutcome outcome in RouletteOutcomes.All())
            {
                config.Roulette.Get(outcome).Enabled = outcome == RouletteOutcome.SlowMovement;
            }

            return new CoopheadSession(config, applier, null, new SequenceRandom(0.5));
        }

        [Fact]
        public void ElPlazoCuentaDesdeElParryAunqueElJugadorSigaReapareciendo()
        {
            var tracker = new ThanksWindowTracker(new ThanksConfig { WindowSeconds = 3f, VoiceGraceSeconds = 0f });
            tracker.SetVoiceAvailable(true);
            ThanksWindow window = tracker.Open(P2, P1, 10f, new List<ThanksWindow>(), true);
            var resolved = new List<ThanksWindow>();

            Assert.Equal(13f, window.ExpiresAt);
            tracker.Tick(12.9f, resolved);
            Assert.Empty(resolved);

            tracker.Tick(13f, resolved);
            Assert.Equal(ThanksResolution.Missed, window.Resolution);
            Assert.True(window.AwaitingRevive);
            Assert.Null(tracker.OnTranscript("gracias", 13.2f));
        }

        [Fact]
        public void LasGraciasAlHacerElParryCuentan()
        {
            var tracker = new ThanksWindowTracker(new ThanksConfig { WindowSeconds = 3f });
            tracker.SetVoiceAvailable(true);
            ThanksWindow window = tracker.Open(P2, P1, 10f, new List<ThanksWindow>(), true);

            Assert.Same(window, tracker.OnTranscript("gracias cari", 10.4f));
            Assert.Equal(ThanksResolution.Thanked, window.Resolution);
        }

        [Fact]
        public void ConfirmarLaReaparicionNoAlargaElPlazo()
        {
            var tracker = new ThanksWindowTracker(new ThanksConfig { WindowSeconds = 3f });
            ThanksWindow window = tracker.Open(P2, P1, 10f, new List<ThanksWindow>(), true);

            Assert.Same(window, tracker.ConfirmRevive(P2));
            Assert.False(window.AwaitingRevive);
            Assert.Equal(13f, window.ExpiresAt);
            Assert.Null(tracker.ConfirmRevive(P2));
        }

        [Fact]
        public void ElCastigoEsperaAQueReaparezca()
        {
            var applier = new FakeApplier();
            CoopheadSession session = NewSession(applier, 2f);
            var translator = new GameEventTranslator(session, null, () => true, () => false);
            translator.LevelStarted("FlowerLevel", 1, 0f);
            translator.GhostSpawned(1, 500, 5f);
            translator.ParryRevive(1, 0, 500, 10f);

            session.Tick(12f); // plazo agotado (2 s desde el parry), pero P2 aún reaparece
            Assert.Empty(applier.Active);
            Assert.Equal(new[] { P2 }, session.PunishmentsWaitingForRevive);
            Assert.Equal(1, session.Rules.Session.Get(P2).ThanksMissed);
            Assert.Contains("P2: sin gracias · castigo al reaparecer", HudFormatter.BuildLines(session, new CoopheadConfig(), new HudContext(), 12f));

            translator.PlayerBackInGame(1, 900, 12.8f);

            Assert.Contains("P2 SlowMovement", applier.Active);
            Assert.Empty(session.PunishmentsWaitingForRevive);
        }

        [Fact]
        public void SiYaHaReaparecidoElCastigoEsInmediato()
        {
            var applier = new FakeApplier();
            CoopheadSession session = NewSession(applier, 4f);
            var translator = new GameEventTranslator(session, null, () => true, () => false);
            translator.LevelStarted("FlowerLevel", 1, 0f);
            translator.GhostSpawned(1, 500, 5f);
            translator.ParryRevive(1, 0, 500, 10f);
            translator.PlayerBackInGame(1, 900, 12.5f);

            session.Tick(14f);

            Assert.Contains("P2 SlowMovement", applier.Active);
        }

        [Fact]
        public void SiElJuegoNoConfirmaElCastigoLlegaTrasLaEsperaMaxima()
        {
            var applier = new FakeApplier();
            CoopheadSession session = NewSession(applier, 2f);
            session.StartLevel("FlowerLevel", 0f);
            session.OnPlayerDied(P2, "g", 5f);
            session.OnPlayerRevived(P2, P1, "g", 10f, true);

            session.Tick(12f);
            Assert.Empty(applier.Active);

            session.Tick(12f + ThanksWindowTracker.MaxReviveWaitSeconds);
            Assert.Contains("P2 SlowMovement", applier.Active);
        }

        [Fact]
        public void SiVuelveAMorirAntesDeReaparecerNoHayCastigo()
        {
            var applier = new FakeApplier();
            CoopheadSession session = NewSession(applier, 2f);
            session.StartLevel("FlowerLevel", 0f);
            session.OnPlayerDied(P2, "a", 5f);
            session.OnPlayerRevived(P2, P1, "a", 10f, true);
            session.Tick(12f);

            session.OnPlayerDied(P2, "b", 12.5f);
            session.Tick(30f);

            Assert.Empty(applier.Active);
            Assert.Empty(session.PunishmentsWaitingForRevive);
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
