using Coophead.Core.Diagnostics;
using Coophead.Core.Roulette;
using Coophead.Core.Rules;
using Xunit;

namespace Coophead.Core.Tests
{
    public class BalanceDirectorTests
    {
        private static PlayerRoundStats Stats(PlayerSlot slot, int deaths = 0, int revivesReceived = 0, int revivesGiven = 0, int credits = 0)
        {
            return new PlayerRoundStats(slot)
            {
                Deaths = deaths,
                RevivesReceived = revivesReceived,
                RevivesGiven = revivesGiven,
                HelpCredits = credits,
            };
        }

        [Fact]
        public void SinPresionNoHayProteccion()
        {
            var director = new BalanceDirector(new BalanceConfig());

            PressureReport report = director.Evaluate(Stats(PlayerSlot.PlayerOne), 0f);

            Assert.Equal(0f, report.Pressure);
            Assert.Equal(0f, report.Protection);
            Assert.Equal(1f, report.NoPenaltyMultiplier);
            Assert.Equal(1f, report.DoublePunishmentMultiplier);
            Assert.Equal(1f, report.DurationMultiplier);
        }

        [Fact]
        public void LaFormulaEsLaDocumentada()
        {
            var director = new BalanceDirector(new BalanceConfig());
            PlayerRoundStats stats = Stats(PlayerSlot.PlayerTwo, deaths: 3, revivesReceived: 2, revivesGiven: 2, credits: 0);

            PressureReport report = director.Evaluate(stats, 0f);

            // 3·1 + 2·1,5 − 2·0,5 = 5 → protección 5/8.
            Assert.Equal(5f, report.Pressure, 3);
            Assert.Equal(0.625f, report.Protection, 3);
            Assert.Equal(1f + (0.625f * 2f), report.NoPenaltyMultiplier, 3);
            Assert.Equal(1f - 0.625f, report.DoublePunishmentMultiplier, 3);
            Assert.Equal(1f - (0.625f * 0.5f), report.DurationMultiplier, 3);
        }

        [Fact]
        public void LaPresionNoBajaDeCeroNiLaProteccionPasaDeUno()
        {
            var director = new BalanceDirector(new BalanceConfig());

            Assert.Equal(0f, director.Evaluate(Stats(PlayerSlot.PlayerOne, revivesGiven: 10, credits: 1), 0f).Pressure);
            Assert.Equal(1f, director.Evaluate(Stats(PlayerSlot.PlayerOne, deaths: 50), 0f).Protection);
        }

        [Fact]
        public void LosCastigosRecientesCuentanSoloDentroDeLaVentana()
        {
            var config = new BalanceConfig { DeathWeight = 0f, RevivesReceivedWeight = 0f, ConsecutivePenaltyWeight = 0f, RecentPenaltyWindowSeconds = 60f };
            var director = new BalanceDirector(config);
            var stats = new PlayerRoundStats(PlayerSlot.PlayerOne);
            stats.RecordRouletteOutcome(RouletteOutcome.SlowFire, 0f);
            stats.RecordRouletteOutcome(RouletteOutcome.SlowMovement, 50f);

            Assert.Equal(2f, director.Evaluate(stats, 55f).Pressure);
            Assert.Equal(1f, director.Evaluate(stats, 100f).Pressure);
        }

        [Fact]
        public void LaPresionDeUnJugadorNoDependeDeLaPareja()
        {
            var director = new BalanceDirector(new BalanceConfig());
            PlayerRoundStats calm = Stats(PlayerSlot.PlayerOne);

            float before = director.Evaluate(calm, 0f).Protection;
            director.Evaluate(Stats(PlayerSlot.PlayerTwo, deaths: 20, revivesReceived: 3), 0f);

            Assert.Equal(before, director.Evaluate(calm, 0f).Protection);
        }

        [Fact]
        public void DesactivadoDevuelveMultiplicadoresNeutros()
        {
            var director = new BalanceDirector(new BalanceConfig { Enabled = false });

            PressureReport report = director.Evaluate(Stats(PlayerSlot.PlayerOne, deaths: 20), 0f);

            Assert.False(report.Enabled);
            Assert.Equal(0f, report.Protection);
            Assert.Equal(1f, report.NoPenaltyMultiplier);
        }

        [Fact]
        public void PresionAltaSubeSinPenalizacionYQuitaCastigoDoble()
        {
            var roulette = new RouletteConfig();
            var service = new RouletteService(roulette, new BalanceConfig(), new SequenceRandom(0.5));
            PlayerRoundStats calm = Stats(PlayerSlot.PlayerOne);
            PlayerRoundStats pressured = Stats(PlayerSlot.PlayerTwo, deaths: 6, revivesReceived: 2);

            var calmWeights = service.ComputeWeights(calm, service.Director.Evaluate(calm, 0f));
            var pressuredWeights = service.ComputeWeights(pressured, service.Director.Evaluate(pressured, 0f));

            Assert.Equal(20f, calmWeights[RouletteOutcome.NoPenalty]);
            Assert.Equal(60f, pressuredWeights[RouletteOutcome.NoPenalty], 3);
            Assert.True(calmWeights.ContainsKey(RouletteOutcome.DoublePunishment));
            Assert.False(pressuredWeights.ContainsKey(RouletteOutcome.DoublePunishment));
        }

        [Fact]
        public void PresionAltaAcortaLosEfectosConUnMinimo()
        {
            var roulette = new RouletteConfig { CooldownSeconds = 0f, MaxConsecutivePenalties = 0 };
            foreach (RouletteOutcome outcome in RouletteOutcomes.All())
            {
                roulette.Get(outcome).Enabled = outcome == RouletteOutcome.SlowFire;
            }

            var service = new RouletteService(roulette, new BalanceConfig(), new SequenceRandom(0.5));
            RouletteResult result = service.Spin(Stats(PlayerSlot.PlayerOne, deaths: 20), 0f);

            Assert.Equal(2f, result.Effects[0].DurationSeconds, 3);

            var strong = new BalanceConfig { DurationReduction = 1f, MinEffectDurationSeconds = 1f };
            var strongService = new RouletteService(roulette, strong, new SequenceRandom(0.5));
            Assert.Equal(1f, strongService.Spin(Stats(PlayerSlot.PlayerOne, deaths: 20), 0f).Effects[0].DurationSeconds, 3);
        }

        [Fact]
        public void LaValidacionCorrigeParametros()
        {
            var config = new BalanceConfig { DeathWeight = -1f, DurationReduction = 3f, MaxPressure = 0f };

            Assert.Equal(3, config.Validate().Count);
            Assert.Equal(0f, config.DeathWeight);
            Assert.Equal(1f, config.DurationReduction);
            Assert.Equal(8f, config.MaxPressure);
        }

        [Fact]
        public void ElInformeEsLegible()
        {
            var director = new BalanceDirector(new BalanceConfig());
            string text = director.Evaluate(Stats(PlayerSlot.PlayerTwo, deaths: 4), 0f).ToString();

            Assert.StartsWith("P2: presión 4", text);
        }
    }
}
