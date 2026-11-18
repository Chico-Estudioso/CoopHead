using System;
using System.Collections.Generic;
using System.Linq;
using Coophead.Core.Diagnostics;
using Coophead.Core.Roulette;
using Coophead.Core.Rules;
using Xunit;

namespace Coophead.Core.Tests
{
    public class RouletteTests
    {
        private static RouletteConfig NeutralConfig()
        {
            // Sin cooldown, repeticiones ni protección por castigos seguidos para aislar los pesos.
            return new RouletteConfig { CooldownSeconds = 0f, AvoidRepeats = false, MaxConsecutivePenalties = 0 };
        }

        private static BalanceConfig NoBalance()
        {
            return new BalanceConfig { Enabled = false };
        }

        private static void OnlyEnable(RouletteConfig config, params RouletteOutcome[] enabled)
        {
            foreach (RouletteOutcome outcome in RouletteOutcomes.All())
            {
                config.Get(outcome).Enabled = enabled.Contains(outcome);
            }
        }

        [Fact]
        public void LasProbabilidadesBaseSonLosPesosNormalizados()
        {
            var service = new RouletteService(NeutralConfig(), NoBalance(), new SequenceRandom(0.5));
            var stats = new PlayerRoundStats(PlayerSlot.PlayerOne);

            RouletteResult result = service.Spin(stats, 0f);

            Assert.Equal(RouletteDecision.Rolled, result.Decision);
            Assert.Equal(1f, result.Probabilities.Values.Sum(), 3);
            Assert.Equal(0.20f, result.Probabilities[RouletteOutcome.SlowMovement], 3);
            Assert.Equal(0.15f, result.Probabilities[RouletteOutcome.InvertedControls], 3);
            Assert.Equal(0.15f, result.Probabilities[RouletteOutcome.SlowFire], 3);
            Assert.Equal(0.20f, result.Probabilities[RouletteOutcome.NoPenalty], 3);
            Assert.Equal(0.15f, result.Probabilities[RouletteOutcome.LimitedDash], 3);
            Assert.Equal(0.15f, result.Probabilities[RouletteOutcome.DoublePunishment], 3);
        }

        [Theory]
        [InlineData(0.00, RouletteOutcome.NoPenalty)]
        [InlineData(0.19, RouletteOutcome.NoPenalty)]
        [InlineData(0.21, RouletteOutcome.SlowMovement)]
        [InlineData(0.41, RouletteOutcome.InvertedControls)]
        [InlineData(0.56, RouletteOutcome.SlowFire)]
        [InlineData(0.71, RouletteOutcome.LimitedDash)]
        [InlineData(0.86, RouletteOutcome.DoublePunishment)]
        [InlineData(0.9999, RouletteOutcome.DoublePunishment)]
        public void LaTiradaRecorreLosResultadosEnOrdenFijo(double roll, RouletteOutcome expected)
        {
            var service = new RouletteService(NeutralConfig(), NoBalance(), new SequenceRandom(roll, 0.1, 0.1));

            RouletteResult result = service.Spin(new PlayerRoundStats(PlayerSlot.PlayerOne), 0f);

            Assert.Equal(expected, result.Outcome);
        }

        [Fact]
        public void PesosInvalidosSeCorrigenACero()
        {
            var config = NeutralConfig();
            config.Get(RouletteOutcome.SlowMovement).Weight = -5f;
            config.Get(RouletteOutcome.SlowFire).Weight = float.NaN;

            IList<string> warnings = config.Validate();

            Assert.Equal(0f, config.Get(RouletteOutcome.SlowMovement).Weight);
            Assert.Equal(0f, config.Get(RouletteOutcome.SlowFire).Weight);
            Assert.True(warnings.Count >= 2);
        }

        [Fact]
        public void PesosCeroONadaActivadoDaSinPenalizacion()
        {
            var config = NeutralConfig();
            foreach (RouletteOutcome outcome in RouletteOutcomes.All())
            {
                config.Get(outcome).Weight = 0f;
            }

            var service = new RouletteService(config, NoBalance(), new SequenceRandom(0.5));
            RouletteResult result = service.Spin(new PlayerRoundStats(PlayerSlot.PlayerOne), 0f);

            Assert.Equal(RouletteDecision.NothingAvailable, result.Decision);
            Assert.Equal(RouletteOutcome.NoPenalty, result.Outcome);
            Assert.Empty(result.Effects);
        }

        [Fact]
        public void ConUnSoloResultadoActivadoSiempreSaleEse()
        {
            var config = NeutralConfig();
            OnlyEnable(config, RouletteOutcome.SlowFire);
            config.AvoidRepeats = true;
            var service = new RouletteService(config, NoBalance(), new SystemRandomSource(1));
            var stats = new PlayerRoundStats(PlayerSlot.PlayerTwo);

            for (int i = 0; i < 20; i++)
            {
                RouletteResult result = service.Spin(stats, i * 100f);
                Assert.Equal(RouletteOutcome.SlowFire, result.Outcome);
                Assert.Single(result.Effects);
            }
        }

        [Fact]
        public void CastigoDobleEligeDosEfectosDistintosYCompatibles()
        {
            var config = NeutralConfig();
            OnlyEnable(config, RouletteOutcome.DoublePunishment, RouletteOutcome.SlowMovement, RouletteOutcome.InvertedControls, RouletteOutcome.SlowFire);
            config.Get(RouletteOutcome.SlowMovement).Weight = 0.0001f;
            config.Get(RouletteOutcome.InvertedControls).Weight = 0.0001f;
            config.Get(RouletteOutcome.SlowFire).Weight = 0.0001f;
            config.Compatibility.MarkIncompatible(PenaltyEffect.SlowMovement, PenaltyEffect.InvertedControls);
            var service = new RouletteService(config, NoBalance(), new SystemRandomSource(7));

            for (int i = 0; i < 200; i++)
            {
                RouletteResult result = service.Spin(new PlayerRoundStats(PlayerSlot.PlayerOne), 0f);
                if (result.Outcome != RouletteOutcome.DoublePunishment)
                {
                    continue;
                }

                Assert.Equal(2, result.Effects.Count);
                PenaltyEffect a = result.Effects[0].Effect;
                PenaltyEffect b = result.Effects[1].Effect;
                Assert.NotEqual(a, b);
                Assert.True(config.Compatibility.AreCompatible(a, b));
            }
        }

        [Fact]
        public void CastigoDobleSinDosEfectosCompatiblesQuedaFueraDelSorteo()
        {
            var config = NeutralConfig();
            OnlyEnable(config, RouletteOutcome.DoublePunishment, RouletteOutcome.SlowMovement, RouletteOutcome.LimitedDash);
            config.Compatibility.MarkIncompatible(PenaltyEffect.SlowMovement, PenaltyEffect.LimitedDash);
            var service = new RouletteService(config, NoBalance(), new SequenceRandom(0.99));

            RouletteResult result = service.Spin(new PlayerRoundStats(PlayerSlot.PlayerOne), 0f);

            Assert.False(result.Probabilities.ContainsKey(RouletteOutcome.DoublePunishment));
            Assert.NotEqual(RouletteOutcome.DoublePunishment, result.Outcome);
        }

        [Fact]
        public void CastigoDobleConUnSoloEfectoActivadoQuedaFuera()
        {
            var config = NeutralConfig();
            OnlyEnable(config, RouletteOutcome.DoublePunishment, RouletteOutcome.SlowMovement);
            var service = new RouletteService(config, NoBalance(), new SequenceRandom(0.99));

            RouletteResult result = service.Spin(new PlayerRoundStats(PlayerSlot.PlayerOne), 0f);

            Assert.Equal(RouletteOutcome.SlowMovement, result.Outcome);
        }

        [Fact]
        public void NoRepiteElMismoCastigoSeguidoSiHayAlternativas()
        {
            var config = NeutralConfig();
            config.AvoidRepeats = true;
            OnlyEnable(config, RouletteOutcome.SlowMovement, RouletteOutcome.SlowFire);
            var service = new RouletteService(config, NoBalance(), new SystemRandomSource(3));
            var stats = new PlayerRoundStats(PlayerSlot.PlayerOne);

            RouletteOutcome previous = service.Spin(stats, 0f).Outcome;
            for (int i = 1; i < 50; i++)
            {
                RouletteOutcome current = service.Spin(stats, i).Outcome;
                Assert.NotEqual(previous, current);
                previous = current;
            }
        }

        [Fact]
        public void SinPenalizacionSiPuedeRepetirse()
        {
            var config = NeutralConfig();
            config.AvoidRepeats = true;
            OnlyEnable(config, RouletteOutcome.NoPenalty, RouletteOutcome.SlowFire);
            config.Get(RouletteOutcome.SlowFire).Weight = 1f;
            config.Get(RouletteOutcome.NoPenalty).Weight = 1000f;
            var service = new RouletteService(config, NoBalance(), new SequenceRandom(0.0));
            var stats = new PlayerRoundStats(PlayerSlot.PlayerOne);

            Assert.Equal(RouletteOutcome.NoPenalty, service.Spin(stats, 0f).Outcome);
            Assert.Equal(RouletteOutcome.NoPenalty, service.Spin(stats, 1f).Outcome);
        }

        [Fact]
        public void ElCooldownProtegeAlJugador()
        {
            var config = NeutralConfig();
            config.CooldownSeconds = 8f;
            OnlyEnable(config, RouletteOutcome.SlowFire, RouletteOutcome.SlowMovement);
            var service = new RouletteService(config, NoBalance(), new SequenceRandom(0.5));
            var stats = new PlayerRoundStats(PlayerSlot.PlayerOne);

            Assert.True(service.Spin(stats, 10f).IsPenalty);

            RouletteResult protectedResult = service.Spin(stats, 15f);
            Assert.Equal(RouletteDecision.ProtectedCooldown, protectedResult.Decision);
            Assert.Equal(RouletteOutcome.NoPenalty, protectedResult.Outcome);

            Assert.True(service.Spin(stats, 18.5f).IsPenalty);
        }

        [Fact]
        public void TrasDosCastigosSeguidosLaTiradaEsProtegida()
        {
            var config = NeutralConfig();
            config.MaxConsecutivePenalties = 2;
            OnlyEnable(config, RouletteOutcome.SlowFire, RouletteOutcome.SlowMovement);
            var service = new RouletteService(config, NoBalance(), new SequenceRandom(0.5));
            var stats = new PlayerRoundStats(PlayerSlot.PlayerOne);

            Assert.True(service.Spin(stats, 0f).IsPenalty);
            Assert.True(service.Spin(stats, 100f).IsPenalty);

            RouletteResult third = service.Spin(stats, 200f);
            Assert.Equal(RouletteDecision.ProtectedConsecutive, third.Decision);
            Assert.Equal(0, stats.ConsecutivePenalties);
            Assert.True(service.Spin(stats, 300f).IsPenalty);
        }

        [Fact]
        public void RuletaDesactivadaNoCastigaNiAnota()
        {
            var config = NeutralConfig();
            config.Enabled = false;
            var service = new RouletteService(config, NoBalance(), new SequenceRandom(0.5));
            var stats = new PlayerRoundStats(PlayerSlot.PlayerOne);

            RouletteResult result = service.Spin(stats, 0f);

            Assert.Equal(RouletteDecision.Disabled, result.Decision);
            Assert.False(result.IsPenalty);
            Assert.Null(stats.LastOutcome);
        }

        [Fact]
        public void LaDuracionUsaLaGeneralOLaEspecificaDelEfecto()
        {
            var config = NeutralConfig();
            OnlyEnable(config, RouletteOutcome.SlowFire);
            config.SetDurationOverride(PenaltyEffect.SlowFire, 6f);
            var service = new RouletteService(config, NoBalance(), new SequenceRandom(0.5));
            Assert.Equal(6f, service.Spin(new PlayerRoundStats(PlayerSlot.PlayerOne), 0f).Effects[0].DurationSeconds);

            config.ClearDurationOverride(PenaltyEffect.SlowFire);
            Assert.Equal(4f, service.Spin(new PlayerRoundStats(PlayerSlot.PlayerOne), 0f).Effects[0].DurationSeconds);
        }

        [Fact]
        public void ElFocoReduceSoloSinPenalizacionDelJugadorConFoco()
        {
            var config = NeutralConfig();
            config.FocusIntensity = 1f;
            var service = new RouletteService(config, NoBalance(), new SequenceRandom(0.5));
            var director = new BalanceDirector(NoBalance());
            var p1 = new PlayerRoundStats(PlayerSlot.PlayerOne);
            var p2 = new PlayerRoundStats(PlayerSlot.PlayerTwo);

            float p1NoPenalty = service.ComputeWeights(p1, director.Evaluate(p1, 0f))[RouletteOutcome.NoPenalty];
            float p2NoPenalty = service.ComputeWeights(p2, director.Evaluate(p2, 0f))[RouletteOutcome.NoPenalty];

            Assert.Equal(20f, p1NoPenalty);
            Assert.Equal(10f, p2NoPenalty);
        }

        [Fact]
        public void ConFocoACeroP1YP2TienenLosMismosPesos()
        {
            var config = NeutralConfig();
            var service = new RouletteService(config, NoBalance(), new SequenceRandom(0.5));
            var director = new BalanceDirector(NoBalance());
            var p1 = new PlayerRoundStats(PlayerSlot.PlayerOne);
            var p2 = new PlayerRoundStats(PlayerSlot.PlayerTwo);

            var w1 = service.ComputeWeights(p1, director.Evaluate(p1, 0f));
            var w2 = service.ComputeWeights(p2, director.Evaluate(p2, 0f));

            Assert.Equal(w1.OrderBy(kv => kv.Key), w2.OrderBy(kv => kv.Key));
        }

        [Fact]
        public void LaMismaSemillaReproduceLaMismaSecuencia()
        {
            var config = new RouletteConfig { DebugSeed = 1234 };
            var a = new RouletteService(config, new BalanceConfig(), null);
            var b = new RouletteService(config, new BalanceConfig(), null);
            var statsA = new PlayerRoundStats(PlayerSlot.PlayerOne);
            var statsB = new PlayerRoundStats(PlayerSlot.PlayerOne);

            for (int i = 0; i < 30; i++)
            {
                RouletteResult ra = a.Spin(statsA, i * 10f);
                RouletteResult rb = b.Spin(statsB, i * 10f);
                Assert.Equal(ra.Outcome, rb.Outcome);
                Assert.Equal(ra.Effects.Select(e => e.Effect), rb.Effects.Select(e => e.Effect));
            }
        }

        [Fact]
        public void LaDistribucionObservadaSeAjustaALasProbabilidades()
        {
            var config = NeutralConfig();
            var service = new RouletteService(config, NoBalance(), new SystemRandomSource(42));
            var counts = RouletteOutcomes.All().ToDictionary(o => o, o => 0);
            const int spins = 20000;

            for (int i = 0; i < spins; i++)
            {
                counts[service.Spin(new PlayerRoundStats(PlayerSlot.PlayerOne), 0f).Outcome]++;
            }

            Assert.InRange(counts[RouletteOutcome.NoPenalty] / (double)spins, 0.18, 0.22);
            Assert.InRange(counts[RouletteOutcome.DoublePunishment] / (double)spins, 0.13, 0.17);
        }

        [Fact]
        public void LaRuletaAnotaCastigosEnLosContadores()
        {
            var config = NeutralConfig();
            OnlyEnable(config, RouletteOutcome.SlowFire);
            var service = new RouletteService(config, NoBalance(), new SequenceRandom(0.5));
            var stats = new PlayerRoundStats(PlayerSlot.PlayerOne);

            service.Spin(stats, 5f);

            Assert.Equal(1, stats.PenaltiesReceived);
            Assert.Equal(1, stats.ConsecutivePenalties);
            Assert.Equal(RouletteOutcome.SlowFire, stats.LastOutcome);
            Assert.Equal(5f, stats.LastPenaltyTime);
        }

        [Fact]
        public void ConversionesEntreResultadosYEfectos()
        {
            foreach (PenaltyEffect effect in RouletteOutcomes.AllEffects())
            {
                Assert.True(effect.ToOutcome().IsSingleEffect());
                Assert.Equal(effect, effect.ToOutcome().ToEffect());
            }

            Assert.Throws<ArgumentException>(() => RouletteOutcome.NoPenalty.ToEffect());
            Assert.Throws<ArgumentException>(() => RouletteOutcome.DoublePunishment.ToEffect());
        }
    }
}
