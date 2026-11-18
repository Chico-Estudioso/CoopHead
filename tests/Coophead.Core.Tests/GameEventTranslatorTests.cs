using Coophead.Core.Integration;
using Coophead.Core.Rules;
using Xunit;

namespace Coophead.Core.Tests
{
    public class GameEventTranslatorTests
    {
        private const int GameP1 = 0;
        private const int GameP2 = 1;

        private readonly ListLog log = new ListLog();
        private bool applyRules = true;
        private bool diagnostics = true;

        private GameEventTranslator NewTranslator(out CoopheadSession session)
        {
            session = new CoopheadSession(new CoopheadConfig(), new FakeApplier(), log);
            return new GameEventTranslator(session, log, () => applyRules, () => diagnostics);
        }

        [Theory]
        [InlineData(0, true, PlayerSlot.PlayerOne)]
        [InlineData(1, true, PlayerSlot.PlayerTwo)]
        [InlineData(2147483646, false, PlayerSlot.PlayerOne)]
        [InlineData(-1, false, PlayerSlot.PlayerOne)]
        public void MapeaSoloPlayerOneYPlayerTwo(int gameId, bool ok, PlayerSlot expected)
        {
            PlayerSlot slot;
            Assert.Equal(ok, GameEventTranslator.TryMapPlayer(gameId, out slot));
            if (ok)
            {
                Assert.Equal(expected, slot);
            }
        }

        [Fact]
        public void FantasmaYRescateCompartenClaveYSeCuentanUnaVez()
        {
            CoopheadSession session;
            GameEventTranslator translator = NewTranslator(out session);
            translator.LevelStarted("FlowerLevel", 100);

            translator.GhostSpawned(GameP2, 501, 1f);
            translator.GhostSpawned(GameP2, 501, 1.1f);
            translator.ParryRevive(GameP2, GameP1, 501, 3f);
            translator.ParryRevive(GameP2, GameP1, 501, 3.1f);

            PlayerRoundStats p2 = session.Rules.Session.Get(PlayerSlot.PlayerTwo);
            Assert.Equal(1, p2.Deaths);
            Assert.Equal(1, p2.TotalRevivesReceived);
            Assert.Equal(1, session.Rules.Session.Get(PlayerSlot.PlayerOne).RevivesGiven);
            Assert.Single(session.Thanks.Pending);
        }

        [Fact]
        public void SinAplicarReglasSoloSeRegistra()
        {
            applyRules = false;
            CoopheadSession session;
            GameEventTranslator translator = NewTranslator(out session);
            translator.LevelStarted("FlowerLevel", 100);

            translator.GhostSpawned(GameP1, 7, 1f);
            translator.ParryRevive(GameP1, GameP2, 7, 2f);

            Assert.Equal(0, session.Rules.Session.Get(PlayerSlot.PlayerOne).Deaths);
            Assert.Empty(session.Thanks.Pending);
            Assert.Contains(log.Entries, e => e.Contains("Rescate por parry del fantasma 7"));
            Assert.Equal(3, translator.Sequence);
        }

        [Fact]
        public void ElRoboDeCorazonNoCuentaPeroDevuelveAlJugadorALaPartida()
        {
            CoopheadSession session;
            GameEventTranslator translator = NewTranslator(out session);
            translator.LevelStarted("FlowerLevel", 100);
            translator.GhostSpawned(GameP1, 9, 1f);

            translator.StealRevive(GameP1, 9);
            translator.PlayerBackInGame(GameP1, 120, 4f);

            PlayerRoundStats p1 = session.Rules.Session.Get(PlayerSlot.PlayerOne);
            Assert.True(p1.IsAlive);
            Assert.Equal(0, p1.TotalRevivesReceived);
            Assert.Empty(session.Thanks.Pending);

            // Tras volver, una nueva muerte se cuenta con normalidad.
            translator.GhostSpawned(GameP1, 10, 6f);
            Assert.Equal(2, p1.Deaths);
        }

        [Fact]
        public void LaConfirmacionTrasUnRescatePorParryNoCambiaNada()
        {
            CoopheadSession session;
            GameEventTranslator translator = NewTranslator(out session);
            translator.LevelStarted("FlowerLevel", 100);
            translator.GhostSpawned(GameP1, 3, 1f);
            translator.ParryRevive(GameP1, GameP2, 3, 2f);

            translator.PlayerBackInGame(GameP1, 200, 2.5f);

            Assert.Equal(1, session.Rules.Session.Get(PlayerSlot.PlayerOne).TotalRevivesReceived);
            Assert.DoesNotContain(log.Entries, e => e.Contains("sin rescate"));
        }

        [Fact]
        public void IdentificadoresInesperadosSeDescartanConAviso()
        {
            CoopheadSession session;
            GameEventTranslator translator = NewTranslator(out session);
            translator.LevelStarted("FlowerLevel", 100);

            translator.GhostSpawned(2147483646, 1, 1f);

            Assert.Contains(log.Entries, e => e.StartsWith("WARN Identificador de jugador inesperado"));
            Assert.Equal(0, session.Rules.Session.Get(PlayerSlot.PlayerOne).Deaths);
        }

        [Fact]
        public void SinDiagnosticoNoSeEscribenTrazas()
        {
            diagnostics = false;
            CoopheadSession session;
            GameEventTranslator translator = NewTranslator(out session);

            translator.DeathReported(GameP1);

            Assert.DoesNotContain(log.Entries, e => e.Contains("[#"));
            Assert.Equal(1, translator.Sequence);
        }

        [Fact]
        public void ElFinDeUnaInstanciaAnteriorNoCierraElNivelNuevo()
        {
            CoopheadSession session;
            GameEventTranslator translator = NewTranslator(out session);
            translator.LevelStarted("FlowerLevel", 100);
            translator.LevelStarted("FlowerLevel", 200);

            translator.LevelEnded(LevelEndReason.Exited, "Level.OnDestroy", 100);

            Assert.True(session.Rules.Session.IsActive);
            translator.LevelEnded(LevelEndReason.Exited, "Level.OnDestroy", 200);
            Assert.False(session.Rules.Session.IsActive);
        }

        [Fact]
        public void ElFinDeNivelLimpiaLaSesion()
        {
            CoopheadSession session;
            GameEventTranslator translator = NewTranslator(out session);
            translator.LevelStarted("FlowerLevel", 100);
            translator.GhostSpawned(GameP1, 3, 1f);
            translator.ParryRevive(GameP1, GameP2, 3, 2f);

            translator.LevelEnded(LevelEndReason.Won, "Level.OnWin", 100);

            Assert.False(session.Rules.Session.IsActive);
            Assert.Empty(session.Thanks.Pending);
        }
    }
}
