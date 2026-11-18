using Coophead.Core.Rules;
using Xunit;

namespace Coophead.Core.Tests
{
    public class RuleEngineTests
    {
        private const PlayerSlot P1 = PlayerSlot.PlayerOne;
        private const PlayerSlot P2 = PlayerSlot.PlayerTwo;

        private int keySeed;

        private static RuleEngine NewEngine(RulesConfig config = null)
        {
            var engine = new RuleEngine(config ?? new RulesConfig { TeamReviveLimit = 0 });
            engine.StartLevel("nivel-prueba");
            return engine;
        }

        /// <summary>Muerte de <paramref name="rescued"/> seguida de su rescate por <paramref name="rescuer"/>.</summary>
        private ReviveResult DieAndRescue(RuleEngine engine, PlayerSlot rescued, PlayerSlot rescuer)
        {
            keySeed++;
            Assert.Equal(EventStatus.Applied, engine.OnPlayerDied(rescued, "g" + keySeed).Status);
            return engine.OnPlayerRevived(rescued, rescuer, "g" + keySeed);
        }

        private static PlayerRoundStats Stats(RuleEngine engine, PlayerSlot slot)
        {
            return engine.Session.Get(slot);
        }

        [Fact]
        public void Caso1_RescatarRestaUnaReanimacionPropia()
        {
            var engine = NewEngine();
            DieAndRescue(engine, P2, P1);
            DieAndRescue(engine, P2, P1);
            Assert.Equal(2, Stats(engine, P2).RevivesReceived);

            ReviveResult result = DieAndRescue(engine, P1, P2);

            Assert.Equal(RescuerReward.CounterReduced, result.Reward);
            Assert.Equal(1, Stats(engine, P2).RevivesReceived);
            Assert.Equal(1, Stats(engine, P2).RevivesGiven);
        }

        [Fact]
        public void Caso2_ConContadorACeroGuardaUnCredito()
        {
            var engine = NewEngine();

            ReviveResult result = DieAndRescue(engine, P1, P2);

            Assert.Equal(RescuerReward.CreditStored, result.Reward);
            Assert.Equal(0, Stats(engine, P2).RevivesReceived);
            Assert.Equal(1, Stats(engine, P2).HelpCredits);
        }

        [Fact]
        public void Caso3_ElCreditoSeConsumeEnLaSiguienteReanimacionRecibida()
        {
            var engine = NewEngine();
            DieAndRescue(engine, P1, P2);

            ReviveResult result = DieAndRescue(engine, P2, P1);

            Assert.True(result.CreditConsumed);
            Assert.Equal(0, Stats(engine, P2).RevivesReceived);
            Assert.Equal(0, Stats(engine, P2).HelpCredits);
            Assert.Equal(1, Stats(engine, P2).TotalRevivesReceived);
            Assert.Equal(1, Stats(engine, P2).CreditsConsumed);
        }

        [Fact]
        public void Caso4_NoSeAcumulaUnSegundoCredito()
        {
            var engine = NewEngine();
            DieAndRescue(engine, P1, P2);

            ReviveResult result = DieAndRescue(engine, P1, P2);

            Assert.Equal(RescuerReward.CreditCapReached, result.Reward);
            Assert.Equal(1, Stats(engine, P2).HelpCredits);
        }

        [Theory]
        [InlineData(PlayerSlot.PlayerOne, PlayerSlot.PlayerTwo)]
        [InlineData(PlayerSlot.PlayerTwo, PlayerSlot.PlayerOne)]
        public void Caso5_LasReglasSonSimetricas(PlayerSlot a, PlayerSlot b)
        {
            var engine = NewEngine();
            DieAndRescue(engine, a, b);
            DieAndRescue(engine, a, b);
            DieAndRescue(engine, b, a);

            Assert.Equal(1, Stats(engine, a).RevivesReceived);
            Assert.Equal(1, Stats(engine, a).RevivesGiven);
            Assert.Equal(0, Stats(engine, b).RevivesReceived);
            Assert.Equal(0, Stats(engine, b).HelpCredits);
            Assert.Equal(1, Stats(engine, b).TotalRevivesReceived);
            Assert.Equal(2, Stats(engine, b).RevivesGiven);
        }

        [Fact]
        public void Caso6_UnEventoDuplicadoNoSeCuentaDosVeces()
        {
            var engine = NewEngine();
            engine.OnPlayerDied(P1, "fantasma-1");
            Assert.Equal(EventStatus.Duplicate, engine.OnPlayerDied(P1, "fantasma-1").Status);

            Assert.Equal(EventStatus.Applied, engine.OnPlayerRevived(P1, P2, "fantasma-1").Status);
            Assert.Equal(EventStatus.Duplicate, engine.OnPlayerRevived(P1, P2, "fantasma-1").Status);

            Assert.Equal(1, Stats(engine, P1).Deaths);
            Assert.Equal(1, Stats(engine, P1).RevivesReceived);
            Assert.Equal(1, Stats(engine, P2).RevivesGiven);
            Assert.Equal(1, Stats(engine, P2).HelpCredits);
        }

        [Fact]
        public void Caso7_CambiarDeNivelLimpiaContadoresYCreditos()
        {
            var engine = NewEngine();
            DieAndRescue(engine, P1, P2);
            DieAndRescue(engine, P1, P2);

            engine.StartLevel("otro-nivel");

            foreach (PlayerSlot slot in PlayerSlots.All())
            {
                Assert.Equal(0, Stats(engine, slot).Deaths);
                Assert.Equal(0, Stats(engine, slot).RevivesReceived);
                Assert.Equal(0, Stats(engine, slot).HelpCredits);
                Assert.True(Stats(engine, slot).IsAlive);
            }

            Assert.Equal(0, engine.Session.TeamRevives);
        }

        [Fact]
        public void Caso8_LlegarAlLimiteIndividualPideUnSoloReinicio()
        {
            var engine = NewEngine();
            Assert.False(DieAndRescue(engine, P1, P2).RestartRequested);
            Assert.False(DieAndRescue(engine, P1, P2).RestartRequested);

            ReviveResult third = DieAndRescue(engine, P1, P2);

            Assert.True(third.RestartRequested);
            Assert.Equal(RestartReason.PlayerLimit, third.LimitReached);
            Assert.True(engine.Session.RestartPending);

            // Con el reinicio pendiente se ignora todo lo demás: no hay una segunda solicitud.
            Assert.Equal(EventStatus.Ignored, engine.OnPlayerDied(P1, "tarde").Status);
        }

        [Fact]
        public void Caso9_FinDeCombateYReinicioDevuelvenResumenYLimpianEstado()
        {
            var engine = NewEngine();
            DieAndRescue(engine, P2, P1);

            LevelSummary summary = engine.EndLevel(LevelEndReason.Won);

            Assert.Equal(LevelEndReason.Won, summary.Reason);
            Assert.Equal(1, summary.PlayerTwo.TotalRevivesReceived);
            Assert.Equal(EventStatus.Ignored, engine.OnPlayerDied(P1, "tras-fin").Status);
            Assert.Null(engine.EndLevel(LevelEndReason.Won));

            engine.StartLevel("nivel-prueba");
            Assert.Equal(0, Stats(engine, P2).TotalRevivesReceived);
            Assert.Equal(1, summary.PlayerTwo.TotalRevivesReceived);
        }

        [Fact]
        public void RescatesAlternosNoLleganAlLimiteIndividualPeroSiAlDeEquipo()
        {
            var engine = NewEngine(new RulesConfig());
            ReviveResult last = null;
            for (int i = 0; i < 3; i++)
            {
                last = DieAndRescue(engine, P1, P2);
                Assert.False(last.RestartRequested);
                last = DieAndRescue(engine, P2, P1);
                if (i < 2)
                {
                    Assert.False(last.RestartRequested);
                }
            }

            Assert.True(last.RestartRequested);
            Assert.Equal(RestartReason.TeamLimit, last.LimitReached);
            Assert.Equal(6, engine.Session.TeamRevives);
            Assert.True(Stats(engine, P1).RevivesReceived < 3);
            Assert.True(Stats(engine, P2).RevivesReceived < 3);
        }

        [Fact]
        public void ElLimiteDeEquipoCuentaReanimacionesBrutasAunqueSeConsumanCreditos()
        {
            var engine = NewEngine(new RulesConfig { TeamReviveLimit = 2, PlayerOneReviveLimit = 0, PlayerTwoReviveLimit = 0 });
            DieAndRescue(engine, P1, P2); // P2 guarda crédito

            ReviveResult result = DieAndRescue(engine, P2, P1); // P2 consume crédito

            Assert.True(result.CreditConsumed);
            Assert.Equal(RestartReason.TeamLimit, result.LimitReached);
        }

        [Fact]
        public void LimitesACeroDesactivanElReinicio()
        {
            var engine = NewEngine(new RulesConfig { PlayerOneReviveLimit = 0, PlayerTwoReviveLimit = 0, TeamReviveLimit = 0 });
            for (int i = 0; i < 10; i++)
            {
                Assert.False(DieAndRescue(engine, P1, P2).RestartRequested);
            }
        }

        [Fact]
        public void LimitePorJugadorConfigurable()
        {
            var engine = NewEngine(new RulesConfig { PlayerOneReviveLimit = 5, PlayerTwoReviveLimit = 1, TeamReviveLimit = 0 });

            Assert.True(DieAndRescue(engine, P2, P1).RestartRequested);
        }

        [Fact]
        public void SinReinicioAutomaticoSoloSeAvisa()
        {
            var engine = NewEngine(new RulesConfig { AutoRestartEnabled = false, TeamReviveLimit = 0 });
            DieAndRescue(engine, P1, P2);
            DieAndRescue(engine, P1, P2);

            ReviveResult third = DieAndRescue(engine, P1, P2);

            Assert.Equal(RestartReason.PlayerLimit, third.LimitReached);
            Assert.False(third.RestartRequested);
            Assert.False(engine.Session.RestartPending);
        }

        [Fact]
        public void SinCreditosCadaReanimacionSumaSiempre()
        {
            var engine = NewEngine(new RulesConfig { HelpCreditsEnabled = false, TeamReviveLimit = 0 });
            DieAndRescue(engine, P2, P1);

            ReviveResult result = DieAndRescue(engine, P1, P2);

            Assert.Equal(RescuerReward.None, result.Reward);
            Assert.Equal(1, Stats(engine, P2).RevivesReceived);
            Assert.Equal(0, Stats(engine, P2).HelpCredits);
        }

        [Fact]
        public void AmbosMuertosSeMarcaComoDerrotaNormalSinReinicio()
        {
            var engine = NewEngine();
            engine.OnPlayerDied(P1, "a");

            DeathResult result = engine.OnPlayerDied(P2, "b");

            Assert.True(result.BothDown);
            Assert.False(engine.Session.RestartPending);
        }

        [Fact]
        public void EventosIncoherentesSeIgnoran()
        {
            var engine = NewEngine();

            Assert.Equal(EventStatus.Ignored, engine.OnPlayerRevived(P1, P2, "x").Status); // P1 no estaba muerto
            engine.OnPlayerDied(P1, "y");
            engine.OnPlayerDied(P2, "z");
            Assert.Equal(EventStatus.Ignored, engine.OnPlayerRevived(P1, P2, "y").Status); // rescatador muerto
            Assert.Equal(EventStatus.Ignored, engine.OnPlayerRevived(P1, P1, "w").Status); // autorrescate
            Assert.Equal(EventStatus.Ignored, engine.OnPlayerDied(P1, "otra-clave").Status); // ya muerto
            Assert.Equal(EventStatus.Ignored, engine.OnPlayerDied(P1, null).Status); // sin clave
            Assert.Equal(EventStatus.Ignored, engine.OnPlayerDied((PlayerSlot)7, "k").Status);
        }

        [Fact]
        public void SinNivelActivoOConReglasDesactivadasSeIgnora()
        {
            var engine = new RuleEngine(new RulesConfig());
            Assert.Equal(EventStatus.Ignored, engine.OnPlayerDied(P1, "a").Status);

            var disabled = NewEngine(new RulesConfig { Enabled = false });
            Assert.Equal(EventStatus.Ignored, disabled.OnPlayerDied(P1, "a").Status);
        }

        [Fact]
        public void CadaIntentoTieneNumeroDeSesionDistinto()
        {
            var engine = NewEngine();
            int first = engine.Session.SessionNumber;
            engine.StartLevel("nivel-prueba");
            Assert.Equal(first + 1, engine.Session.SessionNumber);
        }

        [Fact]
        public void ValidacionCorrigeValoresFueraDeRango()
        {
            var config = new RulesConfig { PlayerOneReviveLimit = -1, TeamReviveLimit = -4, MaxStoredCredits = 99 };

            var warnings = config.Validate();

            Assert.Equal(3, warnings.Count);
            Assert.Equal(0, config.PlayerOneReviveLimit);
            Assert.Equal(0, config.TeamReviveLimit);
            Assert.Equal(RulesConfig.MaxAllowedStoredCredits, config.MaxStoredCredits);
        }
    }
}
